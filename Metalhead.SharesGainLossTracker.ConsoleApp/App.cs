using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.ConsoleApp;

public class App(ILogger<App> logger, SharesOptions sharesOptions, IExcelWorkbookCreatorService excelWorkbookCreatorService)
{
    public async Task RunAsync()
    {
        // Get stocks data for all groups and create an Excel Workbook for each.
        List<string> outputFilePathOpened = [];

        foreach (var shareGroup in sharesOptions.Groups.Where(g => g.Enabled))
        {
            var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
            var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);

            if (sharesOptions.SuffixDateToOutputFilePath == true)
                outputFilePath = $"{outputFilePath}{DateTime.Now.Date:yyyy-MM-dd}";

            var excelFileFullPath = await excelWorkbookCreatorService.CreateWorkbookAsync(
                shareGroup.Model,
                symbolsFullPath,
                shareGroup.ApiUrl,
                shareGroup.EndpointReturnsAdjustedClose,
                shareGroup.ApiDelayPerCallMilliseconds,
                shareGroup.OrderByDateDescending,
                outputFilePath,
                shareGroup.OutputFilenamePrefix,
                sharesOptions.AppendPurchasePriceToStockNameColumn == true);

            if (excelFileFullPath is not null && sharesOptions.OpenOutputFileDirectory == true)
            {
                if (Directory.Exists(outputFilePath))
                {
                    if (!outputFilePathOpened.Any(o => o.Equals(outputFilePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        outputFilePathOpened.Add(outputFilePath);
                        ProcessStartInfo startInfo = new("explorer.exe", outputFilePath);
                        Process.Start(startInfo);
                    }
                }
                else
                    logger.LogError("Folder does not exist: {OutputFilePath}", outputFilePath);
            }
        }
    }
}
