namespace PS4Remarry.Core;

/// <summary>Finds .pkg files in dropped files/folders (recursively) and reads their param.sfo.</summary>
public static class PkgFolderScanner
{
    public static List<string> FindPkgFiles(IEnumerable<string> paths)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        foreach (var p in paths)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;

            if (Directory.Exists(p))
            {
                foreach (var f in Directory.EnumerateFiles(p, "*.pkg", opts))
                    if (IsPkg(f)) found.Add(Path.GetFullPath(f));
            }
            else if (File.Exists(p) && IsPkg(p))
            {
                found.Add(Path.GetFullPath(p));
            }
        }

        return found.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsPkg(string f) =>
        Path.GetExtension(f).Equals(".pkg", StringComparison.OrdinalIgnoreCase);

    public static PkgScanResult Scan(
        IEnumerable<string> paths, IProgress<PkgScanProgress>? progress, CancellationToken ct)
    {
        var result = new PkgScanResult();
        var files = FindPkgFiles(paths);
        result.FilesFound = files.Count;

        for (int i = 0; i < files.Count; i++)
        {
            if (ct.IsCancellationRequested) { result.Cancelled = true; break; }

            progress?.Report(new PkgScanProgress(i, files.Count, Path.GetFileName(files[i])));
            try
            {
                result.Metas.Add(PkgMetaReader.Read(files[i]));
            }
            catch (Exception ex)
            {
                result.Failed.Add($"{Path.GetFileName(files[i])}  ({ex.Message})");
            }
        }

        progress?.Report(new PkgScanProgress(files.Count, files.Count, ""));
        return result;
    }
}
