using System.Text.Json.Serialization;

namespace SharesGainLossTracker.Core.Models
{
    public class AlphaVantageData
    {
        [JsonPropertyName("5. adjusted close")]
        public string AdjustedClose { get; set; }
    }
}