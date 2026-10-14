using System.Buffers.Binary;
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

/// <summary>
/// Creates a DLC unlocker / data PKG from scratch.
///
/// If an icon is supplied, the PKG is built as volume_type=pkg_ps4_ac_data
/// so the PS4 renders the entry (icon + title) in the Add-On content list.
/// If no icon is supplied, it's built as pkg_ps4_ac_nodata — a pure unlock
/// key, which is smaller and matches the classic unlocker behaviour.
///
/// IMPORTANT: img_create rewrites param.sfo when the volume is
/// pkg_ps4_ac_nodata. It strips the img0_* fields out of PUBTOOLINFO, which
/// is what causes the icon and title to disappear on the console even
/// though the DLC itself works. pkg_ps4_ac_data leaves param.sfo alone.
/// </summary>
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

        // SAFETY: refuse a protected root as the output folder.
        if (SafePathGuard.IsProtectedRoot(opts.OutputDir))
            return new DlcUnlockerResult(false,
                "The output folder cannot be a protected Windows folder " +
                "(Desktop, Documents, Downloads, drive root, ...). " +
                "Create a subfolder and use that instead.",
                null);

        // SAFETY: refuse to touch anything outside tools\Work.
        if (!IsInsideWorkRoot(opts.WorkDir, _tools.WorkDir))
            return new DlcUnlockerResult(false,
                $"Refusing to run: work directory '{opts.WorkDir}' is not inside " +
                $"the tools work folder '{_tools.WorkDir}'.",
                null);

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

        bool hasIcon = !string.IsNullOrWhiteSpace(opts.IconPath);
        if (hasIcon && !File.Exists(opts.IconPath))
            return new DlcUnlockerResult(false,
                "Icon file not found: " + opts.IconPath, null);

        // ---- Pick volume type ----
        // Data volume when we have an icon to display, nodata otherwise.
        // See the class-level comment for why this matters.
        string volumeType = hasIcon ? "pkg_ps4_ac_data" : "pkg_ps4_ac_nodata";

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

        var titleId = contentId.Substring(7, 9);

        progress?.Report(ProgressReport.Log(
            $"[dlc] volume_type = {volumeType} " +
            $"(icon {(hasIcon ? "supplied" : "not supplied")})"));

        try
        {
            // ---- 1. param.sfo ----
            progress?.Report(ProgressReport.Pct(20, "Generating param.sfo..."));
            var sfoPath = Path.Combine(sceSys, "param.sfo");
            WriteParamSfo(sfoPath, contentId, titleId, title, opts.IconPath, progress);

            // ---- 2. Optional icon ----
            if (hasIcon)
            {
                progress?.Report(ProgressReport.Pct(30, "Copying icon..."));
                var iconDest = Path.Combine(sceSys, "icon0.png");
                File.Copy(opts.IconPath!, iconDest, overwrite: true);
            }

            // ---- 3. GP4 ----
            progress?.Report(ProgressReport.Pct(40, "Writing dlc.gp4..."));
            var gp4Path = Path.Combine(opts.WorkDir, "dlc.gp4");
            WriteGp4(gp4Path, contentId, hasIcon, volumeType);

            // ---- 4. img_create ----
            progress?.Report(ProgressReport.Pct(50, "Building DLC PKG..."));
            var buildDir = Path.Combine(opts.WorkDir, "build");
            Directory.CreateDirectory(buildDir);

            var rc = await _runner.RunAsync(
                _tools.OrbisPubCmd,
                new[] { "img_create", "--oformat", "pkg", gp4Path, buildDir },
                opts.WorkDir, progress, ct).ConfigureAwait(false);

            if (rc != 0)
                return new DlcUnlockerResult(false,
                    $"img_create failed (exit {rc}).", null);

            // ---- 5. Move to output ----
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
            // Always tidy up the work directory, success or failure. The
            // finished PKG has already been moved to the output folder, so
            // nothing here is needed once we return.
            try
            {
                if (Directory.Exists(opts.WorkDir))
                    FileSystemUtil.WipeDirectory(opts.WorkDir, _tools.WorkDir);
            }
            catch { }
        }
    }

    // ---------- param.sfo ----------

    private static void WriteParamSfo(
        string sfoPath,
        string contentId,
        string titleId,
        string title,
        string? iconPath,
        IProgress<ProgressReport>? progress)
    {
        var creationDate = DateTime.Now.ToString("yyyyMMdd");

        // Work out the icon slot size. If an icon is provided we use its
        // real dimensions; otherwise default to 512. The img0_* fields
        // should always be present — the PS4 Add-On content screen uses
        // them to decide whether to render the entry (icon + title) at
        // all. If they're missing, the DLC installs but is invisible.
        //
        // Note: for pkg_ps4_ac_nodata, img_create will strip these anyway.
        // We still write them so the SFO is consistent and so that
        // switching to a data volume "just works".
        int iconSize = 512;

        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            var dims = TryReadPngSize(iconPath);
            if (dims is (int w, int h))
            {
                iconSize = Math.Max(w, h);
                progress?.Report(ProgressReport.Log(
                    $"[dlc] icon dimensions: {w}x{h}, image size = {iconSize}"));
            }
            else
            {
                progress?.Report(ProgressReport.Log(
                    "[dlc] [warn] could not read icon dimensions — using 512"));
            }
        }

        var pubToolInfo =
            $"c_date={creationDate},img0_l0_size=1,img0_l1_size=0," +
            $"img0_sc_ksize={iconSize},img0_pc_ksize={iconSize}";

        progress?.Report(ProgressReport.Log(
            $"[dlc] PUBTOOLINFO = {pubToolInfo}"));

        var sfo = new SfoWriter();
        sfo.SetUInt32("ATTRIBUTE", 0);
        sfo.SetString("CATEGORY", "ac");
        sfo.SetString("CONTENT_ID", contentId, maxLen: 48);
        sfo.SetString("FORMAT", "obs");
        sfo.SetString("PUBTOOLINFO", pubToolInfo, maxLen: 512);
        sfo.SetUInt32("PUBTOOLVER", 0x03870000);
        sfo.SetString("TITLE", title, maxLen: 128);
        sfo.SetString("TITLE_ID", titleId, maxLen: 12);
        sfo.SetString("VERSION", "01.00", maxLen: 8);

        sfo.Write(sfoPath);

        if (!File.Exists(sfoPath))
            throw new IOException($"SfoWriter did not produce a file at: {sfoPath}");

        var fi = new FileInfo(sfoPath);
        progress?.Report(ProgressReport.Log(
            $"[dlc] param.sfo written: {fi.Length} bytes"));
    }

    private static (int Width, int Height)? TryReadPngSize(string pngPath)
    {
        try
        {
            using var fs = new FileStream(pngPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            Span<byte> header = stackalloc byte[24];
            int read = 0;
            while (read < header.Length)
            {
                int r = fs.Read(header.Slice(read));
                if (r <= 0) return null;
                read += r;
            }

            if (header[0] != 0x89 || header[1] != 0x50 ||
                header[2] != 0x4E || header[3] != 0x47 ||
                header[4] != 0x0D || header[5] != 0x0A ||
                header[6] != 0x1A || header[7] != 0x0A)
                return null;

            if (header[12] != (byte)'I' || header[13] != (byte)'H' ||
                header[14] != (byte)'D' || header[15] != (byte)'R')
                return null;

            int width = BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4));
            int height = BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4));

            if (width <= 0 || height <= 0) return null;
            return (width, height);
        }
        catch
        {
            return null;
        }
    }

    // ---------- GP4 ----------

    private static void WriteGp4(
        string gp4Path,
        string contentId,
        bool hasIcon,
        string volumeType)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
        sb.AppendLine("<psproject fmt=\"gp4\" version=\"1000\">");
        sb.AppendLine("  <volume>");
        sb.AppendLine($"    <volume_type>{volumeType}</volume_type>");
        sb.AppendLine("    <volume_id>PS4VOLUME</volume_id>");
        sb.AppendLine($"    <volume_ts>{timestamp}</volume_ts>");
        sb.AppendLine($"    <package content_id=\"{XmlEscape(contentId)}\" " +
                      "passcode=\"00000000000000000000000000000000\"/>");
        sb.AppendLine("  </volume>");
        sb.AppendLine("  <files img_no=\"0\">");
        sb.AppendLine("    <file targ_path=\"sce_sys/param.sfo\"/>");
        if (hasIcon)
            sb.AppendLine("    <file targ_path=\"sce_sys/icon0.png\"/>");
        sb.AppendLine("  </files>");
        sb.AppendLine("  <rootdir>");
        sb.AppendLine("    <dir targ_name=\"sce_sys\"/>");
        sb.AppendLine("  </rootdir>");
        sb.AppendLine("</psproject>");
        File.WriteAllText(gp4Path, sb.ToString());
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