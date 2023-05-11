using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.ConsoleApp
{
    public class App
    {
        private ILogger<App> Log { get; }
        private Settings AppSettings { get; }
        private IExcelWorkbookCreatorService ExcelWorkbookCreatorService { get; }

        public App(ILogger<App> log, IConfiguration configuration, IExcelWorkbookCreatorService excelWorkbookCreatorService)
        {
            Log = log;
            ExcelWorkbookCreatorService = excelWorkbookCreatorService;
            AppSettings = configuration.GetSection("sharesSettings").Get<Settings>();
        }

        public async Task RunAsync()
        {
            // Get stocks data for all groups and create an Excel Workbook for each.
            List<string> outputFilePathOpened = new();

            foreach (var shareGroup in AppSettings.Groups.Where(g => g.Enabled))
            {
                var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);

                if (AppSettings.SuffixDateToOutputFilePath)
                {
                    outputFilePath = $"{outputFilePath}{DateTime.Now.Date:yyyy-MM-dd}";
                }

                var excelFileFullPath = await ExcelWorkbookCreatorService.CreateWorkbookAsync(
                    shareGroup.Model,
                    symbolsFullPath,
                    shareGroup.ApiUrl,
                    shareGroup.ApiDelayPerCallMilleseconds,
                    shareGroup.OrderByDateDescending,
                    outputFilePath,
                    shareGroup.OutputFilenamePrefix,
                    AppSettings.AppendPurchasePriceToStockNameColumn);

                if (excelFileFullPath is not null && AppSettings.OpenOutputFileDirectory)
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
                    {
                        Log.LogError("Folder does not exist: ", outputFilePath);
                    }
                }
            }
        }
    }
}
