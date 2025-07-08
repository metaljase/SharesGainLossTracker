using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core;

public class AlphaVantage(ILogger<AlphaVantage> log, IProgress<ProgressLog> progress) : IStock
{
    public ILogger<AlphaVantage> Log { get; } = log;
    public IProgress<ProgressLog> Progress { get; } = progress;

    async Task<List<FlattenedStock>> IStock.GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages, bool endpointReturnsAdjustedClose)
    {
        List<AlphaVantageRoot> stocks = [];
        var hadDeserializingErrors = false;
        var hadRateLimitError = false;
        var hadDailyLimitError = false;
        var hadInvalidApiCall = false;
        var hadPaidTierOnlyError = false;

        foreach (var item in httpResponseMessages)
        {
            if (item.IsSuccessStatusCode)
            {
                var stock = await item.Content.ReadFromJsonAsync(MetadataDeSerializerContext.Default.AlphaVantageRoot);
                if (stock is not null && stock.MetaData is not null && stock.Data is not null && stock.Data.Count > 0)
                {
                    stocks.Add(stock);
                }
                else
                {
                    if (stock is not null && !string.IsNullOrWhiteSpace(stock.Note) && stock.Note.EndsWith("if you would like to target a higher API call frequency."))
                    {
                        hadRateLimitError = true;
                    }
                    else if (stock is not null && !string.IsNullOrWhiteSpace(stock.Information) && stock.Information.EndsWith("to instantly remove all daily rate limits."))
                    {
                        hadDailyLimitError = true;
                    }
                    else if (stock is not null && !string.IsNullOrWhiteSpace(stock.Information) && stock.Information.Contains("This is a premium endpoint"))
                    {
                        hadPaidTierOnlyError = true;
                    }
                    else if (stock is not null && !string.IsNullOrWhiteSpace(stock.ErrorMessage) && stock.ErrorMessage.StartsWith("Invalid API call."))
                    {
                        hadInvalidApiCall = true;
                    }
                    else
                    {
                        hadDeserializingErrors = true;
                    }
                }
            }
        }

        if (hadRateLimitError)
        {
            Log.LogError("Rate limit error from stocks API. Try increasing ApiDelayPerCallMilliseconds setting.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Rate limit error from stocks API. Try increasing ApiDelayPerCallMilliseconds setting."));
        }
        if (hadDailyLimitError)
        {
            Log.LogError("Daily API call limit exceeded error from stocks API. Your Alpha Vantage plan may need upgrading.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Daily API call limit exceeded error from stocks API. Your Alpha Vantage plan may need upgrading."));
        }
        if (hadPaidTierOnlyError)
        {
            Log.LogError("Paid tier only error from stocks API. You need to upgrade your Alpha Vantage plan to use this API endpoint.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Paid tier only error from stocks API. You need to upgrade your Alpha Vantage plan to use this API endpoint."));
        }
        if (hadInvalidApiCall)
        {
            Log.LogError("Invalid API call error from stocks API. Possible incorrect stock symbol in shares input file.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Invalid API call error from stocks API. Possible incorrect stock symbol in shares input file."));
        }
        if (hadDeserializingErrors)
        {
            Log.LogError("Error deserializing data from stocks API.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Error deserializing data from stocks API."));
        }

        return GetFlattenedStocks(stocks, endpointReturnsAdjustedClose);
    }

    static List<FlattenedStock> GetFlattenedStocks(List<AlphaVantageRoot> stocks, bool closeValueIsAdjusted)
    {
        var flattenedStocks = new List<FlattenedStock>();

        if (stocks != null)
        {
            foreach (var stock in stocks.Where(s => s.MetaData is not null && s.Data != null))
            {
                foreach (var data in stock.Data!)
                {
                    var close = closeValueIsAdjusted ? Convert.ToDouble(data.Value.AdjustedClose) : Convert.ToDouble(data.Value.Close);
                    flattenedStocks.Add(new FlattenedStock(DateTime.Parse(data.Key), stock.MetaData!.Symbol, close));
                }
            }
        }

        return flattenedStocks;
    }
}
