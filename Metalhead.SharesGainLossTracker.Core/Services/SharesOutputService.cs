using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesOutputService(
    ILogger<SharesOutputService> logger,
    IProgress<ProgressLog> progress,
    IStocksDataService stocksDataService,
    ISharesInputLoader shareInputLoader,
    ISharesInputHelperWrapper sharesInputHelperWrapper,
    ISharesOutputHelperWrapper sharesOutputHelperWrapper)
    : ISharesOutputService
{
    public async Task<List<ShareOutput>?> CreateSharesOutputAsync(
        string model,
        string sharesInputFileFullPath,
        string stocksApiUrl,
        bool endpointReturnsAdjustedClose,
        int apiDelayPerCallMilliseconds,
        bool orderByDateDescending,
        bool appendPriceToStockName)
    {
        logger.LogInformation("Processing input file: {SharesInputFileFullPath}", sharesInputFileFullPath);
        progress.Report(new ProgressLog(MessageImportance.Normal, $"Processing input file: {sharesInputFileFullPath}"));

        IStock stocks = stocksDataService.GetStock(model);
        var sharesInput = shareInputLoader.CreateSharesInput(sharesInputFileFullPath);
        var pollyPolicy = stocksDataService.GetRetryPolicy(apiDelayPerCallMilliseconds);
        var httpResponseMessages = await stocksDataService.FetchStocksDataAsync(pollyPolicy, stocksApiUrl, apiDelayPerCallMilliseconds, sharesInput);

        // Map the data from the API using the appropriate model.
        var flattenedStocks = await stocks.GetStocksDataAsync(httpResponseMessages, endpointReturnsAdjustedClose);

        // Validate data was returned from the API and mapped.
        try
        {
            stocksDataService.IsExpectedStocksDataMapped(flattenedStocks, sharesInput);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentNullException or ArgumentException)
            {
                logger.LogError(ex, "Failed to fetch any stocks data for input file: {SharesInputFileFullPath}", sharesInputFileFullPath);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}", false));
                return null;
            }
            throw;
        }

        // Append share purchase price to stock name, to avoid ambiguity in Excel file when multiple shares of the same stock exist.
        if (appendPriceToStockName)
            sharesInputHelperWrapper.AppendPurchasePriceToStockName(sharesInput);

        // Make duplicate stock names unique to avoid ambiguity when pivoting data.
        sharesInputHelperWrapper.MakeStockNamesUnique(sharesInput);

        List<ShareOutput> sharesOutput = sharesOutputHelperWrapper.CreateSharesOutput(sharesInput, flattenedStocks);

        // Order data by date.
        return orderByDateDescending
            ? [.. sharesOutput.OrderByDescending(o => o.Date)]
            : [.. sharesOutput.OrderBy(o => o.Date)];
    }
}
