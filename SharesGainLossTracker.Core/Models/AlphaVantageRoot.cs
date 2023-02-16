using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharesGainLossTracker.Core.Models
{
    public class AlphaVantageRoot
    {
        [JsonPropertyName("Meta Data")]
        public AlphaVantageMetaData MetaData { get; set; }

        [JsonPropertyName("Time Series (Daily)")]
        public Dictionary<string, AlphaVantageData> Data {get; set;}
    }
}
