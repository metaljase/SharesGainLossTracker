using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using log4net;
using log4net.Config;
using SharesGainLossTracker.Core;
using System.Collections.Generic;

namespace SharesGainLossTracker.ConsoleApp
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static async Task Main()
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

                if (settings.Groups == null)
                {
                    Log.Error("Groups array is missing from appsettings.json.");
                    throw new ArgumentNullException("Groups array is missing from appsettings.json.");
                }
                else if (settings.Groups.Count == 0)
                {
                    Log.Error("Groups array contains zero elements in appsettings.json.");
                    throw new ArgumentException("Groups array contains zero elements in appsettings.json.");
                }

                foreach (var shareGroup in settings.Groups.Where(g => g.Enabled))
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                    if (!string.IsNullOrWhiteSpace(shareGroup.SymbolsFullPath) && !File.Exists(symbolsFullPath))
                    {
                        Log.Error($"Shares input file (in appsettings.json) not found: {symbolsFullPath}");
                        throw new FileNotFoundException($"Shares input file (in appsettings.json) not found.", symbolsFullPath);
                    }

                    var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);
                    if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        Log.Error($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.");
                        throw new ArgumentException($"Output file path '{shareGroup.OutputFilePath}' in appsettings.json contains invalid characters.");
                    }

                    if (shareGroup.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        Log.ErrorFormat($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' contains invalid characters.");
                        throw new ArgumentException($"Output filename prefix '{shareGroup.OutputFilenamePrefix}' in appsettings.json contains invalid characters.");
                    }
                }

                // Get stocks data for all groups and create an Excel workbook for each.
                var shares = new Shares(Log);
                List<string> outputFilePathOpened = new();

                foreach (var shareGroup in settings.Groups.Where(g => g.Enabled))
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(shareGroup.SymbolsFullPath);
                    var outputFilePath = Environment.ExpandEnvironmentVariables(shareGroup.OutputFilePath);
                    var excelFileFullPath = await Shares.CreateWorkbookAsync(shareGroup.Model, symbolsFullPath, shareGroup.ApiUrl, shareGroup.ApiDelayPerCallMilleseconds, shareGroup.OrderByDateDescending, outputFilePath, shareGroup.OutputFilenamePrefix);

                    if (excelFileFullPath != null && settings.OpenOutputFileDirectory)
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
                            Log.ErrorFormat("Folder does not exist: {0}", outputFilePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("Crashed.  See log file for details.", ex);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
