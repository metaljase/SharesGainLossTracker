using System.Collections.Generic;
using System.Data;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public class SharesOutputDataTableHelperWrapper : ISharesOutputDataTableHelperWrapper
{
    public DataTable CreateClosePivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
    {
        return SharesOutputDataTableHelper.CreateClosePivotedDataTable(sharesOutput, dataTableName);
    }

    public DataTable CreateGainLossPivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
    {
        return SharesOutputDataTableHelper.CreateGainLossPivotedDataTable(sharesOutput, dataTableName);
    }
}
