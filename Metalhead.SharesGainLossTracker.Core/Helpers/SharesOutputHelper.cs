using System;
using System.Collections.Generic;
using System.Linq;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

internal class SharesOutputHelper
{
    internal static List<ShareOutput> CreateSharesOutput(List<Share> sharesInput, List<FlattenedStock> flattenedStocks)
    {
        List<ShareOutput> sharesOutput = [];
        foreach (var flattenedStock in flattenedStocks)
        {
            // Get shares that match current stock symbol.  Multiple shares per stock may exist, e.g. with different purchase prices.
            var sharesForSymbol = sharesInput.Where(s => s.Symbol.Equals(flattenedStock.Symbol, StringComparison.OrdinalIgnoreCase));
            foreach (var shareForSymbol in sharesForSymbol)
            {
                sharesOutput.Add(new ShareOutput(shareForSymbol.StockName, shareForSymbol.Symbol, shareForSymbol.PurchasePrice, flattenedStock.Date, flattenedStock.Close));
            }
        }

        return sharesOutput;
    }
}
