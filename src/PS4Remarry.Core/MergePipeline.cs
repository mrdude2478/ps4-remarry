namespace PS4Remarry.Core;

public sealed record MergeOptions(
    string GamePkg,
    string UpdatePkg,
    string OutputDir,
    string WorkDir);

public sealed class MergePipeline
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator  _tools;

    public MergePipeline(ProcessRunner runner, ToolLocator tools)
    {
        _runner = runner;
        _tools  = tools;
    }

    public async Task<string> RunAsync(
        MergeOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

        // SAFETY: refuse to run unless the work dir is inside tools\Work.
        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            throw new InvalidOperationException(
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.");

        var gameFull   = Path.GetFullPath(opts.GamePkg);
        var updateFull = Path.GetFullPath(opts.UpdatePkg);

        if (string.Equals(gameFull, updateFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Game PKG and Update PKG point at the same file. Pick two different files.");

        if (!File.Exists(gameFull))
            throw new FileNotFoundException("Game PKG not found.", gameFull);
        if (!File.Exists(updateFull))
            throw new FileNotFoundException("Update PKG not found.", updateFull);

        if (Directory.Exists(opts.WorkDir))
        {
            var (files, dirs, bytes) = SafePathGuard.MeasureDirectory(opts.WorkDir);
            if (files > 0 || dirs > 0)
                progress?.Report(ProgressReport.Log(
                    $"[safety] clearing work folder: {opts.WorkDir} " +
                    $"({files} files, {dirs} dirs, {Formatting.FormatBytes(bytes)})"));
        }

        Directory.CreateDirectory(opts.WorkDir);
        FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
        Directory.CreateDirectory(opts.WorkDir);

        try
        {
            var gameUnpack   = Path.Combine(opts.WorkDir, "Game-pkg");
            var updateUnpack = Path.Combine(opts.WorkDir, "Update-pkg");
            Directory.CreateDirectory(gameUnpack);
            Directory.CreateDirectory(updateUnpack);

            progress?.Report(ProgressReport.Pct(2, "Reading TITLE_ID..."));
            var titleId = PkgTitleIdReader.ReadTitleId(gameFull);
            progress?.Report(ProgressReport.Log($"  -> TITLE_ID = {titleId}"));

            var mergeOutDir = Path.Combine(opts.OutputDir, $"Merged-{titleId}");
            Directory.CreateDirectory(mergeOutDir);

            var outFull = Path.GetFullPath(mergeOutDir);
            if (string.Equals(Path.GetDirectoryName(gameFull), outFull, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetDirectoryName(updateFull), outFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Output folder would collide with an input folder. Choose a different output path.");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(8, "Extracting game PKG..."));
            var rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[] { "img_extract", "--passcode", "00000000000000000000000000000000",
                        gameFull, gameUnpack },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"img_extract (game) failed (exit {rc}).");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(30, "Extracting update PKG..."));
            rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[] { "img_extract", "--passcode", "00000000000000000000000000000000",
                        updateFull, updateUnpack },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"img_extract (update) failed (exit {rc}).");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(50, "Merging Sc0 into sce_sys..."));
            MoveSc0IntoImage0(gameUnpack);
            MoveSc0IntoImage0(updateUnpack);

            var gameImage0 = Path.Combine(gameUnpack, "Image0");
            if (!Directory.Exists(gameImage0))
                throw new DirectoryNotFoundException(
                    $"Expected '{gameImage0}' after extracting game — layout unexpected.");

            var appFolder = Path.Combine(opts.WorkDir, "Game-app");
            FileSystemUtil.TryDeleteDirectory(appFolder);
            Directory.Move(gameImage0, appFolder);

            progress?.Report(ProgressReport.Pct(60, "Overlaying update files..."));
            var updateImage0 = Path.Combine(updateUnpack, "Image0");
            if (Directory.Exists(updateImage0))
                FileSystemUtil.CopyDirectory(updateImage0, appFolder);

            FileSystemUtil.TryDeleteDirectory(gameUnpack);
            FileSystemUtil.TryDeleteDirectory(updateUnpack);

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(70, "Generating GP4 project..."));
            rc = await _runner.RunAsync(
                _tools.Gengp4App,
                new[] { appFolder },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"gengp4_app failed (exit {rc}).");

            var gp4Path = Path.Combine(opts.WorkDir, "Game-app.gp4");
            if (!File.Exists(gp4Path))
                throw new FileNotFoundException($"GP4 not produced: {gp4Path}");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(80,
                "Building merged PKG (this can take a while)..."));
            rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[] { "img_create", "--oformat", "pkg", gp4Path, mergeOutDir },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"img_create failed (exit {rc}).");

            progress?.Report(ProgressReport.Pct(98, "Cleaning up..."));
            foreach (var f in Directory.GetFiles(mergeOutDir, "compare_delta.log"))
                FileSystemUtil.TryDeleteFile(f);

            progress?.Report(ProgressReport.Pct(100, "Done."));
            return mergeOutDir;
        }
        finally
        {
            try
            {
                if (Directory.Exists(opts.WorkDir))
                    FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
            }
            catch { }
        }
    }

    private static bool IsInsideWorkRoot(string candidate, string root)
    {
        var c = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var r = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return c.Equals(r, StringComparison.OrdinalIgnoreCase)
            || c.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void MoveSc0IntoImage0(string unpackDir)
    {
        var sc0    = Path.Combine(unpackDir, "Sc0");
        var sceSys = Path.Combine(unpackDir, "Image0", "sce_sys");

        if (!Directory.Exists(sc0)) return;

        Directory.CreateDirectory(sceSys);
        FileSystemUtil.CopyDirectory(sc0, sceSys);
        FileSystemUtil.TryDeleteDirectory(sc0);
    }
}