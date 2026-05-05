using System.IO;

namespace Metalhead.SharesGainLossTracker.Core.FileSystem;

public class FileStreamFactory : IFileStreamFactory
{
    public Stream Create(string path, FileMode mode, FileAccess access) => new FileStream(path, mode, access);
}
