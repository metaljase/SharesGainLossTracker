namespace SharesGainLossTracker.Core.Models
{
    public class FlattenedStock
    {
        public string Date { get; set; }

        public string Symbol { get; set; }

        public double AdjustedClose { get; set; }

        public double GainLoss { get; set; }
    }
}
