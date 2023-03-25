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

using log4net;
using log4net.Config;
using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.ConsoleApp
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static async Task Main()
        {
            using IHost host = CreateHostBuilder().Build();
            using var serviceScope = host.Services.CreateScope();

            var services = serviceScope.ServiceProvider;

            try
            {
                await services.GetRequiredService<App>().RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal("Crashed.  See log file for details.", ex);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        static IHostBuilder CreateHostBuilder()
        {
            // Load Log4Net configuration.
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            var configuration = GetConfiguration();
            var settings = GetSettings(configuration);

            var stockApiSources = Assembly.Load("Metalhead.SharesGainLossTracker.Core")
                .GetTypes().Where(type => typeof(IStock).IsAssignableFrom(type) && !type.IsInterface);

            return Host
                .CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddHttpClient();
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.AddSingleton<Settings>(settings);
                    services.AddSingleton<ILog>(Log);
                    services.AddSingleton<IProgress<ProgressLog>, Progress<ProgressLog>>();
                    services.AddSingleton<App>();
                    services.AddSingleton<Shares>();

                    foreach (var stockApiSource in stockApiSources)
                    {
                        services.AddSingleton(typeof(IStock), stockApiSource);
                    }
                });
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

            return builder.Build();
        }

        private static Settings GetSettings(IConfigurationRoot config)
        {
            var settings = config.GetSection("sharesSettings").Get<Settings>();

            if (settings.Groups is null)
            {
                Log.Error("Groups array is missing from appsettings.json.");
                throw new ArgumentNullException("Groups", "Groups array is missing from appsettings.json.");
            }
            else if (!settings.Groups.Any())
            {
                Log.Error("Groups array contains zero elements in appsettings.json.");
                throw new ArgumentException("Groups array contains zero elements in appsettings.json.");
            }
            else if (!settings.Groups.Any(e => e.Enabled))
            {
                Log.Error("No enabled elements in appsettings.json.");
                throw new ArgumentException("No enabled elements in appsettings.json.");
            }

            foreach (var shareGroup in settings.Groups.Where(g => g.Enabled))
            {
                var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                if (!string.IsNullOrWhiteSpace(shareGroup.SymbolsFullPath) && !File.Exists(symbolsFullPath))
                {
                    Log.ErrorFormat("Shares input file (in appsettings.json) not found: {0}", symbolsFullPath);
                    throw new FileNotFoundException($"Shares input file (in appsettings.json) not found.", symbolsFullPath);
                }

                var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);
                if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    Log.ErrorFormat("Output file path '{0}' in appsettings.json contains invalid characters.", shareGroup.OutputFilePath);
                    throw new ArgumentException($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.");
                }

                if (shareGroup.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Log.ErrorFormat("Output filename prefix '{0}' contains invalid characters.", shareGroup.OutputFilenamePrefix);
                    throw new ArgumentException($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' in appsettings.json contains invalid characters.");
                }
            }

            return settings;
        }   
    }
}
