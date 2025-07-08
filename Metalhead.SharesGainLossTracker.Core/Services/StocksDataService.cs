using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class StocksDataService(ILogger<StocksDataService> log, IProgress<ProgressLog> progress, HttpClient httpClient, IEnumerable<IStock> iStocks, ISharesInputHelperWrapper sharesInputHelperWrapper) : IStocksDataService
{
    private ILogger<StocksDataService> Log { get; } = log;
    private IProgress<ProgressLog> Progress { get; } = progress;
    private HttpClient HttpClient { get; } = httpClient;
    private IEnumerable<IStock> IStocks { get; } = iStocks;
    private ISharesInputHelperWrapper SharesInputHelperWrapper { get; } = sharesInputHelperWrapper;

    public IStock GetStock(string model)
    {
        var stockModel = IStocks.FirstOrDefault(s => s.GetType().Name.Equals(model, StringComparison.OrdinalIgnoreCase));

        if (stockModel is null)
        {
            Log.LogError("No class implementing IStock could be found that matches '{Model}' (in settings).", model);
            Progress.Report(new ProgressLog(MessageImportance.Bad, $"No class implementing IStock could be found that matches '{model}' (in settings)."));
            throw new InvalidOperationException($"No class implementing IStock could be found that matches '{model}' (in settings).");
        }

        return stockModel;
    }

    public AsyncRetryPolicy GetRetryPolicy(int apiDelayPerCallMilliseconds)
    {
        return Policy
            .HandleInner<HttpRequestException>()
            .OrInner<TaskCanceledException>()
            .WaitAndRetryAsync(
            [
                TimeSpan.FromMilliseconds(Math.Max(0, apiDelayPerCallMilliseconds)),
                TimeSpan.FromMilliseconds(Math.Max(1000, apiDelayPerCallMilliseconds)),
                TimeSpan.FromMilliseconds(Math.Max(5000, apiDelayPerCallMilliseconds)),
                TimeSpan.FromMilliseconds(Math.Max(10000, apiDelayPerCallMilliseconds)),
                TimeSpan.FromMilliseconds(Math.Max(30000, apiDelayPerCallMilliseconds))
            ], (exception, timeSpan) =>
            {
                Log.LogWarning(exception, "Error fetching stocks data.  Retrying in {RetryInMilliseconds} milliseconds.", timeSpan.TotalMilliseconds);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Error fetching stocks data.  Retrying in {timeSpan.TotalMilliseconds} milliseconds."));
            });
    }

    public static void ValidateUri(string uri)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }
        else if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? uriResult))
        {
            throw new ArgumentException("Invalid URI format.", nameof(uri));
        }
        else if (!(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid URI scheme.", nameof(uri));
        }
    }

    public async Task<HttpResponseMessage[]> FetchStocksDataAsync(AsyncRetryPolicy pollyPolicy, string stocksApiUrl, int apiDelayPerCallMilliseconds, List<Share> sharesInput)
    {
        try
        {
            ValidateUri(stocksApiUrl);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentNullException or ArgumentException)
            {
                Log.LogError(ex, "URL for stocks API is invalid: {StocksApiUrl}", stocksApiUrl);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"URL for stocks API is invalid: {stocksApiUrl}"));
            }
            throw;
        }

        List<HttpResponseMessage> httpResponseMessages = [];
        try
        {
            foreach (var symbolName in SharesInputHelperWrapper.GetDistinctSymbolsNames(sharesInput))
            {
                // Fetch stock data using a Polly policy to trigger a retry if an HttpRequestException is thrown.
                httpResponseMessages.Add(await FetchStockDataAsync(pollyPolicy, stocksApiUrl, symbolName.Symbol, symbolName.StockName));

                // Pause before the next API call to avoid hitting the rate limit.
                await Task.Delay(new TimeSpan(0, 0, 0, 0, apiDelayPerCallMilliseconds));
            }
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException or TaskCanceledException)
            {
                // Swallow final HttpRequestException or TaskCanceledException so any successfully fetched stocks data can be processed.
                Log.LogError(ex, "Error fetching stocks data.  Reached maximum retries.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "Error fetching stocks data.  Reached maximum retries."));
            }
            else
            {
                throw;
            }
        }

        return [.. httpResponseMessages];
    }

    private async Task<HttpResponseMessage> FetchStockDataAsync(AsyncRetryPolicy pollyPolicy, string stocksApiUrl, string stockSymbol, string stockName)
    {
        HttpResponseMessage result = new();

        await pollyPolicy.ExecuteAsync(async () =>
        {
            Log.LogInformation("Sending request for stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
            Progress.Report(new ProgressLog(MessageImportance.Normal, $"Sending request for stocks data: {stockSymbol} ({stockName})"));

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(stocksApiUrl, stockSymbol));

            result = await HttpClient.SendAsync(httpRequestMessage).ContinueWith((task) =>
            {
                HttpResponseMessage response = task.Result;

                if (task.IsCompletedSuccessfully)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Log.LogInformation("Received successful response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                        Progress.Report(new ProgressLog(MessageImportance.Good, $"Received successful response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                    else
                    {
                        Log.LogError("Received failure response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                        Progress.Report(new ProgressLog(MessageImportance.Bad, $"Received failure response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                }
                else
                {
                    Log.LogError(task.Exception, "Failed to receive response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                    Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to receive response fetching stocks data: {stockSymbol} ({stockName})"));
                }

                return response;
            });
        });

        return result;
    }

    public bool IsExpectedStocksDataMapped(List<FlattenedStock> flattenedStocks, List<Share> sharesInput)
    {
        ArgumentNullException.ThrowIfNull(flattenedStocks);

        if (flattenedStocks.Count == 0)
        {
            throw new ArgumentException("Failed to fetch any stocks data.", nameof(flattenedStocks));
        }

        var allStocksFetchedSuccssfully = true;

        foreach (var stock in SharesInputHelperWrapper.GetDistinctSymbolsNames(sharesInput))
        {
            if (flattenedStocks.Any(s => s.Symbol.Equals(stock.Symbol, StringComparison.OrdinalIgnoreCase)))
            {
                Log.LogInformation("Successfully fetched stocks data for: {StockSymbol} ({StockName})", stock.Symbol, stock.StockName);
                Progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully fetched stocks data for: {stock.Symbol} ({stock.StockName})"));
            }
            else
            {
                allStocksFetchedSuccssfully = false;
                Log.LogError("Failed fetching stocks data for: {StockSymbol} ({StockName})", stock.Symbol, stock.StockName);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch stocks data for: {stock.Symbol} ({stock.StockName})"));
            }
        }

        return allStocksFetchedSuccssfully;
    }
}
