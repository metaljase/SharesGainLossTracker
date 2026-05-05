using System.IO;

namespace Metalhead.SharesGainLossTracker.Core.FileSystem;

public class FileSystemFileWrapper : IFileSystemFileWrapper
{
    public bool Exists(string path) => File.Exists(path);

    public string[] ReadAllLines(string path) => File.ReadAllLines(path);
}
