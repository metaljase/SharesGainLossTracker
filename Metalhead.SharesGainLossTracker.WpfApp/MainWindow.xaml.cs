using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ILogger<MainWindow> Log { get; }
        public Settings AppSettings { get; }
        private IConfiguration Configuration { get; }
        public IProgress<ProgressLog> Progress { get; }
        public IExcelWorkbookCreatorService ExcelWorkbookCreatorService { get; }
        public bool AutoScroll { get; set; } = true;
        public bool CreatedExcelFile { get; set; }

        public MainWindow(ILogger<MainWindow> log, IConfiguration configuration, IProgress<ProgressLog> progress, IExcelWorkbookCreatorService excelWorkbookCreatorService)
        {
            Log = log;
            Configuration = configuration;
            Progress = (Progress<ProgressLog>)progress;
            ExcelWorkbookCreatorService = excelWorkbookCreatorService;
            AppSettings = configuration.GetSection("sharesSettings").Get<Settings>();

            InitializeComponent();
            ((Progress<ProgressLog>)Progress).ProgressChanged += ProgressLog;
        }

        public static SolidColorBrush GetDownloadLogForegroundColour(MessageImportance importance)
        {
            return importance switch
            {
                MessageImportance.Good => Brushes.Green,
                MessageImportance.Bad => Brushes.Red,
                _ => Brushes.Black
            };
        }

        public void ProgressLog(object sender, ProgressLog e)
        {
            CreatedExcelFile = e.CreatedExcelFile;

            var textForegroundColour = GetDownloadLogForegroundColour(e.Importance);
            var fontWeight = e.CreatedExcelFile ? FontWeights.Bold : FontWeights.Normal;
            logTextBlock.Inlines.Add(new Run($"{e.DownloadLog}{Environment.NewLine}") { Foreground = textForegroundColour, FontWeight = fontWeight });
        }

        private async void RunButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string excelFileFullPath = null;
            try
            {
                runButton.IsEnabled = false;
                CreatedExcelFile = false;
                logTextBlock.Text = string.Empty;
                List<string> outputFilePathOpened = new();
                
                // Get stocks data for all groups and create an Excel Workbook for each.
                foreach (var shareGroup in AppSettings.Groups.Where(g => g.Enabled))
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                    var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);

                    if (AppSettings.SuffixDateToOutputFilePath)
                    {
                        outputFilePath = $"{outputFilePath}{DateTime.Now.Date:yyyy-MM-dd}";
                    }

                    excelFileFullPath = await ExcelWorkbookCreatorService.CreateWorkbookAsync(
                        shareGroup.Model,
                        symbolsFullPath,
                        shareGroup.ApiUrl,
                        shareGroup.ApiDelayPerCallMilleseconds,
                        shareGroup.OrderByDateDescending,
                        outputFilePath,
                        shareGroup.OutputFilenamePrefix,
                        AppSettings.AppendPurchasePriceToStockNameColumn);

                    if (excelFileFullPath is not null && AppSettings.OpenOutputFileDirectory)
                    {
                        if (Directory.Exists(outputFilePath))
                        {
                            if (!outputFilePathOpened.Any(o => o.Equals(outputFilePath, StringComparison.OrdinalIgnoreCase)))
                            {
                                outputFilePathOpened.Add(outputFilePath);
                                ProcessStartInfo startInfo = new("explorer.exe", outputFilePath);
                                Process.Start(startInfo);
                            }
                        }
                        else
                        {
                            Log.LogError("Folder does not exist: ", outputFilePath);
                        }
                    }
                }

                var finishedForeground = CreatedExcelFile ? Brushes.Green : Brushes.Black;
                logTextBlock.Inlines.Add(new Run($"Finished{Environment.NewLine}") { Foreground = finishedForeground, FontWeight = FontWeights.Bold, FontSize = 28 });
                runButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "An unexpected error occurred.  See log file for details.");
                Progress.Report(new ProgressLog(MessageImportance.Bad, "An unexpected error occurred.  See log file for details.", excelFileFullPath is not null));
            }
        }      

        private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // ExtentHeightChange == 0 indicates additional text didn't trigger the event, therefore the user moved scroll bar's sliding thumb.
            if (e.ExtentHeightChange == 0)
            {
                // Enable auto-scroll if the sliding thumb is the bottom of the scroll bar.
                AutoScroll = (logScrollViewer.VerticalOffset == logScrollViewer.ScrollableHeight);
            }
            else if (AutoScroll)
            {
                // Additional text triggered the event, and auto-scroll is enabled, so move sliding thumb to the bottom of the scroll bar.
                logScrollViewer.ScrollToVerticalOffset(logScrollViewer.ExtentHeight);
            }
        }
    }
}
