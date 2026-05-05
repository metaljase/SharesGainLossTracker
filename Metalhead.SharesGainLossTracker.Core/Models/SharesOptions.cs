using System.Collections.Generic;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class SharesOptions
{
    public const string SectionName = "SharesGainLossTracker";

    public bool OpenOutputFileDirectory { get; set; } = true;
    public bool SuffixDateToOutputFilePath { get; set; } = true;
    public bool AppendPurchasePriceToStockNameColumn { get; set; } = true;
    public List<SharesGroup> Groups { get; set; } = [];
}

public class SharesGroup
{
    public bool Enabled { get; set; }
    public string Model { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string OutputFilenamePrefix { get; set; } = string.Empty;
    public string SymbolsFullPath { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public bool EndpointReturnsAdjustedClose { get; set; }

    public int ApiDelayPerCallMilleseconds
    {
        get => ApiDelayPerCallMilliseconds;
        set => ApiDelayPerCallMilliseconds = value;
    }

    public int ApiDelayPerCallMilliseconds { get; set; }
    public bool OrderByDateDescending { get; set; }
}