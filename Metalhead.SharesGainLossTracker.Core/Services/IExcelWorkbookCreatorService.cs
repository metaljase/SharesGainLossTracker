using System.Threading.Tasks;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public interface IExcelWorkbookCreatorService
{
    Task<string?> CreateWorkbookAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, bool endpointReturnsAdjustedClose, int apiDelayPerCallMillieseconds, bool orderByDateDescending, string outputFilePath, string outputFilenamePrefix, bool appendPriceToStockName);
}