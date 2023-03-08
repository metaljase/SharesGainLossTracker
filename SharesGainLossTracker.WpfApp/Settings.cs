using System.Collections.Generic;

namespace SharesGainLossTracker.WpfApp
{
    public class Settings
    {
        // Current can be used as a static instance, so config is accessible everywhere.
        public static Settings Current;

        public Settings()
        {
            Current = this;
        }
        
        public bool OpenOutputFileDirectory { get; set; }
        public bool SuffixDateToOutputFilePath { get; set; }
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
        public int ApiDelayPerCallMilleseconds { get; set; }
        public bool OrderByDateDescending { get; set; }
    }
}