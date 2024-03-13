using System.Collections.Generic;

namespace Metalhead.SharesGainLossTracker.WpfApp
{
    public class SharesOptions
    {
        public const string SharesSettings = "sharesSettings";

        public bool? OpenOutputFileDirectory { get; set; }
        public bool? SuffixDateToOutputFilePath { get; set; }
        public bool? AppendPurchasePriceToStockNameColumn { get; set; }
        public List<SharesGroup> Groups { get; set; }
    }

    public class SharesGroup
    {
        public bool Enabled { get; set; }
        public string Model { get; set; }
        public string OutputFilePath { get; set; }
        public string OutputFilenamePrefix { get; set; }
        public string SymbolsFullPath { get; set; }
        public string ApiUrl { get; set; }
        public bool EndpointReturnsAdjustedClose { get; set; }
        public int ApiDelayPerCallMilleseconds { get; set; }        
        public bool OrderByDateDescending { get; set; }
    }
}