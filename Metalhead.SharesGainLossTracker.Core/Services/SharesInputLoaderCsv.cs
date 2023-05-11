using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Metalhead.SharesGainLossTracker.Core.Models;
using Metalhead.SharesGainLossTracker.Core.FileSystem;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesInputLoaderCsv : ISharesInputLoader
{
    private ILogger<SharesInputLoaderCsv> Log { get; }
    private IProgress<ProgressLog> Progress { get; }
    private IFileSystemFileWrapper FileSystemFileWrapper { get; }

    public SharesInputLoaderCsv(ILogger<SharesInputLoaderCsv> log, IProgress<ProgressLog> progress, IFileSystemFileWrapper fileSystemFileWrapper)
    {
        Log = log;
        Progress = progress;
        FileSystemFileWrapper = fileSystemFileWrapper;
    }

    public List<Share> CreateSharesInput(string sharesInputFileFullPath)
    {
        return CreateSharesInputFromCsvFile(sharesInputFileFullPath);
    }

    public List<Share> CreateSharesInputFromCsvFile(string sharesInputFileFullPath)
    {
        if (sharesInputFileFullPath is null)
        {
            Log.LogError("Shares input file full path cannot be null.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Shares input file full path cannot be null.", false));
            throw new ArgumentNullException(nameof(sharesInputFileFullPath), "Shares input file full path cannot be null.");
        }

        if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && !FileSystemFileWrapper.Exists(sharesInputFileFullPath))
        {
            Log.LogError("Shares input file not found: {SharesInputFileFullPath}", sharesInputFileFullPath);
            Progress.Report(new ProgressLog(MessageImportance.Bad, $"Shares input file not found: {sharesInputFileFullPath}", false));
            throw new FileNotFoundException($"Shares input file not found.", sharesInputFileFullPath);
        }

        IEnumerable<string> delimitedSharesInput = new List<string>();
        if (!string.IsNullOrWhiteSpace(sharesInputFileFullPath) && FileSystemFileWrapper.Exists(sharesInputFileFullPath))
        {
            var allLines = FileSystemFileWrapper.ReadAllLines(sharesInputFileFullPath);
            delimitedSharesInput = allLines.Where(x => !string.IsNullOrEmpty(x) && x.Contains(','));

            if (allLines.Length == 0 || allLines.Length != delimitedSharesInput.Count())
            {
                Log.LogError("Not all lines in the shares input file are formatted correctly: {SharesInputFileFullPath}", sharesInputFileFullPath);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Not all lines in the shares input file are formatted correctly: {sharesInputFileFullPath}", false));
                throw new InvalidOperationException($"Not all lines in the shares input file are formatted correctly: {sharesInputFileFullPath}");
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
                Log.LogError("Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: {DelimitedLine}", delimitedLine);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: {delimitedLine}", false));
                throw new InvalidOperationException($"Line in shares input CSV does not contain a stock symbol, stock name, and purchase price: {delimitedLine}");
            }

            try
            {
                sharesInput.Add(new Share() { Symbol = elements[0], StockName = elements[1], PurchasePrice = double.Parse(elements[2]) });
            }
            catch (FormatException ex)
            {
                var exception = new InvalidOperationException($"Shares input CSV contains incorrectly formatted value(s): {delimitedLine}", ex);
                Log.LogError(exception, "Shares input CSV contains incorrectly formatted value(s): {DelimitedLine}", delimitedLine);
                Progress.Report(new ProgressLog(MessageImportance.Bad, $"Shares input CSV contains incorrectly formatted value(s): {delimitedLine}", false));
                throw exception;
            }
        }

        if (!sharesInput.Any())
        {
            Log.LogError("Shares input CSV does not contain any lines with correctly formatted values.");
            Progress.Report(new ProgressLog(MessageImportance.Bad, "Shares input CSV does not contain any lines with correctly formatted values.", false));
            throw new InvalidOperationException("Shares input CSV does not contain any lines with correctly formatted values.");
        }

        return sharesInput;
    }
}
