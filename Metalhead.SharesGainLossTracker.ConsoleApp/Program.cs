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

using Metalhead.SharesGainLossTracker.Core;
using Metalhead.SharesGainLossTracker.Core.FileSystem;
using Metalhead.SharesGainLossTracker.Core.Helpers;
using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.Services;

using Metalhead.SharesGainLossTracker.ConsoleApp;

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

// Suppress warnings as Metalhead.SharesGainLossTracker.Core has been configured to only trim it's dependencies, therefore IStock should be available.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
var stockApiSources = Assembly.Load("Metalhead.SharesGainLossTracker.Core")
    .GetTypes().Where(type => typeof(IStock).IsAssignableFrom(type) && !type.IsInterface);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
foreach (var stockApiSource in stockApiSources)
{
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
    builder.Services.AddSingleton(typeof(IStock), stockApiSource);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
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
        Log.Logger.Fatal("Application exited due to invalid app settings:\r\n{ValidationErrors}", ex.Message.Replace("; ", Environment.NewLine));
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

Console.WriteLine();
Console.WriteLine("Press any key to exit.");
Console.ReadKey();
