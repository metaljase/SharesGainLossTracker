using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.FileSystem;
using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

namespace Metalhead.SharesGainLossTracker.ConsoleApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
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
            builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            builder.Services.AddSingleton<IProgress<ProgressLog>, Progress<ProgressLog>>();
            builder.Services.AddSingleton<App>();
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
            
            using var host = builder.Build();
            
            using var serviceScope = host.Services.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;

            try
            {
                await serviceProvider.GetRequiredService<App>().RunAsync();
            }
            catch (Exception ex)
            {
                if (ex is OptionsValidationException)
                {
                    Log.Logger.Error(ex, "Application exited due to invalid app settings.  See log file for details.");
                    // Logger is configured not to write exceptions to the console, but write the validation errors.
                    Console.WriteLine(ex.Message.Replace("; ", Environment.NewLine));
                }
                else
                {
                    Log.Logger.Fatal(ex, "Application exited unexpectedly.  See log file for details.");
                }
            }
            finally
            {
                Log.CloseAndFlush();
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
