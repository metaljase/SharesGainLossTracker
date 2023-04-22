using Metalhead.SharesGainLossTracker.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalhead.SharesGainLossTracker.Core.Services
{
    public interface ISharesOutputService
    {
        Task<List<ShareOutput>> CreateSharesOutputAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, int apiDelayPerCallMillieseconds, bool orderByDateDescending, bool appendPriceToStockName);
    }
}