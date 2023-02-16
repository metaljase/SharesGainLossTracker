using System.Text.Json.Serialization;

namespace SharesGainLossTracker.Core.Models
{
    public class MarketstackRoot
    {
        [JsonPropertyName("data")]
        public MarketstackData[] Data {get; set;}
    }
}
