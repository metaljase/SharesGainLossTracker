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

            foreach (var item in httpResponseMessages)
            {
                if (item.IsSuccessStatusCode)
                {
                    var stock = item.Content.ReadFromJsonAsync<MarketstackRoot>().Result;
                    if (stock != null && stock.Data != null && stock.Data.Length > 0)
                    {
                        stocks.Add(stock);
                    }
                }
            }

            var sharesWithData = sharesInput.Where(ss => stocks.Any(s => s.Data != null && s.Data.FirstOrDefault(d => d.Symbol.Equals(ss.Symbol, StringComparison.OrdinalIgnoreCase)) != null));
            var sharesWithoutData = sharesInput.Where(ss => !sharesWithData.Contains(ss));

            if (sharesWithData.Any())
            {
                foreach (var symbols in sharesWithData)
                {
                    Log.InfoFormat("Successfully fetched stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName);
                    Progress.Report(new ProgressLog(MessageImportance.Good, string.Format("Successfully fetched stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName)));
                }
            }

            if (sharesWithoutData.Any())
            {
                foreach (var symbols in sharesWithoutData)
                {
                    Log.ErrorFormat("Failed fetching stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Failed to fetch stocks data for: {0} ({1})", symbols.Symbol, symbols.StockName)));
                }
            }

            await Task.Run(() => Task.Delay(1));
            return GetFlattenedStocks(stocks);
        }

        static List<FlattenedStock> GetFlattenedStocks(List<MarketstackRoot> stocks)
        {
            var flattenedStocks = new List<FlattenedStock>();

            if (stocks != null)
            {
                flattenedStocks.AddRange(stocks.Where(s => s.Data != null).SelectMany(stock => stock.Data).Select(data => new FlattenedStock() { Symbol = data.Symbol, Date = DateTime.Parse(data.Date).ToString("yyyy-MM-dd"), AdjustedClose = data.AdjustedClose }));
            }

            return flattenedStocks;
        }
    }
}
