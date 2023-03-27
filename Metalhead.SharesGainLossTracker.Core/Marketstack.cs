using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core
{
    public class Marketstack : IStock
    {
        public ILogger<Marketstack> Log { get; }
        public IProgress<ProgressLog> Progress { get; }

        public Marketstack(ILogger<Marketstack> log, IProgress<ProgressLog> progress)
        {
            Log = log;
            Progress = progress;
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
                    if (stock is not null && stock.Data is not null && stock.Data.Length > 0)
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
                Log.LogError("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting."));
            }

            return GetFlattenedStocks(stocks);
        }

        static List<FlattenedStock> GetFlattenedStocks(List<MarketstackRoot> stocks)
        {
            var flattenedStocks = new List<FlattenedStock>();

            if (stocks is not null)
            {
                flattenedStocks.AddRange(stocks.Where(s => s.Data is not null).SelectMany(stock => stock.Data).Select(data => new FlattenedStock(DateTime.Parse(data.Date), data.Symbol, data.AdjustedClose)));
            }

            return flattenedStocks;
        }
    }
}
