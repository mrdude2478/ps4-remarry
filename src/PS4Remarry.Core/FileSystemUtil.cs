namespace PS4Remarry.Core;

public static class FileSystemUtil
{
    /// <summary>Recursively copy a directory tree, creating dest if needed.</summary>
    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, dir);
            Directory.CreateDirectory(Path.Combine(dest, rel));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    /// <summary>
    /// Delete contents of a dir but keep the dir itself (creates it if missing).
    ///
    /// SAFETY: This method is the most destructive thing in the codebase, so
    /// it enforces two guards before touching anything:
    ///   1. SafePathGuard.AssertSafeToWipe() — refuses protected Windows folders
    ///      and drive roots.
    ///   2. If <paramref name="expectedRoot"/> is supplied, the target must be
    ///      inside it. The pipelines pass tools\Work here.
    /// </summary>
    public static void WipeDirectory(string dir, string? expectedRoot = null)
    {
        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); return; }

        SafePathGuard.AssertSafeToWipe(dir);

        if (!string.IsNullOrWhiteSpace(expectedRoot))
        {
            var fullTarget = Path.GetFullPath(dir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullRoot = Path.GetFullPath(expectedRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!fullTarget.StartsWith(fullRoot + Path.DirectorySeparatorChar,
                                       StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fullTarget, fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new SafePathGuard.UnsafePathException(
                    fullTarget,
                    $"path is not inside the expected work root ({fullRoot})");
            }
        }

        foreach (var f in Directory.GetFiles(dir))
        {
            try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); } catch { }
        }
        foreach (var d in Directory.GetDirectories(dir))
        {
            try { Directory.Delete(d, recursive: true); } catch { }
        }
    }

    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }
        catch { }
    }

    public static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }
}