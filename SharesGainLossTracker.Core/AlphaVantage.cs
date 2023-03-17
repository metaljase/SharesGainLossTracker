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

            if (hadDeserializingErrors)
            {
                Log.ErrorFormat("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds setting.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, string.Format("Encountered deserialization errors. Try increasing ApiDelayPerCallMilleseconds settings.")));
            }

            await Task.Run(() => Task.CompletedTask);
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
