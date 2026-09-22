namespace PS4Remarry.Core;

public sealed record RemarryOptions(
    string GamePkg,
    string UpdatePkg,
    string OutputDir,
    string WorkDir);

public sealed class RemarryPipeline
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator  _tools;

    public RemarryPipeline(ProcessRunner runner, ToolLocator tools)
    {
        _runner = runner;
        _tools  = tools;
    }

    public async Task<string> RunAsync(
        RemarryOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

        // SAFETY: refuse to run unless the work dir is inside tools\Work.
        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            throw new InvalidOperationException(
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.");

        // SAFETY: log what we're about to wipe, in case it's ever non-empty.
        if (Directory.Exists(opts.WorkDir))
        {
            var (files, dirs, bytes) = SafePathGuard.MeasureDirectory(opts.WorkDir);
            if (files > 0 || dirs > 0)
                progress?.Report(ProgressReport.Log(
                    $"[safety] clearing work folder: {opts.WorkDir} " +
                    $"({files} files, {dirs} dirs, {Formatting.FormatBytes(bytes)})"));
        }

        Directory.CreateDirectory(opts.OutputDir);
        FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
        Directory.CreateDirectory(opts.WorkDir);

        var preexistingPkgs = new HashSet<string>(
            Directory.GetFiles(opts.OutputDir, "*.pkg"),
            StringComparer.OrdinalIgnoreCase);

        bool completedSuccessfully = false;

        try
        {
            progress?.Report(ProgressReport.Pct(2, "Reading TITLE_ID from game PKG..."));
            var titleId = PkgTitleIdReader.ReadTitleId(opts.GamePkg);
            progress?.Report(ProgressReport.Log($"  -> TITLE_ID = {titleId}"));

            var folderName = $"{titleId}-patch";
            var folderPath = Path.Combine(opts.WorkDir, folderName);
            var unpackPath = Path.Combine(opts.WorkDir, "Update-pkg");
            var gp4Path    = Path.Combine(opts.WorkDir, folderName + ".gp4");
            var buildDir   = Path.Combine(opts.WorkDir, "build");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(10, "Extracting update PKG..."));
            Directory.CreateDirectory(unpackPath);
            var rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[]
                {
                    "img_extract",
                    "--passcode", "00000000000000000000000000000000",
                    opts.UpdatePkg,
                    unpackPath
                },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"img_extract failed (exit {rc}).");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(40, "Merging Sc0 into Image0\\sce_sys..."));
            var image0 = Path.Combine(unpackPath, "Image0");
            var sc0    = Path.Combine(unpackPath, "Sc0");

            if (!Directory.Exists(image0))
            {
                if (Directory.Exists(sc0))
                {
                    Directory.Move(sc0, image0);
                    var sceSys2 = Path.Combine(image0, "sce_sys");
                    Directory.CreateDirectory(sceSys2);
                }
                else
                {
                    throw new DirectoryNotFoundException(
                        $"Expected '{image0}' after extraction — update PKG layout unexpected.");
                }
            }
            else if (Directory.Exists(sc0))
            {
                var sceSys = Path.Combine(image0, "sce_sys");
                Directory.CreateDirectory(sceSys);
                FileSystemUtil.CopyDirectory(sc0, sceSys);
                FileSystemUtil.TryDeleteDirectory(sc0);
            }

            if (!Directory.Exists(image0))
                throw new DirectoryNotFoundException(
                    $"Expected '{image0}' after extraction — update PKG layout unexpected.");

            FileSystemUtil.TryDeleteDirectory(folderPath);
            Directory.Move(image0, folderPath);
            FileSystemUtil.TryDeleteDirectory(unpackPath);

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(55, "Generating GP4 project..."));
            rc = await _runner.RunAsync(
                _tools.Gengp4Patch,
                new[] { folderPath },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"gengp4_patch failed (exit {rc}).");
            if (!File.Exists(gp4Path))
                throw new FileNotFoundException($"GP4 not produced: {gp4Path}");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(65, "Marrying update to base game..."));
            rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[]
                {
                    "gp4_proj_update",
                    "--app_path", opts.GamePkg,
                    gp4Path
                },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"gp4_proj_update failed (exit {rc}).");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(75,
                "Building remarried PKG (this can take a while)..."));
            Directory.CreateDirectory(buildDir);

            long expectedSize = 1;
            try
            {
                var updateInfo = new FileInfo(opts.UpdatePkg);
                if (updateInfo.Exists) expectedSize = (long)(updateInfo.Length * 1.05);
            }
            catch { }

            using (var monitor = new PkgBuildProgressMonitor(
                       buildDir, expectedSize, progress ?? new Progress<ProgressReport>(),
                       ct, ignoreFiles: null))
            {
                rc = await _runner.RunAsync(
                    _tools.OrbisPubCmd,
                    new[]
                    {
                        "img_create",
                        "--oformat", "pkg",
                        gp4Path,
                        buildDir
                    },
                    opts.WorkDir, progress, ct).ConfigureAwait(false);
            }

            if (rc != 0) throw new InvalidOperationException($"img_create failed (exit {rc}).");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(95, "Moving finished PKG into place..."));
            var builtPkg = Directory.GetFiles(buildDir, "*.pkg").FirstOrDefault();
            if (builtPkg is null)
                throw new InvalidOperationException(
                    "img_create succeeded but produced no .pkg file in the build folder.");

            var finalPath = Path.Combine(opts.OutputDir, Path.GetFileName(builtPkg));

            if (File.Exists(finalPath))
            {
                try { File.SetAttributes(finalPath, FileAttributes.Normal); } catch { }
                FileSystemUtil.TryDeleteFile(finalPath);
            }

            File.Move(builtPkg, finalPath);

            progress?.Report(ProgressReport.Pct(98, "Cleaning up..."));
            CleanupOutputLogs(opts.OutputDir);

            progress?.Report(ProgressReport.Pct(100, "Done."));
            completedSuccessfully = true;
            return finalPath;
        }
        finally
        {
            try
            {
                if (Directory.Exists(opts.WorkDir))
                    FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
            }
            catch { }

            if (!completedSuccessfully)
                CleanupPartialOutput(opts.OutputDir, preexistingPkgs);
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

    private static void CleanupPartialOutput(string outputDir, HashSet<string> preexisting)
    {
        try
        {
            if (!Directory.Exists(outputDir)) return;

            foreach (var file in Directory.GetFiles(outputDir, "*.pkg"))
            {
                if (preexisting.Contains(file)) continue;

                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch { }
            }

            foreach (var log in Directory.GetFiles(outputDir, "compare_delta.log"))
                FileSystemUtil.TryDeleteFile(log);
        }
        catch { }
    }

    private static void CleanupOutputLogs(string outputDir)
    {
        try
        {
            foreach (var f in Directory.GetFiles(outputDir, "compare_delta.log"))
                FileSystemUtil.TryDeleteFile(f);
        }
        catch { }
    }
}