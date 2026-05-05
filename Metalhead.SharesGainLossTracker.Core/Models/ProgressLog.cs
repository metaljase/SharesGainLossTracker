namespace Metalhead.SharesGainLossTracker.Core.Models;

public enum MessageImportance
{
    Good,
    Bad,
    Normal
};

public class ProgressLog(MessageImportance importance, string message, bool createdExcelFile = false)
{
    public MessageImportance Importance { get; set; } = importance;
    public string DownloadLog { get; set; } = message;
    public bool CreatedExcelFile { get; set; } = createdExcelFile;
}
