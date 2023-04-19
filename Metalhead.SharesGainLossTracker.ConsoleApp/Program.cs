using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;
using Serilog;

namespace Metalhead.SharesGainLossTracker.ConsoleApp
{
    public class Program
    {
        static async Task Main()
        {
            using IHost host = CreateHostBuilder().Build();
            using var serviceScope = host.Services.CreateScope();

            var services = serviceScope.ServiceProvider;

            try
            {
                ValidateSettings(host.Services.GetRequiredService<Settings>());
                await services.GetRequiredService<App>().RunAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Application exited unexpectedly.  See log file for details.");
            }
            finally
            {
                Log.CloseAndFlush();
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static IHostBuilder CreateHostBuilder()
        {
            var configuration = GetConfiguration();
            var settings = configuration.GetSection("sharesSettings").Get<Settings>();

            var stockApiSources = Assembly.Load("Metalhead.SharesGainLossTracker.Core")
                .GetTypes().Where(type => typeof(IStock).IsAssignableFrom(type) && !type.IsInterface);

            return Host
                .CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddHttpClient();
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.AddSingleton<Settings>(settings);
                    services.AddSingleton<IProgress<ProgressLog>, Progress<ProgressLog>>();
                    services.AddSingleton<App>();
                    services.AddSingleton<IExcelWorkbookCreatorService, ExcelWorkbookCreatorService>();
                    services.AddSingleton<ISharesInputLoader, SharesInputLoaderCsv>();
                    services.AddSingleton<IStocksDataService, StocksDataService>();
                    services.AddSingleton<SharesInputLoaderService, SharesInputLoaderService>();
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
                .UseSerilog();
        }

        private static IConfigurationRoot GetConfiguration()
        {
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
                throw new ArgumentNullException("Groups", "Groups array is missing from appsettings.json.");
            }
            else if (!settings.Groups.Any())
            {
                Log.Logger.Error("Groups array contains zero elements in appsettings.json.");
                throw new ArgumentException("Groups array contains zero elements in appsettings.json.");
            }
            else if (!settings.Groups.Any(e => e.Enabled))
            {
                Log.Logger.Error("No enabled elements in appsettings.json.");
                throw new ArgumentException("No enabled elements in appsettings.json.");
            }

            foreach (var shareGroup in settings.Groups.Where(g => g.Enabled))
            {
                var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                if (!string.IsNullOrWhiteSpace(shareGroup.SymbolsFullPath) && !File.Exists(symbolsFullPath))
                {
                    Log.Error("Shares input file (in appsettings.json) not found: {0}", symbolsFullPath);
                    throw new FileNotFoundException($"Shares input file (in appsettings.json) not found.", symbolsFullPath);
                }

                var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);
                if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    Log.Error("Output file path '{0}' in appsettings.json contains invalid characters.", shareGroup.OutputFilePath);
                    throw new ArgumentException($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.");
                }

                if (shareGroup.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Log.Error("Output filename prefix '{0}' contains invalid characters.", shareGroup.OutputFilenamePrefix);
                    throw new ArgumentException($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' in appsettings.json contains invalid characters.");
                }
            }
        }
    }
}
