using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models
{
    public class AlphaVantageData
    {
        [JsonPropertyName("5. adjusted close")]
        public string AdjustedClose { get; set; }
    }
}