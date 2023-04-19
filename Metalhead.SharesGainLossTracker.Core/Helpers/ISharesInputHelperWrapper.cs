using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public interface ISharesInputHelperWrapper
{
    IEnumerable<Share> GetDistinctSymbolsNames(List<Share> sharesInput);
    void AppendPurchasePriceToStockName(List<Share> sharesInput);
    void MakeStockNamesUnique(List<Share> sharesInput);
}
