using System.IO;

namespace Metalhead.SharesGainLossTracker.Core;

public interface IFileStreamFactory
{
    Stream Create(string path, FileMode mode, FileAccess access);
}
