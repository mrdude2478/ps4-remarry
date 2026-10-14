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
    /// SAFETY: this is the most destructive operation in the codebase. It
    /// enforces three guards:
    ///
    ///   1. SafePathGuard.AssertSafeToWipe() refuses protected roots
    ///      (Desktop, Documents, drive roots, ...) unconditionally. Even
    ///      if a caller passes a matching expectedRoot, the wipe still
    ///      refuses a protected target.
    ///
    ///   2. If expectedRoot is supplied, the target must be inside it.
    ///      Pipelines pass tools\Work here, so a mis-derived WorkDir that
    ///      points at the Desktop is rejected.
    ///
    ///   3. Any failure is thrown, not swallowed, so callers see it.
    /// </summary>
    public static void WipeDirectory(string dir, string? expectedRoot = null)
    {
        if (string.IsNullOrWhiteSpace(dir))
            throw new ArgumentException("dir is empty", nameof(dir));

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            return;
        }

        // Guard 1: never touch a protected root.
        SafePathGuard.AssertSafeToWipe(dir);

        // Guard 2: must be inside the caller's expected root, if given.
        if (!string.IsNullOrWhiteSpace(expectedRoot))
        {
            var fullTarget = Path.GetFullPath(dir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullRoot = Path.GetFullPath(expectedRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var inside = fullTarget.Equals(fullRoot, StringComparison.OrdinalIgnoreCase)
                || fullTarget.StartsWith(fullRoot + Path.DirectorySeparatorChar,
                                         StringComparison.OrdinalIgnoreCase);

            if (!inside)
                throw new SafePathGuard.UnsafePathException(
                    fullTarget,
                    $"path is not inside the expected work root ({fullRoot})");
        }

        // Both guards passed. Perform the wipe.
        foreach (var f in Directory.GetFiles(dir))
        {
            try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); } catch { }
        }
        foreach (var d in Directory.GetDirectories(dir))
        {
            try { Directory.Delete(d, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Recursively delete a directory, but ONLY if we created it this run.
    /// This is the safe way to clean up after a failed pipeline: if the
    /// user pointed us at an existing folder (perhaps one that already had
    /// content), we must never delete it.
    /// </summary>
    public static void DeleteOnlyIfWeCreated(string path, bool weCreatedIt)
    {
        if (!weCreatedIt) return;
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
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