using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models
{
    public class AlphaVantageData
    {
        [JsonPropertyName("4. close")]
        public string Close { get; set; }

        [JsonPropertyName("5. adjusted close")]        
        public string AdjustedClose { get; set; }
    }
}