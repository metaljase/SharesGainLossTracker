using System.Collections.Generic;
using System.Data;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

public interface ISharesOutputDataTableHelperWrapper
{
    DataTable CreateGainLossPivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName);
    DataTable CreateAdjustedClosePivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName);
}
