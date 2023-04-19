using System;

namespace Metalhead.SharesGainLossTracker.Core.Models
{
    public class ShareOutput
    {
        public string StockName { get; set; }
        public string Symbol { get; set; }
        public double PurchasePrice { get; set; }
        public string Date { get; }
        public double? AdjustedClose { get; set; }
        public double? GainLoss { get; }


        public ShareOutput(string stockName, string symbol, double purchasePrice, DateTime date, double adjustedClose)
        {
            StockName = stockName;
            Symbol = symbol;
            PurchasePrice = purchasePrice;
            Date = date.ToString("yyyy-MM-dd");
            AdjustedClose = adjustedClose;

            if (purchasePrice == 0)
            {
                GainLoss = (adjustedClose > 0) ? 100 : 0;
            }
            else
            {
                GainLoss = Math.Round((adjustedClose - purchasePrice) / purchasePrice * 100.0, 1);
            }
        }
    }
}
