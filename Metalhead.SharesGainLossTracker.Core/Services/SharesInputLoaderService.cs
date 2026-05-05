using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesInputLoaderService(ISharesInputLoader shareInputLoader)
{
    public List<Share> LoadSharesInput(string shareInputFileFullPath) => shareInputLoader.CreateSharesInput(shareInputFileFullPath);
}
