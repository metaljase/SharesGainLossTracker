using System.Text.Json.Serialization;

namespace SharesGainLossTracker.Core.Models
{
    public class AlphaVantageMetaData
    {
        [JsonPropertyName("2. Symbol")]
        public string Symbol { get; set; }
    }
}
