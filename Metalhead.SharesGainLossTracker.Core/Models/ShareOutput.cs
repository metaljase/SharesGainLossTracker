using System;

namespace Metalhead.SharesGainLossTracker.Core.Models
{
    public class ShareOutput
    {
        public string StockName { get; set; }
        public string Symbol { get; set; }
        public double PurchasePrice { get; set; }
        public string Date { get; }
        public double? Close { get; set; }
        public double? GainLoss { get; }


        public ShareOutput(string stockName, string symbol, double purchasePrice, DateTime date, double close)
        {
            StockName = stockName;
            Symbol = symbol;
            PurchasePrice = purchasePrice;
            Date = date.ToString("yyyy-MM-dd");
            Close = close;

            if (purchasePrice == 0)
            {
                GainLoss = (close > 0) ? 100 : 0;
            }
            else
            {
                GainLoss = Math.Round((close - purchasePrice) / purchasePrice * 100.0, 1);
            }
        }
    }
}
