namespace Metalhead.SharesGainLossTracker.Core;

public interface IFileSystemFileWrapper
{
    string[] ReadAllLines(string path);
    bool Exists(string path);
}
