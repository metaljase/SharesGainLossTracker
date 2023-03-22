using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core
{
    public interface IStock
    {
        Task<List<FlattenedStock>> GetStocksDataAsync(HttpResponseMessage[] httpResponseMessages);
    }
}
