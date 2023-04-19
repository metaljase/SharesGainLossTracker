using System.IO;

namespace Metalhead.SharesGainLossTracker.Core;

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
