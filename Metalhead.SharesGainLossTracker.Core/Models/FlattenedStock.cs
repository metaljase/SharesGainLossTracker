using System;

namespace Metalhead.SharesGainLossTracker.Core.Models;

public class FlattenedStock(DateTime date, string symbol, double close)
{
    public DateTime Date { get; set; } = date;

    public string Symbol { get; set; } = symbol;

    public double Close { get; set; } = close;
}
