using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public class SharesInputHelperWrapper : ISharesInputHelperWrapper
{
    public void AppendPurchasePriceToStockName(List<Share> sharesInput)
    {
        SharesInputHelper.AppendPurchasePriceToStockName(sharesInput);
    }

    public IEnumerable<Share> GetDistinctSymbolsNames(List<Share> sharesInput)
    {
        return SharesInputHelper.GetDistinctSymbolsNames(sharesInput);
    }

    public void MakeStockNamesUnique(List<Share> sharesInput)
    {
        SharesInputHelper.MakeStockNamesUnique(sharesInput);
    }
}
