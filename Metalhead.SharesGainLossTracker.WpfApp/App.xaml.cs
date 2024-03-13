using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.FileSystem;
using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IHost Host { get; private set; }

        public App()
        {
            string[] args = Environment.GetCommandLineArgs();

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
            // WARNING: When overriding appsettings.json with environment settings, be careful with arrays.  Different
            // amounts of elements in arrays will be mixed into appsettings.json, i.e. not wiped over and rewritten.

            builder.Logging.ClearProviders().AddSerilog();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Services.AddOptions<SharesOptions>().Bind(builder.Configuration.GetSection(SharesOptions.SharesSettings));
            builder.Services.AddSingleton<IValidateOptions<SharesOptions>, SharesValidation>();
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SharesOptions>>().Value);

            builder.Services.AddHttpClient();
            
            builder.Services.AddSingleton<IProgress<ProgressLog>, Progress<ProgressLog>>();
            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<IExcelWorkbookCreatorService, ExcelWorkbookCreatorService>();
            builder.Services.AddSingleton<ISharesInputLoader, SharesInputLoaderCsv>();
            builder.Services.AddSingleton<IStocksDataService, StocksDataService>();
            builder.Services.AddSingleton<SharesInputLoaderService, SharesInputLoaderService>();
            builder.Services.AddSingleton<ISharesOutputService, SharesOutputService>();
            builder.Services.AddSingleton<IFileSystemFileWrapper, FileSystemFileWrapper>();
            builder.Services.AddSingleton<IFileStreamFactory, FileStreamFactory>();
            builder.Services.AddSingleton<ISharesInputHelperWrapper, SharesInputHelperWrapper>();
            builder.Services.AddSingleton<ISharesOutputDataTableHelperWrapper, SharesOutputDataTableHelperWrapper>();
            builder.Services.AddSingleton<ISharesOutputHelperWrapper, SharesOutputHelperWrapper>();

            var stockApiSources = Assembly.Load("Metalhead.SharesGainLossTracker.Core")
                .GetTypes().Where(type => typeof(IStock).IsAssignableFrom(type) && !type.IsInterface);
            foreach (var stockApiSource in stockApiSources)
            {
                builder.Services.AddSingleton(typeof(IStock), stockApiSource);
            }

            Host = builder.Build();

            using var serviceScope = Host.Services.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host!.StartAsync();            

            try
            {
                Host.Services.GetRequiredService<MainWindow>().Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Application exited unexpectedly.  See log file for details.");
                if (ex is OptionsValidationException)
                {
                    MessageBox.Show(ex.Message.Replace("; ", Environment.NewLine), "SharesGainLossTracker", MessageBoxButton.OK);
                }
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
