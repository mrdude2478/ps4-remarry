namespace PS4Remarry.Core;

public sealed record RepackExtractOptions(
    string SourcePkg,
    string ExtractFolder,
    string WorkDir);

public sealed record RepackBuildOptions(
    string ExtractFolder,
    string OutputFolder,
    string WorkDir,
    /// <summary>
    /// For patch (update) repacks, the path to the base game PKG. It gets
    /// written into the GP4 as app_path so the update is married to the game.
    /// Null or empty for base game repacks.
    /// </summary>
    string? BaseGamePkg = null);

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
/// and rebuilds a new PKG from that folder. Handles games, updates, and
/// data DLC. Nodata DLC (unlock keys) can be extracted but not rebuilt.
/// </summary>
public sealed class RepackPipeline
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator  _tools;

    private const string ParamSfoBackupSuffix = ".ps4repack.param.sfo.bak";

    public RepackPipeline(ProcessRunner runner, ToolLocator tools)
    {
        _runner = runner;
        _tools  = tools;
    }

    public static string GetParamSfoBackupPath(string extractFolder)
    {
        var trimmed = extractFolder.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return trimmed + ParamSfoBackupSuffix;
    }

    public static IEnumerable<string> GetOrphanedFiles(string extractFolder)
    {
        yield return GetParamSfoBackupPath(extractFolder);

        var trimmed = extractFolder.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(trimmed);
        var parent = Path.GetDirectoryName(trimmed);

        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
            yield break;

        foreach (var gp4 in Directory.EnumerateFiles(parent, "*.gp4"))
        {
            var baseName = Path.GetFileNameWithoutExtension(gp4);
            if (NamesRelate(folderName, baseName))
                yield return gp4;
        }
    }

    private static bool NamesRelate(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
        if (a.StartsWith(b, StringComparison.OrdinalIgnoreCase)) return true;
        if (b.StartsWith(a, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // ---------- inspect ----------

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

    // ---------- extract ----------

    public async Task ExtractAsync(
        RepackExtractOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            throw new InvalidOperationException(
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.");

        var sourceFull = Path.GetFullPath(opts.SourcePkg);
        if (!File.Exists(sourceFull))
            throw new FileNotFoundException("Source PKG not found.", sourceFull);

        var extractFull = Path.GetFullPath(opts.ExtractFolder);

        // SAFETY: track whether we created the extract folder. This is what
        // decides whether we're allowed to delete it on failure.
        bool weCreatedExtractFolder = !Directory.Exists(extractFull);

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

            progress?.Report(ProgressReport.Pct(50, "Normalising extract layout..."));

            var image0 = Path.Combine(tempExtract, "Image0");
            var sc0    = Path.Combine(tempExtract, "Sc0");

            if (Directory.Exists(image0))
            {
                // Normal game/update/data-DLC layout: Image0 exists.
                // Merge Sc0 into Image0\sce_sys, then delete Sc0.
                if (Directory.Exists(sc0))
                {
                    var sceSys = Path.Combine(image0, "sce_sys");
                    Directory.CreateDirectory(sceSys);
                    FileSystemUtil.CopyDirectory(sc0, sceSys);
                    FileSystemUtil.TryDeleteDirectory(sc0);
                }
            }
            else if (Directory.Exists(sc0))
            {
                // Nodata DLC or unusual layout: no Image0, but Sc0 exists.
                // Promote Sc0 to be our Image0 so the rest of the pipeline
                // sees a consistent structure.
                progress?.Report(ProgressReport.Log(
                    "[repack] no Image0 folder — promoting Sc0 to Image0."));
                Directory.Move(sc0, image0);
            }
            else
            {
                // Neither folder. Some PKGs (especially very simple DLC
                // unlock keys) dump their contents straight into the extract
                // root. Move those into a synthesised Image0.
                progress?.Report(ProgressReport.Log(
                    "[repack] neither Image0 nor Sc0 found — " +
                    "moving extract root contents into Image0."));
                Directory.CreateDirectory(image0);

                foreach (var entry in Directory.EnumerateFileSystemEntries(tempExtract))
                {
                    var name = Path.GetFileName(entry);
                    if (name.Equals("Image0", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var target = Path.Combine(image0, name);
                    if (Directory.Exists(entry))
                        Directory.Move(entry, target);
                    else
                        File.Move(entry, target);
                }
            }

            ct.ThrowIfCancellationRequested();

            progress?.Report(ProgressReport.Pct(70, "Moving contents into place..."));

            if (!Directory.Exists(image0))
                throw new DirectoryNotFoundException(
                    $"Extraction produced no usable content. " +
                    $"Expected '{image0}' after layout normalisation.");

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

            // Back up the original param.sfo so we can restore it after
            // gengp4 rewrites APP_VER and VERSION.
            if (paramSfoExists)
            {
                var backupPath = GetParamSfoBackupPath(extractFull);
                try
                {
                    File.Copy(paramSfoPath, backupPath, overwrite: true);
                    progress?.Report(ProgressReport.Log(
                        $"  param.sfo backup: {Path.GetFileName(backupPath)}"));
                }
                catch (Exception ex)
                {
                    progress?.Report(ProgressReport.Log(
                        $"  [warn] could not back up param.sfo: {ex.Message}"));
                }
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
            // SAFETY: only delete the extract folder if we created it. If it
            // existed before we started, the user owns it and any content
            // they already had must survive our failure.
            FileSystemUtil.DeleteOnlyIfWeCreated(extractFull, weCreatedExtractFolder);
            try { FileSystemUtil.TryDeleteFile(GetParamSfoBackupPath(extractFull)); } catch { }
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

    // ---------- repack ----------

    public async Task<string> RepackAsync(
        RepackBuildOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

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
        bool isPatch    = category.Equals("gp", StringComparison.OrdinalIgnoreCase);
        bool isDlc      = category.Equals("gde", StringComparison.OrdinalIgnoreCase)
                       || category.Equals("gdk", StringComparison.OrdinalIgnoreCase)
                       || category.Equals("gda", StringComparison.OrdinalIgnoreCase)
                       || category.Equals("ac",  StringComparison.OrdinalIgnoreCase);

        var genExe = isBaseGame ? _tools.Gengp4App : _tools.Gengp4Patch;
        progress?.Report(ProgressReport.Log(
            $"[repack] using {Path.GetFileName(genExe)} " +
            $"({(isBaseGame ? "base game" : isPatch ? "patch/update" : "DLC")})"));

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

        // ------------------------------------------------------------------
        // Patch fixes. gengp4_patch.exe produces a GP4 with the wrong
        // volume_type, wrong storage_type, and no app_path reference.
        // DLC needs a different volume_type still. Fix all cases here.
        // ------------------------------------------------------------------
        if (isPatch || isDlc)
        {
            try
            {
                var gp4Text = File.ReadAllText(gp4Path);
                bool changed = false;

                if (isPatch)
                {
                    // --- Fix 1: volume_type pkg_ps4_app -> pkg_ps4_patch ---
                    if (gp4Text.Contains("<volume_type>pkg_ps4_app</volume_type>"))
                    {
                        gp4Text = gp4Text.Replace(
                            "<volume_type>pkg_ps4_app</volume_type>",
                            "<volume_type>pkg_ps4_patch</volume_type>");
                        changed = true;
                        progress?.Report(ProgressReport.Log(
                            "[repack] changed volume_type to \"pkg_ps4_patch\"."));
                    }

                    // --- Fix 2: storage_type digital50 -> digital25 ---
                    if (gp4Text.Contains("storage_type=\"digital50\""))
                    {
                        gp4Text = gp4Text.Replace(
                            "storage_type=\"digital50\"",
                            "storage_type=\"digital25\"");
                        changed = true;
                        progress?.Report(ProgressReport.Log(
                            "[repack] changed storage_type to \"digital25\"."));
                    }

                    // --- Fix 3: app_path ---
                    if (string.IsNullOrWhiteSpace(opts.BaseGamePkg) || !File.Exists(opts.BaseGamePkg))
                    {
                        progress?.Report(ProgressReport.Log(
                            "[repack] [warn] no base game PKG provided for patch repack."));
                    }
                    else if (!gp4Text.Contains("app_path="))
                    {
                        var appPath = Path.GetFullPath(opts.BaseGamePkg)
                            .Replace("&", "&amp;")
                            .Replace("\"", "&quot;");

                        int pkgStart = gp4Text.IndexOf("<package ", StringComparison.OrdinalIgnoreCase);
                        if (pkgStart >= 0)
                        {
                            int pkgEnd = gp4Text.IndexOf("/>", pkgStart, StringComparison.Ordinal);
                            if (pkgEnd >= 0)
                            {
                                gp4Text = gp4Text.Substring(0, pkgEnd)
                                        + $" app_path=\"{appPath}\" "
                                        + gp4Text.Substring(pkgEnd);
                                changed = true;
                                progress?.Report(ProgressReport.Log(
                                    $"[repack] added app_path=\"{opts.BaseGamePkg}\" to GP4."));
                            }
                        }
                    }
                }
                else if (isDlc)
                {
                    // DLC: pkg_ps4_app -> pkg_ps4_ac_data
                    if (gp4Text.Contains("<volume_type>pkg_ps4_app</volume_type>"))
                    {
                        gp4Text = gp4Text.Replace(
                            "<volume_type>pkg_ps4_app</volume_type>",
                            "<volume_type>pkg_ps4_ac_data</volume_type>");
                        changed = true;
                        progress?.Report(ProgressReport.Log(
                            "[repack] changed volume_type to \"pkg_ps4_ac_data\"."));
                    }

                    if (gp4Text.Contains("storage_type=\"digital50\""))
                    {
                        gp4Text = gp4Text.Replace(
                            "storage_type=\"digital50\"",
                            "storage_type=\"digital25\"");
                        changed = true;
                        progress?.Report(ProgressReport.Log(
                            "[repack] changed storage_type to \"digital25\"."));
                    }
                }

                if (changed)
                    File.WriteAllText(gp4Path, gp4Text);
            }
            catch (Exception ex)
            {
                progress?.Report(ProgressReport.Log(
                    $"[repack] [warn] could not modify GP4: {ex.Message}"));
            }

            // Restore original param.sfo. gengp4 rewrites APP_VER and VERSION.
            var backupPath = GetParamSfoBackupPath(extractFull);
            if (File.Exists(backupPath))
            {
                try
                {
                    File.Copy(backupPath, paramSfoPath, overwrite: true);
                    progress?.Report(ProgressReport.Log(
                        "[repack] restored original param.sfo after GP4 generation."));
                }
                catch (Exception ex)
                {
                    progress?.Report(ProgressReport.Log(
                        $"[repack] [warn] could not restore param.sfo: {ex.Message}"));
                }
            }
            else
            {
                progress?.Report(ProgressReport.Log(
                    "[repack] [warn] no param.sfo backup found — using tool-modified version."));
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

    // ---------- helpers ----------

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