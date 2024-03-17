using System.Collections.Generic;

namespace Metalhead.SharesGainLossTracker.ConsoleApp;

public class SharesOptions
{
    public const string SharesSettings = "SharesGainLossTracker";

    public bool? OpenOutputFileDirectory { get; set; }
    public bool? SuffixDateToOutputFilePath { get; set; }
    public bool? AppendPurchasePriceToStockNameColumn { get; set; }
    public List<SharesGroup>? Groups { get; set; }
}

public class SharesGroup
{
    public bool Enabled { get; set; }
    public string? Model { get; set; } = null;
    public string? OutputFilePath { get; set; } = null;
    public string OutputFilenamePrefix { get; set; } = string.Empty;
    public string? SymbolsFullPath { get; set; } = null;
    public string? ApiUrl { get; set; } = null;
    public bool EndpointReturnsAdjustedClose { get; set; }
    public int ApiDelayPerCallMilleseconds { get; set; }        
    public bool OrderByDateDescending { get; set; }
}