using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Metalhead.Extensions;
using Metalhead.SharesGainLossTracker.Core.Models;
using Polly;
using Polly.Retry;

namespace Metalhead.SharesGainLossTracker.Core
{
    public class Shares
    {
        private static ILogger<Shares> Log;
        private static IProgress<ProgressLog> Progress;
        private static HttpClient HttpClient;
        private static IEnumerable<IStock> IStocks;

        public Shares(ILogger<Shares> log, IProgress<ProgressLog> progress, HttpClient httpClient, IEnumerable<IStock> iStocks)
        {
            Log = log;
            Progress = progress;
            HttpClient = httpClient;
            IStocks = iStocks;
        }

        public static async Task<string> CreateWorkbookAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, int apiDelayPerCallSeconds, bool orderByDateDescending, string outputFilePath, string outputFilenamePrefix, bool appendPriceToStockName)
        {
            Log.LogInformation("Processing input file: {SharesInputFileFullPath}", sharesInputFileFullPath);
            Progress.Report(new ProgressLog(MessageImportance.Normal, $"Processing input file: {sharesInputFileFullPath}"));

            IStock stocks = IStocks.FirstOrDefault(s => s.GetType().Name.Equals(model, StringComparison.OrdinalIgnoreCase));
            if (stocks is null)
            {
                throw new InvalidOperationException($"No class implementing IStock could be found that matches '{model}' (in settings).");
            }

            var sharesInput = CreateSharesInputFromCsvFile(sharesInputFileFullPath);

            var httpResponseMessages = await GetStocksDataAsync(stocksApiUrl, apiDelayPerCallSeconds, sharesInput);

            // Map the data from the API using the appropriate model.
            var flattenedStocks = await stocks.GetStocksDataAsync(httpResponseMessages);

            // Validate data was returned from the API and mapped.
            try
            {
                ValidateFlattenedStocks(flattenedStocks, sharesInput);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException or ArgumentException)
                {
                    Log.LogError(ex, "Failed to fetch any stocks data for input file: ", sharesInputFileFullPath);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}", false));
                    return null;
                }
                throw;
            }

            // Append share purchase price to stock name, to avoid ambiguity in Excel file when multiple shares of the same stock exist.
            AppendPurchasePriceToStockName(appendPriceToStockName, sharesInput);

            // Make duplicate stock names unique to avoid ambiguity when pivoting data.
            MakeStockNamesUnique(sharesInput);

            List<ShareOutput> sharesOutput = CreateSharesOutput(sharesInput, flattenedStocks);

            // Order data by date.
            sharesOutput = orderByDateDescending ? sharesOutput.OrderByDescending(o => o.Date).ToList() : sharesOutput.OrderBy(o => o.Date).ToList();

            // Get a DataTable containing the gain/loss, and a DataTable containing the adjusted close price.
            List<DataTable> dataTables = new()
            {
                GetGainLossPivotedDataTable(sharesOutput, "Gain/Loss"),
                GetAdjustedClosePivotedDataTable(sharesOutput, "Adjusted Close")
            };

            // Create an Excel Workbook from the DataTables.
            try
            {
                return CreateWorkbook(dataTables, "Shares", outputFilePath, outputFilenamePrefix);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException or InvalidOperationException)
                {
                    Log.LogError(ex, "Error creating Excel Workbook due to no data.");
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Error creating Excel Workbook due to no data.", false));
                    return null;
                }
                throw;
            }
        }

        public static List<Share> CreateSharesInputFromCsvFile(string sharesInputFileFullPath)
        {
            if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && !File.Exists(sharesInputFileFullPath))
            {
                Log.LogError("Shares input file not found: {SharesInputFileFullPath}", sharesInputFileFullPath);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Shares input file not found: {sharesInputFileFullPath}", false));
                throw new FileNotFoundException($"Shares input file not found.", sharesInputFileFullPath);
            }

            IEnumerable<string> delimitedSharesInput = new List<string>();
            if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && File.Exists(sharesInputFileFullPath))
            {
                delimitedSharesInput = File.ReadAllLines(sharesInputFileFullPath).Where(x => !string.IsNullOrEmpty(x) && x.Contains(','));

                if (!delimitedSharesInput.Any())
                {
                    Log.LogError("No correctly formatted shares found in input file: {SharesInputFileFullPath}", sharesInputFileFullPath);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"No correctly formatted shares found in input file: {sharesInputFileFullPath}", false));
                    throw new InvalidOperationException($"No correctly formatted shares found in input file: {sharesInputFileFullPath}");
                }
            }

            return CreateSharesInputFromCsv(delimitedSharesInput);
        }

        public static List<Share> CreateSharesInputFromCsv(IEnumerable<string> delimitedSharesInput)
        {
            // Split delimited shares input, trim whitespace, and output to shares input object.
            var sharesInput = delimitedSharesInput.Select(item => item.Split(',').Select(a => a.Trim()).ToList())
                .Select(s => new Share() { Symbol = s[0], StockName = s[1], PurchasePrice = double.Parse(s[2]) })
                .ToList();

            if (!sharesInput.Any())
            {
                Log.LogError("No correctly formatted shares input.");
                throw new InvalidOperationException("No correctly formatted shares input.");
            }

            return sharesInput;
        }

        public static async Task<HttpResponseMessage[]> GetStocksDataAsync(string stocksApiUrl, int apiDelayPerCallMilleseconds, List<Share> sharesInput)
        {
            if (!Uri.TryCreate(stocksApiUrl, UriKind.Absolute, out Uri stocksApiUri) || (stocksApiUri.Scheme != Uri.UriSchemeHttp && stocksApiUri.Scheme != Uri.UriSchemeHttps))
            {
                Log.LogError("URL for stocks API is invalid.");
                throw new ArgumentException("URL for stocks API is invalid.");
            }

            var policy = GetRetryPolicy(apiDelayPerCallMilleseconds);
            List<HttpResponseMessage> httpResponseMessages = new();
            try
            {
                foreach (var symbolName in GetDistinctSymbolsNames(sharesInput))
                {
                    // Fetch stock data using a Polly policy to trigger a retry if an HttpRequestException is thrown.
                    httpResponseMessages.Add(await policy.ExecuteAsync(async () =>
                    {
                        return await GetStockDataAsync(stocksApiUrl, apiDelayPerCallMilleseconds, symbolName.Symbol, symbolName.StockName);
                    }));
                }
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException or TaskCanceledException)
                {
                    // Swallow final HttpRequestException or TaskCanceledException so any successfully fetched stocks data can be processed.
                    Log.LogError(ex, "Error fetching stocks data.  Reached maximum retries.");
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Error fetching stocks data.  Reached maximum retries."));
                }
                else
                {
                    throw;
                }
            }

            return httpResponseMessages.ToArray();
        }

        private static AsyncRetryPolicy GetRetryPolicy(int apiDelayPerCallMilleseconds)
        {
            return Policy
                .HandleInner<HttpRequestException>()
                .OrInner<TaskCanceledException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromMilliseconds(Math.Max(0, apiDelayPerCallMilleseconds)),
                    TimeSpan.FromMilliseconds(Math.Max(1000, apiDelayPerCallMilleseconds)),
                    TimeSpan.FromMilliseconds(Math.Max(5000, apiDelayPerCallMilleseconds)),
                    TimeSpan.FromMilliseconds(Math.Max(10000, apiDelayPerCallMilleseconds)),
                    TimeSpan.FromMilliseconds(Math.Max(30000, apiDelayPerCallMilleseconds))
                }, (exception, timeSpan) =>
                {
                    Log.LogWarning(exception, "Error fetching stocks data.  Retrying in {RetryInMilliseconds} milliseconds.", timeSpan.TotalMilliseconds);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Error fetching stocks data.  Retrying in {timeSpan.TotalMilliseconds} milliseconds."));
                });
        }

        private static async Task<HttpResponseMessage> GetStockDataAsync(string stocksApiUrl, int apiDelayPerCallMilleseconds, string stockSymbol, string stockName)
        {
            Log.LogInformation("Sending request for stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
            Progress.Report(new ProgressLog(MessageImportance.Normal, $"Sending request for stocks data: {stockSymbol} ({stockName})"));

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(stocksApiUrl, stockSymbol));

            var result = await HttpClient.SendAsync(httpRequestMessage).ContinueWith((task) =>
            {
                HttpResponseMessage response = task.Result;

                if (task.IsCompletedSuccessfully)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Log.LogInformation("Received successful response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                        Progress.Report(new ProgressLog(MessageImportance.Good, $"Received successful response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                    else
                    {
                        Log.LogError("Received failure response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                        Progress.Report(new ProgressLog(MessageImportance.Bad, $"Received failure response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                }
                else
                {
                    Log.LogError(task.Exception, "Failed to receive response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to receive response fetching stocks data: {stockSymbol} ({stockName})"));
                }

                return response;
            });

            // Pause before the next API call to avoid hitting the rate limit.
            await Task.Delay(new TimeSpan(0, 0, 0, 0, apiDelayPerCallMilleseconds));

            return result;
        }

        private static void ValidateFlattenedStocks(List<FlattenedStock> flattenedStocks, List<Share> sharesInput)
        {
            if (flattenedStocks is null)
            {
                throw new ArgumentNullException(nameof(flattenedStocks));
            }

            if (!flattenedStocks.Any())
            {
                throw new ArgumentException("Failed to fetch any stocks data.", nameof(flattenedStocks));
            }

            foreach (var stock in GetDistinctSymbolsNames(sharesInput))
            {
                if (flattenedStocks.Any(s => s.Symbol.Equals(stock.Symbol, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.LogInformation("Successfully fetched stocks data for: {StockSymbol} ({StockName})", stock.Symbol, stock.StockName);
                    Progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully fetched stocks data for: {stock.Symbol} ({stock.StockName})"));
                }
                else
                {
                    Log.LogError("Failed fetching stocks data for: {StockSymbol} ({StockName})", stock.Symbol, stock.StockName);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch stocks data for: {stock.Symbol} ({stock.StockName})"));
                }
            }
        }

        private static IEnumerable<Share> GetDistinctSymbolsNames(List<Share> sharesInput)
        {
            // Get distinct stock symbols from the input with the first associated stock name found (could be multiple).
            return sharesInput
                .GroupBy(s => s.Symbol) // Group by the Symbol property
                .Select(g => g.First()) // Select the first item of each group
                .Select(s => new Share { Symbol = s.Symbol, StockName = s.StockName });
        }

        private static void AppendPurchasePriceToStockName(bool appendPriceToStockName, List<Share> sharesInput)
        {
            if (appendPriceToStockName)
            {
                foreach (var shareInput in sharesInput)
                {
                    shareInput.StockName = $"{shareInput.StockName} {shareInput.PurchasePrice}";
                }
            }
        }

        private static void MakeStockNamesUnique(List<Share> sharesInput)
        {
            var duplicateStockNames = sharesInput.Select(s => s.StockName).GroupBy(s => s).Where(g => g.Count() > 1).Select(s => s.Key);

            foreach (var duplicateStockName in duplicateStockNames)
            {
                var duplicateCount = 0;
                foreach (var shareInput in sharesInput.Where(s => s.StockName.Equals(duplicateStockName, StringComparison.OrdinalIgnoreCase)))
                {
                    shareInput.StockName = $"{shareInput.StockName} {duplicateCount += 1}";
                }
            }
        }

        private static List<ShareOutput> CreateSharesOutput(List<Share> sharesInput, List<FlattenedStock> flattenedStocks)
        {
            List<ShareOutput> sharesOutput = new();
            foreach (var flattenedStock in flattenedStocks)
            {
                // Get shares that match current stock symbol.  Multiple shares per stock may exist, e.g. with different purchase prices.
                var sharesForSymbol = sharesInput.Where(s => s.Symbol.Equals(flattenedStock.Symbol, StringComparison.OrdinalIgnoreCase));
                foreach (var shareForSymbol in sharesForSymbol)
                {
                    sharesOutput.Add(new ShareOutput(shareForSymbol.StockName, shareForSymbol.Symbol, shareForSymbol.PurchasePrice, flattenedStock.Date, flattenedStock.AdjustedClose));
                }
            }

            return sharesOutput;
        }

        public static DataTable GetGainLossPivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
        {
            DataTable pivotDataTable = new();

            if (sharesOutput.Count > 0)
            {
                // Pivot stocks so date rows are grouped.
                pivotDataTable = sharesOutput.ToPivotedDataTable(
                    item => item.StockName,
                    item => item.Date,
                    items => items.Any() ? items.Single().GainLoss : null);
            }

            pivotDataTable.TableName = dataTableName;
            return pivotDataTable;
        }

        public static DataTable GetAdjustedClosePivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
        {
            DataTable pivotDataTable = new();

            if (sharesOutput.Count > 0)
            {
                // Pivot stocks so date rows are grouped.
                pivotDataTable = sharesOutput.ToPivotedDataTable(
                    item => item.StockName,
                    item => item.Date,
                    items => items.Any() ? items.Single().AdjustedClose : null);
            }

            pivotDataTable.TableName = dataTableName;
            return pivotDataTable;
        }

        public static string CreateWorkbook(List<DataTable> dataTables, string workbookTitle, string outputFilePath, string outputFilenamePrefix)
        {
            if (dataTables is null)
            {
                throw new ArgumentNullException(nameof(dataTables));
            }
            else if (dataTables.Any(dt => dt.Rows.Count == 0))
            {
                throw new InvalidOperationException("Cannot create Excel Workbook because DataTable has no rows.");
            }

            var fullPath = GetOutputFullPath(outputFilePath, outputFilenamePrefix);

            // Create Excel workbook from List<DataTable> and save.
            dataTables.ToExcelWorkbook(workbookTitle, new FileInfo(fullPath));

            Log.LogInformation("Successfully created: {ExcelWorkbookFileFullPath}", fullPath);
            Progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully created: {fullPath}", true));
            return fullPath;
        }

        public static string GetOutputFullPath(string outputFilePath, string outputFilenamePrefix)
        {
            // Validate outputFilePath.
            if (outputFilePath is null)
            {
                throw new ArgumentNullException(nameof(outputFilePath), "Output file path for Excel workbook cannot be null.");
            }
            else if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                throw new ArgumentException("Output file path for Excel workbook cannot be empty/whitespace.", nameof(outputFilePath));
            }

            if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException($"Output file path for Excel workbook ('{outputFilePath}') contains invalid characters.", nameof(outputFilePath));
            }

            // Format path and filename.
            if (!Path.GetExtension(outputFilenamePrefix).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                outputFilenamePrefix += ".xlsx";
            }

            outputFilenamePrefix = string.Format("{0}{1}{2}",
                Path.GetFileNameWithoutExtension(outputFilenamePrefix),
                DateTime.Now.ToString("yyyy-MM-dd HHmmss"),
                Path.GetExtension(outputFilenamePrefix)).Trim();
            
            return Path.Combine(outputFilePath, outputFilenamePrefix);
        }
    }
}