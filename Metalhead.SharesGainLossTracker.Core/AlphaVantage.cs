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
    public class AlphaVantage : IStock
    {
        private readonly ILogger<AlphaVantage> Log;
        private readonly IProgress<ProgressLog> Progress;

        public AlphaVantage(ILogger<AlphaVantage> log, IProgress<ProgressLog> progress)
        {
            this.Log = log;
            this.Progress = progress;
        }

        async Task<List<FlattenedStock>> IStock.GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages)
        {
            List<AlphaVantageRoot> stocks = new();
            var hadDeserializingErrors = false;

            foreach (var item in httpResponseMessages)
            {
                if (item.IsSuccessStatusCode)
                {
                    var stock = await item.Content.ReadFromJsonAsync<AlphaVantageRoot>();
                    if (stock is not null && stock.MetaData is not null && stock.Data is not null && stock.Data.Count > 0)
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
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds settings."));
            }

            return GetFlattenedStocks(stocks);
        }

        static List<FlattenedStock> GetFlattenedStocks(List<AlphaVantageRoot> stocks)
        {
            var flattenedStocks = new List<FlattenedStock>();

            if (stocks != null)
            {
                foreach (var stock in stocks.Where(s => s.MetaData is not null && s.Data != null))
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
