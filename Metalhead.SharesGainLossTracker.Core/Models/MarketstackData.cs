﻿using System.Text.Json.Serialization;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class MarketstackData
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; set; }

    [JsonPropertyName("close")]
    public double Close { get; set; }

    [JsonPropertyName("adj_close")]
    public double AdjustedClose { get; set; }

    [JsonPropertyName("date")]
    public required string Date { get; set; }
}