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

        public static async Task<string> CreateWorkbookAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, int apiDelayPerCallSeconds, bool orderByDateDescending, string outputFilePath, string outputFilenamePrefix)
        {
            var sharesInput = CreateSharesInputFromCsvFile(sharesInputFileFullPath);

            // Append stock symbol to the end of the stock name if duplicate stock names exist.
            var duplicateSymbolNames = sharesInput.Select(s => s.StockName).GroupBy(s => s).Where(g => g.Count() > 1).Select(s => s.Key);
            var cleanedSharesInput = new List<Share>();
            foreach (var shareInput in sharesInput)
            {
                if (duplicateSymbolNames.Any(d => d.Equals(shareInput.StockName, StringComparison.OrdinalIgnoreCase)))
                {
                    cleanedSharesInput.Add(new Share() { Symbol = shareInput.Symbol, StockName = $"{shareInput.StockName} ({shareInput.Symbol})", PurchasePrice = shareInput.PurchasePrice });
                }
                else
                {
                    cleanedSharesInput.Add(new Share() { Symbol = shareInput.Symbol, StockName = shareInput.StockName, PurchasePrice = shareInput.PurchasePrice });
                }
            }

            var httpResponseMessages = await GetStocksDataAsync(stocksApiUrl, cleanedSharesInput, apiDelayPerCallSeconds);

            IStock stocks = null;
            if (model.Equals("AlphaVantage", StringComparison.OrdinalIgnoreCase))
            {
                stocks = new AlphaVantage(Log, Progress);
            }
            else if (model.Equals("Marketstack", StringComparison.OrdinalIgnoreCase))
            {
                stocks = new Marketstack(Log, Progress);
            }

            var flattenedStocks = await stocks.GetStocksDataAsync(httpResponseMessages, cleanedSharesInput);

            // Order data by date.
            flattenedStocks = orderByDateDescending ? flattenedStocks.OrderByDescending(o => o.Date).ToList() : flattenedStocks.OrderBy(o => o.Date).ToList();

            if (flattenedStocks.Count == 0)
            {
                Log.Error("Failed to fetch any stocks data, therefore unable to create Excel file.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Failed to fetch any stocks data, therefore unable to create Excel file.", false));
                return null;
            }

            // Calculate the share gain/loss percentage.
            foreach (var flattenedStock in flattenedStocks)
            {
                var share = cleanedSharesInput.FirstOrDefault(s => s.Symbol.Equals(flattenedStock.Symbol, StringComparison.OrdinalIgnoreCase));
                flattenedStock.GainLoss = Math.Round((flattenedStock.AdjustedClose - share.PurchasePrice) / share.PurchasePrice * 100, 1);
            }

            var pivotedDataTable = GetPivotedDataTable(flattenedStocks);

            // Replace stock symbol with stock name on column headings.
            for (var i = 1; i <= pivotedDataTable.Columns.Count - 1; i++)
            {
                var symbol = cleanedSharesInput.FirstOrDefault(s => s.Symbol.Equals(pivotedDataTable.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase)).StockName;
                if (symbol != null)
                {
                    pivotedDataTable.Columns[i].ColumnName = symbol;
                }
            }

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
            // Split delimited shares input, trim whitespace, remove shares with duplicate symbols, and output to shares input object.
            var sharesInput = delimitedSharesInput.Select(item => item.Split(',').Select(a => a.Trim()).ToList())
                .GroupBy(s => s[0])
                .Select(s => s.First())
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

            List<Task<HttpResponseMessage>> tasks = new();

            // Use thread-safe collection.
            var sharesInputFetchedSuccessfully = new ConcurrentBag<Share>();
            var stocksData = new List<HttpResponseMessage>();

            Task<HttpResponseMessage[]> results = null;

            await Helper.ExponentialRetryAsync(
                5,
                async () =>
                {
                    tasks.Clear();
                    foreach (var shareInput in sharesInput.Where(s => !sharesInputFetchedSuccessfully.Any(c => c.Symbol.Equals(s.Symbol, StringComparison.OrdinalIgnoreCase))))
                    {
                        Log.Info($"Sending request for stocks data: {shareInput.Symbol} ({shareInput.StockName})");
                        Progress.Report(new ProgressLog(MessageImportance.Normal, string.Format("Sending request for stocks data: {0} ({1})", shareInput.Symbol, shareInput.StockName)));

                        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(stocksApiUrl, shareInput.Symbol));
                        var httpResponseMessageTask = Client.SendAsync(httpRequestMessage);

                        httpResponseMessageTask.GetAwaiter().OnCompleted(() =>
                        {
                            if (httpResponseMessageTask.IsCompletedSuccessfully)
                            {
                                var httpResponseMessage = httpResponseMessageTask.Result;

                                if (httpResponseMessage.IsSuccessStatusCode)
                                {
                                    sharesInputFetchedSuccessfully.Add(new Share() { Symbol = shareInput.Symbol, StockName = shareInput.StockName, PurchasePrice = shareInput.PurchasePrice });
                                    
                                    Log.Info($"Received successful response fetching stocks data: {shareInput.Symbol} ({shareInput.StockName})");
                                    Progress.Report(new ProgressLog(MessageImportance.Good, string.Format("Received successful response fetching stocks data: {0} ({1})", shareInput.Symbol, shareInput.StockName)));
                                }
                                else
                                {
                                    Log.Error($"Received failure response fetching stocks data: {shareInput.Symbol} ({shareInput.StockName})");
                                    Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Received failure response fetching stocks data: {0} ({1})", shareInput.Symbol, shareInput.StockName)));
                                }
                            }
                            else
                            {
                                Log.Error($"Failed to receive response fetching stocks data: {shareInput.Symbol}  ( {shareInput.StockName})", httpResponseMessageTask.Exception);
                                Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Failed to receive response fetching stocks data: {0} ({1})", shareInput.Symbol, shareInput.StockName)));
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

        public static DataTable GetPivotedDataTable(List<FlattenedStock> flattenedStocks)
        {
            DataTable pivotDataTable = new();

            if (flattenedStocks.Count > 0)
            {
                // Pivot flattened stocks so date rows are grouped.
                // Should throw exception if the same stock has more than one 'close' value for the same date.
                pivotDataTable = flattenedStocks.ToPivotedDataTable(
                    item => item.Symbol,
                    item => item.Date,
                    items => items.Any() ? items.Single().GainLoss : 0);
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