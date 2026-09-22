namespace PS4Remarry.Core;

public sealed record RepackExtractOptions(
    string SourcePkg,
    string ExtractFolder,
    string WorkDir);

public sealed record RepackBuildOptions(
    string ExtractFolder,
    string OutputFolder,
    string WorkDir);

public sealed record RepackInspection(
    string SourcePkg,
    long FileSize,
    string TitleId,
    string ContentId,
    string Category,
    string CategoryLabel,
    bool KeystoneNeeded);

/// <summary>
/// Extracts a PS4 PKG into a folder ready for modification and repacking,
/// and rebuilds a new PKG from that folder. Games and updates only.
/// </summary>
public sealed class RepackPipeline
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator  _tools;

    public RepackPipeline(ProcessRunner runner, ToolLocator tools)
    {
        _runner = runner;
        _tools  = tools;
    }

    public RepackInspection Inspect(string sourcePkg)
    {
        var full = Path.GetFullPath(sourcePkg);
        if (!File.Exists(full))
            throw new FileNotFoundException("Source PKG not found.", full);

        var fi = new FileInfo(full);

        string titleId = "";
        string contentId = "";
        string category = "";

        try { titleId = PkgTitleIdReader.ReadTitleId(full); } catch { }

        try
        {
            var sfo = SfoParser.ParseToDictionary(full);
            if (sfo.TryGetValue("CONTENT_ID", out var cid)) contentId = cid.Trim();
            if (sfo.TryGetValue("CATEGORY",   out var cat)) category  = cat.Trim();
        }
        catch (SfoParseException) { }

        var (label, needsKeystone) = DescribeCategory(category);

        return new RepackInspection(full, fi.Length, titleId, contentId, category, label, needsKeystone);
    }

    private static (string Label, bool KeystoneNeeded) DescribeCategory(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "gd"  => ("Base game (Game Data)",              true),
            "gp"  => ("Update (Game Patch)",                false),
            "gde" => ("DLC (Game Data Ext)",                false),
            "gdk" => ("DLC (Game Data K)",                  false),
            "gda" => ("DLC (Game Data A)",                  false),
            "ac"  => ("Additional Content",                 false),
            ""    => ("Unknown (no CATEGORY in SFO)",       false),
            _     => ($"Unknown category '{category}'",     false),
        };
    }

    public async Task ExtractAsync(
        RepackExtractOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

        // SAFETY: refuse to run unless the work dir is inside tools\Work.
        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            throw new InvalidOperationException(
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.");

        var sourceFull = Path.GetFullPath(opts.SourcePkg);
        if (!File.Exists(sourceFull))
            throw new FileNotFoundException("Source PKG not found.", sourceFull);

        var extractFull = Path.GetFullPath(opts.ExtractFolder);

        if (Directory.Exists(extractFull) &&
            Directory.EnumerateFileSystemEntries(extractFull).Any())
            throw new InvalidOperationException(
                $"Extract folder already exists and is not empty: {extractFull}\n" +
                "Choose an empty folder or delete the existing one first.");

        progress?.Report(ProgressReport.Pct(2, "Inspecting source PKG..."));
        var inspection = Inspect(sourceFull);
        progress?.Report(ProgressReport.Log($"  TITLE_ID:  {inspection.TitleId}"));
        progress?.Report(ProgressReport.Log($"  CATEGORY:  {inspection.Category} ({inspection.CategoryLabel})"));
        progress?.Report(ProgressReport.Log($"  Keystone:  {(inspection.KeystoneNeeded ? "required" : "not required")}"));

        var workFull = Path.GetFullPath(opts.WorkDir);

        if (Directory.Exists(workFull))
        {
            var (files, dirs, bytes) = SafePathGuard.MeasureDirectory(workFull);
            if (files > 0 || dirs > 0)
                progress?.Report(ProgressReport.Log(
                    $"[safety] clearing work folder: {workFull} " +
                    $"({files} files, {dirs} dirs, {Formatting.FormatBytes(bytes)})"));
        }

        FileSystemUtil.WipeDirectory(workFull, _tools.WorkDir);
        Directory.CreateDirectory(workFull);
        Directory.CreateDirectory(extractFull);

        try
        {
            var tempExtract = Path.Combine(workFull, "Extract");
            Directory.CreateDirectory(tempExtract);

            progress?.Report(ProgressReport.Pct(10, "Extracting PKG..."));
            var rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[]
                {
                    "img_extract",
                    "--passcode", "00000000000000000000000000000000",
                    sourceFull,
                    tempExtract
                },
                workFull, progress, ct).ConfigureAwait(false);
            if (rc != 0) throw new InvalidOperationException($"img_extract failed (exit {rc}).");

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(50, "Merging Sc0 into Image0\\sce_sys..."));
            var image0 = Path.Combine(tempExtract, "Image0");
            var sc0    = Path.Combine(tempExtract, "Sc0");

            if (!Directory.Exists(image0))
                throw new DirectoryNotFoundException(
                    $"Expected '{image0}' after extraction — PKG layout unexpected.");

            if (Directory.Exists(sc0))
            {
                var sceSys = Path.Combine(image0, "sce_sys");
                Directory.CreateDirectory(sceSys);
                FileSystemUtil.CopyDirectory(sc0, sceSys);
                FileSystemUtil.TryDeleteDirectory(sc0);
            }

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(70, "Moving contents into place..."));
            MoveDirectoryContents(image0, extractFull);
            FileSystemUtil.TryDeleteDirectory(tempExtract);

            var sceSysPath   = Path.Combine(extractFull, "sce_sys");
            var paramSfoPath = Path.Combine(sceSysPath, "param.sfo");
            var icon0Path    = Path.Combine(sceSysPath, "icon0.png");
            var keystonePath = Path.Combine(sceSysPath, "keystone");

            bool paramSfoExists = File.Exists(paramSfoPath);
            bool icon0Exists    = File.Exists(icon0Path);
            bool keystoneExists = File.Exists(keystonePath);

            progress?.Report(ProgressReport.Log(""));
            progress?.Report(ProgressReport.Log("=== Extract summary ==="));
            progress?.Report(ProgressReport.Log($"  param.sfo:  {(paramSfoExists ? "present" : "MISSING")}"));
            progress?.Report(ProgressReport.Log($"  icon0.png:  {(icon0Exists    ? "present" : "not present (optional)")}"));
            progress?.Report(ProgressReport.Log($"  keystone:   {(keystoneExists ? "present" : inspection.KeystoneNeeded ? "MISSING (required for base game!)" : "not present (OK for update/DLC)")}"));

            if (inspection.KeystoneNeeded && !keystoneExists)
            {
                progress?.Report(ProgressReport.Log(""));
                progress?.Report(ProgressReport.Log("[warn] This is a base game but its keystone was not found."));
                progress?.Report(ProgressReport.Log("[warn] Repacking without a keystone will break save data."));
            }

            if (!paramSfoExists)
            {
                progress?.Report(ProgressReport.Log(""));
                progress?.Report(ProgressReport.Log("[warn] param.sfo is missing. Repack will fail without it."));
            }

            var log = new RepackExtractLog
            {
                SourcePkg      = sourceFull,
                SourceFileSize = inspection.FileSize,
                TitleId        = inspection.TitleId,
                ContentId      = inspection.ContentId,
                Category       = inspection.Category,
                CategoryLabel  = inspection.CategoryLabel,
                KeystoneNeeded = inspection.KeystoneNeeded,
                KeystoneFound  = keystoneExists,
                ExtractedAt    = DateTime.Now,
                ExtractTool    = "PS4 Remarry (orbis-pub-cmd 3.87)",
            };
            log.Write(extractFull);

            progress?.Report(ProgressReport.Log($"  Sidecar log: {RepackExtractLog.FileName}"));
            progress?.Report(ProgressReport.Log("======================="));
            progress?.Report(ProgressReport.Log(""));
            progress?.Report(ProgressReport.Log("Extraction complete. Modify the folder freely,"));
            progress?.Report(ProgressReport.Log("then click Repack to build a new PKG."));
            progress?.Report(ProgressReport.Pct(100, "Done."));
        }
        catch
        {
            try { FileSystemUtil.TryDeleteDirectory(extractFull); } catch { }
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(workFull))
                    FileSystemUtil.WipeDirectory(workFull, _tools.WorkDir);
            }
            catch { }
        }
    }

    public async Task<string> RepackAsync(
        RepackBuildOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

        // SAFETY: refuse to run unless the work dir is inside tools\Work.
        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            throw new InvalidOperationException(
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.");

        var extractFull = Path.GetFullPath(opts.ExtractFolder);
        if (!Directory.Exists(extractFull))
            throw new DirectoryNotFoundException("Extract folder not found: " + extractFull);

        var outputFull = Path.GetFullPath(opts.OutputFolder);
        Directory.CreateDirectory(outputFull);

        var log = RepackExtractLog.TryRead(extractFull);
        string category;
        string categoryLabel;
        bool keystoneNeeded;

        if (log is not null)
        {
            category       = log.Category;
            categoryLabel  = log.CategoryLabel;
            keystoneNeeded = log.KeystoneNeeded;

            progress?.Report(ProgressReport.Log(
                $"[repack] sidecar log says CATEGORY={category} ({categoryLabel}), " +
                $"keystone {(keystoneNeeded ? "required" : "not required")}"));
        }
        else
        {
            var paramSfo = Path.Combine(extractFull, "sce_sys", "param.sfo");
            category = "";
            if (File.Exists(paramSfo))
            {
                try
                {
                    var sfo = SfoParser.ParseToDictionary(paramSfo);
                    if (sfo.TryGetValue("CATEGORY", out var c)) category = c.Trim();
                }
                catch { }
            }
            var d = DescribeCategory(category);
            categoryLabel  = d.Label;
            keystoneNeeded = d.KeystoneNeeded;

            progress?.Report(ProgressReport.Log(
                $"[repack] no sidecar log; read CATEGORY={category} ({categoryLabel}) from param.sfo"));
        }

        var keystonePath = Path.Combine(extractFull, "sce_sys", "keystone");
        bool keystonePresent = File.Exists(keystonePath);

        if (keystoneNeeded && !keystonePresent)
        {
            progress?.Report(ProgressReport.Log(
                "[warn] Base game repack but keystone is MISSING — saves will break."));
        }
        else if (!keystoneNeeded && keystonePresent)
        {
            progress?.Report(ProgressReport.Log(
                "[repack] keystone present in an update/DLC — unusual but harmless."));
        }

        var paramSfoPath = Path.Combine(extractFull, "sce_sys", "param.sfo");
        if (!File.Exists(paramSfoPath))
            throw new FileNotFoundException(
                "param.sfo is missing from sce_sys — cannot repack.", paramSfoPath);

        bool isBaseGame = category.Equals("gd", StringComparison.OrdinalIgnoreCase) || keystoneNeeded;
        var genExe = isBaseGame ? _tools.Gengp4App : _tools.Gengp4Patch;
        progress?.Report(ProgressReport.Log(
            $"[repack] using {Path.GetFileName(genExe)} ({(isBaseGame ? "base game" : "patch/update")})"));

        progress?.Report(ProgressReport.Pct(5, "Generating GP4 project..."));
        var rc = await _runner.RunAsync(
            genExe,
            new[] { extractFull },
            opts.WorkDir, progress, ct).ConfigureAwait(false);
        if (rc != 0) throw new InvalidOperationException(
            $"{Path.GetFileName(genExe)} failed (exit {rc}).");

        var folderName = new DirectoryInfo(extractFull).Name;
        var gp4Path = Path.Combine(opts.WorkDir, folderName + ".gp4");

        if (!File.Exists(gp4Path))
        {
            var alternate = Path.Combine(Path.GetDirectoryName(extractFull) ?? "", folderName + ".gp4");
            if (File.Exists(alternate))
            {
                gp4Path = alternate;
            }
            else
            {
                var found = Directory.EnumerateFiles(opts.WorkDir, "*.gp4").FirstOrDefault()
                         ?? Directory.EnumerateFiles(Path.GetDirectoryName(extractFull) ?? opts.WorkDir, "*.gp4")
                                     .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                                     .FirstOrDefault();
                if (found is null)
                    throw new FileNotFoundException(
                        "GP4 was not produced by " + Path.GetFileName(genExe) + ".");
                gp4Path = found;
            }
        }

        ct.ThrowIfCancellationRequested();

        var buildDir = Path.Combine(opts.WorkDir, "build");
        Directory.CreateDirectory(buildDir);

        progress?.Report(ProgressReport.Pct(30, "Building new PKG (this can take a while)..."));
        rc = await _runner.RunAsync(
            _tools.OrbisPubCmd,
            new[] { "img_create", "--oformat", "pkg", gp4Path, buildDir },
            opts.WorkDir, progress, ct).ConfigureAwait(false);
        if (rc != 0) throw new InvalidOperationException($"img_create failed (exit {rc}).");

        var builtPkg = Directory.GetFiles(buildDir, "*.pkg").FirstOrDefault()
            ?? throw new InvalidOperationException("img_create produced no .pkg.");

        var finalPath = Path.Combine(outputFull, Path.GetFileName(builtPkg));
        if (File.Exists(finalPath))
        {
            try { File.SetAttributes(finalPath, FileAttributes.Normal); } catch { }
            FileSystemUtil.TryDeleteFile(finalPath);
        }
        File.Move(builtPkg, finalPath);

        progress?.Report(ProgressReport.Pct(98, "Cleaning up..."));
        FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);

        progress?.Report(ProgressReport.Pct(100, "Done."));
        return finalPath;
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

    private static void MoveDirectoryContents(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var dir in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(dir);
            var target = Path.Combine(dest, name);
            if (Directory.Exists(target))
                MoveDirectoryContents(dir, target);
            else
                Directory.Move(dir, target);
        }

        foreach (var file in Directory.GetFiles(source))
        {
            var target = Path.Combine(dest, Path.GetFileName(file));
            File.Move(file, target, overwrite: true);
        }
    }
}