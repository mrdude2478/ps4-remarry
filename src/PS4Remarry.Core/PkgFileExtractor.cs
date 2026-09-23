namespace PS4Remarry.Core;

/// <summary>
/// Extracts a single file from a PS4 PKG by invoking
/// `orbis-pub-cmd img_extract --passcode ... "pkg":"internal/path" "output"`.
/// </summary>
public static class PkgFileExtractor
{
    public sealed record ExtractResult(
        bool Success,
        string? Error,
        string? ExtractedPath);

    public static async Task<ExtractResult> ExtractOneAsync(
        string pkgPath,
        string internalPath,
        string outputDirectory,
        ToolLocator tools,
        ProcessRunner runner,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
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

        // The file should now exist under outputDirectory at the same
        // relative path it had inside the PKG. Build that path so the caller
        // can show it to the user.
        var relative = internalPath.Replace('\\', '/').TrimStart('/');
        var extractedPath = Path.Combine(outputDirectory,
            relative.Replace('/', Path.DirectorySeparatorChar));

        return new ExtractResult(true, null, extractedPath);
    }
}