using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.FileSystem;
using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;
using Serilog;

namespace Metalhead.SharesGainLossTracker.WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost Host { get; }

        public App()
        {
            string[] args = Environment.GetCommandLineArgs();

            var stockApiSources = Assembly.Load("Metalhead.SharesGainLossTracker.Core")
                .GetTypes().Where(type => typeof(IStock).IsAssignableFrom(type) && !type.IsInterface);

            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(hostConfig =>
                {
                    hostConfig.SetBasePath(Directory.GetCurrentDirectory());
                    hostConfig.AddCommandLine(args);
                    hostConfig.AddEnvironmentVariables(prefix: "DOTNET_");
                })
                .ConfigureAppConfiguration((hostingContext, hostConfig) =>
                {
                    hostConfig.AddConfiguration(GetConfiguration(hostingContext, hostConfig));
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IConfiguration>(hostContext.Configuration);
                    services.AddSingleton<IProgress<ProgressLog>, Progress<ProgressLog>>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<IExcelWorkbookCreatorService, ExcelWorkbookCreatorService>();
                    services.AddSingleton<ISharesInputLoader, SharesInputLoaderCsv>();
                    services.AddSingleton<IStocksDataService, StocksDataService>();
                    services.AddSingleton<SharesInputLoaderService, SharesInputLoaderService>();
                    services.AddSingleton<ISharesOutputService, SharesOutputService>();
                    services.AddSingleton<IFileSystemFileWrapper, FileSystemFileWrapper>();
                    services.AddSingleton<IFileStreamFactory, FileStreamFactory>();
                    services.AddSingleton<ISharesInputHelperWrapper, SharesInputHelperWrapper>();
                    services.AddSingleton<ISharesOutputDataTableHelperWrapper, SharesOutputDataTableHelperWrapper>();
                    services.AddSingleton<ISharesOutputHelperWrapper, SharesOutputHelperWrapper>();

                    foreach (var stockApiSource in stockApiSources)
                    {
                        services.AddSingleton(typeof(IStock), stockApiSource);
                    }
                })
                .UseSerilog()
                .Build();
        }

        private static IConfigurationRoot GetConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder builder)
        {
            // Load configuration and settings.
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true);

            if (hostingContext.HostingEnvironment.IsDevelopment())
            {
                builder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
            }

            // WARNING: When overriding appsettings.json with environment settings, be careful with arrays.  Different
            // amounts of elements in arrays will be mixed into appsettings.json, i.e. not wiped over and rewritten.

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())
                .Enrich.FromLogContext()
                .CreateLogger();

            return builder.Build();
        }

        private static void ValidateSettings(Settings settings)
        {
            if (settings.Groups is null)
            {
                Log.Logger.Error("Groups array is missing from appsettings.json.");
                MessageBox.Show("Groups array is missing from appsettings.json.", "SharesGainLossTracker", MessageBoxButton.OK);
                throw new ArgumentNullException("Groups", "Groups array is missing from appsettings.json.");
            }
            else if (!settings.Groups.Any())
            {
                Log.Logger.Error("Groups array contains zero elements in appsettings.json.");
                MessageBox.Show("Groups array contains zero elements in appsettings.json", "SharesGainLossTracker", MessageBoxButton.OK);
                throw new ArgumentException("Groups array contains zero elements in appsettings.json.");
            }
            else if (!settings.Groups.Any(e => e.Enabled))
            {
                Log.Logger.Error("No enabled elements in appsettings.json.");
                MessageBox.Show("No enabled elements in appsettings.json", "SharesGainLossTracker", MessageBoxButton.OK);
                throw new ArgumentException("No enabled elements in appsettings.json.");
            }

            foreach (var shareGroup in settings.Groups.Where(g => g.Enabled))
            {
                var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                if (!string.IsNullOrWhiteSpace(shareGroup.SymbolsFullPath) && !File.Exists(symbolsFullPath))
                {
                    Log.Logger.Error("Shares input file (in appsettings.json) not found: {SymbolsFullPath}", symbolsFullPath);
                    MessageBox.Show($"Shares input file (in appsettings.json) not found: {symbolsFullPath}", "SharesGainLossTracker", MessageBoxButton.OK);
                    throw new FileNotFoundException($"Shares input file (in appsettings.json) not found.", symbolsFullPath);
                }

                var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);
                if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    Log.Logger.Error("Output file path '{OutputFilePath}' in appsettings.json contains invalid characters.", shareGroup.OutputFilePath);
                    MessageBox.Show($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.", "SharesGainLossTracker", MessageBoxButton.OK);
                    throw new ArgumentException($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.");
                }

                if (shareGroup.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Log.Logger.Error("Output filename prefix '{OutputFilenamePrefix}' contains invalid characters.", shareGroup.OutputFilenamePrefix);
                    MessageBox.Show($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' contains invalid characters.", "SharesGainLossTracker", MessageBoxButton.OK);
                    throw new ArgumentException($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' in appsettings.json contains invalid characters.");
                }
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host!.StartAsync();            

            try
            {
                ValidateSettings(Host.Services.GetRequiredService<IConfiguration>().GetSection("sharesSettings").Get<Settings>());
                Host.Services.GetRequiredService<MainWindow>().Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Application exited unexpectedly.  See log file for details.");
                Environment.Exit(0);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            await Host!.StopAsync();
            base.OnExit(e);
        }
    }
}
