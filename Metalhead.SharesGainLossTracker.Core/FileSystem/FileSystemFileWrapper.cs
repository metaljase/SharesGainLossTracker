using System.IO;

namespace Metalhead.SharesGainLossTracker.Core.FileSystem;

public class FileSystemFileWrapper : IFileSystemFileWrapper
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public string[] ReadAllLines(string path)
    {
        return File.ReadAllLines(path);
    }
}
