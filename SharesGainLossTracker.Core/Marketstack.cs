using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using log4net;
using SharesGainLossTracker.Core.Models;

namespace SharesGainLossTracker.Core
{
    public class Marketstack : IStock
    {
        private readonly ILog Log;
        private readonly IProgress<ProgressLog> Progress;

        public Marketstack(ILog log, IProgress<ProgressLog> progress)
        {
            this.Log = log;
            this.Progress = progress;
        }

        async Task<List<FlattenedStock>> IStock.GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages, List<Share> sharesInput)
        {
            List<MarketstackRoot> stocks = new();
            var hadDeserializingErrors = false;

            foreach (var item in httpResponseMessages)
            {
                if (item.IsSuccessStatusCode)
                {
                    var stock = item.Content.ReadFromJsonAsync<MarketstackRoot>().Result;
                    if (stock != null && stock.Data != null && stock.Data.Length > 0)
                    {
                        stocks.Add(stock);
                    }
                    else
                    {
                        hadDeserializingErrors = true;
                    }
                }
            }

            // Get a distinct list of stock symbols from the input, with the first associated stock name found (could be multiple).
            List<Share> distinctStocks = sharesInput
                .GroupBy(s => s.Symbol) // Group by the Symbol property
                .Select(g => g.First()) // Select the first item of each group
                .Select(s => new Share { Symbol = s.Symbol, StockName = s.StockName })
                .ToList();

            var stocksWithData = distinctStocks.Where(ss => stocks.Any(s => s.Data != null && s.Data.FirstOrDefault(d => d.Symbol.Equals(ss.Symbol, StringComparison.OrdinalIgnoreCase)) != null));
            var stocksWithoutData = distinctStocks.Where(ss => !stocksWithData.Contains(ss));

            if (stocksWithData.Any())
            {
                foreach (var symbols in stocksWithData)
                {
                    Log.InfoFormat("Successfully fetched stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName);
                    Progress.Report(new ProgressLog(MessageImportance.Good, string.Format("Successfully fetched stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName)));
                }
            }

            if (stocksWithoutData.Any())
            {
                foreach (var symbols in stocksWithoutData)
                {
                    Log.ErrorFormat("Failed fetching stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Failed to fetch stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName)));
                }
            }

            if (hadDeserializingErrors)
            {
                Log.ErrorFormat("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting.")));
            }

            await Task.Run(() => Task.CompletedTask);
            return GetFlattenedStocks(stocks);
        }

        static List<FlattenedStock> GetFlattenedStocks(List<MarketstackRoot> stocks)
        {
            var flattenedStocks = new List<FlattenedStock>();

            if (stocks != null)
            {
                flattenedStocks.AddRange(stocks.Where(s => s.Data != null).SelectMany(stock => stock.Data).Select(data => new FlattenedStock(DateTime.Parse(data.Date), data.Symbol, data.AdjustedClose)));
            }

            return flattenedStocks;
        }
    }
}
