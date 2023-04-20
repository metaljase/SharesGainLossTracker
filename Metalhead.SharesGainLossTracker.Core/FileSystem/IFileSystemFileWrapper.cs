namespace Metalhead.SharesGainLossTracker.Core.FileSystem;

public interface IFileSystemFileWrapper
{
    string[] ReadAllLines(string path);
    bool Exists(string path);
}
