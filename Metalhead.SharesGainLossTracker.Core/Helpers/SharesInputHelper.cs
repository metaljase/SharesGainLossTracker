using System;
using System.Collections.Generic;
using System.Linq;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

internal class SharesInputHelper
{
    internal static IEnumerable<Share> GetDistinctSymbolsNames(List<Share> sharesInput)
    {
        // Get distinct stock symbols from the input with the first associated stock name found (could be multiple).
        return sharesInput
            .GroupBy(s => s.Symbol, StringComparer.InvariantCultureIgnoreCase) // Group by the Symbol property
            .Select(g => g.First()) // Select the first item of each group
            .Select(s => new Share { Symbol = s.Symbol, StockName = s.StockName });
    }

    internal static void AppendPurchasePriceToStockName(List<Share> sharesInput)
    {
        foreach (var shareInput in sharesInput)
        {
            shareInput.StockName = $"{shareInput.StockName} {shareInput.PurchasePrice}";
        }
    }

    internal static void MakeStockNamesUnique(List<Share> sharesInput)
    {
        var duplicateStockNames = sharesInput.Select(s => s.StockName).GroupBy(s => s).Where(g => g.Count() > 1).Select(s => s.Key);

        foreach (var duplicateStockName in duplicateStockNames)
        {
            var duplicateCount = 0;
            foreach (var shareInput in sharesInput.Where(s => s.StockName.Equals(duplicateStockName, StringComparison.OrdinalIgnoreCase)))
            {
                shareInput.StockName = $"{shareInput.StockName} {duplicateCount += 1}";
            }
        }
    }
}
