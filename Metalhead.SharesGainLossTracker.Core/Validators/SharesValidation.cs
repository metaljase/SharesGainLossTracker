using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Validators;

public class SharesValidation() : IValidateOptions<SharesOptions>
{
    public ValidateOptionsResult Validate(string? name, SharesOptions options)
    {
        var validationResults = new List<ValidationResult>();

        if (options.Groups.Count == 0)
            validationResults.Add(new ValidationResult($"{nameof(SharesOptions.Groups)} array contains zero elements in app settings"));
        else if (!options.Groups.Any(g => g.Enabled))
            validationResults.Add(new ValidationResult($"No enabled elements found in {nameof(SharesOptions.Groups)} array in app settings"));
        else
        {
            foreach (var group in options.Groups.Where(g => g.Enabled))
            {
                if (string.IsNullOrWhiteSpace(group.Model))
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.Model)} not specified in app settings."));

                if (string.IsNullOrWhiteSpace(group.OutputFilePath))
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.OutputFilePath)} not specified in app settings."));
                else
                {
                    var outputFilePath = Environment.ExpandEnvironmentVariables(group.OutputFilePath);
                    if (outputFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        validationResults.Add(new ValidationResult($"{nameof(SharesGroup.OutputFilePath)} '{group.OutputFilePath}' in app settings contains invalid characters."));
                }

                if (string.IsNullOrWhiteSpace(group.OutputFilenamePrefix))
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.OutputFilenamePrefix)} not specified in app settings."));
                else if (group.OutputFilenamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.OutputFilenamePrefix)} '{group.OutputFilenamePrefix}' specified in app settings contains invalid characters."));

                if (string.IsNullOrWhiteSpace(group.SymbolsFullPath))
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.SymbolsFullPath)} not specified in app settings."));
                else
                {
                    var symbolsFullPath = Environment.ExpandEnvironmentVariables(group.SymbolsFullPath);
                    if (!File.Exists(symbolsFullPath))
                        validationResults.Add(new ValidationResult($"Cannot find shares input file specified in app settings."));
                }

                if (string.IsNullOrWhiteSpace(group.ApiUrl))
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.ApiUrl)} not specified in app settings."));
                else if (!Uri.TryCreate(group.ApiUrl, UriKind.Absolute, out _))
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.ApiUrl)} specified in app settings is not a valid URL."));

                if (group.ApiDelayPerCallMilliseconds < 0)
                    validationResults.Add(new ValidationResult($"{nameof(SharesGroup.ApiDelayPerCallMilliseconds)} cannot be less than 0."));
            }
        }

        if (validationResults.Count > 0)
        {
            var failures = validationResults.Where(v => v.ErrorMessage is not null).Select(v => v.ErrorMessage!);
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}