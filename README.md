# What is SharesGainLossTracker?
SharesGainLossTracker is an app that creates a daily breakdown of percentage gains/losses and close or adjusted close prices for specified shares, typically shares of stocks you have purchased.  It can be run as a .NET console app or a .NET WPF app.

# How does SharesGainLossTracker work?
SharesGainLossTracker will create an Excel Workbook file containing two Worksheets.  Both Worksheets will have a column for each stock specified.  One Worksheet will contain the gain/loss percentage and the other will contain the close or adjusted close price for each day.

The gain/loss percentage is calculated by comparing the share purchase price, and the end-of-day close or adjusted close price.  The shares you want to track, and the purchase price, should be specified in a CSV file.  Shares you want to track can be split-up into different groups resulting in a seperate Excel Workbook for each group.

SharesGainLossTracker uses 3rd party APIs for stocks data, and currently [Marketstack.com](https://marketstack.com?utm_source=FirstPromoter&utm_medium=Affiliate&fpr=metaljase) and [Alpha Vantage](https://www.alphavantage.co/) are supported.  They both offer free and paid tiers, and SharesGainLossTracker will work with their free tiers.  Marketstack and Alpha Vantage rate limit calls to their APIs to a certain amount of calls per milliesecond/day.

At the time of writing, Marketstack's free tier includes calls to their API endpoint that returns both the close and adjusted close prices, whereas Alpha Vantage's free tier only returns the close price; their paid tier includes the adjusted close price.

You will need to sign up to a free (or paid) tier, which will give you a key to access their API.  This key needs to be inserted into the `appsettings` file.

# Setup instructions
1) Download the latest version of the console app or WPF app for your Windows PC from [Releases](https://github.com/metaljase/SharesGainLossTracker/releases), then extract the files from the zip file.

2) Perform the steps in the [configuration instructions section](#configuration-instructions).

3) Run the executable file (.exe) extracted from the zip file. 

OR

1) Clone the SharesGainLossTracker repository.

2) Open the .NET solution in Visual Studio 2022 (or a compatible alternative).

3) Perform the steps in the [configuration instructions section](#configuration-instructions).

4) Set either `SharesGainLossTracker.ConsoleApp` or `SharesGainLossTracker.WpfApp` as the startup project.

5) Build the solution and run!

# Configuration instructions
Open the `appsettings.json` file (or `appsettings.Development.json` if running in development mode) and edit the values:

| Setting                              | Description   |
| -------------------------------------|:---------------
| OpenOutputFileDirectory              | `true` or `false` sets whether or not the directory of the output file is opened upon creation.
| SuffixDateToOutputFilePath           | `true` or `false` sets whether or not the date is suffixed to the `OutputFilePath`.
| AppendPurchasePriceToStockNameColumn | `true` or `false` sets whether or not the purchase price is appended to the stock name column in the Excel file.
| Enabled                              | Set to `true` or `false` to enable/disable API calls and thus Excel file creation.
| Model                                | `Marketstack` for Marketstack API, or `AlphaVantage` for Alpha Vantage API.
| OutputFilePath                       | Path where Excel file is created.
| OutputFilenamePrefix                 | The Excel filename will be a date/time stamp. A prefix can be specified.
| SymbolsFullPath                      | Path and filename of the CSV file containing shares and purchase prices to be tracked.
| ApiUrl                               | URL of 3rd party stocks API.  Replace `<API KEY>` with your API key.
| ApiDelayPerCallMillieseconds         | Delay between API calls to keep within rate limit. One API call per stock.
| EndpointReturnsAdjustedClose         | `true` or `false` sets whether or not the API endpoint returns an adjusted close price.
| OrderByDateDescending                | `true` or `false` sets whether to sort dates in Excel file in ascending or descending order.

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

# Log file
Both apps write to a log file detailing the progress of each run.  As default, the log file is located in the same directory as the app.  Data is written to the same log file for the current month, after which a new log file will be written to.  12 log files are retained before the oldest is deleted.

A 3rd party library ([Serilog](https://serilog.net/)) is used for logging, and its configuration is located in `appsettings.json`.  This allows for the logs to be customised, e.g. output format and control over when log files are created/deleted.

The format of the output displayed in the console app can be customised by changing the `outputTemplate` in `appsettings.json`.

# Supporting other 3rd party stocks APIs
Support can be added by writing additional classes that implement the `SharesGainLossTracker.Core.Models.IStock` interface.
