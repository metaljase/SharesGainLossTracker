using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core;

public class Marketstack(ILogger<Marketstack> log, IProgress<ProgressLog> progress) : IStock
{
    public ILogger<Marketstack> Log { get; } = log;
    public IProgress<ProgressLog> Progress { get; } = progress;

    async Task<List<FlattenedStock>> IStock.GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages, bool endpointReturnsAdjustedClose)
    {
        List<MarketstackRoot> stocks = [];
        var hadDeserializingError = false;
        var hadInvalidEndpointError = false;
        var hadEndpointAccessRestrictedError = false;
        var hadNoValidSymbolsError = false;
        var hadRateLimitError = false;
        var hadMonthlyRequestsLimitError = false;
        var hadOtherError = false;

        foreach (var item in httpResponseMessages)
        {
            if (item.IsSuccessStatusCode)
            {
                var stock = await item.Content.ReadFromJsonAsync(MetadataDeSerializerContext.Default.MarketstackRoot);
                if (stock is not null && stock.Data is not null)
                {
                    if (stock.Data.Length > 0)
                    {
                        stocks.Add(stock);
                    }
                }
                else
                {
                    hadDeserializingError = true;
                }
            }
            else
            {
                var error = await item.Content.ReadFromJsonAsync(MetadataDeSerializerContext.Default.MarketstackErrorRoot);
                if (error is null || error.Error is null)
                    hadDeserializingError = true;
                else
                {
                    if (error.Error.Code.Equals("invalid_api_function", StringComparison.InvariantCultureIgnoreCase)
                        || error.Error.Code.Equals("not_found_error", StringComparison.InvariantCultureIgnoreCase))
                        hadInvalidEndpointError = true;
                    else if (error.Error.Code.Equals("function_access_restricted", StringComparison.InvariantCultureIgnoreCase))
                        hadEndpointAccessRestrictedError = true;
                    else if (error.Error.Code.Equals("no_valid_symbols_provided", StringComparison.InvariantCultureIgnoreCase))
                        hadNoValidSymbolsError = true;
                    else if (error.Error.Code.Equals("rate_limit_reached", StringComparison.InvariantCultureIgnoreCase))
                        hadRateLimitError = true;
                    else if (error.Error.Code.Equals("too_many_requests", StringComparison.InvariantCultureIgnoreCase)
                        || error.Error.Code.Equals("usage_limit_reached", StringComparison.InvariantCultureIgnoreCase))
                        hadMonthlyRequestsLimitError = true;
                    else
                        hadOtherError = true;
                }
            }
        }

        if (hadDeserializingError)
        {
            Log.LogError("Error deserializing stocks data.  Try increasing the ApiDelayPerCallMilliseconds setting.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Error deserializing stocks data.  Try increasing the ApiDelayPerCallMilliseconds setting."));
        }
        if (hadInvalidEndpointError)
        {
            Log.LogError("Invalid endpoint error from stocks API.  Verify the endpoint URL is correct, especially the stock symbol.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Invalid endpoint error from stocks API.  Verify the endpoint URL is correct, especially the stock symbol."));
        }
        if (hadEndpointAccessRestrictedError)
        {
            Log.LogError("Access restricted error from stocks API.  Your plan may need upgrading to use this API endpoint.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Access restricted error from stocks API.  Your plan may need upgrading to use this API endpoint."));
        }
        if (hadNoValidSymbolsError)
        {
            Log.LogError("Invalid stock symbol error from stocks API.  Verify the stock symbols in the input file.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Invalid stock symbol error from stocks API.  Verify the stock symbols in the input file."));
        }
        if (hadRateLimitError)
        {
            Log.LogError("Rate limit exceeded error from stocks API.  Try increasing the ApiDelayPerCallMilliseconds setting.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Rate limit exceeded error from stocks API.  Try increasing the ApiDelayPerCallMilliseconds setting."));
        }
        if (hadMonthlyRequestsLimitError)
        {
            Log.LogError("Monthly API calls limit reached error from stocks API.  Plans with a higher limit may be available.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Monthly API calls limit reached error from stocks API.  Plans with a higher limit may be available."));
        }
        if (hadOtherError)
        {
            Log.LogError("Unknown error from stocks API.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Unknown error from stocks API."));
        }

        return GetFlattenedStocks(stocks, endpointReturnsAdjustedClose);
    }

    static List<FlattenedStock> GetFlattenedStocks(List<MarketstackRoot> stocks, bool closeValueIsAdjusted)
    {
        var flattenedStocks = new List<FlattenedStock>();

        if (stocks is not null)
        {
            flattenedStocks.AddRange(stocks.Where(s => s.Data is not null).SelectMany(stock => stock.Data!).Select(data => new FlattenedStock(DateTime.Parse(data.Date), data.Symbol, closeValueIsAdjusted ? data.AdjustedClose : data.Close)));
        }

        return flattenedStocks;
    }
}
