using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class MarketstackRoot
{
    [JsonPropertyName("data")]
    public required MarketstackData[] Data {get; set;}
}
