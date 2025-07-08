using System.Collections.Generic;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public interface ISharesOutputService
{
    Task<List<ShareOutput>?> CreateSharesOutputAsync(string model, string sharesInputFileFullPath, string stocksApiUrl, bool endpointReturnsAdjustedClose, int apiDelayPerCallMilliseconds, bool orderByDateDescending, bool appendPriceToStockName);
}