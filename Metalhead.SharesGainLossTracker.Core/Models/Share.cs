namespace Metalhead.SharesGainLossTracker.Core.Models;

public class Share
{
    public required string Symbol { get; set; }

    public required string StockName { get; set; }

    public double PurchasePrice { get; set; }
}
