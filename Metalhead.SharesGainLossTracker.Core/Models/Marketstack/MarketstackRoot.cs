using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models.Marketstack;

public class MarketstackRoot
{
    [JsonPropertyName("data")]
    public MarketstackData[]? Data {get; set;}
}
