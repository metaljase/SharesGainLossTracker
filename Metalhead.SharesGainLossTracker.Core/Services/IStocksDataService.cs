﻿using Polly.Retry;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public interface IStocksDataService
{
    Task<HttpResponseMessage[]> FetchStocksDataAsync(AsyncRetryPolicy pollyPolicy, string stocksApiUrl, int apiDelayPerCallMilliseconds, List<Share> sharesInput);
    AsyncRetryPolicy GetRetryPolicy(int apiDelayPerCallMilliseconds);
    IStock GetStock(string model);
    bool IsExpectedStocksDataMapped(List<FlattenedStock> flattenedStocks, List<Share> sharesInput);
}