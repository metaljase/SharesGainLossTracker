using System.Collections.Generic;
using System.Data;
using System.Linq;

using Metalhead.Extensions;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Helpers;

internal class SharesOutputDataTableHelper
{
    internal static DataTable CreateGainLossPivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
    {
        DataTable pivotDataTable = new();

        if (sharesOutput.Count > 0)
        {
            // Pivot stocks so date rows are grouped.
            pivotDataTable = sharesOutput.ToPivotedDataTable(
                item => item.StockName,
                item => item.Date,
                items => items.Any() ? items.Single().GainLoss : null);
        }

        pivotDataTable.TableName = dataTableName;
        return pivotDataTable;
    }

    internal static DataTable CreateClosePivotedDataTable(List<ShareOutput> sharesOutput, string dataTableName)
    {
        DataTable pivotDataTable = new();

        if (sharesOutput.Count > 0)
        {
            // Pivot stocks so date rows are grouped.
            pivotDataTable = sharesOutput.ToPivotedDataTable(
                item => item.StockName,
                item => item.Date,
                items => items.Any() ? items.Single().Close : null);
        }

        pivotDataTable.TableName = dataTableName;
        return pivotDataTable;
    }
}
