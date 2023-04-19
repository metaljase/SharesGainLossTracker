using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public interface ISharesInputLoader
{
    List<Share> CreateSharesInput(string sharesInputFileFullPath);
}
