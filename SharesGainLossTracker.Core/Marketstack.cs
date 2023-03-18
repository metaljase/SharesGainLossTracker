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

        async Task<List<FlattenedStock>> IStock.GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages)
        {
            List<MarketstackRoot> stocks = new();
            var hadDeserializingErrors = false;

            foreach (var item in httpResponseMessages)
            {
                if (item.IsSuccessStatusCode)
                {
                    var stock = await item.Content.ReadFromJsonAsync<MarketstackRoot>();
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

            if (hadDeserializingErrors)
            {
                Log.Error("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting."));
            }

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
