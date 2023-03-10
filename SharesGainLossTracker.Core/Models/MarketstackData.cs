using System.Text.Json.Serialization;

namespace SharesGainLossTracker.Core.Models
{
    public class MarketstackData
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("adj_close")]
        public double AdjustedClose { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }
    }
}