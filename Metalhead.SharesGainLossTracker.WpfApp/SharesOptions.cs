using System.Collections.Generic;

namespace Metalhead.SharesGainLossTracker.WpfApp;

public class SharesOptions
{
    public const string SharesSettings = "SharesGainLossTracker";

    public bool? OpenOutputFileDirectory { get; set; }
    public bool? SuffixDateToOutputFilePath { get; set; }
    public bool? AppendPurchasePriceToStockNameColumn { get; set; }
    public required List<SharesGroup> Groups { get; set; }
}

public class SharesGroup
{
    public bool Enabled { get; set; }
    public required string Model { get; set; }
    public required string OutputFilePath { get; set; }
    public string OutputFilenamePrefix { get; set; } = string.Empty;
    public required string SymbolsFullPath { get; set; }
    public required string ApiUrl { get; set; }
    public bool EndpointReturnsAdjustedClose { get; set; }

    private int _apiDelayPerCallMilliseconds;

    public int ApiDelayPerCallMilleseconds
    {
        get => _apiDelayPerCallMilliseconds;
        set => _apiDelayPerCallMilliseconds = value;
    }

    public int ApiDelayPerCallMilliseconds
    {
        get => _apiDelayPerCallMilliseconds;
        set => _apiDelayPerCallMilliseconds = value;
    }

    public bool OrderByDateDescending { get; set; }
}