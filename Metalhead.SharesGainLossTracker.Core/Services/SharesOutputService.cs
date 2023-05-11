using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesOutputService : ISharesOutputService
{
    private ILogger<SharesOutputService> Log { get; }
    private IProgress<ProgressLog> Progress { get; }
    private IStocksDataService StocksDataService { get; }
    private ISharesInputLoader ShareInputLoader { get; }
    private ISharesInputHelperWrapper SharesInputHelper { get; }
    private ISharesOutputHelperWrapper SharesOutputHelper { get; }

    public SharesOutputService(ILogger<SharesOutputService> log, IProgress<ProgressLog> progress, IStocksDataService stocksDataService, ISharesInputLoader shareInputLoader, ISharesInputHelperWrapper sharesInputHelperWrapper, ISharesOutputHelperWrapper sharesOutputHelperWrapper)
    {
        Log = log;
        Progress = progress;
        StocksDataService = stocksDataService;
        ShareInputLoader = shareInputLoader;
        SharesInputHelper = sharesInputHelperWrapper;
        SharesOutputHelper = sharesOutputHelperWrapper;
    }

    public async Task<List<ShareOutput>> CreateSharesOutputAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, int apiDelayPerCallMillieseconds, bool orderByDateDescending, bool appendPriceToStockName)
    {
        Log.LogInformation("Processing input file: {SharesInputFileFullPath}", sharesInputFileFullPath);
        Progress.Report(new ProgressLog(MessageImportance.Normal, $"Processing input file: {sharesInputFileFullPath}"));

        IStock stocks = StocksDataService.GetStock(model);
        var sharesInput = ShareInputLoader.CreateSharesInput(sharesInputFileFullPath);
        var pollyPolicy = StocksDataService.GetRetryPolicy(apiDelayPerCallMillieseconds);
        var httpResponseMessages = await StocksDataService.FetchStocksDataAsync(pollyPolicy, stocksApiUrl, apiDelayPerCallMillieseconds, sharesInput);

        // Map the data from the API using the appropriate model.
        var flattenedStocks = await stocks.GetStocksDataAsync(httpResponseMessages);

        // Validate data was returned from the API and mapped.
        try
        {
            StocksDataService.IsExpectedStocksDataMapped(flattenedStocks, sharesInput);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentNullException or ArgumentException)
            {
                Log.LogError(ex, "Failed to fetch any stocks data for input file: ", sharesInputFileFullPath);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Failed to fetch any stocks data for input file: {sharesInputFileFullPath}", false));
                return null;
            }
            throw;
        }

        // Append share purchase price to stock name, to avoid ambiguity in Excel file when multiple shares of the same stock exist.
        if (appendPriceToStockName)
        {
            SharesInputHelper.AppendPurchasePriceToStockName(sharesInput);
        }

        // Make duplicate stock names unique to avoid ambiguity when pivoting data.
        SharesInputHelper.MakeStockNamesUnique(sharesInput);

        List<ShareOutput> sharesOutput = SharesOutputHelper.CreateSharesOutput(sharesInput, flattenedStocks);

        // Order data by date.
        return orderByDateDescending ? sharesOutput.OrderByDescending(o => o.Date).ToList() : sharesOutput.OrderBy(o => o.Date).ToList();
    }
}
