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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Rate limit error from stocks API. Try increasing ApiDelayPerCallMilleseconds setting.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Rate limit error from stocks API. Try increasing ApiDelayPerCallMilleseconds setting.", reportedLog.DownloadLog);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Daily API call limit exceeded error from stocks API. Your Alpha Vantage plan may need upgrading.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Daily API call limit exceeded error from stocks API. Your Alpha Vantage plan may need upgrading.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenPaidTierOnlyError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = AlphaVantageMockData.CreateAlphaVantagePaidTierOnlyHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Paid tier only error from stocks API. You need to upgrade your Alpha Vantage plan to use this API endpoint.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Paid tier only error from stocks API. You need to upgrade your Alpha Vantage plan to use this API endpoint.", reportedLog.DownloadLog);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStocksDataAsync_WhenInvalidApiCallError_LogsAndReportsError()
    {
        // Arrange
        ProgressLog? reportedLog = null;
        _mockProgress.Setup(p => p.Report(It.IsAny<ProgressLog>()))
            .Callback<ProgressLog>(log => reportedLog = log);
        var httpResponse = AlphaVantageMockData.CreateAlphaVantageInvalidApiCallHttpResponse();
        var responses = new[] { httpResponse };

        // Act
        var result = await ((IStock)_sut).GetStocksDataAsync(responses, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Invalid API call error from stocks API. Possible incorrect stock symbol in shares input file.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.NotNull(reportedLog);
        Assert.Equal(MessageImportance.Bad, reportedLog.Importance);
        Assert.Equal("Invalid API call error from stocks API. Possible incorrect stock symbol in shares input file.", reportedLog.DownloadLog);
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
