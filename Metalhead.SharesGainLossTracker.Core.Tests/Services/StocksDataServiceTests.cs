using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Polly;
using Polly.Retry;
using System.Net;
using System.Reflection;
using Xunit;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Services;

public class StocksDataServiceTests
{
    private readonly StocksDataService _sut;
    private readonly Mock<ILogger<StocksDataService>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly HttpClient _httpClient;
    private readonly Mock<IEnumerable<IStock>> _mockStockSources = new();
    private readonly Mock<ISharesInputHelperWrapper> _mockSharesInputHelperWrapper = new();

    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler = new();
    private readonly AsyncRetryPolicy _pollyPolicy;

    public StocksDataServiceTests()
    {
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _pollyPolicy = GetRetryPolicy();
        _mockStockSources
            .Setup(x => x.GetEnumerator())
            .Returns(() => new List<IStock>
            {
                new Mock<IStock>().Object,
                new Mock<IStock>().Object
            }.GetEnumerator());

        _sut = new StocksDataService(_mockLogger.Object, _mockProgress.Object, _httpClient, _mockStockSources.Object, _mockSharesInputHelperWrapper.Object);
    }
    private static AsyncRetryPolicy GetRetryPolicy()
    {
        return Policy
            .HandleInner<HttpRequestException>()
            .OrInner<TaskCanceledException>()
            .RetryAsync(5);
    }

    [Fact]
    public void GetStock_ReturnsStockModel_WhenStockModelExists()
    {
        // Arrange
        string stockSourceModel = "IStockProxy";

        // Act
        var result = _sut.GetStock(stockSourceModel);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IStock>(result);
    }

    [Fact]
    public void GetStock_ThrowsInvalidOperationException_WhenStockModelDoesNotExist()
    {
        // Arrange
        string stockSourceModel = "Metallica";

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GetStock(stockSourceModel));
        Assert.Equal($"No class implementing IStock could be found that matches '{stockSourceModel}' (in settings).", ex.Message);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"No class implementing IStock could be found that matches '{stockSourceModel}' (in settings)."))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"No class implementing IStock could be found that matches '{stockSourceModel}' (in settings).");
    }

    [Theory]
    [InlineData("http://api.examplestocksapi.com/v1/eod?symbols={0}&access_key=666")]
    [InlineData("https://api.examplestocksapi.com/v1/eod?symbols={0}&access_key=666")]
    public void ValidateUri_DoesNotThrowException_GivenValidUri(string uri)
    {
        // Arrange
        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.ValidateUri), BindingFlags.Static | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        void Act() => methodInfo.Invoke(_sut, [uri]);

        var exception = Record.Exception(Act);
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUri_ThrowsArgumentNullException_GivenNullUri()
    {
        // Arrange
        string? uri = null;
        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.ValidateUri), BindingFlags.Static | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var ex = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(_sut, [uri]));
        Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal("uri", ((ArgumentNullException)ex.InnerException).ParamName);
    }

    [Theory]
    [InlineData("http://*api.examplestocksapi.com/v1/eod?symbols={0}&access_key=666")]
    [InlineData("mail://api.examplestocksapi.com/v1/eod?symbols={0}&access_key=666")]
    public void ValidateUri_ThrowsArgumentException_GivenInvalidUriFormatOrInvalidUriScheme(string uri)
    {
        // Arrange
        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.ValidateUri), BindingFlags.Static | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var ex = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(_sut, [uri]));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Equal("uri", ((ArgumentException)ex.InnerException).ParamName);
    }

    [Fact]
    public void IsExpectedStocksDataMapped_ReturnTrue_GivenAllStocksFetchedSuccessfully()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInput();
        var flattenedStocks = new List<FlattenedStock>
        {
            new(DateTime.Now, "MSFT", 279.51),
            new(DateTime.Now, "TSLA", 189.53),
            new(DateTime.Now, "OCDO.LON", 520.65)
        };

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());
        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.IsExpectedStocksDataMapped), BindingFlags.Instance | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var result = methodInfo.Invoke(_sut, [flattenedStocks, sharesInput]);
        Assert.NotNull(result);
        Assert.True((bool)result);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Good && log.DownloadLog.Contains($"Successfully fetched stocks data for:"))), Times.Exactly(3));
        _mockLogger.VerifyLogging(LogLevel.Information, "Successfully fetched stocks data for: ", Times.Exactly(3));
    }

    [Fact]
    public void IsExpectedStocksDataMapped_ReturnsFalse_GivenOnlySomeStocksFetchedSuccessfully()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInput();
        var flattenedStocks = new List<FlattenedStock>
        {
            new(DateTime.Now, "MSFT", 279.51),
            new(DateTime.Now, "TSLA", 189.53)
        };

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());
        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.IsExpectedStocksDataMapped), BindingFlags.Instance | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var result = methodInfo.Invoke(_sut, [flattenedStocks, sharesInput]);

        Assert.NotNull(result);
        Assert.False((bool)result);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Good && log.DownloadLog.Contains($"Successfully fetched stocks data for:"))), Times.Exactly(2));
        _mockLogger.VerifyLogging(LogLevel.Information, "Successfully fetched stocks data for: ", Times.Exactly(2));
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Contains($"Failed to fetch stocks data for:"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Failed fetching stocks data for: ");
    }

    [Fact]
    public void IsExpectedStocksDataMapped_ThrowsArgumentNullException_GivenNullFlattenedStocksAndValidSharesInput()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInput();
        List<FlattenedStock>? flattenedStocks = null;

        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.IsExpectedStocksDataMapped), BindingFlags.Instance | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var ex = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(_sut, [flattenedStocks, sharesInput]));
        Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal("flattenedStocks", ((ArgumentNullException)ex.InnerException).ParamName);
    }

    [Fact]
    public void IsExpectedStocksDataMapped_ThrowsArgumentException_GivenEmptyFlattenedStocksAndValidSharesInput()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInput();
        var flattenedStocks = new List<FlattenedStock>();

        var methodInfo = typeof(StocksDataService).GetMethod(nameof(StocksDataService.IsExpectedStocksDataMapped), BindingFlags.Instance | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var ex = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(_sut, [flattenedStocks, sharesInput]));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Equal("flattenedStocks", ((ArgumentException)ex.InnerException).ParamName);
    }

    [Fact]
    public async Task FetchStocksDataAsync_ReturnsHttpResponseMessages_WhenStatusCodeSuccessful()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());

        // Act
        var result = await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl, apiDelayPerCallMilleseconds, sharesInput);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result[0].StatusCode);
        Assert.Equal(HttpStatusCode.OK, result[1].StatusCode);
        Assert.Equal(HttpStatusCode.OK, result[2].StatusCode);
    }

    [Fact]
    public async Task FetchStocksDataAsync_ReturnsHttpResponseMessagesForSuccessfulCodesAndSwallowsHttpRequestException_WhenMaximumRetryLimitExceeded()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        var expectedHttpResponseMessage1 = new HttpResponseMessage(HttpStatusCode.OK);
        expectedHttpResponseMessage1.Content = new StringContent("Stock data 1");
        var expectedHttpResponseMessage2 = new HttpResponseMessage(HttpStatusCode.OK);
        expectedHttpResponseMessage2.Content = new StringContent("Stock data 2");

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains(sharesInput[0].Symbol)), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedHttpResponseMessage1);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains(sharesInput[1].Symbol)), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedHttpResponseMessage2);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains(sharesInput[2].Symbol)), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());

        // Act
        var result = await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl, apiDelayPerCallMilleseconds, sharesInput);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(expectedHttpResponseMessage1, result[0]);
        Assert.Equal(expectedHttpResponseMessage2, result[1]);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"Error fetching stocks data.  Reached maximum retries."))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Error fetching stocks data.  Reached maximum retries.");
    }

    [Fact]
    public async Task FetchStocksDataAsync_SwallowsHttpRequestException_WhenMaximumRetryLimitExceeded()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("No such host is known."));

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());

        // Act
        var result = await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl, apiDelayPerCallMilleseconds, sharesInput);

        // Assert
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals("Error fetching stocks data.  Reached maximum retries."))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Error fetching stocks data.  Reached maximum retries.");
    }

    [Fact]
    public async Task FetchStocksDataAsync_SwallowsTaskCanceledException_WhenMaximumRetryLimitExceeded()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("No such host is known."));

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());

        // Act
        var result = await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl, apiDelayPerCallMilleseconds, sharesInput);

        // Assert
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals("Error fetching stocks data.  Reached maximum retries."))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Error fetching stocks data.  Reached maximum retries.");
    }

    [Fact]
    public async Task FetchStocksDataAsync_ThrowsArgumentNullException_GivenNullUrl()
    {
        // Arrange
        string? stocksApiUrl = null;
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentNullException());

        // Act and Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl!, apiDelayPerCallMilleseconds, sharesInput));
        Assert.IsType<ArgumentNullException>(ex);
        Assert.Equal("uri", ex.ParamName);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"URL for stocks API is invalid: {stocksApiUrl}"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"URL for stocks API is invalid: {stocksApiUrl}");
    }

    [Theory]
    [InlineData("http://*api.examplestocksapi.com/v1/eod?symbols={0}&access_key=666")]
    [InlineData("mail://api.examplestocksapi.com/v1/eod?symbols={0}&access_key=666")]
    public async Task FetchStocksDataAsync_ThrowsArgumentException_GivenInvalidUrl(string stocksApiUrl)
    {
        // Arrange
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new ArgumentException("uri"));

        // Act and Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl, apiDelayPerCallMilleseconds, sharesInput));
        Assert.IsType<ArgumentException>(ex);
        Assert.Equal("uri", ex.ParamName);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"URL for stocks API is invalid: {stocksApiUrl}"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"URL for stocks API is invalid: {stocksApiUrl}");
    }

    [Fact]
    public async Task FetchStocksDataAsync_ThrowsException_WhenExceptionIsOtherThanHttpRequestExceptionOrTaskCanceledExceptionIsThrown()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMilleseconds = 0;
        var sharesInput = MockData.CreateSharesInput();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception());

        _mockSharesInputHelperWrapper.Setup(x => x.GetDistinctSymbolsNames(sharesInput)).Returns(MockData.CreateSharesInput());

        // Act and Assert
        var ex = await Assert.ThrowsAsync<AggregateException>(async () => await _sut.FetchStocksDataAsync(_pollyPolicy, stocksApiUrl, apiDelayPerCallMilleseconds, sharesInput));
        Assert.IsType<Exception>(ex.InnerException);
    }

    [Fact]
    public async Task FetchStockDataAsync_ReturnsHttpResponseMessage_WhenStatusCodeSuccessful()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var stockSymbol = "MSFT";
        var stockName = "Microsoft Corp (MSFT)";

        var expectedHttpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("MSFT data")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedHttpResponseMessage);

        var methodInfo = typeof(StocksDataService).GetMethod("FetchStockDataAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var task = (Task<HttpResponseMessage>?)methodInfo.Invoke(_sut, [_pollyPolicy, stocksApiUrl, stockSymbol, stockName]);
        HttpResponseMessage? result = null;
        if (task is not null)
        {
            result = await task;
        }
        Assert.NotNull(task);
        Assert.NotNull(result);
        Assert.Equal(expectedHttpResponseMessage, result);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Normal && log.DownloadLog.Contains("Sending request for stocks data:"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Information, $"Sending request for stocks data: {stockSymbol} ({stockName})");
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Good && log.DownloadLog.Contains("Received successful response fetching stocks data:"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Information, $"Received successful response fetching stocks data: {stockSymbol} ({stockName})");
    }

    [Fact]
    public async Task FetchStockDataAsync_ReturnsHttpResponseMessage_WhenStatusCodeUnsuccessful()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var stockSymbol = "MSFT";
        var stockName = "Microsoft Corp (MSFT)";

        var expectedHttpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("MSFT data")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedHttpResponseMessage);

        var methodInfo = typeof(StocksDataService).GetMethod("FetchStockDataAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var task = (Task<HttpResponseMessage>?)methodInfo.Invoke(_sut, [_pollyPolicy, stocksApiUrl, stockSymbol, stockName]);
        HttpResponseMessage? result = null;
        if (task is not null)
        {
            result = await task;
        }
        Assert.NotNull(task);
        Assert.NotNull(result);
        Assert.Equal(expectedHttpResponseMessage, result);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Normal && log.DownloadLog.Contains("Sending request for stocks data:"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Information, $"Sending request for stocks data: {stockSymbol} ({stockName})");
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Contains("Received failure response fetching stocks data:"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"Received failure response fetching stocks data: {stockSymbol} ({stockName})");
    }

    [Fact]
    public async Task FetchStockDataAsync_ThrowsHttpRequestException_WhenMaximumRetryLimitExceeded()
    {
        // Arrange
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var stockSymbol = "MSFT";
        var stockName = "Microsoft Corp (MSFT)";

        var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("MSFT data")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        // Act and Assert
        var methodInfo = typeof(StocksDataService).GetMethod("FetchStockDataAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            var task = (Task?)methodInfo?.Invoke(_sut, [_pollyPolicy, stocksApiUrl, stockSymbol, stockName]);
            await task!.ConfigureAwait(false);
        });
    }
}
