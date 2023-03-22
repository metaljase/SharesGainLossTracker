using System;

namespace Metalhead.SharesGainLossTracker.Core.Models
{
    public class FlattenedStock
    {
        public FlattenedStock(DateTime date, string symbol, double adjustedClose)
        {
            Date = date;
            Symbol = symbol;
            AdjustedClose = adjustedClose;
        }

        public DateTime Date { get; set; }

        public string Symbol { get; set; }

        public double AdjustedClose { get; set; }
    }
}
