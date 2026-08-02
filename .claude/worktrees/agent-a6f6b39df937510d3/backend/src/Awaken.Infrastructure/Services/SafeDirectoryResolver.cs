using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Awaken.Infrastructure.Services;

public class SafeDirectoryResolver(IConfiguration config) : ISafeDirectoryResolver
{
    private readonly string? _rootDirectory = config["ExerciseImport:RootDirectory"];

    public string? RootDirectory => _rootDirectory;

    public string? Resolve(string batchKey)
    {
        if (string.IsNullOrWhiteSpace(_rootDirectory))
            return null;

        if (string.IsNullOrWhiteSpace(batchKey))
            return null;

        // Reject absolute paths or paths with .. traversal patterns upfront
        if (Path.IsPathRooted(batchKey))
            return null;

        if (batchKey.Contains("..") || batchKey.Contains('\0'))
            return null;

        // TrimEnd: Path.GetFullPath preserves a trailing separator when the input already has
        // one (e.g. Path.GetTempPath() always ends with '\'/'/'). Without this, the StartsWith
        // check below compares against a double separator and never matches, making Resolve
        // always return null whenever RootDirectory is configured with a trailing separator.
        var root = Path.GetFullPath(_rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var resolved = Path.GetFullPath(Path.Combine(root, batchKey));

        // Ensure the resolved path is actually inside the root
        if (!resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(resolved, root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return resolved;
    }
}
