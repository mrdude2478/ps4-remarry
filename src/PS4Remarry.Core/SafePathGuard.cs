namespace PS4Remarry.Core;

/// <summary>
/// Central safety checks for destructive filesystem operations. Every call
/// to FileSystemUtil.WipeDirectory() goes through here first.
///
/// Three distinct concepts:
///
///   * Protected root: a well-known user folder (Desktop, Documents, ...)
///     or a drive root. Wiping one destroys user data, so we refuse.
///
///   * Protected descendant: a path that lives inside a protected root.
///     Some of those are legitimate work areas (tools\Work under a project
///     on the Desktop), others aren't. Callers decide which is allowed.
///
///   * Safe work root: the tools\Work subtree, which is the only place
///     any pipeline is allowed to recursively delete.
/// </summary>
public static class SafePathGuard
{
    public sealed class UnsafePathException : Exception
    {
        public string AttemptedPath { get; }
        public string Reason { get; }

        public UnsafePathException(string attemptedPath, string reason)
            : base($"Refusing to operate on '{attemptedPath}': {reason}")
        {
            AttemptedPath = attemptedPath;
            Reason = reason;
        }
    }

    // ---------------------------------------------------------------
    // Protected path enumeration
    // ---------------------------------------------------------------

    private static IEnumerable<string> GetProtectedPathsRaw()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            yield return Path.Combine(userProfile, "Downloads");
            yield return Path.Combine(userProfile, "Desktop");
            yield return Path.Combine(userProfile, "Documents");
            yield return Path.Combine(userProfile, "Pictures");
            yield return Path.Combine(userProfile, "Music");
            yield return Path.Combine(userProfile, "Videos");
            yield return Path.Combine(userProfile, "OneDrive");
        }

        var publicProfile = Environment.GetEnvironmentVariable("PUBLIC");
        if (!string.IsNullOrEmpty(publicProfile))
            yield return Path.Combine(publicProfile, "Desktop");

        // Drive roots.
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
                yield return drive.RootDirectory.FullName;
        }
    }

    private static List<string> NormalisedProtectedPaths()
    {
        return GetProtectedPathsRaw()
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p =>
            {
                try
                {
                    return Path.GetFullPath(p)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                catch { return ""; }
            })
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Normalise(string path)
    {
        try
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path ?? "";
        }
    }

    // ---------------------------------------------------------------
    // Public checks
    // ---------------------------------------------------------------

    /// <summary>
    /// True if the given path is exactly one of the well-known user
    /// folders (Desktop, Documents, Downloads, ...) or a drive root.
    /// Subfolders of those return false.
    /// </summary>
    public static bool IsProtectedRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var full = Normalise(path);
        return NormalisedProtectedPaths()
            .Any(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Throws if the path is a protected root. Used wherever we accept a
    /// user-chosen folder (output, work, etc.) and want to refuse the
    /// handful of folders that must never be used directly.
    /// </summary>
    public static void AssertNotProtectedRoot(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        if (IsProtectedRoot(path))
            throw new UnsafePathException(path,
                $"{label} cannot be a protected Windows folder " +
                "(Desktop, Documents, Downloads, drive root, ...). " +
                "Please create a subfolder and use that instead.");
    }

    /// <summary>
    /// Throws if the given path is, or is inside, a protected Windows
    /// folder. Used for the tools folder, which must not live on the
    /// Desktop or in Documents even inside a subfolder.
    /// </summary>
    public static void AssertNotProtected(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var full = Normalise(path);

        foreach (var prot in NormalisedProtectedPaths())
        {
            if (string.Equals(full, prot, StringComparison.OrdinalIgnoreCase))
                throw new UnsafePathException(full,
                    $"path is a protected Windows folder ({prot})");

            if (full.StartsWith(prot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new UnsafePathException(full,
                    $"path is inside a protected Windows folder ({prot})");
        }
    }

    /// <summary>
    /// The full check that WipeDirectory performs before deleting. Refuses:
    ///   - empty or whitespace paths
    ///   - drive roots
    ///   - protected roots (Desktop itself, not a subfolder)
    ///   - paths shorter than 4 characters
    /// </summary>
    public static void AssertSafeToWipe(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new UnsafePathException(path ?? "", "path is empty");

        var full = Normalise(path);

        if (IsDriveRoot(full))
            throw new UnsafePathException(full, "path is a drive root");

        if (full.Length < 4)
            throw new UnsafePathException(full, "path is too short to be safe");

        if (IsProtectedRoot(full))
            throw new UnsafePathException(full,
                "path is a protected Windows folder (Desktop, Documents, ...)");
    }

    private static bool IsDriveRoot(string path)
    {
        if (path.Length == 2 && path[1] == ':') return true;
        if (path.Length == 3 && path[1] == ':' &&
            (path[2] == '\\' || path[2] == '/')) return true;
        return false;
    }

    // ---------------------------------------------------------------
    // Measurement (used for confirmation dialogs)
    // ---------------------------------------------------------------

    public static (int FileCount, int DirCount, long TotalBytes) MeasureDirectory(string path)
    {
        int files = 0, dirs = 0;
        long bytes = 0;

        try
        {
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                files++;
                try { bytes += new FileInfo(f).Length; } catch { }
            }

            foreach (var _ in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
                dirs++;
        }
        catch { }

        return (files, dirs, bytes);
    }
}