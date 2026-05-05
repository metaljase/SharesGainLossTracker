using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

using Metalhead.SharesGainLossTracker.Core.FileSystem;
using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesInputLoaderCsv(
    ILogger<SharesInputLoaderCsv> logger,
    IProgress<ProgressLog> progress,
    IFileSystemFileWrapper fileSystemFileWrapper)
    : ISharesInputLoader
{
    public List<Share> CreateSharesInput(string sharesInputFileFullPath) => CreateSharesInputFromCsvFile(sharesInputFileFullPath);

    public List<Share> CreateSharesInputFromCsvFile(string sharesInputFileFullPath)
    {
        if (sharesInputFileFullPath is null)
        {
            logger.LogError("Shares input file full path cannot be null.");
            progress.Report(new ProgressLog(MessageImportance.Bad, "Shares input file full path cannot be null.", false));
            throw new ArgumentNullException(nameof(sharesInputFileFullPath), "Shares input file full path cannot be null.");
        }

        if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && !fileSystemFileWrapper.Exists(sharesInputFileFullPath))
        {
            logger.LogError("Shares input file not found: {SharesInputFileFullPath}", sharesInputFileFullPath);
            progress.Report(new ProgressLog(MessageImportance.Bad, $"Shares input file not found: {sharesInputFileFullPath}", false));
            throw new FileNotFoundException($"Shares input file not found.", sharesInputFileFullPath);
        }

        IEnumerable<string> delimitedSharesInput = [];
        if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && fileSystemFileWrapper.Exists(sharesInputFileFullPath))
        {
            var allLines = fileSystemFileWrapper.ReadAllLines(sharesInputFileFullPath);
            delimitedSharesInput = allLines.Where(x => !string.IsNullOrEmpty(x) && x.Contains(','));

            if (allLines.Length == 0 || allLines.Length != delimitedSharesInput.Count())
            {
                logger.LogError(
                    "Not all lines in the shares input file are formatted correctly: {SharesInputFileFullPath}", sharesInputFileFullPath);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"Not all lines in the shares input file are formatted correctly: {sharesInputFileFullPath}", false));
                throw new InvalidOperationException(
                    $"Not all lines in the shares input file are formatted correctly: {sharesInputFileFullPath}");
            }
        }

        return CreateSharesInputFromCsv(delimitedSharesInput);
    }

    public List<Share> CreateSharesInputFromCsv(IEnumerable<string> delimitedSharesInput)
    {
        var sharesInput = new List<Share>();
        foreach (var delimitedLine in delimitedSharesInput)
        {
            var elements = delimitedLine.Split(',').Select(a => a.Trim()).ToArray();

            if (elements.Length != 3 || elements.Any(e => e.Length == 0))
            {
                logger.LogError("Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: {DelimitedLine}", delimitedLine);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: {delimitedLine}", false));
                throw new InvalidOperationException(
                    $"Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: {delimitedLine}");
            }

            try
            {
                sharesInput.Add(new Share() { Symbol = elements[0], StockName = elements[1], PurchasePrice = double.Parse(elements[2]) });
            }
            catch (FormatException ex)
            {
                var exception = new InvalidOperationException(
                    $"Shares input CSV contains incorrectly formatted value(s): {delimitedLine}", ex);
                logger.LogError(exception, "Shares input CSV contains incorrectly formatted value(s): {DelimitedLine}", delimitedLine);
                progress.Report(new ProgressLog(MessageImportance.Bad, $"Shares input CSV contains incorrectly formatted value(s): {delimitedLine}", false));
                throw exception;
            }
        }

        if (sharesInput.Count == 0)
        {
            logger.LogError("Shares input CSV does not contain any lines with correctly formatted values.");
            progress.Report(new ProgressLog(MessageImportance.Bad, "Shares input CSV does not contain any lines with correctly formatted values.", false));
            throw new InvalidOperationException("Shares input CSV does not contain any lines with correctly formatted values.");
        }

        return sharesInput;
    }
}
