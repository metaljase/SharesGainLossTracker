using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class MarketstackError
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("context")]
    public object? Context { get; set; }
}