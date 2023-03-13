using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using log4net;
using SharesGainLossTracker.Core.Models;
using Metalhead.Extensions;
using Metalhead.Helpers;

namespace SharesGainLossTracker.Core
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

            var flattenedStocks = await stocks.GetStocksDataAsync(httpResponseMessages, sharesInput);

            if (flattenedStocks is null || flattenedStocks.Count == 0)
            {
                Log.Error("Failed to fetch any stocks data, therefore unable to create Excel file.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Failed to fetch any stocks data, therefore unable to create Excel file.", false));
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

            var pivotedDataTable = GetPivotedDataTable(shareOutputs);

            return CreateWorkbook(pivotedDataTable, "Shares", outputFilePath, outputFilenamePrefix);
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

            if (sharesInput.Count == 0)
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

            // Get a distinct list of stock symbols, so data is only fetched once per stock when multiple shares of stocks exist.
            List<string> distinctSymbols = sharesInput.Select(s => s.Symbol).Distinct().ToList();
            Dictionary<string, string> symbolStockNames = new();
            foreach (var distinctSymbol in distinctSymbols)
            {
                symbolStockNames.Add(distinctSymbol, sharesInput.FirstOrDefault(s => s.Symbol.Equals(distinctSymbol, StringComparison.OrdinalIgnoreCase)).StockName);
            }

            List<Task<HttpResponseMessage>> tasks = new();

            // Use thread-safe collection.
            var stocksSuccessfullyFetched = new ConcurrentBag<string>();
            var stocksData = new List<HttpResponseMessage>();

            Task<HttpResponseMessage[]> results = null;

            await Helper.ExponentialRetryAsync(
                5,
                async () =>
                {
                    tasks.Clear();
                    foreach (var symbolStockName in symbolStockNames.Where(s => !stocksSuccessfullyFetched.Any(c => c.Equals(s.Key, StringComparison.OrdinalIgnoreCase))))
                    {
                        Log.Info($"Sending request for stocks data: {symbolStockName.Key} ({symbolStockName.Value})");
                        Progress.Report(new ProgressLog(MessageImportance.Normal, string.Format("Sending request for stocks data: {0} ({1})", symbolStockName.Key, symbolStockName.Value)));

                        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(stocksApiUrl, symbolStockName.Key));
                        var httpResponseMessageTask = Client.SendAsync(httpRequestMessage);

                        httpResponseMessageTask.GetAwaiter().OnCompleted(() =>
                        {
                            if (httpResponseMessageTask.IsCompletedSuccessfully)
                            {
                                var httpResponseMessage = httpResponseMessageTask.Result;

                                if (httpResponseMessage.IsSuccessStatusCode)
                                {
                                    stocksSuccessfullyFetched.Add(symbolStockName.Key);

                                    Log.Info($"Received successful response fetching stocks data: {symbolStockName.Key} ({symbolStockName.Value})");
                                    Progress.Report(new ProgressLog(MessageImportance.Good, string.Format("Received successful response fetching stocks data: {0} ({1})", symbolStockName.Key, symbolStockName.Value)));
                                }
                                else
                                {
                                    Log.Error($"Received failure response fetching stocks data: {symbolStockName.Key} ({symbolStockName.Value})");
                                    Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Received failure response fetching stocks data: {0} ({1})", symbolStockName.Key, symbolStockName.Value)));
                                }
                            }
                            else
                            {
                                Log.Error($"Failed to receive response fetching stocks data: {symbolStockName.Key}  ( {symbolStockName.Value})", httpResponseMessageTask.Exception);
                                Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Failed to receive response fetching stocks data: {0} ({1})", symbolStockName.Key, symbolStockName.Value)));
                            }
                        });

                        tasks.Add(httpResponseMessageTask);
                        await Task.Delay(new TimeSpan(0, 0, 0, 0, apiDelayPerCallMilleseconds)).ConfigureAwait(false);
                    }

                    results = Task.WhenAll(tasks);

                    try
                    {
                        // Will throw an exception if any tasks failed (triggering retry), but any successful tasks will be extracted in 'finally' block.
                        await results.ConfigureAwait(false);
                    }
                    finally
                    {
                        stocksData.AddRange(tasks.Where(t => t.IsCompletedSuccessfully).Select(h => h.Result).Where(h => h.IsSuccessStatusCode).ToArray());
                    }
                },
                ex => ex is HttpRequestException || ex is TaskCanceledException,
                retryDelay =>
                {
                    return retryDelay switch
                    {
                        1 => Math.Max(0, apiDelayPerCallMilleseconds),
                        2 => Math.Max(1000, apiDelayPerCallMilleseconds),
                        3 => Math.Max(5000, apiDelayPerCallMilleseconds),
                        4 => Math.Max(10000, apiDelayPerCallMilleseconds),
                        _ => Math.Max(30000, apiDelayPerCallMilleseconds)
                    };
                }).ConfigureAwait(false);

            return stocksData.ToArray();
        }

        public static DataTable GetPivotedDataTable(List<ShareOutput> sharesOutput)
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

            return pivotDataTable;
        }

        public static string CreateWorkbook(DataTable dataTable, string worksheetName, string outputFilePath, string outputFilenamePrefix)
        {
            // Validate dataTable.
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null.");
            }
            else if (dataTable.Rows.Count == 0)
            {
                throw new InvalidOperationException("Cannot create Excel workbook because DataTable is invalid.");
            }

            // Validate outputFilePath.
            if (outputFilePath == null)
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

            // Create Excel workbook from DataTable and save.
            var fullPath = Path.Combine(outputFilePath, outputFilenamePrefix);

            worksheetName = string.IsNullOrWhiteSpace(worksheetName) ? "Shares" : worksheetName;
            dataTable.ToExcelWorkbook(worksheetName, new FileInfo(fullPath));

            Log.Info($"Successfully created: {fullPath}");
            Progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully created: {fullPath}", true));
            return fullPath;
        }
    }
}