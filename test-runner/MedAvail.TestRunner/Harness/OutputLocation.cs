using System;
using System.IO;

namespace MedAvail.TestRunner.Harness;

/// <summary>
/// Resolves where test reports are written. Precedence:
///   1. explicit override (CLI --output or Sql/Test config) if provided
///   2. the repository root (nearest ancestor containing the .sln), + /test-results
///   3. the executable directory + /test-results (fallback)
/// </summary>
public static class OutputLocation
{
    public static string Resolve(string? overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
            return Path.GetFullPath(overridePath!);

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        var baseDir = repoRoot ?? AppContext.BaseDirectory;
        return Path.Combine(baseDir, "test-results");
    }

    private static string? FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            // Repo root = first ancestor that contains a .sln file.
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
