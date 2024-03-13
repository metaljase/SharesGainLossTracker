using System.Data;

using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Tests;

public class MockData
{
    public static string[] CreateSharesInputCsv()
    {
        return
        [
            "MSFT, Microsoft Corp (MSFT), 287.14",
            "TSLA, Tesla Inc (TSLA), 184.77",
            "OCDO.LON, Ocado Group plc (OCDO), 522.40"
        ];
    }

    public static string[] CreateSharesInputCsvContainingInvalidLines()
    {
        return
        [
            "MSFT, Microsoft Corp (MSFT),",
            "TSLA, Tesla Inc (TSLA), 184.77",
            "OCDO.LON, 522.40",
            "",
        ];
    }

    public static List<Share> CreateSharesInput()
    {
        return
        [
            new Share { Symbol = "MSFT", StockName = "Microsoft Corp (MSFT)", PurchasePrice = 287.14 },
            new Share { Symbol = "TSLA", StockName = "Tesla Inc (TSLA)", PurchasePrice = 184.77 },
            new Share { Symbol = "OCDO.LON", StockName = "Ocado Group plc (OCDO)", PurchasePrice = 522.40 }
        ];
    }

    public static List<Share> CreateSharesInputWithDuplicateSymbols()
    {
        return
        [
            new Share { Symbol = "MSFT", StockName = "Microsoft Corp (MSFT)", PurchasePrice = 287.14 },
            new Share { Symbol = "TSLA", StockName = "Tesla Inc (TSLA)", PurchasePrice = 184.77 },
            new Share { Symbol = "TSLA", StockName = "Tesla Inc (TSLA) X", PurchasePrice = 114.11 },
            new Share { Symbol = "ocdo.lon", StockName = "ocado group plc (ocdo)", PurchasePrice = 522.41 },
            new Share { Symbol = "OCDO.LON", StockName = "Ocado Group plc (OCDO)", PurchasePrice = 501.01 },
            new Share { Symbol = "OCDO.LON", StockName = "Ocado Group plc (OCDO)", PurchasePrice = 424.23 },
            new Share { Symbol = "OCDO.LON", StockName = "OCADO GROUP PLC (OCDO)", PurchasePrice = 600.31 },
        ];
    }

    public static List<Share> GetDistinctSymbolsNamesFromSharesInputWithDuplicateSymbols()
    {
        return
        [
            new Share { Symbol = "MSFT", StockName = "Microsoft Corp (MSFT)", PurchasePrice = 287.14 },
            new Share { Symbol = "TSLA", StockName = "Tesla Inc (TSLA)", PurchasePrice = 184.77 },
            new Share { Symbol = "ocdo.lon", StockName = "ocado group plc (ocdo)", PurchasePrice = 522.41 },
            new Share { Symbol = "OCDO.LON", StockName = "Ocado Group plc (OCDO)", PurchasePrice = 501.01 },
        ];
    }

    public static List<Share> CreateSharesInputWithDuplicateSymbolsAndAppendPurchasePrice()
    {
        var sharesInput = CreateSharesInputWithDuplicateSymbols();
        var sharesInputHelperWrapper = new SharesInputHelperWrapper();
        sharesInputHelperWrapper.AppendPurchasePriceToStockName(sharesInput);

        return sharesInput;
    }

    public static List<FlattenedStock> CreateFlattenedStock()
    {
        return
        [
            new FlattenedStock(new DateTime(2023, 3, 29, 23, 59, 48), "MSFT", 279.51),
            new FlattenedStock(new DateTime(2023, 3, 29, 23, 59, 59), "TSLA", 189.53),
            new FlattenedStock(new DateTime(2023, 3, 30, 0, 0, 10), "OCDO.LON", 520.65)
        ];
    }

    public static List<ShareOutput> CreateSharesOutput()
    {
        return [.. new List<ShareOutput>
        {
            new("Microsoft Corp (MSFT) 287.14", "MSFT", 287.14, new DateTime(2023, 3, 29, 23, 59, 48), 279.51),
            new("Tesla Inc (TSLA) 184.77", "TSLA", 184.77, new DateTime(2023, 3, 29, 23, 59, 59), 189.53),
            new("Tesla Inc (TSLA) X 114.11", "TSLA", 114.11, new DateTime(2023, 3, 29, 23, 59, 59), 189.53),
            new("ocado group plc (ocdo) 522.41", "ocdo.lon", 522.41, new DateTime(2023, 3, 30, 0, 0, 10), 520.65),
            new("Ocado Group plc (OCDO) 501.01", "OCDO.LON", 501.01, new DateTime(2023, 3, 30, 0, 0, 10), 520.65),
            new("Ocado Group plc (OCDO) 424.23", "OCDO.LON", 424.23, new DateTime(2023, 3, 30, 0, 0, 10), 520.65),
            new("OCADO GROUP PLC (OCDO) 600.31", "OCDO.LON", 600.31, new DateTime(2023, 3, 30, 0, 0, 10), 520.65),
        }.OrderByDescending(o => o.Date)];
    }

    public static DataTable CreateGainLossDataTable()
    {
        var sharesOutputDataTableHelperWrapper = new SharesOutputDataTableHelperWrapper();
        return sharesOutputDataTableHelperWrapper.CreateGainLossPivotedDataTable(CreateSharesOutput(), "Gain/Loss");
    }

    public static DataTable CreateCloseDataTable(bool createAdjustedClose)
    {
        var sharesOutputDataTableHelperWrapper = new SharesOutputDataTableHelperWrapper();
        var dataTableName = createAdjustedClose ? "Adjusted Close" : "Close";
        return sharesOutputDataTableHelperWrapper.CreateClosePivotedDataTable(CreateSharesOutput(), dataTableName);
    }

    public static List<DataTable> CreateGainLossDataTableAndCloseDataTable()
    {
        return
        [
            CreateCloseDataTable(true),
            CreateGainLossDataTable()
        ];
    }
}
