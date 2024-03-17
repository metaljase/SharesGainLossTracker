using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using Metalhead.SharesGainLossTracker.Core.FileSystem;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Services;

public class SharesInputLoaderCsvTests
{
    private readonly SharesInputLoaderCsv _sut;
    private readonly Mock<ILogger<SharesInputLoaderCsv>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly Mock<IFileSystemFileWrapper> _mockFileSystemWrapper = new();

    public SharesInputLoaderCsvTests()
    {
        _mockProgress = new Mock<IProgress<ProgressLog>>();

        _mockFileSystemWrapper
            .Setup(f => f.Exists(It.Is<string>(s => s == "MockedFile.csv" || s == "MockedFileContainingInvalidLines.csv")))
            .Returns(true);

        _mockFileSystemWrapper
            .Setup(f => f.ReadAllLines("MockedFile.csv"))
            .Returns(MockData.CreateSharesInputCsv());

        _mockFileSystemWrapper
            .Setup(f => f.ReadAllLines("MockedFileContainingInvalidLines.csv"))
            .Returns(MockData.CreateSharesInputCsvContainingInvalidLines());

        _sut = new SharesInputLoaderCsv(_mockLogger.Object, _mockProgress.Object, _mockFileSystemWrapper.Object);
    }

    [Fact]
    public void CreateSharesInput_ReturnsSharesInput_GivenValidFileFullPath()
    {
        // Arrange
        var sharesInputFilePath = "MockedFile.csv";

        // Act
        var sharesInput = _sut.CreateSharesInput(sharesInputFilePath);

        // Assert
        Assert.Equal(3, sharesInput.Count);
        Assert.Equal("MSFT", sharesInput[0].Symbol);
        Assert.Equal("Microsoft Corp (MSFT)", sharesInput[0].StockName);
        Assert.Equal(287.14, sharesInput[0].PurchasePrice);

        Assert.Equal("TSLA", sharesInput[1].Symbol);
        Assert.Equal("Tesla Inc (TSLA)", sharesInput[1].StockName);
        Assert.Equal(184.77, sharesInput[1].PurchasePrice);

        Assert.Equal("OCDO.LON", sharesInput[2].Symbol);
        Assert.Equal("Ocado Group plc (OCDO)", sharesInput[2].StockName);
        Assert.Equal(522.40, sharesInput[2].PurchasePrice);
    }

    [Fact]
    public void CreateSharesInputFromCsvFile_ReturnsSharesInput_GivenValidFileFullPath()
    {
        // Arrange
        var sharesInputFilePath = "MockedFile.csv";

        // Act
        var sharesInput = _sut.CreateSharesInputFromCsvFile(sharesInputFilePath);

        // Assert
        Assert.Equal(3, sharesInput.Count);
        Assert.Equal("MSFT", sharesInput[0].Symbol);
        Assert.Equal("Microsoft Corp (MSFT)", sharesInput[0].StockName);
        Assert.Equal(287.14, sharesInput[0].PurchasePrice);

        Assert.Equal("TSLA", sharesInput[1].Symbol);
        Assert.Equal("Tesla Inc (TSLA)", sharesInput[1].StockName);
        Assert.Equal(184.77, sharesInput[1].PurchasePrice);

        Assert.Equal("OCDO.LON", sharesInput[2].Symbol);
        Assert.Equal("Ocado Group plc (OCDO)", sharesInput[2].StockName);
        Assert.Equal(522.40, sharesInput[2].PurchasePrice);
    }

    [Fact]
    public void CreateSharesInputFromCsvFile_ThrowsFileNotFoundException_GivenFileDoesNotExist()
    {
        // Arrange
        var sharesInputFilePath = "NonExistentFile.csv";

        // Act and Assert
        var ex = Assert.Throws<FileNotFoundException>(() => _sut.CreateSharesInputFromCsvFile(sharesInputFilePath));
        Assert.Contains(sharesInputFilePath, ex.FileName);
        Assert.StartsWith("Shares input file not found.", ex.Message);
        _mockLogger.VerifyLogging(LogLevel.Error, $"Shares input file not found: {sharesInputFilePath}");
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"Shares input file not found: {sharesInputFilePath}"))), Times.Once);
    }

    [Fact]
    public void CreateSharesInputFromCsvFile_ThrowsArgumentNullException_GivenNullFileFullPath()
    {
        // Arrange
        string? sharesInputFilePath = null;

        // Act and Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _sut.CreateSharesInputFromCsvFile(sharesInputFilePath!));
        Assert.Contains($"Shares input file full path cannot be null.", ex.Message);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals("Shares input file full path cannot be null."))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Shares input file full path cannot be null.");
    }

    [Fact]
    public void CreateSharesInputFromCsvFile_ThrowsInvalidOperationException_GivenIncorrectlyDelimitedSharesInput()
    {
        // Arrange
        var sharesInputFilePath = "MockedFileContainingInvalidLines.csv";

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.CreateSharesInputFromCsvFile(sharesInputFilePath));
        Assert.Contains($"Not all lines in the shares input file are formatted correctly: {sharesInputFilePath}", ex.Message);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"Not all lines in the shares input file are formatted correctly: {sharesInputFilePath}"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"Not all lines in the shares input file are formatted correctly: {sharesInputFilePath}");
    }

    [Fact]
    public void CreateSharesInputFromCsv_ReturnsSharesInput_GivenInputIsValid()
    {
        // Arrange
        var sharesInputCsv = MockData.CreateSharesInputCsv();

        // Act
        var sharesInput = _sut.CreateSharesInputFromCsv(sharesInputCsv);

        // Assert
        Assert.Equal(3, sharesInput.Count);
        Assert.Equal("MSFT", sharesInput[0].Symbol);
        Assert.Equal("Microsoft Corp (MSFT)", sharesInput[0].StockName);
        Assert.Equal(287.14, sharesInput[0].PurchasePrice);

        Assert.Equal("TSLA", sharesInput[1].Symbol);
        Assert.Equal("Tesla Inc (TSLA)", sharesInput[1].StockName);
        Assert.Equal(184.77, sharesInput[1].PurchasePrice);

        Assert.Equal("OCDO.LON", sharesInput[2].Symbol);
        Assert.Equal("Ocado Group plc (OCDO)", sharesInput[2].StockName);
        Assert.Equal(522.40, sharesInput[2].PurchasePrice);
    }

    [Fact]
    public void CreateSharesInputFromCsv_ThrowsInvalidOperationException_GivenEmptyInput()
    {
        // Arrange
        var sharesInputCsv = new List<string>();

        // Act and Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _sut.CreateSharesInputFromCsv(sharesInputCsv));
        Assert.Equal("Shares input CSV does not contain any lines with correctly formatted values.", ex.Message);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals("Shares input CSV does not contain any lines with correctly formatted values."))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Shares input CSV does not contain any lines with correctly formatted values.");
    }

    [Theory]
    [InlineData("MSFT, Microsoft Corporation")]
    [InlineData("MSFT, Microsoft Corporation, ")]
    [InlineData("Microsoft Corporation, 287.14")]
    [InlineData(" , Microsoft Corporation, 287.14")]
    [InlineData("MSFT, , 287.14")]
    [InlineData("  ,  ,  ")]
    [InlineData("  ,  ,  ,  ")]
    public void CreateSharesInputFromCsv_ThrowsInvalidOperationException_GivenMissingValue(string sharesInputCsv)
    {
        // Act and Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _sut.CreateSharesInputFromCsv([sharesInputCsv]));
        Assert.StartsWith("Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: ", ex.Message);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.StartsWith("Line in shares input CSV does not contain a stock symbol, stock name, and purchase price:"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: ");
    }

    [Theory]
    [InlineData("MSFT, Microsoft Corporation, Metallica")]
    [InlineData("MSFT, Microsoft Corporation, +")]
    [InlineData("MSFT, Microsoft Corporation, \"")]
    [InlineData("MSFT, Microsoft Corporation, ...")]
    public void CreateSharesInputFromCsv_ThrowsInvalidOperationException_GivenIncorrectlyFormattedValue(string sharesInputCsv)
    {
        // Act and Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _sut.CreateSharesInputFromCsv([sharesInputCsv]));
        Assert.StartsWith("Shares input CSV contains incorrectly formatted value(s): ", ex.Message);
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.StartsWith("Shares input CSV contains incorrectly formatted value(s):"))), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, "Shares input CSV contains incorrectly formatted value(s): ");
    }
}
