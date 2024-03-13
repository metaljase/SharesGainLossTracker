using Metalhead.SharesGainLossTracker.Core.Helpers;
using Xunit;

namespace Metalhead.SharesGainLossTracker.Core.Tests.Helpers;

public class SharesOutputDataTableHelperWrapperTests
{
    [Fact]
    public void CreateGainLossPivotedDataTable_ReturnsDataTableWithPivotedGainsLoss_GivenValidShareOutput()
    {
        // Arrange
        var sharesOutput = MockData.CreateSharesOutput();
        var dataTableName = "Gain/Loss";
        var sut = new SharesOutputDataTableHelperWrapper();

        // Act
        var result = sut.CreateGainLossPivotedDataTable(sharesOutput, dataTableName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataTableName, result.TableName);
        Assert.Equal(2, result.Rows.Count);

        Assert.Equal("Date", result.Columns[0].ColumnName = "Date");
        Assert.Equal("Microsoft Corp (MSFT) 287.14", result.Columns[1].ColumnName = "Microsoft Corp (MSFT) 287.14");
        Assert.Equal("Ocado Group plc (OCDO) 424.23", result.Columns[2].ColumnName = "Ocado Group plc (OCDO) 424.23");
        Assert.Equal("Ocado Group plc (OCDO) 501.01", result.Columns[3].ColumnName = "Ocado Group plc (OCDO) 501.01");
        Assert.Equal("ocado group plc (ocdo) 522.41", result.Columns[4].ColumnName = "ocado group plc (ocdo) 522.41");
        Assert.Equal("OCADO GROUP PLC (OCDO) 600.31", result.Columns[5].ColumnName = "OCADO GROUP PLC (OCDO) 600.31");
        Assert.Equal("Tesla Inc (TSLA) 184.77", result.Columns[6].ColumnName = "Tesla Inc (TSLA) 184.77");
        Assert.Equal("Tesla Inc (TSLA) X 114.11", result.Columns[7].ColumnName = "Tesla Inc (TSLA) X 114.11");

        Assert.Equal(new DateTime(2023, 3, 30).ToString("yyyy-MM-dd"), result.Rows[0]["Date"]);
        Assert.IsType<DBNull>(result.Rows[0]["Microsoft Corp (MSFT) 287.14"]);
        Assert.Equal(22.7, result.Rows[0]["Ocado Group plc (OCDO) 424.23"]);
        Assert.Equal(3.9, result.Rows[0]["Ocado Group plc (OCDO) 501.01"]);
        Assert.Equal(-0.3, result.Rows[0]["ocado group plc (ocdo) 522.41"]);
        Assert.Equal(-13.3, result.Rows[0]["OCADO GROUP PLC (OCDO) 600.31"]);
        Assert.IsType<DBNull>(result.Rows[0]["Tesla Inc (TSLA) 184.77"]);
        Assert.IsType<DBNull>(result.Rows[0]["Tesla Inc (TSLA) X 114.11"]);

        Assert.Equal(new DateTime(2023, 3, 29).ToString("yyyy-MM-dd"), result.Rows[1]["Date"]);
        Assert.Equal(-2.7, result.Rows[1]["Microsoft Corp (MSFT) 287.14"]);
        Assert.IsType<DBNull>(result.Rows[1]["Ocado Group plc (OCDO) 424.23"]);
        Assert.IsType<DBNull>(result.Rows[1]["Ocado Group plc (OCDO) 501.01"]);
        Assert.IsType<DBNull>(result.Rows[1]["ocado group plc (ocdo) 522.41"]);
        Assert.IsType<DBNull>(result.Rows[1]["OCADO GROUP PLC (OCDO) 600.31"]);
        Assert.Equal(2.6, result.Rows[1]["Tesla Inc (TSLA) 184.77"]);
        Assert.Equal(66.1, result.Rows[1]["Tesla Inc (TSLA) X 114.11"]);
    }

    [Theory]
    [InlineData("Close")]
    [InlineData("Adjusted Close")]
    public void CreateClosePivotedDataTable_ReturnsDataTableWithPivotedClose_GivenValidShareOutput(string dataTableName)
    {
        // Arrange
        var sharesOutput = MockData.CreateSharesOutput();
        var sut = new SharesOutputDataTableHelperWrapper();

        // Act
        var result = sut.CreateClosePivotedDataTable(sharesOutput, dataTableName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataTableName, result.TableName);
        Assert.Equal(2, result.Rows.Count);

        Assert.Equal("Date", result.Columns[0].ColumnName = "Date");
        Assert.Equal("Microsoft Corp (MSFT) 287.14", result.Columns[1].ColumnName = "Microsoft Corp (MSFT) 287.14");
        Assert.Equal("Ocado Group plc (OCDO) 424.23", result.Columns[2].ColumnName = "Ocado Group plc (OCDO) 424.23");
        Assert.Equal("Ocado Group plc (OCDO) 501.01", result.Columns[3].ColumnName = "Ocado Group plc (OCDO) 501.01");
        Assert.Equal("ocado group plc (ocdo) 522.41", result.Columns[4].ColumnName = "ocado group plc (ocdo) 522.41");
        Assert.Equal("OCADO GROUP PLC (OCDO) 600.31", result.Columns[5].ColumnName = "OCADO GROUP PLC (OCDO) 600.31");
        Assert.Equal("Tesla Inc (TSLA) 184.77", result.Columns[6].ColumnName = "Tesla Inc (TSLA) 184.77");
        Assert.Equal("Tesla Inc (TSLA) X 114.11", result.Columns[7].ColumnName = "Tesla Inc (TSLA) X 114.11");

        Assert.Equal(new DateTime(2023, 3, 30).ToString("yyyy-MM-dd"), result.Rows[0]["Date"]);
        Assert.IsType<DBNull>(result.Rows[0]["Microsoft Corp (MSFT) 287.14"]);
        Assert.Equal(520.65, result.Rows[0]["Ocado Group plc (OCDO) 424.23"]);
        Assert.Equal(520.65, result.Rows[0]["Ocado Group plc (OCDO) 501.01"]);
        Assert.Equal(520.65, result.Rows[0]["ocado group plc (ocdo) 522.41"]);
        Assert.Equal(520.65, result.Rows[0]["OCADO GROUP PLC (OCDO) 600.31"]);
        Assert.IsType<DBNull>(result.Rows[0]["Tesla Inc (TSLA) 184.77"]);
        Assert.IsType<DBNull>(result.Rows[0]["Tesla Inc (TSLA) X 114.11"]);

        Assert.Equal(new DateTime(2023, 3, 29).ToString("yyyy-MM-dd"), result.Rows[1]["Date"]);
        Assert.Equal(279.51, result.Rows[1]["Microsoft Corp (MSFT) 287.14"]);
        Assert.IsType<DBNull>(result.Rows[1]["Ocado Group plc (OCDO) 424.23"]);
        Assert.IsType<DBNull>(result.Rows[1]["Ocado Group plc (OCDO) 501.01"]);
        Assert.IsType<DBNull>(result.Rows[1]["ocado group plc (ocdo) 522.41"]);
        Assert.IsType<DBNull>(result.Rows[1]["OCADO GROUP PLC (OCDO) 600.31"]);
        Assert.Equal(189.53, result.Rows[1]["Tesla Inc (TSLA) 184.77"]);
        Assert.Equal(189.53, result.Rows[1]["Tesla Inc (TSLA) X 114.11"]);
    }
}
