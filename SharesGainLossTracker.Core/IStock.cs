using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using SharesGainLossTracker.Core.Models;

namespace SharesGainLossTracker.Core
{
    public interface IStock
    {
        Task<List<FlattenedStock>> GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages, List<Share> sharesInput);
    }
}
