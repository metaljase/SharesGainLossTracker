using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public class SharesOutputHelperWrapper : ISharesOutputHelperWrapper
{
    public List<ShareOutput> CreateSharesOutput(List<Share> sharesInput, List<FlattenedStock> flattenedStocks)
    {
        return SharesOutputHelper.CreateSharesOutput(sharesInput, flattenedStocks);
    }
}
