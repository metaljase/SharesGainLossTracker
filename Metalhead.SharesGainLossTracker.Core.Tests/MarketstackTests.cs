using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Tests;

public class MarketstackTests
{
    private readonly Mock<ILogger<Marketstack>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly Marketstack _sut;

    public MarketstackTests()
    {
        _sut = new Marketstack(_mockLogger.Object, _mockProgress.Object);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenRateLimitExceededError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackRateLimitHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Rate limit exceeded error from stocks API.  Try increasing the ApiDelayPerCallMilliseconds setting.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Rate limit exceeded error from stocks API.  Try increasing the ApiDelayPerCallMilliseconds setting.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenMonthlyLimitExceededError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackMonthlyLimitHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Monthly API calls limit reached error from stocks API.  Plans with a higher limit may be available.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Monthly API calls limit reached error from stocks API.  Plans with a higher limit may be available.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenTooManyRequestsError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackTooManyRequestsHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Monthly API calls limit reached error from stocks API.  Plans with a higher limit may be available.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Monthly API calls limit reached error from stocks API.  Plans with a higher limit may be available.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenAccessRestrictedError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackAccessRestrictedHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Access restricted error from stocks API.  Your plan may need upgrading to use this API endpoint.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Access restricted error from stocks API.  Your plan may need upgrading to use this API endpoint.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenInvalidEndpointError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackInvalidEndpointHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Invalid endpoint error from stocks API.  Verify the endpoint URL is correct, especially the stock symbol.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Invalid endpoint error from stocks API.  Verify the endpoint URL is correct, especially the stock symbol.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenNotFoundError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackNotFoundHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Invalid endpoint error from stocks API.  Verify the endpoint URL is correct, especially the stock symbol.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Invalid endpoint error from stocks API.  Verify the endpoint URL is correct, especially the stock symbol.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenNoValidSymbolsError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackNoValidSymbolsHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Invalid stock symbol error from stocks API.  Verify the stock symbols in the input file.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Invalid stock symbol error from stocks API.  Verify the stock symbols in the input file.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenDeserializingError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackDeserializingErrorHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Error deserializing stocks data.  Try increasing the ApiDelayPerCallMilliseconds setting.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Error deserializing stocks data.  Try increasing the ApiDelayPerCallMilliseconds setting.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenOtherError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = MarketstackMockData.CreateMarketstackOtherErrorHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Unknown error from stocks API.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Unknown error from stocks API.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }
}
