using System;

namespace SharesGainLossTracker.Core.Models
{
    public class FlattenedStock
    {
        public FlattenedStock(DateTime date, string symbol, double adjustedClose)
        {
            Date = date.ToString("yyyy-MM-dd");
            Symbol = symbol;
            AdjustedClose = adjustedClose;
        }

        public string Date { get; set; }

        public string Symbol { get; set; }

        public double AdjustedClose { get; set; }

        public double GainLoss { get; set; }
    }
}
