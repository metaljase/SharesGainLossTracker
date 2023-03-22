using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using log4net;
using Metalhead.Extensions;
using Metalhead.SharesGainLossTracker.Core.Models;
using Polly;
using Polly.Retry;

namespace Metalhead.SharesGainLossTracker.Core
{
    public class Shares
    {
        private static ILog Log;
        private static IProgress<ProgressLog> Progress;
        private static readonly HttpClient Client = new();

        public Shares(ILog log)
        {
            Log = log;
            Progress = new Progress<ProgressLog>();
        }

        public Shares(ILog log, IProgress<ProgressLog> progress)
        {
            Log = log;
            Progress = progress;
        }

        public static async Task<string> CreateWorkbookAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, int apiDelayPerCallSeconds, bool orderByDateDescending, string outputFilePath, string outputFilenamePrefix, bool appendPriceToStockName)
        {
            Log.Info($"Processing input file: {sharesInputFileFullPath}");
            Progress.Report(new ProgressLog(MessageImportance.Normal, $"Processing input file: {sharesInputFileFullPath}"));

            var sharesInput = CreateSharesInputFromCsvFile(sharesInputFileFullPath);

            var httpResponseMessages = await GetStocksDataAsync(stocksApiUrl, sharesInput, apiDelayPerCallSeconds);

            // Map the data from the API using the appropriate model.
            IStock stocks = null;
            if (model.Equals("AlphaVantage", StringComparison.OrdinalIgnoreCase))
            {
                stocks = new AlphaVantage(Log, Progress);
            }
            else if (model.Equals("Marketstack", StringComparison.OrdinalIgnoreCase))
            {
                stocks = new Marketstack(Log, Progress);
            }

            var flattenedStocks = await stocks.GetStocksDataAsync(httpResponseMessages);

            // Validate data was returned from the API and mapped.
            try
            {
                ValidateFlattenedStocks(flattenedStocks, sharesInput);
            }
            catch (ArgumentNullException)
            {
                Log.Error($"Failed to fetch any stocks data for input file: {sharesInputFileFullPath})");
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}", false));
                return null;
            }
            catch (ArgumentException)
            {
                Log.Error($"Failed to fetch any stocks data for input file: {sharesInputFileFullPath})");
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}", false));
                return null;
            }

            // Append share purchase price to stock name, to avoid ambiguity in Excel file when multiple shares of the same stock exist.
            if (appendPriceToStockName)
            {
                foreach (var shareInput in sharesInput)
                {
                    shareInput.StockName = $"{shareInput.StockName} {shareInput.PurchasePrice}";
                }
            }

            // Append sequential number to duplicate stock names to avoid ambiguity when pivoting data.
            var duplicateSymbolNames = sharesInput.Select(s => s.StockName).GroupBy(s => s).Where(g => g.Count() > 1).Select(s => s.Key);

            foreach (var duplicateStockName in duplicateSymbolNames)
            {
                var duplicateCount = 0;
                foreach (var shareInput in sharesInput.Where(s => s.StockName.Equals(duplicateStockName, StringComparison.OrdinalIgnoreCase)))
                {
                    shareInput.StockName = $"{shareInput.StockName} {duplicateCount += 1}";
                }
            }

            List<ShareOutput> shareOutputs = new();
            foreach (var flattenedStock in flattenedStocks)
            {
                // Get shares that match current stock symbol.  Multiple shares per stock may exist, e.g. with different purchase prices.
                var sharesForSymbol = sharesInput.Where(s => s.Symbol.Equals(flattenedStock.Symbol, StringComparison.OrdinalIgnoreCase));
                foreach (var shareForSymbol in sharesForSymbol)
                {
                    shareOutputs.Add(new ShareOutput(shareForSymbol.StockName, shareForSymbol.Symbol, shareForSymbol.PurchasePrice, flattenedStock.Date, flattenedStock.AdjustedClose));
                }
            }

            // Order data by date.
            shareOutputs = orderByDateDescending ? shareOutputs.OrderByDescending(o => o.Date).ToList() : shareOutputs.OrderBy(o => o.Date).ToList();

            // Get a DataTable containing the gain/loss, and a DataTable containing the adjusted close price.
            List<DataTable> dataTables = new()
            {
                GetGainLossPivotedDataTable(shareOutputs, "Gain/Loss"),
                GetAdjustedClosePivotedDataTable(shareOutputs, "Adjusted Close")
            };

            // Create an Excel Workbook from the DataTables.
            return CreateWorkbook(dataTables, outputFilePath, outputFilenamePrefix);
        }

        public static List<Share> CreateSharesInputFromCsvFile(string sharesInputFileFullPath)
        {
            if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && !File.Exists(sharesInputFileFullPath))
            {
                Log.Error($"Shares input file not found: {sharesInputFileFullPath}");
                throw new FileNotFoundException($"Shares input file not found.", sharesInputFileFullPath);
            }

            IEnumerable<string> delimitedSharesInput = new List<string>();
            if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && File.Exists(sharesInputFileFullPath))
            {
                delimitedSharesInput = File.ReadAllLines(sharesInputFileFullPath).Where(x => !string.IsNullOrEmpty(x) && x.Contains(','));

                if (!delimitedSharesInput.Any())
                {
                    Log.Error($"No correctly formatted shares input found in input file: {sharesInputFileFullPath}");
                    throw new InvalidOperationException($"No correctly formatted shares input found in input file: {sharesInputFileFullPath}");
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
                Log.Error("No correctly formatted shares input.");
                throw new InvalidOperationException("No correctly formatted shares input.");
            }

            return sharesInput;
        }

        public static async Task<HttpResponseMessage[]> GetStocksDataAsync(string stocksApiUrl, List<Share> sharesInput, int apiDelayPerCallMilleseconds)
        {
            if (!Uri.TryCreate(stocksApiUrl, UriKind.Absolute, out Uri stocksApiUri) || (stocksApiUri.Scheme != Uri.UriSchemeHttp && stocksApiUri.Scheme != Uri.UriSchemeHttps))
            {
                Log.Error("URL for stocks API is invalid.");
                throw new ArgumentException("URL for stocks API is invalid.");
            }            

            // Get a distinct list of stock symbols, so data is only fetched once per stock when multiple shares of the same stock exist.
            Dictionary<string, string> symbolStockNames = new();
            foreach (var distinctSymbol in sharesInput.Select(s => s.Symbol).Distinct())
            {
                symbolStockNames.Add(distinctSymbol, sharesInput.FirstOrDefault(s => s.Symbol.Equals(distinctSymbol, StringComparison.OrdinalIgnoreCase)).StockName);
            }

            var policy = GetRetryPolicy(apiDelayPerCallMilleseconds);

            List<HttpResponseMessage> httpResponseMessages = new();
            try
            {
                foreach (var stockSymbolName in symbolStockNames)
                {
                    // Fetch stock data using a Polly policy to trigger a retry if an HttpRequestException is thrown.
                    httpResponseMessages.Add(await policy.ExecuteAsync(async () =>
                    {
                        return await GetStocksDataAsync(stocksApiUrl, apiDelayPerCallMilleseconds, stockSymbolName.Key, stockSymbolName.Value);
                    }));
                }
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException or TaskCanceledException)
                {
                    // Swallow final HttpRequestException or TaskCanceledException so any successfully fetched stocks data can be processed.
                    Log.Error($"Exception fetching stocks data.  Reached maximum retries.", ex);
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
                    Log.Error($"Exception fetching stocks data.  Retrying in {timeSpan.TotalMilliseconds} milliseconds.", exception);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Error fetching stocks data.  Retrying in {timeSpan.TotalMilliseconds} milliseconds."));
                });
        }

        private static async Task<HttpResponseMessage> GetStocksDataAsync(string stocksApiUrl, int apiDelayPerCallMilleseconds, string stockSymbol, string stockName)
        {
            Log.Info($"Sending request for stocks data: {stockSymbol} ({stockName})");
            Progress.Report(new ProgressLog(MessageImportance.Normal, $"Sending request for stocks data: {stockSymbol} ({stockName})"));

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(stocksApiUrl, stockSymbol));

            var result = await Client.SendAsync(httpRequestMessage).ContinueWith((task) =>
            {
                HttpResponseMessage response = task.Result;

                if (task.IsCompletedSuccessfully)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Info($"Received successful response fetching stocks data: {stockSymbol} ({stockName})");
                        Progress.Report(new ProgressLog(MessageImportance.Good, $"Received successful response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                    else
                    {
                        Log.Error($"Received failure response fetching stocks data: {stockSymbol} ({stockName})");
                        Progress.Report(new ProgressLog(MessageImportance.Bad, $"Received failure response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                }
                else
                {
                    Log.Error($"Failed to receive response fetching stocks data: {stockSymbol} ({stockName})", task.Exception);
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
                throw new ArgumentNullException(nameof(flattenedStocks), "Failed to fetch any stocks data.");
            }

            if (!flattenedStocks.Any())
            {
                throw new ArgumentException("Failed to fetch any stocks data.", nameof(flattenedStocks));
            }

            // Get a distinct list of stock symbols from the input, with the first associated stock name found (could be multiple).
            List<Share> distinctStocks = sharesInput
                .GroupBy(s => s.Symbol) // Group by the Symbol property
                .Select(g => g.First()) // Select the first item of each group
                .Select(s => new Share { Symbol = s.Symbol, StockName = s.StockName })
                .ToList();

            var stocksWithData = distinctStocks.Where(ss => flattenedStocks.Any(s => s.Symbol.Equals(ss.Symbol, StringComparison.OrdinalIgnoreCase)));
            var stocksWithoutData = distinctStocks.Where(ss => !stocksWithData.Contains(ss));

            if (stocksWithData.Any())
            {
                foreach (var symbols in stocksWithData)
                {
                    Log.Info($"Successfully fetched stocks data for: {symbols.Symbol} ({symbols.StockName})");
                    Progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully fetched stocks data for: {symbols.Symbol} ({symbols.StockName})"));
                }
            }

            if (stocksWithoutData.Any())
            {
                foreach (var symbols in stocksWithoutData)
                {
                    Log.Error($"Failed fetching stocks data for: {symbols.Symbol} ({symbols.StockName})");
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch stocks data for: {symbols.Symbol} ({symbols.StockName})"));
                }
            }
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

        public static string CreateWorkbook(List<DataTable> dataTables, string outputFilePath, string outputFilenamePrefix)
        {
            // Validate dataTable.
            if (dataTables is null)
            {
                throw new ArgumentNullException(nameof(dataTables), "DataTable cannot be null.");
            }
            else if (dataTables.Any(dt => dt.Rows.Count == 0))
            {
                throw new InvalidOperationException("Cannot create Excel workbook because DataTable is invalid.");
            }

            var fullPath = GetOutputFullPath(outputFilePath, outputFilenamePrefix);

            // Create Excel workbook from List<DataTable> and save.
            dataTables.ToExcelWorkbook("Shares", new FileInfo(fullPath));

            Log.Info($"Successfully created: {fullPath}");
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