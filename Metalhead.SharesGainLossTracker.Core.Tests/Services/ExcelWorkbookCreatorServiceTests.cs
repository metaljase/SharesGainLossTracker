using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;
using Moq;
using Polly.Retry;
using Xunit;
using Metalhead.SharesGainLossTracker.Core.FileSystem;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Services;

public class ExcelWorkbookCreatorServiceTests
{
    private readonly ExcelWorkbookCreatorService _sut;
    private readonly Mock<ILogger<ExcelWorkbookCreatorService>> _mockLogger = new();
    private readonly Mock<IProgress<ProgressLog>> _mockProgress = new();
    private readonly Mock<IStocksDataService> _mockStocksDataService = new();
    private readonly Mock<ISharesInputLoader> _mockSharesInputLoader = new();
    private readonly Mock<IFileStreamFactory> _mockFileStreamFactory = new();
    private readonly Mock<ISharesInputHelperWrapper> _mockSharesInputHelper = new();
    private readonly Mock<ISharesOutputDataTableHelperWrapper> _mockSharesOutputDataTableHelper = new();
    private readonly Mock<ISharesOutputHelperWrapper> _mockSharesOutputHelper = new();

    public ExcelWorkbookCreatorServiceTests()
    {
        _mockProgress = new Mock<IProgress<ProgressLog>>();

        _sut = new ExcelWorkbookCreatorService(_mockLogger.Object, _mockProgress.Object, _mockStocksDataService.Object, _mockSharesInputLoader.Object, _mockFileStreamFactory.Object, _mockSharesInputHelper.Object, _mockSharesOutputDataTableHelper.Object, _mockSharesOutputHelper.Object);
    }

    [Fact]
    public async Task CreateWorkbookAsync_ReturnsWorkbookPath_GivenValidInput()
    {
        // Arrange
        var sharesInputFileFullPath = "My Shares.csv";
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMillieseconds = 0;
        var orderByDateDescending = true;
        var outputFilePath = @"C:\Temp\";
        var outputFilenamePrefix = "My Shares ";
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

        _mockSharesOutputDataTableHelper
            .Setup(x => x.CreateGainLossPivotedDataTable(It.IsAny<List<ShareOutput>>(), "Gain/Loss"))
            .Returns(MockData.CreateGainLossDataTable())
            .Verifiable();

        _mockSharesOutputDataTableHelper
            .Setup(x => x.CreateAdjustedClosePivotedDataTable(It.IsAny<List<ShareOutput>>(), "Adjusted Close"))
            .Returns(MockData.CreateAdjustedCloseDataTable())
            .Verifiable();

        var _mockMemoryStream = new Mock<MemoryStream>();
        _mockMemoryStream.Setup(x => x.WriteTo(_mockMemoryStream.Object)).Verifiable();
        string outputFullFilePath = string.Empty;
        _mockFileStreamFactory
            .Setup(x => x.Create(It.IsAny<string>(), FileMode.CreateNew, FileAccess.Write))
            .Callback<string, FileMode, FileAccess>((path, mode, access) => outputFullFilePath = path)
            .Returns(_mockMemoryStream.Object);

        // Act
        var result = await _sut.CreateWorkbookAsync(
            "IStockProxy",
            sharesInputFileFullPath,
            stocksApiUrl,
            apiDelayPerCallMillieseconds,
            orderByDateDescending,
            outputFilePath,
            outputFilenamePrefix,
            appendPriceToStockName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(outputFullFilePath, result);

        _mockStocksDataService.Verify(x => x.GetStock(It.IsAny<string>()), Times.Once);
        _mockSharesInputLoader.Verify(x => x.CreateSharesInput(It.IsAny<string>()), Times.Once);
        _mockStocksDataService.Verify(x => x.GetRetryPolicy(It.IsAny<int>()), Times.Once);
        _mockStocksDataService.Verify(x => x.FetchStocksDataAsync(It.IsAny<AsyncRetryPolicy>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Share>>()), Times.Once);
        mockIStock.Verify(x => x.GetStocksDataAsync(It.IsAny<HttpResponseMessage[]>()), Times.Once);
        _mockStocksDataService.Verify(x => x.IsExpectedStocksDataMapped(It.IsAny<List<FlattenedStock>>(), It.IsAny<List<Share>>()), Times.Once);
        _mockSharesInputHelper.Verify(x => x.AppendPurchasePriceToStockName(It.IsAny<List<Share>>()), Times.Once);
        _mockSharesInputHelper.Verify(x => x.MakeStockNamesUnique(It.IsAny<List<Share>>()), Times.Once);
        _mockSharesOutputHelper.Verify(x => x.CreateSharesOutput(It.IsAny<List<Share>>(), It.IsAny<List<FlattenedStock>>()), Times.Once);
        _mockSharesOutputDataTableHelper.Verify(x => x.CreateGainLossPivotedDataTable(It.IsAny<List<ShareOutput>>(), "Gain/Loss"), Times.Once);
        _mockSharesOutputDataTableHelper.Verify(x => x.CreateAdjustedClosePivotedDataTable(It.IsAny<List<ShareOutput>>(), "Adjusted Close"), Times.Once);
        _mockFileStreamFactory.Verify(x => x.Create(It.IsAny<string>(), FileMode.CreateNew, FileAccess.Write), Times.Once);
    }

    public static IEnumerable<object[]> IsExpectedStocksDataMappedExceptions =>
        new List<object[]>
        {
            new object[] { new ArgumentNullException("flattenedStocks") },
            new object[] { new ArgumentException("Failed to fetch any stocks data.", "flattenedStocks") },
        };

    [Theory]
    [MemberData(nameof(IsExpectedStocksDataMappedExceptions))]
    public async Task CreateWorkbookAsync_DoesNotThrowArgumentNullExceptionOrArgumentException_GivenIsExpectedStocksDataMappedThrowsArgumentNullExceptionOrArgumentException(Exception exception)
    {
        // Arrange
        var sharesInputFileFullPath = "My Shares.csv";
        var stocksApiUrl = "https://api.examplestocksapi.com/v1/eod?symbols={0}";
        var apiDelayPerCallMillieseconds = 0;
        var orderByDateDescending = true;
        var outputFilePath = @"C:\Temp\";
        var outputFilenamePrefix = "My Shares ";
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

        _mockSharesOutputDataTableHelper
            .Setup(x => x.CreateGainLossPivotedDataTable(It.IsAny<List<ShareOutput>>(), "Gain/Loss"))
            .Returns(MockData.CreateGainLossDataTable())
            .Verifiable();

        _mockSharesOutputDataTableHelper
            .Setup(x => x.CreateAdjustedClosePivotedDataTable(It.IsAny<List<ShareOutput>>(), "Adjusted Close"))
            .Returns(MockData.CreateAdjustedCloseDataTable())
            .Verifiable();

        var _mockMemoryStream = new Mock<MemoryStream>();
        _mockMemoryStream.Setup(x => x.WriteTo(_mockMemoryStream.Object)).Verifiable();
        string outputFullFilePath = string.Empty;
        _mockFileStreamFactory
            .Setup(x => x.Create(It.IsAny<string>(), FileMode.CreateNew, FileAccess.Write))
            .Callback<string, FileMode, FileAccess>((path, mode, access) => outputFullFilePath = path)
            .Returns(_mockMemoryStream.Object);

        // Act
        var result = await _sut.CreateWorkbookAsync(
            "IStockProxy",
            sharesInputFileFullPath,
            stocksApiUrl,
            apiDelayPerCallMillieseconds,
            orderByDateDescending,
            outputFilePath,
            outputFilenamePrefix,
            appendPriceToStockName);

        // Assert
        _mockStocksDataService.Verify(x => x.GetStock(It.IsAny<string>()), Times.Once);
        _mockSharesInputLoader.Verify(x => x.CreateSharesInput(It.IsAny<string>()), Times.Once);
        _mockStocksDataService.Verify(x => x.GetRetryPolicy(It.IsAny<int>()), Times.Once);
        _mockStocksDataService.Verify(x => x.FetchStocksDataAsync(It.IsAny<AsyncRetryPolicy>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<Share>>()), Times.Once);
        mockIStock.Verify(x => x.GetStocksDataAsync(It.IsAny<HttpResponseMessage[]>()), Times.Once);
        _mockStocksDataService.Verify(x => x.IsExpectedStocksDataMapped(It.IsAny<List<FlattenedStock>>(), It.IsAny<List<Share>>()), Times.Once);
        _mockLogger.VerifyLogging(LogLevel.Error, $"Failed to fetch any stocks data for input file: {outputFullFilePath}");
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Bad && log.DownloadLog.Equals($"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}"))), Times.Once);
        _mockSharesInputHelper.Verify(x => x.AppendPurchasePriceToStockName(It.IsAny<List<Share>>()), Times.Never);
        _mockSharesInputHelper.Verify(x => x.MakeStockNamesUnique(It.IsAny<List<Share>>()), Times.Never);
        _mockSharesOutputHelper.Verify(x => x.CreateSharesOutput(It.IsAny<List<Share>>(), It.IsAny<List<FlattenedStock>>()), Times.Never);
        _mockSharesOutputDataTableHelper.Verify(x => x.CreateGainLossPivotedDataTable(It.IsAny<List<ShareOutput>>(), "Gain/Loss"), Times.Never);
        _mockSharesOutputDataTableHelper.Verify(x => x.CreateAdjustedClosePivotedDataTable(It.IsAny<List<ShareOutput>>(), "Adjusted Close"), Times.Never);
        _mockFileStreamFactory.Verify(x => x.Create(It.IsAny<string>(), FileMode.CreateNew, FileAccess.Write), Times.Never);
    }

    [Fact]
    public async Task CreateWorkbookAsMemoryStreamAsync_CreatesExcelWorkbook()
    {
        // Arrange
        var dataTables = MockData.CreateGainLossDataTableAndAdjustedCloseDataTable();
        var workbookTitle = "Shares";
        var methodInfo = typeof(ExcelWorkbookCreatorService).GetMethod("CreateWorkbookAsMemoryStreamAsync", BindingFlags.Static | BindingFlags.Public);

        // Act and Assert
        Assert.NotNull(methodInfo);
        var task = (Task<MemoryStream>?)methodInfo.Invoke(_sut, new object[] { dataTables, workbookTitle });
        MemoryStream? result = null;
        if (task != null)
        {
            result = await task;
        }
        Assert.NotNull(task);
        Assert.NotNull(result);
        Assert.IsType<MemoryStream>(result);
    }

    [Fact]
    public async Task CreateWorkbookAsMemoryStreamAsync_ThrowsArgumentNullExceptionNullException_GivenNullDataTable()
    {
        // Arrange
        List<DataTable> dataTables = null;
        var workbookTitle = "Shares";

        // Act and Assert
        var methodInfo = typeof(ExcelWorkbookCreatorService).GetMethod("CreateWorkbookAsMemoryStreamAsync", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(methodInfo);
        var task = Assert.ThrowsAsync<ArgumentNullException>(() => (Task<MemoryStream>?)methodInfo.Invoke(_sut, new object[] { dataTables, workbookTitle }));
        ArgumentNullException? result = null;
        if (task != null)
        {
            result = await task!;
        }
        Assert.IsType<ArgumentNullException>(result);
        Assert.Equal(nameof(dataTables), result.ParamName);
    }

    [Fact]
    public async Task CreateWorkbookAsMemoryStreamAsync_ThrowsInvalidOperationException_GivenDataTableWithNoRows()
    {
        // Arrange
        List<DataTable> dataTables = new() { new DataTable(), new DataTable() };
        var workbookTitle = "Shares";

        // Act and Assert
        var methodInfo = typeof(ExcelWorkbookCreatorService).GetMethod("CreateWorkbookAsMemoryStreamAsync", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(methodInfo);
        var task = Assert.ThrowsAsync<InvalidOperationException>(() => (Task<MemoryStream>?)methodInfo.Invoke(_sut, new object[] { dataTables, workbookTitle }));
        InvalidOperationException? result = null;
        if (task != null)
        {
            result = await task!;
        }
        Assert.IsType<InvalidOperationException>(result);
        Assert.Equal("Cannot create MemoryStream containing Excel Workbook because DataTable has no rows.", result.Message);
    }

    [Fact]
    public void SaveMemoryStreamToFile_SavesExcelWorkbookAsFile()
    {
        // Arrange
        var outputFilePath = @"C:\Temp\";
        var outputFilenamePrefix = "My Shares ";
        var outputFullFilePath = ExcelWorkbookCreatorService.GetOutputFullPath(outputFilePath, outputFilenamePrefix);

        // Create a mock MemoryStream object to replace FileStream, preventing the Excel file being saved to disk.
        var _mockMemoryStream = new Mock<MemoryStream>();
        _mockMemoryStream.Setup(x => x.WriteTo(_mockMemoryStream.Object)).Verifiable();
        _mockFileStreamFactory.Setup(x => x.Create(outputFullFilePath, FileMode.CreateNew, FileAccess.Write)).Returns(_mockMemoryStream.Object);

        // Act
        var result = _sut.SaveMemoryStreamToFile(_mockMemoryStream.Object, outputFullFilePath);

        // Assert
        Assert.Equal(outputFullFilePath, result);
        _mockMemoryStream.Verify();
        _mockLogger.VerifyLogging(LogLevel.Information, $"Successfully created: {outputFullFilePath}");
        _mockProgress.Verify(x => x.Report(It.Is<ProgressLog>(log => log.Importance == MessageImportance.Good && log.DownloadLog.Equals($"Successfully created: {outputFullFilePath}"))), Times.Once);
    }

    [Theory]
    [InlineData(@"C:\Temp", "", @"C:\Temp\")]
    [InlineData(@"C:\Temp", "AlphaVantage - My Shares ", @"C:\Temp\AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp", @"\AlphaVantage - My Shares ", @"C:\Temp\AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp", " ", @"C:\Temp\ ")]
    [InlineData(@"C:\Temp", " AlphaVantage - My Shares ", @"C:\Temp\ AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp", @" \AlphaVantage - My Shares ", @"C:\Temp\AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp\", "", @"C:\Temp\")]
    [InlineData(@"C:\Temp\", "AlphaVantage - My Shares ", @"C:\Temp\AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp\", @"\AlphaVantage - My Shares ", @"C:\Temp\AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp\", " ", @"C:\Temp\ ")]
    [InlineData(@"C:\Temp\", " AlphaVantage - My Shares ", @"C:\Temp\ AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp\", @" \AlphaVantage - My Shares ", @"C:\Temp\AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp ", "", @"C:\Temp \")]
    [InlineData(@"C:\Temp ", "AlphaVantage - My Shares ", @"C:\Temp \AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp ", @"\AlphaVantage - My Shares ", @"C:\Temp \AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp ", " ", @"C:\Temp \ ")]
    [InlineData(@"C:\Temp ", " AlphaVantage - My Shares ", @"C:\Temp \ AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp ", @" \AlphaVantage - My Shares ", @"C:\Temp \AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp \", "", @"C:\Temp \")]
    [InlineData(@"C:\Temp \", "AlphaVantage - My Shares ", @"C:\Temp \AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp \", @"\AlphaVantage - My Shares ", @"C:\Temp \AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp \", " ", @"C:\Temp \ ")]
    [InlineData(@"C:\Temp \", " AlphaVantage - My Shares ", @"C:\Temp \ AlphaVantage - My Shares ")]
    [InlineData(@"C:\Temp \", @" \AlphaVantage - My Shares ", @"C:\Temp \AlphaVantage - My Shares ")]
    public void GetOutputFullPath_ReturnsFullPath_GivenValidFilePathAndValidFileNamePrefix(string outputFilePath, string outputFilenamePrefix, string resultShouldStartWith)
    {
        // Act
        string result = ExcelWorkbookCreatorService.GetOutputFullPath(outputFilePath, outputFilenamePrefix);
        string resultEndsWith = result.Substring(result.Length - 22);

        // Assert
        Assert.Equal($"{resultShouldStartWith}{resultEndsWith}", result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
    public void GetOutputFullPath_ThrowsArgumentNullException_GivenNullFilePathAndValidOrInvalidFilenamePrefix(string outputFilePath, string outputFilenamePrefix)
    {
        // Act and Assert
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => ExcelWorkbookCreatorService.GetOutputFullPath(outputFilePath, outputFilenamePrefix));
        Assert.Equal(nameof(outputFilePath), ex.ParamName);
    }

    [Theory]
    [InlineData(@"", null)]
    [InlineData(@" ", null)]
    [InlineData(@"", "AlphaVantage - My Shares ")]
    [InlineData(@" ", "AlphaVantage - My Shares ")]
    public void GetOutputFullPath_ThrowsArgumentException_GivenEmptyOrWhitespaceFilePathAndValidOrInvalidFilenamePrefix(string outputFilePath, string outputFilenamePrefix)
    {
        // Act and Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() => ExcelWorkbookCreatorService.GetOutputFullPath(outputFilePath, outputFilenamePrefix));
        Assert.Equal(nameof(outputFilePath), ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetOutputFullPath_ThrowsArgumentException_GivenInvalidFilePathAndValidOrInvalidFilenamePrefix(string outputFilenamePrefix)
    {
        // Arrange
        string outputFilePath = string.Join("", Path.GetInvalidPathChars());

        // Act and Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() => ExcelWorkbookCreatorService.GetOutputFullPath(outputFilePath, outputFilenamePrefix));
        Assert.Equal(nameof(outputFilePath), ex.ParamName);
    }

    [Theory]
    [InlineData(@"C:\Temp", null)]
    [InlineData(@"C:\Temp\", null)]
    public void GetOutputFullPath_ThrowsArgumentNullException_GivenValidFilePathAndNullFilenamePrefix(string outputFilePath, string outputFilenamePrefix)
    {
        // Act and Assert
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => ExcelWorkbookCreatorService.GetOutputFullPath(outputFilePath, outputFilenamePrefix));
        Assert.Equal(nameof(outputFilenamePrefix), ex.ParamName);
    }
}
