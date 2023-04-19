using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public interface ISharesOutputHelperWrapper
{
    List<ShareOutput> CreateSharesOutput(List<Share> sharesInput, List<FlattenedStock> flattenedStocks);
}
