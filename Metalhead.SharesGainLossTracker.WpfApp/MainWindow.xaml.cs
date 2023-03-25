﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using log4net;
using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ILog Log;
        private static Settings AppSettings;
        private readonly Progress<ProgressLog> Progress;
        private readonly Shares Shares;
        private bool AutoScroll = true;
        private bool CreatedExcelFile = false;

        public MainWindow(ILog log, Settings settings, IProgress<ProgressLog> progress, Shares shares)
        {
            Log = log;
            AppSettings = settings;
            Progress = (Progress<ProgressLog>)progress;
            Shares = shares;

            InitializeComponent();
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
            try
            {
                runButton.IsEnabled = false;
                CreatedExcelFile = false;
                logTextBlock.Text = string.Empty;
                List<string> outputFilePathOpened = new();
                
                Progress.ProgressChanged += ProgressLog;                

                // Get stocks data for all groups and create an Excel workbook for each.
                foreach (var shareGroup in AppSettings.Groups.Where(g => g.Enabled))
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                    var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);

                    if (AppSettings.SuffixDateToOutputFilePath)
                    {
                        outputFilePath = $"{outputFilePath}{DateTime.Now.Date:yyyy-MM-dd}";
                    }

                    var excelFileFullPath = await Shares.CreateWorkbookAsync(
                        shareGroup.Model,
                        symbolsFullPath,
                        shareGroup.ApiUrl,
                        shareGroup.ApiDelayPerCallMilleseconds,
                        shareGroup.OrderByDateDescending,
                        outputFilePath,
                        shareGroup.OutputFilenamePrefix,
                        AppSettings.AppendPurchasePriceToStockNameColumn);

                    if (excelFileFullPath != null && AppSettings.OpenOutputFileDirectory)
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
                            Log.Error($"Folder does not exist: {outputFilePath}");
                        }
                    }
                }

                var finishedForeground = CreatedExcelFile ? Brushes.Green : Brushes.Black;
                logTextBlock.Inlines.Add(new Run($"Finished{Environment.NewLine}") { Foreground = finishedForeground, FontWeight = FontWeights.Bold, FontSize = 28 });
                runButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
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
