namespace Awaken.Application.Common.Interfaces;

public interface ISafeDirectoryResolver
{
    /// <summary>
    /// Resolves a batch key (relative subdirectory name) to an absolute path
    /// that is guaranteed to be within the configured root directory.
    /// Returns null if the resolved path would escape the root, if the root
    /// is not configured, or if the batch key is invalid.
    /// </summary>
    string? Resolve(string batchKey);

    string? RootDirectory { get; }
}
