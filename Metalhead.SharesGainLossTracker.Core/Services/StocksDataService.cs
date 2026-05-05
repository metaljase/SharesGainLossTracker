using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class StocksDataService(
    ILogger<StocksDataService> logger,
    IProgress<ProgressLog> progress,
    HttpClient httpClient,
    IEnumerable<IStock> iStocks,
    ISharesInputHelperWrapper sharesInputHelperWrapper)
    : IStocksDataService
{
    public IStock GetStock(string model)
    {
        var stockModel = iStocks.FirstOrDefault(s => s.GetType().Name.Equals(model, StringComparison.OrdinalIgnoreCase));

        if (stockModel is null)
        {
            logger.LogError("No class implementing IStock could be found that matches '{Model}' (in settings).", model);
            progress.Report(new ProgressLog(MessageImportance.Bad, $"No class implementing IStock could be found that matches '{model}' (in settings)."));
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
                logger.LogWarning(exception, "Error fetching stocks data.  Retrying in {RetryInMilliseconds} milliseconds.", timeSpan.TotalMilliseconds);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"Error fetching stocks data.  Retrying in {timeSpan.TotalMilliseconds} milliseconds."));
            });
    }

    public static void ValidateUri(string uri)
    {
        if (uri is null)
            throw new ArgumentNullException(nameof(uri));
        else if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? uriResult))
            throw new ArgumentException("Invalid URI format.", nameof(uri));
        else if (!(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            throw new ArgumentException("Invalid URI scheme.", nameof(uri));
    }

    public async Task<HttpResponseMessage[]> FetchStocksDataAsync(
        AsyncRetryPolicy pollyPolicy, string stocksApiUrl, int apiDelayPerCallMilliseconds, List<Share> sharesInput)
    {
        try
        {
            ValidateUri(stocksApiUrl);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentNullException or ArgumentException)
            {
                logger.LogError(ex, "URL for stocks API is invalid: {StocksApiUrl}", stocksApiUrl);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"URL for stocks API is invalid: {stocksApiUrl}"));
            }
            throw;
        }

        List<HttpResponseMessage> httpResponseMessages = [];
        try
        {
            foreach (var symbolName in sharesInputHelperWrapper.GetDistinctSymbolsNames(sharesInput))
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
                logger.LogError(ex, "Error fetching stocks data.  Reached maximum retries.");
                progress.Report(new ProgressLog(MessageImportance.Bad, "Error fetching stocks data.  Reached maximum retries."));
            }
            else
                throw;
        }

        return [.. httpResponseMessages];
    }

    private async Task<HttpResponseMessage> FetchStockDataAsync(
        AsyncRetryPolicy pollyPolicy, string stocksApiUrl, string stockSymbol, string stockName)
    {
        HttpResponseMessage result = new();

        await pollyPolicy.ExecuteAsync(async () =>
        {
            logger.LogInformation("Sending request for stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
            progress.Report(new ProgressLog(MessageImportance.Normal, $"Sending request for stocks data: {stockSymbol} ({stockName})"));
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(stocksApiUrl, stockSymbol));

            result = await httpClient.SendAsync(httpRequestMessage).ContinueWith((task) =>
            {
                HttpResponseMessage response = task.Result;

                if (task.IsCompletedSuccessfully)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.LogInformation("Received successful response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                        progress.Report(new ProgressLog(MessageImportance.Good, $"Received successful response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                    else
                    {
                        logger.LogError("Received failure response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                        progress.Report(new ProgressLog(MessageImportance.Bad, $"Received failure response fetching stocks data: {stockSymbol} ({stockName})"));
                    }
                }
                else
                {
                    logger.LogError(task.Exception, "Failed to receive response fetching stocks data: {StockSymbol} ({StockName})", stockSymbol, stockName);
                    progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to receive response fetching stocks data: {stockSymbol} ({stockName})"));
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
            throw new ArgumentException("Failed to fetch any stocks data.", nameof(flattenedStocks));

        var allStocksFetchedSuccssfully = true;

        foreach (var stock in sharesInputHelperWrapper.GetDistinctSymbolsNames(sharesInput))
        {
            if (flattenedStocks.Any(s => s.Symbol.Equals(stock.Symbol, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogInformation("Successfully fetched stocks data for: {StockSymbol} ({StockName})", stock.Symbol, stock.StockName);
                progress.Report(new ProgressLog(MessageImportance.Good, $"Successfully fetched stocks data for: {stock.Symbol} ({stock.StockName})"));
            }
            else
            {
                allStocksFetchedSuccssfully = false;
                logger.LogError("Failed fetching stocks data for: {StockSymbol} ({StockName})", stock.Symbol, stock.StockName);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch stocks data for: {stock.Symbol} ({stock.StockName})"));
            }
        }

        return allStocksFetchedSuccssfully;
    }
}
