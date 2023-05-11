using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesInputLoaderService
{
    private ISharesInputLoader ShareInputLoader { get; }

    public SharesInputLoaderService(ISharesInputLoader shareInputLoader)
    {
        ShareInputLoader = shareInputLoader;
    }

    public List<Share> LoadSharesInput(string shareInputFileFullPath)
    {
        return ShareInputLoader.CreateSharesInput(shareInputFileFullPath);
    }
}
