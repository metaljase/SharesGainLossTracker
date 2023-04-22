using Microsoft.Extensions.Logging;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;
using Moq;
using Polly.Retry;
using Xunit;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Services;

public class SharesOutputServiceTests
{
    private readonly SharesOutputService _sut;
    private readonly Mock<ILogger<SharesOutputService>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly Mock<IStocksDataService> _mockStocksDataService = new();
    private readonly Mock<ISharesInputLoader> _mockSharesInputLoader = new();
    private readonly Mock<ISharesInputHelperWrapper> _mockSharesInputHelper = new();
    private readonly Mock<ISharesOutputHelperWrapper> _mockSharesOutputHelper = new();

    public SharesOutputServiceTests()
    {
        _mockProgress = new Mock<IProgress<ProgressLog>>();

        _sut = new SharesOutputService(_mockLogger.Object, _mockProgress.Object, _mockStocksDataService.Object, _mockSharesInputLoader.Object, _mockSharesInputHelper.Object, _mockSharesOutputHelper.Object);
    }

    [Fact]
    public async Task CreateSharesOutputAsync_ReturnsWorkbookPath_GivenValidInput()
    {
        // Arrange
        var sharesInputFileFullPath = "My Shares.csv";
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMillieseconds = 0;
        var orderByDateDescending = true;
        var appendPriceToStockName = true;

        Mock<IEnumerable<IStock>> mockStockSources = new();
        mockStockSources
            .Setup(x => x.GetEnumerator())
            .Returns(() => new List<IStock>
            {
                new Mock<IStock>().Object,
                new Mock<IStock>().Object
            }.GetEnumerator());

        Mock<IStock> mockIStock = new();
        _mockStocksDataService.Setup(x => x.GetStock(It.IsAny<string>())).Returns(mockIStock.Object).Verifiable();

        _mockSharesInputLoader.Setup(x => x.CreateSharesInput(It.IsAny<string>())).Returns(new List<Share>()).Verifiable();

        _mockStocksDataService.Setup(x => x.GetRetryPolicy(It.IsAny<int>())).Verifiable();

        _mockStocksDataService
            .Setup(x => x.FetchStocksDataAsync(It.IsAny<AsyncRetryPolicy>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Share>>()))
            .Verifiable();

        mockIStock.Setup(x => x.GetStocksDataAsync(It.IsAny<HttpResponseMessage[]>())).Verifiable();

        _mockStocksDataService.Setup(x => x.IsExpectedStocksDataMapped(It.IsAny<List<FlattenedStock>>(), It.IsAny<List<Share>>())).Verifiable();

        _mockSharesInputHelper.Setup(x => x.AppendPurchasePriceToStockName(It.IsAny<List<Share>>())).Verifiable();

        _mockSharesInputHelper.Setup(x => x.MakeStockNamesUnique(It.IsAny<List<Share>>())).Verifiable();

        _mockSharesOutputHelper
            .Setup(x => x.CreateSharesOutput(It.IsAny<List<Share>>(), It.IsAny<List<FlattenedStock>>()))
            .Returns(MockData.CreateSharesOutput())
            .Verifiable();

        // Act
        var result = await _sut.CreateSharesOutputAsync(
            "IStockProxy",
            sharesInputFileFullPath,
            stocksApiUrl,
            apiDelayPerCallMillieseconds,
            orderByDateDescending,
            appendPriceToStockName);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<ShareOutput>>(result);

        _mockStocksDataService.Verify(x => x.GetStock(It.IsAny<string>()), Times.Once);
        _mockSharesInputLoader.Verify(x => x.CreateSharesInput(It.IsAny<string>()), Times.Once);
        _mockStocksDataService.Verify(x => x.GetRetryPolicy(It.IsAny<int>()), Times.Once);
        _mockStocksDataService.Verify(x => x.FetchStocksDataAsync(It.IsAny<AsyncRetryPolicy>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Share>>()), Times.Once);
        mockIStock.Verify(x => x.GetStocksDataAsync(It.IsAny<HttpResponseMessage[]>()), Times.Once);
        _mockStocksDataService.Verify(x => x.IsExpectedStocksDataMapped(It.IsAny<List<FlattenedStock>>(), It.IsAny<List<Share>>()), Times.Once);
        _mockSharesInputHelper.Verify(x => x.AppendPurchasePriceToStockName(It.IsAny<List<Share>>()), Times.Once);
        _mockSharesInputHelper.Verify(x => x.MakeStockNamesUnique(It.IsAny<List<Share>>()), Times.Once);
        _mockSharesOutputHelper.Verify(x => x.CreateSharesOutput(It.IsAny<List<Share>>(), It.IsAny<List<FlattenedStock>>()), Times.Once);
    }

    public static IEnumerable<object[]> IsExpectedStocksDataMappedExceptions =>
        new List<object[]>
        {
            new object[] { new ArgumentNullException("flattenedStocks") },
            new object[] { new ArgumentException("Failed to fetch any stocks data.", "flattenedStocks") }
        };

    [Theory]
    [MemberData(nameof(IsExpectedStocksDataMappedExceptions))]
    public async Task CreateSharesOutputAsync_DoesNotThrowArgumentNullExceptionOrArgumentException_GivenIsExpectedStocksDataMappedThrowsArgumentNullExceptionOrArgumentException(Exception exception)
    {
        // Arrange
        var sharesInputFileFullPath = "My Shares.csv";
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMillieseconds = 0;
        var orderByDateDescending = true;
        var appendPriceToStockName = true;

        Mock<IEnumerable<IStock>> mockStockSources = new();
        mockStockSources
            .Setup(x => x.GetEnumerator())
            .Returns(() => new List<IStock>
            {
            new Mock<IStock>().Object,
            new Mock<IStock>().Object
            }.GetEnumerator());

        Mock<IStock> mockIStock = new();
        _mockStocksDataService.Setup(x => x.GetStock(It.IsAny<string>())).Returns(mockIStock.Object).Verifiable();

        _mockSharesInputLoader.Setup(x => x.CreateSharesInput(It.IsAny<string>())).Returns(new List<Share>()).Verifiable();

        _mockStocksDataService.Setup(x => x.GetRetryPolicy(It.IsAny<int>())).Verifiable();

        _mockStocksDataService
            .Setup(x => x.FetchStocksDataAsync(It.IsAny<AsyncRetryPolicy>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Share>>()))
            .Verifiable();

        mockIStock.Setup(x => x.GetStocksDataAsync(It.IsAny<HttpResponseMessage[]>())).Verifiable();

        _mockStocksDataService.Setup(x => x.IsExpectedStocksDataMapped(It.IsAny<List<FlattenedStock>>(), It.IsAny<List<Share>>()))
            .Throws(exception)
            .Verifiable();

        _mockSharesInputHelper.Setup(x => x.AppendPurchasePriceToStockName(It.IsAny<List<Share>>())).Verifiable();

        _mockSharesInputHelper.Setup(x => x.MakeStockNamesUnique(It.IsAny<List<Share>>())).Verifiable();

        _mockSharesOutputHelper
            .Setup(x => x.CreateSharesOutput(It.IsAny<List<Share>>(), It.IsAny<List<FlattenedStock>>()))
            .Returns(MockData.CreateSharesOutput())
            .Verifiable();

        // Act
        var result = await _sut.CreateSharesOutputAsync(
            "IStockProxy",
            sharesInputFileFullPath,
            stocksApiUrl,
            apiDelayPerCallMillieseconds,
            orderByDateDescending,
            appendPriceToStockName);

        // Assert
        _mockStocksDataService.Verify(x => x.GetStock(It.IsAny<string>()), Times.Once);
        _mockSharesInputLoader.Verify(x => x.CreateSharesInput(It.IsAny<string>()), Times.Once);
        _mockStocksDataService.Verify(x => x.GetRetryPolicy(It.IsAny<int>()), Times.Once);
        _mockStocksDataService.Verify(x => x.FetchStocksDataAsync(It.IsAny<AsyncRetryPolicy>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Share>>()), Times.Once);
        mockIStock.Verify(x => x.GetStocksDataAsync(It.IsAny<HttpResponseMessage[]>()), Times.Once);
        _mockStocksDataService.Verify(x => x.IsExpectedStocksDataMapped(It.IsAny<List<FlattenedStock>>(), It.IsAny<List<Share>>()), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"Failed to fetch any stocks data for input file: ");
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}"))), Times.Once);
        _mockSharesInputHelper.Verify(x => x.AppendPurchasePriceToStockName(It.IsAny<List<Share>>()), Times.Never);
        _mockSharesInputHelper.Verify(x => x.MakeStockNamesUnique(It.IsAny<List<Share>>()), Times.Never);
        _mockSharesOutputHelper.Verify(x => x.CreateSharesOutput(It.IsAny<List<Share>>(), It.IsAny<List<FlattenedStock>>()), Times.Never);
    }
}
