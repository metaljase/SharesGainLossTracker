using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Tests;

public class AlphaVantageTests
{
    private readonly Mock<ILogger<AlphaVantage>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly AlphaVantage _sut;

    public AlphaVantageTests()
    {
        _sut = new AlphaVantage(_mockLogger.Object, _mockProgress.Object);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenRateLimitExceededError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = AlphaVantageMockData.CreateAlphaVantageRateLimitHttpResponse();
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
    public async Task GetStocksDataAsync_WhenDailyLimitExceededError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = AlphaVantageMockData.CreateAlphaVantageDailyLimitHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Daily API calls limit reached error from stocks API.  Plans with a higher limit may be available.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Daily API calls limit reached error from stocks API.  Plans with a higher limit may be available.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenAccessRestrictedError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = AlphaVantageMockData.CreateAlphaVantageAccessRestrictedHttpResponse();
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
        var httpResponse = AlphaVantageMockData.CreateAlphaVantageInvalidEndpointHttpResponse();
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
    public async Task GetStocksDataAsync_WhenDeserializingError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = AlphaVantageMockData.CreateAlphaVantageDeserializingErrorHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Error deserializing data from stocks API.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Error deserializing data from stocks API.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }
}
