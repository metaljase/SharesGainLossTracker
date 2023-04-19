using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

using Metalhead.SharesGainLossTracker.Core.Models;

namespace Metalhead.SharesGainLossTracker.Core.Services;

public class SharesInputLoaderService
{
    public ILogger<SharesInputLoaderService> Log { get; }
    public IProgress<ProgressLog> Progress { get; }
    public ISharesInputLoader ShareInputLoader { get; }

    public SharesInputLoaderService(ILogger<SharesInputLoaderService> log, IProgress<ProgressLog> progress, ISharesInputLoader shareInputLoader)
    {
        Log = log;
        Progress = progress;
        ShareInputLoader = shareInputLoader;
    }

    public List<Share> LoadSharesInput(string shareInputFileFullPath)
    {
        return ShareInputLoader.CreateSharesInput(shareInputFileFullPath);
    }
}
