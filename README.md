# What is SharesGainLossTracker?
SharesGainLossTracker is an app that creates a daily breakdown of percentage gains/losses for specified shares, typically shares of stocks you have purchased.  It can be run as a .NET console app or a .NET WFP app.

# How does SharesGainLossTracker work?
SharesGainLossTracker will create an Excel file containing a column for each stock specified, and a row for each day containing the gain/loss percentage.  

The gain/loss percentage is calculated by comparing the share purchase price, and the end-of-day adjusted closing price.  The shares you want to track, and the purchase price, should be specified in a CSV file.

Limitation: Only **one** share purchase price **per individual stock** can be tracked in an Excel output file.  As a workaround, multiple purchases of shares of the same stock can be split across separate CSV input files.

SharesGainLossTracker uses 3rd party APIs for stocks data, and currently [Marketstack](https://marketstack.com/) and [Alpha Vantage](https://www.alphavantage.co/) are supported.  They both offer free and paid tiers, and SharesGainLossTracker will work with their free tiers.  API calls on free tiers are rate limited to a certain amount of calls per milliesecond/day.

You will need to sign up to a free tier, which will give you a key to access their API.  This key needs to be inserted into the `appsettings` file.

# Setup instructions
SharesGainLossTracker was initially written as a POC console app for my dad, therefore I haven't bothered creating any GitHub Actions, workflows, or binaries to download.  As a result, to run SharesGainLossTracker, you'll need to use Visual Studio 2022 (or a compatible alternative) to compile and run the app.

1) Clone the SharesGainLossTracker repository.

2) Open the .NET solution in Visual Studio 2022 (or a compatible alternative).

3) Open the `appsettings.json` file (or `appsettings.Development.json` if running in development mode) and edit the values:

| Setting                      | Description   |
| -----------------------------|:---------------
| OpenOutputFileDirectory      | `true` or `false` sets whether or not the directory of the output file is opened upon creation.
| Enabled                      | Set to `true` or `false` to enable/disable API calls and thus Excel file creation.
| Model                        | `Marketstack` for Marketstack API, or `AlphaVantage` for Alpha Vantage API.
| OutputFilePath               | Path where Excel file is created.
| OutputFilenamePrefix         | The Excel filename will be a date/time stamp. A prefix can be specified.
| SymbolsFullPath              | Path and filename of the CSV file containing shares and purchase prices to be tracked.
| ApiUrl                       | URL of 3rd party stocks API.  Replace `<API KEY>` with your API key.
| ApiDelayPerCallMillieseconds | Delay between API calls to keep within rate limit. One API call per stock.
| OrderByDateDescending        | `true` or `false` sets whether to sort dates in Excel file in ascending or discending order.

Below are example CSV files that should be referenced in `appsettings.json` against `SymbolsFullPath`.  Delimited values are: Stock symbol, Excel column name, Share purchase price.  Note: Stock symbols can differ between Marketstack and Alpha Vantage.

Marketstack:
```
GOOGL,Alphabet Inc,89.50
AZN.XLON,Astra Zeneca plc,80.43
BP.XLON,B.P. plc,200.95
BAG.XLON,Barr (AG),537.65
CARD.XLON,Card Factory plc,140.19
```

Alpha Vantage:
```
GOOGL,Alphabet Inc,89.50
AZN.LON,Astra Zeneca plc,80.43
BP.LON,B.P. plc,200.95
BAG.LON,Barr (AG),537.65
CARD.LON,Card Factory plc,140.19
```

4) Set either `SharesGainLossTracker.ConsoleApp` or `SharesGainLossTracker.WpfApp` as the startup project.

5) Build the solution and run!


# Supporting other 3rd party stocks APIs
Support can be added by writing additional classes that implement the `SharesGainLossTracker.Core.Models.IStock` interface.
