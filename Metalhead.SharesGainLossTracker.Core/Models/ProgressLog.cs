namespace Metalhead.SharesGainLossTracker.Core.Models
{
    public enum MessageImportance
    {
        Good,
        Bad,
        Normal
    };

    public class ProgressLog
    {
        public ProgressLog(MessageImportance importance, string message, bool createdExcelFile = false)
        {
            Importance = importance;
            DownloadLog = message;
            CreatedExcelFile = createdExcelFile;
        }

        public MessageImportance Importance { get; set; }
        public string DownloadLog { get; set; }
        public bool CreatedExcelFile { get; set; }
    }
}
