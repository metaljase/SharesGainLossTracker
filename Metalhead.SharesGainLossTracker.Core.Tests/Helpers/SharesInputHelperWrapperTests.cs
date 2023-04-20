using Metalhead.SharesGainLossTracker.Core.Helpers;
using Xunit;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Helpers;

public class SharesInputHelperWrapperTests
{
    [Fact]
    public void GetDistinctSymbolsNames_ReturnsDistinctSymbolsAndNames_GivenDuplicateSymbols()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInputWithDuplicateSymbols();
        var sut = new SharesInputHelperWrapper();

        // Act
        var result = sut.GetDistinctSymbolsNames(sharesInput);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.True(result.All(s => sharesInput.Any(si => si.Symbol == s.Symbol && si.StockName == s.StockName)));
    }

    [Fact]
    public void AppendPurchasePriceToStockName_AppendsPriceToStockName_GivenSharesInputWithoutPriceAppended()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInputWithDuplicateSymbols();
        var sut = new SharesInputHelperWrapper();

        // Act
        sut.AppendPurchasePriceToStockName(sharesInput);

        // Assert
        Assert.Equal("Microsoft Corp (MSFT) 287.14", sharesInput[0].StockName);
        Assert.Equal("Tesla Inc (TSLA) 184.77", sharesInput[1].StockName);
        Assert.Equal("Tesla Inc (TSLA) X 114.11", sharesInput[2].StockName);
        Assert.Equal("ocado group plc (ocdo) 522.41", sharesInput[3].StockName);
        Assert.Equal("Ocado Group plc (OCDO) 501.01", sharesInput[4].StockName);
        Assert.Equal("Ocado Group plc (OCDO) 424.23", sharesInput[5].StockName);
        Assert.Equal("OCADO GROUP PLC (OCDO) 600.31", sharesInput[6].StockName);
    }

    [Fact]
    public void MakeStockNamesUnique_AppendsSequentialNumbersToDuplicates_GivenFourDuplicateStockNames()
    {
        // Arrange
        var sharesInput = MockData.CreateSharesInputWithDuplicateSymbols();
        var sut = new SharesInputHelperWrapper();

        // Act
        sut.MakeStockNamesUnique(sharesInput);

        // Assert            
        Assert.Equal("MSFT", sharesInput[0].Symbol);
        Assert.Equal("Microsoft Corp (MSFT)", sharesInput[0].StockName);
        Assert.Equal("TSLA", sharesInput[1].Symbol);
        Assert.Equal("Tesla Inc (TSLA)", sharesInput[1].StockName);
        Assert.Equal("TSLA", sharesInput[2].Symbol);
        Assert.Equal("Tesla Inc (TSLA) X", sharesInput[2].StockName);
        Assert.Equal("ocdo.lon", sharesInput[3].Symbol);
        Assert.Equal("ocado group plc (ocdo) 1", sharesInput[3].StockName);
        Assert.Equal("OCDO.LON", sharesInput[4].Symbol);
        Assert.Equal("Ocado Group plc (OCDO) 2", sharesInput[4].StockName);
        Assert.Equal("OCDO.LON", sharesInput[5].Symbol);
        Assert.Equal("Ocado Group plc (OCDO) 3", sharesInput[5].StockName);
        Assert.Equal("OCDO.LON", sharesInput[6].Symbol);
        Assert.Equal("OCADO GROUP PLC (OCDO) 4", sharesInput[6].StockName);
    }
}
