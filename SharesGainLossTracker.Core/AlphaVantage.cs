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
    public class AlphaVantage : IStock
    {
        private readonly ILog Log;
        private readonly IProgress<ProgressLog> Progress;

        public AlphaVantage(ILog log, IProgress<ProgressLog> progress)
        {
            this.Log = log;
            this.Progress = progress;
        }

        async Task<List<FlattenedStock>> IStock.GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages, List<Share> sharesInput)
        {
            List<AlphaVantageRoot> stocks = new();
            var hadDeserializingErrors = false;

            foreach (var item in httpResponseMessages)
            {
                if (item.IsSuccessStatusCode)
                {
                    var stock = item.Content.ReadFromJsonAsync<AlphaVantageRoot>().Result;
                    if (stock != null && stock.MetaData != null && stock.Data != null && stock.Data.Count > 0)
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

            var stocksWithData = distinctStocks.Where(ss => stocks.Any(s => s.MetaData != null && s.MetaData.Symbol == ss.Symbol && s.Data != null && s.Data.Count > 0));
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
                Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds settings.")));
            }

            await Task.Run(() => Task.Delay(1));
            return GetFlattenedStocks(stocks);
        }

        static List<FlattenedStock> GetFlattenedStocks(List<AlphaVantageRoot> stocks)
        {
            var flattenedStocks = new List<FlattenedStock>();

            if (stocks != null)
            {
                foreach (var stock in stocks.Where(s => s.MetaData != null && s.Data != null))
                {
                    foreach (var data in stock.Data)
                    {
                        flattenedStocks.Add(new FlattenedStock(DateTime.Parse(data.Key), stock.MetaData.Symbol, Convert.ToDouble(data.Value.AdjustedClose)));
                    }
                }
            }

            return flattenedStocks;
        }
    }
}
