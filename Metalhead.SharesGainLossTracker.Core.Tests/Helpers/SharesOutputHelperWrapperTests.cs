using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Xunit;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Helpers;

public class SharesOutputHelperWrapperTests
{
    [Fact]
    public void CreateSharesOutput_ReturnsSharesOutput_GivenSharesInputAndFlattenStock()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInputWithDuplicateSymbolsAndAppendPurchasePrice();
        var flattenedStocks = new List<FlattenedStock>
        {
            new(new DateTime(2023, 3, 29, 23, 59, 48), "MSFT", 279.51),
            new(new DateTime(2023, 3, 29, 23, 59, 59), "TSLA", 189.53),
            new(new DateTime(2023, 3, 30, 0, 0, 10), "OCDO.LON", 520.65)
        };
        var sut = new SharesOutputHelperWrapper();

        // Act
        var result = sut.CreateSharesOutput(sharesInput, flattenedStocks);

        // Assert
        Assert.NotNull(result);
        var orderedResults = result.OrderBy(s => s.StockName).ToArray();

        Assert.Equal("MSFT", orderedResults[0].Symbol);
        Assert.Equal("Microsoft Corp (MSFT) 287.14", orderedResults[0].StockName);
        Assert.Equal(287.14, orderedResults[0].PurchasePrice);
        Assert.Equal(279.51, orderedResults[0].Close);
        Assert.Equal("2023-03-29", orderedResults[0].Date);

        Assert.Equal("OCDO.LON", orderedResults[1].Symbol);
        Assert.Equal("Ocado Group plc (OCDO) 424.23", orderedResults[1].StockName);
        Assert.Equal(424.23, orderedResults[1].PurchasePrice);
        Assert.Equal(520.65, orderedResults[1].Close);
        Assert.Equal("2023-03-30", orderedResults[1].Date);

        Assert.Equal("OCDO.LON", orderedResults[2].Symbol);
        Assert.Equal("Ocado Group plc (OCDO) 501.01", orderedResults[2].StockName);
        Assert.Equal(501.01, orderedResults[2].PurchasePrice);
        Assert.Equal(520.65, orderedResults[2].Close);
        Assert.Equal("2023-03-30", orderedResults[2].Date);

        Assert.Equal("ocdo.lon", orderedResults[3].Symbol);
        Assert.Equal("ocado group plc (ocdo) 522.41", orderedResults[3].StockName);
        Assert.Equal(522.41, orderedResults[3].PurchasePrice);
        Assert.Equal(520.65, orderedResults[3].Close);
        Assert.Equal("2023-03-30", orderedResults[3].Date);

        Assert.Equal("OCDO.LON", orderedResults[4].Symbol);
        Assert.Equal("OCADO GROUP PLC (OCDO) 600.31", orderedResults[4].StockName);
        Assert.Equal(600.31, orderedResults[4].PurchasePrice);
        Assert.Equal(520.65, orderedResults[4].Close);
        Assert.Equal("2023-03-30", orderedResults[4].Date);

        Assert.Equal("TSLA", orderedResults[5].Symbol);
        Assert.Equal("Tesla Inc (TSLA) 184.77", orderedResults[5].StockName);
        Assert.Equal(184.77, orderedResults[5].PurchasePrice);
        Assert.Equal(189.53, orderedResults[5].Close);
        Assert.Equal("2023-03-29", orderedResults[5].Date);

        Assert.Equal("TSLA", orderedResults[6].Symbol);
        Assert.Equal("Tesla Inc (TSLA) X 114.11", orderedResults[6].StockName);
        Assert.Equal(114.11, orderedResults[6].PurchasePrice);
        Assert.Equal(189.53, orderedResults[6].Close);
        Assert.Equal("2023-03-29", orderedResults[6].Date);
    }
}
