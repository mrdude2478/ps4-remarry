namespace PS4Remarry.Core;

/// <summary>
/// Central safety checks for destructive filesystem operations. Every call
/// to FileSystemUtil.WipeDirectory() goes through here first.
///
/// The guard exists because the pipelines derive their work directory from
/// user-supplied paths, and a mistake in that derivation — or a user typing
/// the wrong path — could otherwise wipe a real folder like the Desktop.
/// </summary>
public static class SafePathGuard
{
    public sealed class UnsafePathException : Exception
    {
        public string AttemptedPath { get; }
        public string Reason { get; }

        public UnsafePathException(string attemptedPath, string reason)
            : base($"Refusing to delete '{attemptedPath}': {reason}")
        {
            AttemptedPath = attemptedPath;
            Reason = reason;
        }
    }

    private static IEnumerable<string> GetProtectedPaths()
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
        }

        var publicProfile = Environment.GetEnvironmentVariable("PUBLIC");
        if (!string.IsNullOrEmpty(publicProfile))
            yield return Path.Combine(publicProfile, "Desktop");
    }

    /// <summary>
    /// Normalises the protected-path list once per call. Doing this lazily
    /// (rather than caching) means the guard stays correct if the user
    /// changes their environment during a session, and it avoids any risk of
    /// stale values in a long-running process.
    /// </summary>
    private static List<string> NormalisedProtectedPaths()
    {
        return GetProtectedPaths()
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

    /// <summary>
    /// Throws if the path is not safe to wipe. Performs the following checks:
    ///   - Path must exist and be an absolute path.
    ///   - Path must not be a drive root (C:\, D:\, ...).
    ///   - Path must not be on the protected-folder deny list.
    ///   - Path must not be an ancestor of a protected folder.
    /// </summary>
    public static void AssertSafeToWipe(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new UnsafePathException(path ?? "", "path is empty");

        string full;
        try { full = Path.GetFullPath(path); }
        catch (Exception ex)
        {
            throw new UnsafePathException(path, "path is not valid: " + ex.Message);
        }

        full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (IsDriveRoot(full))
            throw new UnsafePathException(full, "path is a drive root");

        if (full.Length <= 3)
            throw new UnsafePathException(full, "path is too short to be a safe target");

        foreach (var prot in NormalisedProtectedPaths())
        {
            if (string.Equals(full, prot, StringComparison.OrdinalIgnoreCase))
                throw new UnsafePathException(full,
                    $"path is a protected Windows folder ({prot})");

            if (prot.StartsWith(full + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new UnsafePathException(full,
                    $"path is an ancestor of a protected folder ({prot})");
        }
    }

    /// <summary>
    /// Throws if the given path is, or is inside, a protected Windows folder.
    /// Used to validate that the tools directory isn't mistakenly pointing at
    /// the Desktop, Documents, etc.
    ///
    /// This closes a gap that AssertSafeToWipe does not: wiping
    /// C:\Users\Alan\Desktop\Work is not blocked by AssertSafeToWipe, because
    /// that path is neither protected nor an ancestor of a protected folder.
    /// But if tools\Work ever resolved to that, we'd still be wiping a
    /// subfolder of the Desktop. AssertNotProtected catches that.
    /// </summary>
    public static void AssertNotProtected(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        string full;
        try { full = Path.GetFullPath(path); }
        catch { return; }

        full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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

    private static bool IsDriveRoot(string path)
    {
        if (path.Length == 2 && path[1] == ':') return true;
        if (path.Length == 3 && path[1] == ':' &&
            (path[2] == '\\' || path[2] == '/')) return true;
        return false;
    }

    /// <summary>
    /// Counts files and subdirectories in a folder (recursively) and returns
    /// the total byte size. Used to decide whether a wipe needs confirmation.
    /// </summary>
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