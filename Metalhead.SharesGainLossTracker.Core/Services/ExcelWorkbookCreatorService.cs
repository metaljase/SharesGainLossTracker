using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Metalhead.Extensions;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.FileSystem;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class ExcelWorkbookCreatorService : IExcelWorkbookCreatorService
{
    public ILogger<ExcelWorkbookCreatorService> Log { get; }
    public IProgress<ProgressLog> Progress { get; }
    public IStocksDataService StocksDataService { get; }
    public ISharesInputLoader ShareInputLoader { get; }
    public ISharesOutputService SharesOutputService { get; }
    public IFileStreamFactory FileStreamFactory { get; }
    public ISharesInputHelperWrapper SharesInputHelper { get; }
    public ISharesOutputDataTableHelperWrapper SharesOutputDataTableHelper { get; }
    public ISharesOutputHelperWrapper SharesOutputHelper { get; }

    public ExcelWorkbookCreatorService(ILogger<ExcelWorkbookCreatorService> log, IProgress<ProgressLog> progress, IStocksDataService stocksDataService, ISharesInputLoader shareInputLoader, ISharesOutputService sharesOutputService, IFileStreamFactory fileStreamFactory, ISharesInputHelperWrapper sharesInputHelper, ISharesOutputDataTableHelperWrapper sharesOutputDataTableHelper, ISharesOutputHelperWrapper sharesOutputHelper)
    {
        Log = log;
        Progress = progress;
        StocksDataService = stocksDataService;
        ShareInputLoader = shareInputLoader;
        SharesOutputService = sharesOutputService;
        FileStreamFactory = fileStreamFactory;
        SharesInputHelper = sharesInputHelper;
        SharesOutputDataTableHelper = sharesOutputDataTableHelper;
        SharesOutputHelper = sharesOutputHelper;
    }

    public async Task<string> CreateWorkbookAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, int apiDelayPerCallMillieseconds, bool orderByDateDescending, string outputFilePath, string outputFilenamePrefix, bool appendPriceToStockName)
    {
        var sharesOutput = await SharesOutputService.CreateSharesOutputAsync(model, sharesInputFileFullPath, stocksApiUrl, apiDelayPerCallMillieseconds, orderByDateDescending, appendPriceToStockName);

        if (sharesOutput is null)
        {
            return null;
        }

        // Create a DataTable containing the gain/loss, and a DataTable containing the adjusted close price.
        List<DataTable> dataTables = new()
        {
            SharesOutputDataTableHelper.CreateGainLossPivotedDataTable(sharesOutput, "Gain/Loss"),
            SharesOutputDataTableHelper.CreateAdjustedClosePivotedDataTable(sharesOutput, "Adjusted Close")
        };

        // Create an Excel Workbook from the DataTables.
        try
        {
            var memoryStream = await CreateWorkbookAsMemoryStreamAsync(dataTables, "Shares");
            var fullPath = GetOutputFullPath(outputFilePath, outputFilenamePrefix);
            return SaveMemoryStreamToFile(memoryStream, fullPath);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentNullException or ArgumentException or InvalidOperationException)
            {
                Log.LogError(ex, "Error creating Excel Workbook due to no data.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Error creating Excel Workbook due to no data.", false));
                return null;
            }
            throw;
        }
    }

    public async static Task<MemoryStream> CreateWorkbookAsMemoryStreamAsync(List<DataTable> dataTables, string workbookTitle)
    {
        if (dataTables is null)
        {
            throw new ArgumentNullException(nameof(dataTables));
        }
        else if (dataTables.Any(dt => dt is null))
        {
            throw new ArgumentException("Cannot create MemoryStream containing Excel Workbook because one or more DataTables are null.", nameof(dataTables));
        }
        else if (dataTables.Any(dt => dt.Rows.Count == 0))
        {
            throw new InvalidOperationException("Cannot create MemoryStream containing Excel Workbook because DataTable has no rows.");
        }

        return await dataTables.ToExcelWorkbookMemoryStreamAsync(workbookTitle, 2, 2);
    }

    public string SaveMemoryStreamToFile(MemoryStream excelWorkbook, string fullFilePath)
    {
        DirectoryInfo directoryInfo = new(Path.GetDirectoryName(fullFilePath));
        directoryInfo.Create();

        using (Stream fileStream = FileStreamFactory.Create(fullFilePath, FileMode.CreateNew, FileAccess.Write))
        {
            excelWorkbook.WriteTo(fileStream);
        }

        Log.LogInformation("Successfully created: {OutputFileFullPath}", fullFilePath);
        Progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully created: {fullFilePath}", true));

        return fullFilePath;
    }

    public static string GetOutputFullPath(string outputFilePath, string outputFilenamePrefix)
    {
        // Validate outputFilePath.
        if (outputFilePath is null)
        {
            throw new ArgumentNullException(nameof(outputFilePath), "Output file path for Excel Workbook cannot be null.");
        }
        else if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            throw new ArgumentException("Output file path for Excel Workbook cannot be empty/whitespace.", nameof(outputFilePath));
        }

        if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new ArgumentException($"Output file path for Excel Workbook ('{outputFilePath}') contains invalid characters.", nameof(outputFilePath));
        }

        // Validate outputFilenamePrefix.
        if (outputFilenamePrefix is null)
        {
            throw new ArgumentNullException(nameof(outputFilenamePrefix), "Output filename prefix for Excel Workbook cannot be null.");
        }

        // Format path and filename.
        if (!Path.GetExtension(outputFilenamePrefix).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            outputFilenamePrefix += ".xlsx";
        }

        outputFilenamePrefix = string.Format("{0}{1}{2}",
            Path.GetFileNameWithoutExtension(outputFilenamePrefix),
            DateTime.Now.ToString("yyyy-MM-dd HHmmss"),
            Path.GetExtension(outputFilenamePrefix));

        return Path.Combine(outputFilePath, outputFilenamePrefix);
    }
}
