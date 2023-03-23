﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

using log4net;
using log4net.Config;

namespace Metalhead.SharesGainLossTracker.WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Load Log4Net configuration.
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                // Load configuration and settings.
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                    .AddEnvironmentVariables();

                // WARNING: When overriding appsettings.json with environment settings, be careful with arrays.  Different
                // amounts of elements in arrays will be mixed into appsettings.json, i.e. not wiped over and rewritten.

                var config = builder.Build();
                var settings = config.GetSection("sharesSettings").Get<Settings>();


                if (settings.Groups is null)
                {
                    Log.Error("Groups array is missing from appsettings.json.");
                    MessageBox.Show("Groups array is missing from appsettings.json.", "SharesGainLossTracker", MessageBoxButton.OK);
                    throw new ArgumentNullException("Groups array is missing from appsettings.json.");
                }
                else if (!settings.Groups.Any())
                {
                    Log.Error("Groups array contains zero elements in appsettings.json.");
                    MessageBox.Show("Groups array contains zero elements in appsettings.json", "SharesGainLossTracker", MessageBoxButton.OK);
                    throw new ArgumentException("Groups array contains zero elements in appsettings.json.");
                }
                else if (!settings.Groups.Any(e => e.Enabled))
                {
                    Log.Error("No enabled elements in appsettings.json.");
                    MessageBox.Show("No enabled elements in appsettings.json", "SharesGainLossTracker", MessageBoxButton.OK);
                    throw new ArgumentException("No enabled elements in appsettings.json.");
                }

                foreach (var shareGroup in settings.Groups.Where(g => g.Enabled))
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                    if (!string.IsNullOrWhiteSpace(shareGroup.SymbolsFullPath) && !File.Exists(symbolsFullPath))
                    {
                        Log.ErrorFormat("Shares input file (in appsettings.json) not found: {0}", symbolsFullPath);
                        MessageBox.Show($"Shares input file (in appsettings.json) not found: {symbolsFullPath}", "SharesGainLossTracker", MessageBoxButton.OK);
                        throw new FileNotFoundException($"Shares input file (in appsettings.json) not found.", symbolsFullPath);
                    }

                    var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);
                    if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        Log.ErrorFormat("Output file path '{0}' in appsettings.json contains invalid characters.", shareGroup.OutputFilePath);
                        MessageBox.Show($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.", "SharesGainLossTracker", MessageBoxButton.OK);
                        throw new ArgumentException($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.");
                    }

                    if (shareGroup.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        Log.ErrorFormat("Output filename prefix '{0}' contains invalid characters.", shareGroup.OutputFilenamePrefix);
                        MessageBox.Show($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' contains invalid characters.", "SharesGainLossTracker", MessageBoxButton.OK);
                        throw new ArgumentException($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' in appsettings.json contains invalid characters.");
                    }
                }

                base.OnStartup(e);

                new MainWindow(Log, settings).Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
                Environment.Exit(0);
            }
        }
    }
}
