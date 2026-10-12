namespace PS4Remarry.Core;

/// <summary>
/// Extracts files from a PS4 PKG by invoking
/// `orbis-pub-cmd img_extract --passcode ... "pkg":"internal/path" "output"`.
/// </summary>
public static class PkgFileExtractor
{
    public sealed record ExtractResult(
        bool Success,
        string? Error,
        string? ExtractedPath);

    /// <summary>
    /// Extracts a single file from a PKG.
    ///
    /// The file lands at <c>outputDirectory\&lt;internalPath&gt;</c>, i.e. its
    /// full path inside the PKG is recreated on disk. Pass a non-null
    /// <paramref name="stripPrefix"/> to remove a folder prefix from the
    /// destination path — this is what "Extract Folder" uses so that
    /// extracting <c>Image0/sce_sys/trophy</c> produces
    /// <c>&lt;output&gt;\trophy\...</c> instead of
    /// <c>&lt;output&gt;\Image0\sce_sys\trophy\...</c>.
    /// </summary>
    public static async Task<ExtractResult> ExtractOneAsync(
        string pkgPath,
        string internalPath,
        string outputDirectory,
        ToolLocator tools,
        ProcessRunner runner,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default,
        string? stripPrefix = null)
    {
        if (!File.Exists(pkgPath))
            return new ExtractResult(false, "Source PKG not found.", null);

        if (string.IsNullOrWhiteSpace(internalPath))
            return new ExtractResult(false, "No internal path specified.", null);

        Directory.CreateDirectory(outputDirectory);

        // The tool expects the source as "pkgfile:internal/path" with the
        // whole thing as a single argument. We pass it as one ArgumentList
        // entry so spaces in the pkg path are preserved.
        var source = $"{Path.GetFullPath(pkgPath)}:{internalPath.Replace('\\', '/')}";

        progress?.Report(ProgressReport.Log(
            $"[extract] {internalPath} -> {outputDirectory}"));

        int exitCode;
        try
        {
            exitCode = await runner.RunAsync(
                tools.OrbisPubCmd,
                new[]
                {
                    "img_extract",
                    "--passcode", "00000000000000000000000000000000",
                    source,
                    outputDirectory
                },
                tools.WorkDir,
                progress,
                ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new ExtractResult(false, "Cancelled.", null);
        }
        catch (Exception ex)
        {
            return new ExtractResult(false, ex.Message, null);
        }

        if (exitCode != 0)
            return new ExtractResult(false, $"img_extract failed (exit {exitCode}).", null);

        // Compute the destination path. If stripPrefix is given, remove it
        // from the front of the internal path before resolving.
        var relative = internalPath.Replace('\\', '/').TrimStart('/');

        if (!string.IsNullOrEmpty(stripPrefix))
        {
            var prefix = stripPrefix.Replace('\\', '/').TrimEnd('/') + "/";
            if (relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring(prefix.Length);
        }

        var extractedPath = Path.Combine(outputDirectory,
            relative.Replace('/', Path.DirectorySeparatorChar));

        return new ExtractResult(true, null, extractedPath);
    }
}