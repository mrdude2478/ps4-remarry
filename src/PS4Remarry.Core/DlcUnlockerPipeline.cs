using System.Text;

namespace PS4Remarry.Core;

public sealed record DlcUnlockerOptions(
    string ContentId,
    string Title,
    string? IconPath,
    string OutputDir,
    string WorkDir);

public sealed record DlcUnlockerResult(
    bool Success,
    string? Error,
    string? OutputPath);

public sealed class DlcUnlockerPipeline
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator _tools;

    public DlcUnlockerPipeline(ProcessRunner runner, ToolLocator tools)
    {
        _runner = runner;
        _tools = tools;
    }

    public async Task<DlcUnlockerResult> RunAsync(
        DlcUnlockerOptions opts,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        _tools.EnsureToolsExist();

        // SAFETY: the output folder must not be a protected root
        // (Desktop, Documents, Downloads, drive root, ...). Subfolders of
        // those are fine; only the roots themselves are refused.
        if (SafePathGuard.IsProtectedRoot(opts.OutputDir))
            return new DlcUnlockerResult(false,
                $"Output folder cannot be a protected Windows folder " +
                $"({opts.OutputDir}). Please create a subfolder for your " +
                "DLC files (for example, Desktop\\MyDLC) and use that.",
                null);

        // SAFETY: refuse to touch anything outside tools\Work.
        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            return new DlcUnlockerResult(false,
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.", null);

        // ---- Validate inputs ----
        var contentId = (opts.ContentId ?? "").Trim();
        var title = (opts.Title ?? "").Trim();

        if (string.IsNullOrEmpty(contentId))
            return new DlcUnlockerResult(false, "Content ID is required.", null);

        if (contentId.Length < 16)
            return new DlcUnlockerResult(false,
                "Content ID is too short to derive the title ID. " +
                "Expected format: XX0000-CUSA00000_00-XXXXXXXXXXXXXXXX", null);

        if (string.IsNullOrEmpty(title))
            return new DlcUnlockerResult(false, "Title is required.", null);

        try { Directory.CreateDirectory(opts.OutputDir); }
        catch (Exception ex)
        {
            return new DlcUnlockerResult(false,
                "Cannot create output folder: " + ex.Message, null);
        }

        // ---- Prepare work area ----
        try
        {
            FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
        }
        catch (SafePathGuard.UnsafePathException ex)
        {
            return new DlcUnlockerResult(false,
                "Refusing to clear work folder: " + ex.Message, null);
        }

        Directory.CreateDirectory(opts.WorkDir);

        var sceSys = Path.Combine(opts.WorkDir, "sce_sys");
        Directory.CreateDirectory(sceSys);

        try
        {
            var titleId = contentId.Substring(7, 9);

            progress?.Report(ProgressReport.Pct(10, "Writing param.sfx..."));
            var sfxPath = Path.Combine(opts.WorkDir, "param.sfx");
            WriteParamSfx(sfxPath, contentId, titleId, title);

            progress?.Report(ProgressReport.Pct(20, "Generating param.sfo..."));
            var sfoPath = Path.Combine(sceSys, "param.sfo");
            var rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[] { "sfo_create", sfxPath, sfoPath },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0)
                return new DlcUnlockerResult(false, $"sfo_create failed (exit {rc}).", null);

            bool hasIcon = false;
            if (!string.IsNullOrWhiteSpace(opts.IconPath))
            {
                if (!File.Exists(opts.IconPath))
                    return new DlcUnlockerResult(false,
                        "Icon file not found: " + opts.IconPath, null);

                progress?.Report(ProgressReport.Pct(30, "Copying icon..."));
                var iconDest = Path.Combine(sceSys, "icon0.png");
                File.Copy(opts.IconPath, iconDest, overwrite: true);
                hasIcon = true;
            }

            progress?.Report(ProgressReport.Pct(40, "Writing dlc.gp4..."));
            var gp4Path = Path.Combine(opts.WorkDir, "dlc.gp4");
            WriteGp4(gp4Path, opts.WorkDir, contentId, hasIcon);

            progress?.Report(ProgressReport.Pct(50, "Building DLC PKG..."));
            var buildDir = Path.Combine(opts.WorkDir, "build");
            Directory.CreateDirectory(buildDir);

            rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[] { "img_create", "--oformat", "pkg", gp4Path, buildDir },
                opts.WorkDir, progress, ct).ConfigureAwait(false);
            if (rc != 0)
                return new DlcUnlockerResult(false, $"img_create failed (exit {rc}).", null);

            var builtPkg = Directory.GetFiles(buildDir, "*.pkg").FirstOrDefault();
            if (builtPkg is null)
                return new DlcUnlockerResult(false,
                    "img_create succeeded but produced no .pkg file.", null);

            var finalName = $"{contentId}-A0000-V0100.pkg";
            var finalPath = Path.Combine(opts.OutputDir, finalName);

            if (File.Exists(finalPath))
            {
                try { File.SetAttributes(finalPath, FileAttributes.Normal); } catch { }
                FileSystemUtil.TryDeleteFile(finalPath);
            }

            File.Move(builtPkg, finalPath);

            progress?.Report(ProgressReport.Pct(100, "Done."));
            return new DlcUnlockerResult(true, null, finalPath);
        }
        finally
        {
            // SAFETY: wipe only the work folder. This is doubly guarded —
            // IsInsideWorkRoot at the top refused to run if it was wrong,
            // and WipeDirectory refuses protected roots regardless.
            try
            {
                if (Directory.Exists(opts.WorkDir))
                    FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
            }
            catch { }
        }
    }

    private static void WriteParamSfx(string path, string contentId, string titleId, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
        sb.AppendLine("<paramsfo>");
        sb.AppendLine("  <param key=\"ATTRIBUTE\">0</param>");
        sb.AppendLine("  <param key=\"CATEGORY\">ac</param>");
        sb.AppendLine($"  <param key=\"CONTENT_ID\">{contentId}</param>");
        sb.AppendLine("  <param key=\"FORMAT\">obs</param>");
        sb.AppendLine($"  <param key=\"TITLE\">{title}</param>");
        sb.AppendLine($"  <param key=\"TITLE_ID\">{titleId}</param>");
        sb.AppendLine("  <param key=\"VERSION\">01.00</param>");
        sb.AppendLine("</paramsfo>");
        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteGp4(string path, string workDir, string contentId, bool hasIcon)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sfoPath = Path.Combine(workDir, "sce_sys", "param.sfo");
        var iconPath = Path.Combine(workDir, "sce_sys", "icon0.png");

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
        sb.AppendLine("<psproject fmt=\"gp4\" version=\"1000\">");
        sb.AppendLine("  <volume>");
        sb.AppendLine("    <volume_type>pkg_ps4_ac_nodata</volume_type>");
        sb.AppendLine("    <volume_id>PS4VOLUME</volume_id>");
        sb.AppendLine($"    <volume_ts>{timestamp}</volume_ts>");
        sb.AppendLine($"    <package content_id=\"{XmlEscape(contentId)}\" " +
                      "passcode=\"00000000000000000000000000000000\"/>");
        sb.AppendLine("  </volume>");
        sb.AppendLine("  <files img_no=\"0\">");
        sb.AppendLine($"    <file targ_path=\"sce_sys/param.sfo\" orig_path=\"{XmlEscape(sfoPath)}\"/>");
        if (hasIcon)
            sb.AppendLine($"    <file targ_path=\"sce_sys/icon0.png\" orig_path=\"{XmlEscape(iconPath)}\"/>");
        sb.AppendLine("  </files>");
        sb.AppendLine("  <rootdir>");
        sb.AppendLine("    <dir targ_name=\"sce_sys\"/>");
        sb.AppendLine("  </rootdir>");
        sb.AppendLine("</psproject>");
        File.WriteAllText(path, sb.ToString());
    }

    private static string XmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
         .Replace("\"", "&quot;").Replace("'", "&apos;");

    private static bool IsInsideWorkRoot(string candidate, string root)
    {
        var c = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var r = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return c.Equals(r, StringComparison.OrdinalIgnoreCase)
            || c.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}