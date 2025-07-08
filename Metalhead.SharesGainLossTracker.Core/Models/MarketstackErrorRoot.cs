using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class MarketstackErrorRoot
{
    [JsonPropertyName("error")]
    public required MarketstackError? Error { get; set; }
}