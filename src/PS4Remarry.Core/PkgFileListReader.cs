using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Reads the list of files inside a PS4 PKG by running
/// `orbis-pub-cmd.exe img_file_list` and parsing the output.
///
/// The process output goes straight to a StringBuilder the caller can
/// inspect. Parsing happens afterwards, on the accumulated text, so the
/// per-line UI callbacks don't have to do any bookkeeping.
/// </summary>
public static class PkgFileListReader
{
    public sealed record Entry(string FullPath, long Size);

    public sealed class TreeNode
    {
        public string Name { get; }
        public string FullPath { get; }
        public bool IsFolder { get; }
        public long Size { get; }
        public List<TreeNode> Children { get; } = new();

        public TreeNode(string name, string fullPath, bool isFolder, long size)
        {
            Name = name;
            FullPath = fullPath;
            IsFolder = isFolder;
            Size = size;
        }
    }

    public sealed record ListResult(
        bool Success,
        string? Error,
        TreeNode? Root,
        int FileCount,
        long TotalBytes);

    /// <summary>
    /// Runs img_file_list and streams every line of its output to the
    /// supplied StringBuilder. Does not parse; call
    /// <see cref="ParseFromLines"/> afterwards.
    /// </summary>
    public static async Task<(bool Success, string? Error)> RunImgFileListAsync(
        string pkgPath,
        ToolLocator tools,
        ProcessRunner runner,
        StringBuilder capture,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        var full = Path.GetFullPath(pkgPath);
        if (!File.Exists(full))
            return (false, "Source PKG not found.");

        var captureProgress = new Progress<ProgressReport>(r =>
        {
            if (!string.IsNullOrEmpty(r.Message))
            {
                capture.AppendLine(r.Message);

                // Only forward log lines that are useful. Filter out our own
                // prompt echo to reduce noise; still capture it above.
                if (!r.Message.StartsWith(">"))
                    progress?.Report(r);
            }
            else if (r.Percent is not null)
            {
                progress?.Report(r);
            }
        });

        progress?.Report(ProgressReport.Pct(0, "Running img_file_list..."));

        int exitCode;
        try
        {
            exitCode = await runner.RunAsync(
                tools.OrbisPubCmd,
                new[]
                {
                    "img_file_list",
                    "--passcode", "00000000000000000000000000000000",
                    full
                },
                tools.WorkDir,
                captureProgress,
                ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return (false, "Cancelled.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        if (exitCode != 0)
            return (false, $"img_file_list failed (exit {exitCode}).");

        return (true, null);
    }

    /// <summary>
    /// Parses the raw text of img_file_list's output into a tree.
    ///
    /// The tool emits one file path per line, with NO size column:
    ///     Image0/sce_module/libc.prx
    ///     Image0/sce_discmap.plt
    ///     Sc0
    ///
    /// We still handle a trailing size if one is present (older or future
    /// tool builds may emit it), but for the current SDK tools the size is
    /// always 0. The UI shows "unknown" in that case.
    /// </summary>
    public static ListResult ParseFromLines(IEnumerable<string> rawLines)
    {
        var entries = new List<Entry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in rawLines)
        {
            if (raw is null) continue;
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("[")) continue;
            if (line.StartsWith(">")) continue;
            if (line.StartsWith("Please wait")) continue;
            if (line.StartsWith("===")) continue;
            if (line.StartsWith("---")) continue;
            if (line.StartsWith("img_file_list")) continue;
            if (line.StartsWith("Source:")) continue;

            long size = 0;
            string pathPart = line;

            int lastSpace = line.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                var maybeSize = line.Substring(lastSpace + 1).Trim();
                if (long.TryParse(maybeSize, out long parsedSize))
                {
                    pathPart = line.Substring(0, lastSpace).Trim();
                    size = parsedSize;
                }
            }

            if (string.IsNullOrWhiteSpace(pathPart)) continue;
            if (pathPart.Contains(' ')) continue;

            pathPart = pathPart.Replace('\\', '/').TrimStart('/');

            if (!pathPart.Contains('/') && !pathPart.Contains('.'))
                continue;

            // Skip duplicates. img_file_list can emit the same path multiple
            // times because the PKG has both an Sc0 and an Image0 folder, and
            // some metadata files appear in both. We only want one tree node
            // for each unique path.
            if (!seen.Add(pathPart))
                continue;

            entries.Add(new Entry(pathPart, size));
        }

        if (entries.Count == 0)
            return new ListResult(false,
                "No parseable file entries found in img_file_list output.",
                null, 0, 0);

        var root = BuildTree(entries, out long totalBytes);

        return new ListResult(true, null, root, entries.Count, totalBytes);
    }

    private static TreeNode BuildTree(List<Entry> entries, out long totalBytes)
    {
        var root = new TreeNode("(root)", "", isFolder: true, size: 0);
        var index = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

        totalBytes = 0;

        foreach (var entry in entries)
        {
            var parts = entry.FullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var current = root;
            var accumulated = "";

            for (int i = 0; i < parts.Length - 1; i++)
            {
                accumulated = accumulated.Length == 0
                    ? parts[i]
                    : accumulated + "/" + parts[i];

                if (!index.TryGetValue(accumulated, out var folder))
                {
                    folder = new TreeNode(parts[i], accumulated, isFolder: true, size: 0);
                    index[accumulated] = folder;
                    current.Children.Add(folder);
                }
                current = folder;
            }

            var fileName = parts[^1];
            var filePath = accumulated.Length == 0 ? fileName : accumulated + "/" + fileName;
            var file = new TreeNode(fileName, filePath, isFolder: false, size: entry.Size);
            current.Children.Add(file);
            totalBytes += entry.Size;
        }

        RollUpSizes(root);
        SortTree(root);
        return root;
    }

    private static long RollUpSizes(TreeNode node)
    {
        if (!node.IsFolder) return node.Size;

        long sum = 0;
        foreach (var child in node.Children)
            sum += RollUpSizes(child);
        return sum;
    }

    private static void SortTree(TreeNode node)
    {
        node.Children.Sort((a, b) =>
        {
            if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var child in node.Children)
            if (child.IsFolder) SortTree(child);
    }

    /// <summary>
    /// Runs `orbis-pub-cmd img_info` and returns the raw output as a list of
    /// lines. This is instant — it only reads the PKG header, not the contents.
    /// </summary>
    public static async Task<(bool Success, string? Error, List<string> Lines)> RunImgInfoAsync(
        string pkgPath,
        ToolLocator tools,
        ProcessRunner runner,
        CancellationToken ct = default)
    {
        var full = Path.GetFullPath(pkgPath);
        if (!File.Exists(full))
            return (false, "Source PKG not found.", new List<string>());

        var lines = new List<string>();
        var capture = new Progress<ProgressReport>(r =>
        {
            if (!string.IsNullOrEmpty(r.Message))
                lines.Add(r.Message);
        });

        int exitCode;
        try
        {
            exitCode = await runner.RunAsync(
                tools.OrbisPubCmd,
                new[] { "img_info", full },
                tools.WorkDir,
                capture,
                ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return (false, "Cancelled.", lines);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, lines);
        }

        if (exitCode != 0)
            return (false, $"img_info failed (exit {exitCode}).", lines);

        return (true, null, lines);
    }

    /// <summary>
    /// Extracts the [Params] section from img_info output. Returns the lines
    /// between the [Params] header and the next [Section] header, with any
    /// bracketed header itself included for context.
    /// </summary>
    public static List<string> ExtractParamsSection(IEnumerable<string> imgInfoLines)
    {
        var result = new List<string>();
        bool inParams = false;

        foreach (var raw in imgInfoLines)
        {
            if (raw is null) continue;
            var line = raw.TrimEnd();

            if (line.StartsWith("["))
            {
                // We've hit a new section header.
                inParams = line.Equals("[Params]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inParams) continue;
            if (line.Length == 0) continue;

            result.Add(line);
        }

        return result;
    }
}