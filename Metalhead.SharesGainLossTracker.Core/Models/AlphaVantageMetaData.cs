using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class AlphaVantageMetaData
{
    [JsonPropertyName("2. Symbol")]
    public required string Symbol { get; set; }
}
