using System.Collections.Generic;
using System.Data;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public class SharesOutputDataTableHelperWrapper : ISharesOutputDataTableHelperWrapper
{
    public DataTable CreateAdjustedClosePivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
    {
        return SharesOutputDataTableHelper.CreateAdjustedClosePivotedDataTable(sharesOutput, dataTableName);
    }

    public DataTable CreateGainLossPivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
    {
        return SharesOutputDataTableHelper.CreateGainLossPivotedDataTable(sharesOutput, dataTableName);
    }
}
