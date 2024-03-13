using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Metalhead.SharesGainLossTracker.WpfApp;

public class SharesValidation(IConfiguration config) : IValidateOptions<SharesOptions>
{
    public SharesOptions Config { get; private set; } = config.GetSection(SharesOptions.SharesSettings).Get<SharesOptions>();

    public ValidateOptionsResult Validate(string name, SharesOptions options)
    {
        var validationResults = new List<ValidationResult>();

        if (options.OpenOutputFileDirectory is null)
        {
            validationResults.Add(new ValidationResult("OpenOutputFileDirectory is not specified in app settings."));
        }

        if (options.SuffixDateToOutputFilePath is null)
        {
            validationResults.Add(new ValidationResult("SuffixDateToOutputFilePath is not specified in app settings."));
        }

        if (options.AppendPurchasePriceToStockNameColumn is null)
        {
            validationResults.Add(new ValidationResult("AppendPurchasePriceToStockNameColumn is not specified in app settings."));
        }

        if (options.Groups is null)
        {
            validationResults.Add(new ValidationResult("Groups array is not specified in app settings."));
        }
        else if (options.Groups.Count == 0)
        {
            validationResults.Add(new ValidationResult("Groups array contains zero elements in app settings"));
        }
        else if (!options.Groups.Any(g => g.Enabled))
        {
            validationResults.Add(new ValidationResult("No enabled elements found in Groups array in app settings"));
        }
        else
        {
            foreach (var group in options.Groups.Where(g => g.Enabled))
            {
                if (string.IsNullOrWhiteSpace(group.Model))
                {
                    validationResults.Add(new ValidationResult("Model not specified in app settings."));
                }

                if (string.IsNullOrEmpty(group.OutputFilePath))
                {
                    validationResults.Add(new ValidationResult("OutputFilePath not specified in app settings."));
                }
                else
                {
                    var outputFilePath = Environment.ExpandEnvironmentVariables(group.OutputFilePath);
                    if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        validationResults.Add(new ValidationResult($"OutputFilePath '{group.OutputFilePath}' in app settings contains invalid characters."));
                    }
                }

                if (string.IsNullOrWhiteSpace(group.OutputFilenamePrefix))
                {
                    validationResults.Add(new ValidationResult("OutputFilenamePrefix not specified in app settings."));
                }
                else if (group.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    validationResults.Add(new ValidationResult($"Output filename prefix '{group.OutputFilenamePrefix}' specified in app settings contains invalid characters."));
                }

                if (string.IsNullOrEmpty(group.SymbolsFullPath))
                {
                    validationResults.Add(new ValidationResult("SymbolsFullPath not specified in app settings."));
                }
                else
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(group.SymbolsFullPath);
                    if (!File.Exists(symbolsFullPath))
                    {
                        validationResults.Add(new ValidationResult($"Cannot find shares input file specified in app settings."));
                    }
                }

                if (string.IsNullOrWhiteSpace(group.ApiUrl))
                {
                    validationResults.Add(new ValidationResult("ApiUrl not specified in app settings."));
                }
                else if (!Uri.TryCreate(group.ApiUrl, UriKind.Absolute, out _))
                {
                    validationResults.Add(new ValidationResult("ApiUrl specified in app settings is not a valid URL."));
                }

                if (group.ApiDelayPerCallMilleseconds < 0)
                {
                    validationResults.Add(new ValidationResult("ApiDelayPerCallMilleseconds cannot be less than 0."));
                }
            }
        }

        if (validationResults.Count > 0)
        {
            return ValidateOptionsResult.Fail(validationResults.Select(vr => vr.ErrorMessage));
        }

        return ValidateOptionsResult.Success;
    }
}