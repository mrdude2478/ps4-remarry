using System.Diagnostics;
using System.Drawing;
using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class MainForm : Form
{
    private readonly ProcessRunner _runner = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string? _lastLoggedLine;

    private string? _lastBuiltUpdatePkg;

    private string? _coverArtCachePath;
    private SfoParser.CoverArtResult? _coverArtCache;

    private string? _lastGameInfoDumpedFor;
    private string? _lastUpdateInfoDumpedFor;

    public MainForm()
    {
        InitializeComponent();

        txtOutput.Text = Path.Combine(AppContext.BaseDirectory, "Finished");

        try
        {
            ToolLocator.Default().EnsureToolsExist();
            AppendLog("Ready. Tools verified. Drop a .pkg into either textbox, or click Browse.");
        }
        catch (Exception ex)
        {
            AppendLog("TOOLS MISSING — the remarry step will fail until fixed:");
            AppendLog(ex.Message);
            statusLabel.Text = "Tools missing.";
        }

        UpdateRunButtonState();
    }

    // ---------- Browse handlers ----------

    private void btnBrowseGame_Click(object? sender, EventArgs e)
    {
        var p = PickPkg("Select the base GAME pkg");
        if (p is not null)
        {
            txtGame.Text = p;
            InvalidateCoverArtCache();
            UpdateRunButtonState();
            _ = DumpPkgInfoAsync(p, "Game PKG", isUpdate: false);
        }
    }

    private void btnBrowseUpdate_Click(object? sender, EventArgs e)
    {
        var p = PickPkg("Select the UPDATE (patch) pkg");
        if (p is not null)
        {
            txtUpdate.Text = p;
            InvalidateUpdateInfo();
            UpdateRunButtonState();
            _ = DumpPkgInfoAsync(p, "Update PKG", isUpdate: true);
        }
    }

    private void btnBrowseOutput_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose the output folder for the remarried PKG",
            SelectedPath = Directory.Exists(txtOutput.Text) ? txtOutput.Text : AppContext.BaseDirectory
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            txtOutput.Text = dlg.SelectedPath;
    }

    private async void btnRenameGame_Click(object? sender, EventArgs e)
    {
        await RenameAsync(isGame: true);
    }

    private async void btnRenameUpdate_Click(object? sender, EventArgs e)
    {
        await RenameAsync(isGame: false);
    }

    private async Task RenameAsync(bool isGame)
    {
        if (_isBusy) return;

        var field = isGame ? txtGame : txtUpdate;
        var path = field.Text.Trim();

        if (!File.Exists(path))
        {
            Warn(isGame ? "Load a game PKG first." : "Load an update PKG first.");
            return;
        }

        var answer = MessageBox.Show(this,
            "This will rename:\n\n" +
            Path.GetFileName(path) + "\n\n" +
            "based on its internal metadata. Continue?",
            "Rename PKG", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        SetRenameButtonsEnabled(false);
        try
        {
            var result = await Task.Run(() => PkgRenamer.Rename(path));
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Rename PKG",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.Equals(result.OldPath, result.NewPath, StringComparison.OrdinalIgnoreCase))
            {
                field.Text = result.NewPath;

                if (isGame)
                {
                    InvalidateCoverArtCache();
                    _ = DumpPkgInfoAsync(result.NewPath, "Game PKG", isUpdate: false);
                }
                else
                {
                    InvalidateUpdateInfo();
                    _ = DumpPkgInfoAsync(result.NewPath, "Update PKG", isUpdate: true);
                }

                AppendLog("");
                AppendLog($"[rename] {Path.GetFileName(result.OldPath)}");
                AppendLog($"[rename] -> {Path.GetFileName(result.NewPath)}");
            }
            else
            {
                AppendLog($"[rename] {result.Message}");
            }

            UpdateRunButtonState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Rename failed:\n" + ex.Message, "Rename PKG",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetRenameButtonsEnabled(true);
        }
    }

    private void SetRenameButtonsEnabled(bool enabled)
    {
        btnRenameGame.Enabled = enabled && !_isBusy && File.Exists(txtGame.Text.Trim());
        btnRenameUpdate.Enabled = enabled && !_isBusy && File.Exists(txtUpdate.Text.Trim());
    }

    private string? PickPkg(string title)
    {
        using var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = "PS4 PKG (*.pkg)|*.pkg|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileName : null;
    }

    // ---------- Drag & drop ----------

    private void PkgBox_DragEnter(object? sender, DragEventArgs e)
    {
        if (_isBusy) { e.Effect = DragDropEffects.None; return; }
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void PkgBox_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;

        var pkg = files.FirstOrDefault(f =>
            f.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase)) ?? files[0];

        if (sender == txtGame)
        {
            txtGame.Text = pkg;
            InvalidateCoverArtCache();
            _ = DumpPkgInfoAsync(pkg, "Game PKG", isUpdate: false);
        }
        else if (sender == txtUpdate)
        {
            txtUpdate.Text = pkg;
            InvalidateUpdateInfo();
            _ = DumpPkgInfoAsync(pkg, "Update PKG", isUpdate: true);
        }

        AppendLog($"Loaded: {pkg}");
        UpdateRunButtonState();
    }

    // ---------- Run / Cancel ----------

    private async void btnRun_Click(object? sender, EventArgs e)
    {
        if (_isBusy) return;

        var game = txtGame.Text.Trim();
        var update = txtUpdate.Text.Trim();
        var output = txtOutput.Text.Trim();

        if (!File.Exists(game)) { Warn("Game PKG not found."); return; }
        if (!File.Exists(update)) { Warn("Update PKG not found."); return; }
        if (string.IsNullOrWhiteSpace(output)) { Warn("Pick an output folder."); return; }

        try { Directory.CreateDirectory(output); }
        catch (Exception ex) { Warn("Cannot create output folder: " + ex.Message); return; }

        // ---- Pre-flight: region / title ID check ----
        try
        {
            var mm = await CheckRegionMismatchAsync();
            if (mm.HasData && mm.TitleIdMismatch)
            {
                var go = MessageBox.Show(this,
                    "The update PKG was built for a different game.\n\n" +
                    $"Game Title ID:   {mm.GameTitleId}\n" +
                    $"Update Title ID: {mm.UpdateTitleId}\n\n" +
                    "Run Remarry will rewrite the update's metadata to match your game.\n\n" +
                    "This works for region-equivalent releases, but may produce an update\n" +
                    "that misbehaves if the two games have different content.\n\n" +
                    "Continue anyway?",
                    "Title ID mismatch",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (go != DialogResult.Yes)
                {
                    AppendLog("[skip] cancelled due to Title ID mismatch.");
                    statusLabel.Text = "Cancelled.";
                    return;
                }
            }
        }
        catch { /* best effort — if the check fails, proceed normally */ }

        // ---- Pre-flight: does the target output file already exist? ----
        try
        {
            var sfo = await Task.Run(() => SfoParser.ParseToDictionary(update));
            if (sfo.TryGetValue("CONTENT_ID", out var contentIdRaw) && !string.IsNullOrWhiteSpace(contentIdRaw))
            {
                var contentId = contentIdRaw.Trim();

                var existing = Directory.EnumerateFiles(output, "*.pkg")
                    .FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f)
                            .StartsWith(contentId, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    var choice = MessageBox.Show(this,
                        "A file with the same name already exists:\n\n" +
                        Path.GetFileName(existing) + "\n\n" +
                        "Overwrite it with the new build, or cancel to keep the existing file?",
                        "File already exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (choice != DialogResult.Yes)
                    {
                        AppendLog($"[skip] output already exists: {Path.GetFileName(existing)}");
                        statusLabel.Text = "Cancelled (output exists).";
                        return;
                    }

                    try
                    {
                        File.SetAttributes(existing, FileAttributes.Normal);
                        File.Delete(existing);
                        AppendLog($"[overwrite] removed existing {Path.GetFileName(existing)}");
                    }
                    catch (Exception ex)
                    {
                        Warn("Could not remove the existing output file:\n" + ex.Message +
                             "\n\nIt may be open in another program.");
                        return;
                    }
                }
            }
        }
        catch { /* best effort — if the check fails, proceed normally */ }

        var tools = ToolLocator.Default();

        // SAFETY: if the work folder has content, ask before we wipe it.
        if (!ConfirmWorkFolderWipe(tools.WorkDir))
            return;

        var before = new HashSet<string>(
            Directory.GetFiles(output, "*.pkg"),
            StringComparer.OrdinalIgnoreCase);

        _lastBuiltUpdatePkg = null;
        btnVerifyOutput.Enabled = false;

        _cts = new CancellationTokenSource();
        _lastLoggedLine = null;
        SetBusy(true);
        progressBar.Value = 0;
        txtLog.Clear();
        AppendLog($"Game:   {game}");
        AppendLog($"Update: {update}");
        AppendLog($"Output: {output}");
        AppendLog("---------------------------------------------");

        var progress = new Progress<ProgressReport>(r =>
        {
            if (r.Percent is double p)
                progressBar.Value = (int)Math.Clamp(p, 0, 100);

            if (!string.IsNullOrEmpty(r.Message))
                AppendLog(r.Message);
        });

        try
        {
            var pipeline = new RemarryPipeline(_runner, tools);
            var opts = new RemarryOptions(game, update, output, tools.WorkDir);

            await Task.Run(() => pipeline.RunAsync(opts, progress, _cts.Token), _cts.Token);

            var after = Directory.GetFiles(output, "*.pkg");
            var newPkg = after.FirstOrDefault(f => !before.Contains(f)) ?? after.FirstOrDefault();
            if (newPkg is not null)
            {
                _lastBuiltUpdatePkg = newPkg;
                btnVerifyOutput.Enabled = true;
                AppendLog("");
                AppendLog($"Newly built: {Path.GetFileName(newPkg)}");
                AppendLog("Click 'Verify Output' to confirm it matches the base game.");
            }

            statusLabel.Text = "Done.";
            MessageBox.Show(this, "Remarry complete.\n\nOutput folder:\n" + output,
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            progressBar.Value = 0;
            AppendLog("Cancelled by user.");
            statusLabel.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            progressBar.Value = 0;
            AppendLog("ERROR: " + ex.Message);
            statusLabel.Text = "Error.";
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            progressBar.Value = 0;
            statusLabel.Text = "Cancelling...";
        }
    }

    private async void btnMerge_Click(object? sender, EventArgs e)
    {
        if (_isBusy) return;

        var game = txtGame.Text.Trim();
        var update = txtUpdate.Text.Trim();
        var output = txtOutput.Text.Trim();

        if (!File.Exists(game)) { Warn("Game PKG not found."); return; }
        if (!File.Exists(update)) { Warn("Update PKG not found."); return; }
        if (string.IsNullOrWhiteSpace(output)) { Warn("Pick an output folder."); return; }

        if (string.Equals(Path.GetFullPath(game),
                          Path.GetFullPath(update),
                          StringComparison.OrdinalIgnoreCase))
        {
            Warn("Game and Update PKGs point at the same file.");
            return;
        }

        var tools = ToolLocator.Default();

        // SAFETY: if the work folder has content, ask before we wipe it.
        if (!ConfirmWorkFolderWipe(tools.WorkDir))
            return;

        try
        {
            var gameSize = new FileInfo(game).Length;
            var updateSize = new FileInfo(update).Length;
            var required = gameSize + updateSize + (long)(512L * 1024 * 1024);
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(output))!);
            if (drive.IsReady && drive.AvailableFreeSpace < required)
            {
                var go = MessageBox.Show(this,
                    $"The merged PKG will need roughly {Formatting.FormatBytes(required)} of free space on {drive.Name}.\n\n" +
                    $"Currently available: {Formatting.FormatBytes(drive.AvailableFreeSpace)}.\n\n" +
                    "Continue anyway?",
                    "Low disk space", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (go != DialogResult.Yes) return;
            }
        }
        catch { }

        var answer = MessageBox.Show(this,
            "This will build a NEW merged base-game PKG from:\n\n" +
            Path.GetFileName(game) + "\n+\n" + Path.GetFileName(update) +
            "\n\nOriginal files will NOT be modified.\n\n" +
            "Merged PKGs take a while (10–40 minutes for large games). Continue?",
            "Merge Game + Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        _cts = new CancellationTokenSource();
        _lastLoggedLine = null;
        SetBusy(true);
        progressBar.Value = 0;
        txtLog.Clear();
        AppendLog("=== Merge Game + Update ===");
        AppendLog($"Game:   {game}");
        AppendLog($"Update: {update}");
        AppendLog($"Output: {output}");
        AppendLog("---------------------------------------------");

        var progress = new Progress<ProgressReport>(r =>
        {
            if (r.Percent is double p)
                progressBar.Value = (int)Math.Clamp(p, 0, 100);
            if (!string.IsNullOrEmpty(r.Message))
                AppendLog(r.Message);
        });

        try
        {
            var pipeline = new MergePipeline(_runner, tools);
            var opts = new MergeOptions(game, update, output, tools.WorkDir);

            var outDir = await Task.Run(() => pipeline.RunAsync(opts, progress, _cts.Token), _cts.Token);

            var merged = Directory.GetFiles(outDir, "*.pkg").FirstOrDefault();
            if (merged is not null)
            {
                AppendLog("");
                AppendLog($"Merged PKG: {merged}");
                AppendLog("Click 'Open Output' to view the folder.");
            }

            statusLabel.Text = "Merge done.";
            MessageBox.Show(this,
                "Merge complete.\n\nOutput folder:\n" + outDir,
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            progressBar.Value = 0;
            AppendLog("Merge cancelled.");
            statusLabel.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            progressBar.Value = 0;
            AppendLog("ERROR: " + ex.Message);
            statusLabel.Text = "Error.";
            MessageBox.Show(this, ex.Message, "Merge error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void btnOpenOutput_Click(object? sender, EventArgs e)
    {
        var outDir = txtOutput.Text.Trim();
        if (!Directory.Exists(outDir)) { Warn("Output folder does not exist yet."); return; }
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{outDir}\"") { UseShellExecute = true });
    }

    // ---------- Verify Output ----------

    private async void btnVerifyOutput_Click(object? sender, EventArgs e)
    {
        if (_isBusy) return;

        var game = txtGame.Text.Trim();
        var newPkg = _lastBuiltUpdatePkg;

        if (!File.Exists(game))
        {
            Warn("The original game PKG is no longer available at:\n" + game);
            return;
        }
        if (newPkg is null || !File.Exists(newPkg))
        {
            Warn("No newly built PKG to verify. Run a remarry first.");
            btnVerifyOutput.Enabled = false;
            return;
        }

        _isBusy = true;
        SetBusy(true);
        AppendLog("");
        AppendLog("=== Verifying built output ===");
        AppendLog($"Game:   {Path.GetFileName(game)}");
        AppendLog($"Output: {Path.GetFileName(newPkg)}");

        try
        {
            var gameDigest = await Task.Run(() => MarryDigestReader.GetChecksum(game));
            var newDigest = await Task.Run(() => MarryDigestReader.GetChecksum(newPkg));

            AppendLog($"  game digest  = {gameDigest}");
            AppendLog($"  built digest = {newDigest}");

            if (gameDigest == "-" || newDigest == "-")
            {
                AppendLog("Result: unable to read one or both digests.");
                statusLabel.Text = "Verify: inconclusive.";
                MessageBox.Show(this,
                    "Couldn't read one or both digests.\nSee the log for details.",
                    "Verify Output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool match = string.Equals(gameDigest, newDigest, StringComparison.OrdinalIgnoreCase);
            if (match)
            {
                AppendLog("Result: MATCH — the built update is married to this game.");
                statusLabel.Text = "Verify: match.";
                MessageBox.Show(this,
                    "✓ Built update matches the base game.\n\nThe remarry succeeded.",
                    "Verify Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AppendLog("Result: MISMATCH — the built update does NOT match this game.");
                statusLabel.Text = "Verify: mismatch.";
                MessageBox.Show(this,
                    "✗ Built update does NOT match the base game.\n\n" +
                    "Something went wrong during the remarry.\nSee the log for the digests.",
                    "Verify Output", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            AppendLog("ERROR during verify: " + ex.Message);
            statusLabel.Text = "Verify: error.";
            MessageBox.Show(this, ex.Message, "Verify Output",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isBusy = false;
            SetBusy(false);
        }
    }

    // ---------- Cover art (Icon / Background) ----------

    private async void btnShowIcon_Click(object? sender, EventArgs e)
    {
        await ShowCoverArtAsync(wantIcon: true);
    }

    private async void btnShowBackground_Click(object? sender, EventArgs e)
    {
        await ShowCoverArtAsync(wantIcon: false);
    }

    private async Task ShowCoverArtAsync(bool wantIcon)
    {
        if (_isBusy) return;

        var gamePath = txtGame.Text.Trim();
        if (!File.Exists(gamePath))
        {
            Warn("Load a game PKG first.");
            return;
        }

        SetCoverButtonsEnabled(false);
        try
        {
            if (_coverArtCachePath != gamePath || _coverArtCache is null)
            {
                var path = gamePath;
                _coverArtCache = await Task.Run(() => SfoParser.ExtractCoverArt(path));
                _coverArtCachePath = gamePath;
            }

            var chosen = wantIcon ? _coverArtCache.Icon0 : _coverArtCache.Pic1;
            var title = wantIcon ? "Game Icon" : "Game Background";

            if (!chosen.Found || chosen.Bytes is null || chosen.Bytes.Length == 0)
            {
                MessageBox.Show(this,
                    $"No {(wantIcon ? "icon" : "background")} available in this PKG.\n\n{chosen.Status}",
                    title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var viewer = new ImageViewerForm(
                $"{title} — {Path.GetFileName(gamePath)}",
                chosen.Bytes!,
                chosen.Status,
                BuildSuggestedFileName(gamePath, wantIcon));
            viewer.Show(this);
        }
        catch (Exception ex)
        {
            Warn("Could not extract cover art: " + ex.Message);
        }
        finally
        {
            SetCoverButtonsEnabled(true);
        }
    }

    private void SetCoverButtonsEnabled(bool enabled)
    {
        bool hasGame = File.Exists(txtGame.Text.Trim());
        btnShowIcon.Enabled = enabled && hasGame && !_isBusy;
        btnShowBackground.Enabled = enabled && hasGame && !_isBusy;
    }

    private void InvalidateCoverArtCache()
    {
        _coverArtCache = null;
        _coverArtCachePath = null;
        _lastGameInfoDumpedFor = null;
    }

    private void InvalidateUpdateInfo()
    {
        _lastUpdateInfoDumpedFor = null;
    }

    private static string BuildSuggestedFileName(string gamePkgPath, bool wantIcon)
    {
        var stem = Path.GetFileNameWithoutExtension(gamePkgPath);

        int dash = stem.IndexOf("-Game-", StringComparison.OrdinalIgnoreCase);
        if (dash > 0) stem = stem.Substring(0, dash);

        return wantIcon
            ? stem + "-icon.png"
            : stem + "-background.png";
    }

    // ---------- PKG info on load ----------

    private async Task DumpPkgInfoAsync(string pkgPath, string label, bool isUpdate)
    {
        var tracker = isUpdate ? _lastUpdateInfoDumpedFor : _lastGameInfoDumpedFor;
        if (string.Equals(tracker, pkgPath, StringComparison.OrdinalIgnoreCase))
            return;

        if (!File.Exists(pkgPath)) return;

        if (isUpdate) _lastUpdateInfoDumpedFor = pkgPath;
        else _lastGameInfoDumpedFor = pkgPath;

        try
        {
            var info = await Task.Run(() => ReadPkgInfo(pkgPath));

            AppendLog("");
            AppendLog($"=== {label} info ===");
            AppendLog($"File:        {Path.GetFileName(pkgPath)}");

            foreach (var line in info.Lines)
                AppendLog(line);

            AppendLog(new string('=', label.Length + 14));
            AppendLog("");
        }
        catch (Exception ex)
        {
            AppendLog($"(Couldn't read PKG info: {ex.Message})");
        }

        await ReportMismatchIfAnyAsync();
    }

    private async Task ReportMismatchIfAnyAsync()
    {
        if (!File.Exists(txtGame.Text.Trim()) || !File.Exists(txtUpdate.Text.Trim()))
            return;

        var r = await CheckRegionMismatchAsync();
        if (!r.HasData) return;

        if (!r.RegionMismatch && !r.TitleIdMismatch)
            return;

        AppendLog("");
        AppendLog("=== Region / Title ID check ===");
        AppendLog($"Game:   region={r.GameRegion,-3} title={r.GameTitleId}  content={r.GameContentId}");
        AppendLog($"Update: region={r.UpdateRegion,-3} title={r.UpdateTitleId}  content={r.UpdateContentId}");

        if (r.TitleIdMismatch)
        {
            AppendLog("");
            AppendLog("[warn] Title IDs differ. The update was built for a different game.");
            AppendLog("[warn] Run Remarry will rewrite the update to match your game,");
            AppendLog("[warn] but if the games are structurally different the result may misbehave.");
        }
        else if (r.RegionMismatch)
        {
            AppendLog("");
            AppendLog($"[warn] Region mismatch: game is {r.GameRegion}, update is {r.UpdateRegion}.");
            AppendLog("[warn] Run Remarry will rewrite the update to match the game's region.");
        }

        AppendLog("===============================");
        AppendLog("");
    }

    private async Task<MismatchCheckResult> CheckRegionMismatchAsync()
    {
        var gamePath = txtGame.Text.Trim();
        var updatePath = txtUpdate.Text.Trim();

        if (!File.Exists(gamePath) || !File.Exists(updatePath))
            return MismatchCheckResult.NoData();

        try
        {
            var gameInfo = await Task.Run(() => ReadPkgInfo(gamePath));
            var updateInfo = await Task.Run(() => ReadPkgInfo(updatePath));

            string? gameTitle = gameInfo.Lines
                .FirstOrDefault(l => l.StartsWith("TITLE_ID:"))
                ?.Substring("TITLE_ID:".Length).Trim();

            string? updateTitle = updateInfo.Lines
                .FirstOrDefault(l => l.StartsWith("TITLE_ID:"))
                ?.Substring("TITLE_ID:".Length).Trim();

            string? gameContent = gameInfo.Lines
                .FirstOrDefault(l => l.StartsWith("CONTENT_ID:"))
                ?.Substring("CONTENT_ID:".Length).Trim();

            string? updateContent = updateInfo.Lines
                .FirstOrDefault(l => l.StartsWith("CONTENT_ID:"))
                ?.Substring("CONTENT_ID:".Length).Trim();

            if (string.IsNullOrEmpty(gameContent) || string.IsNullOrEmpty(updateContent))
                return MismatchCheckResult.NoData();

            var gameRegion = ExtractRegionPrefix(gameContent);
            var updateRegion = ExtractRegionPrefix(updateContent);

            bool titleMismatch = !string.Equals(gameTitle, updateTitle, StringComparison.OrdinalIgnoreCase);
            bool regionMismatch = !string.Equals(gameRegion, updateRegion, StringComparison.OrdinalIgnoreCase);

            return new MismatchCheckResult(
                HasData: true,
                RegionMismatch: regionMismatch,
                TitleIdMismatch: titleMismatch,
                GameRegion: gameRegion,
                UpdateRegion: updateRegion,
                GameTitleId: gameTitle,
                UpdateTitleId: updateTitle,
                GameContentId: gameContent,
                UpdateContentId: updateContent);
        }
        catch
        {
            return MismatchCheckResult.NoData();
        }
    }

    private static string ExtractRegionPrefix(string contentId)
    {
        if (string.IsNullOrEmpty(contentId)) return "";

        int i = 0;
        while (i < contentId.Length && char.IsLetter(contentId[i])) i++;
        return i > 0 ? contentId.Substring(0, i).ToUpperInvariant() : "";
    }

    private sealed record MismatchCheckResult(
        bool HasData,
        bool RegionMismatch,
        bool TitleIdMismatch,
        string GameRegion,
        string UpdateRegion,
        string? GameTitleId,
        string? UpdateTitleId,
        string GameContentId,
        string UpdateContentId)
    {
        public static MismatchCheckResult NoData() =>
            new(false, false, false, "", "", null, null, "", "");
    }

    private sealed class PkgInfo
    {
        public List<string> Lines { get; } = new();
    }

    private static PkgInfo ReadPkgInfo(string pkgPath)
    {
        var info = new PkgInfo();

        string titleId = "?";
        try { titleId = PkgTitleIdReader.ReadTitleId(pkgPath); } catch { }
        info.Lines.Add($"TITLE_ID:    {titleId}");

        try
        {
            var sfo = SfoParser.ParseToDictionary(pkgPath);
            AddIfPresent(info, sfo, "CONTENT_ID");
            AddIfPresent(info, sfo, "TITLE");
            AddIfPresent(info, sfo, "TITLE_XX");
            AddIfPresent(info, sfo, "CATEGORY");
            AddIfPresent(info, sfo, "APP_VER");
            AddIfPresent(info, sfo, "VERSION");
            AddIfPresent(info, sfo, "ATTRIBUTE");

            if (sfo.TryGetValue("SYSTEM_VER", out var sysVerHex) && !string.IsNullOrWhiteSpace(sysVerHex))
            {
                string readableVer = "Unknown";
                try
                {
                    uint verInt = Convert.ToUInt32(sysVerHex.Replace("0x", ""), 16);
                    readableVer = $"{(verInt >> 24) & 0xFF:X2}.{(verInt >> 12) & 0xFFF:X3}";
                }
                catch { }
                info.Lines.Add($"System Ver:  {readableVer}");
            }

            if (sfo.TryGetValue("PUBTOOLINFO", out var pubToolInfo) && !string.IsNullOrWhiteSpace(pubToolInfo))
            {
                string sdkVer = "Unknown";
                int sdkIndex = pubToolInfo.IndexOf("sdk_ver=", StringComparison.OrdinalIgnoreCase);
                if (sdkIndex >= 0)
                {
                    int start = sdkIndex + "sdk_ver=".Length;
                    int end = pubToolInfo.IndexOf(',', start);
                    if (end < 0) end = pubToolInfo.Length;

                    string rawSdk = pubToolInfo.Substring(start, end - start).Trim();
                    try
                    {
                        uint sdkInt = Convert.ToUInt32(rawSdk, 16);
                        sdkVer = $"{(sdkInt >> 24) & 0xFF:X2}.{(sdkInt >> 12) & 0xFFF:X3}";
                    }
                    catch { }
                }
                info.Lines.Add($"SDK Ver:     {sdkVer}");
            }

            AddIfPresent(info, sfo, "PARENTAL_LEVEL");
        }
        catch (SfoParseException)
        {
            info.Lines.Add("(param.sfo could not be parsed)");
        }

        try
        {
            var art = SfoParser.ExtractCoverArt(pkgPath);
            info.Lines.Add($"Icon:        {(art.Icon0.Found ? $"present ({art.Icon0.Bytes?.Length ?? 0:N0} bytes)" : "none")}");
            info.Lines.Add($"Background:  {(art.Pic1.Found ? $"present ({art.Pic1.Bytes?.Length ?? 0:N0} bytes)" : "none")}");
        }
        catch { }

        try
        {
            var fi = new FileInfo(pkgPath);
            info.Lines.Add($"File size:   {Formatting.FormatBytes(fi.Length)}");
        }
        catch { }

        return info;
    }

    private static void AddIfPresent(PkgInfo info, Dictionary<string, string> sfo, string key)
    {
        if (sfo.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            info.Lines.Add((key + ":").PadRight(13) + v);
    }

    // ---------- Helpers ----------

    private void UpdateRunButtonState()
    {
        bool readyToBuild = !_isBusy
                         && File.Exists(txtGame.Text.Trim())
                         && File.Exists(txtUpdate.Text.Trim());

        btnRun.Enabled = readyToBuild;
        btnMerge.Enabled = readyToBuild;

        SetCoverButtonsEnabled(!_isBusy);
        SetRenameButtonsEnabled(!_isBusy);
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        btnBrowseGame.Enabled = !busy;
        btnBrowseUpdate.Enabled = !busy;
        btnBrowseOutput.Enabled = !busy;
        btnRun.Enabled = !busy && File.Exists(txtGame.Text.Trim()) && File.Exists(txtUpdate.Text.Trim());
        btnMerge.Enabled = !busy && File.Exists(txtGame.Text.Trim()) && File.Exists(txtUpdate.Text.Trim());
        btnCancel.Enabled = busy;
        btnVerifyOutput.Enabled = !busy && _lastBuiltUpdatePkg is not null;
        txtGame.Enabled = !busy;
        txtUpdate.Enabled = !busy;
        txtOutput.Enabled = !busy;
        Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        statusLabel.Text = busy ? "Working..." : "Ready.";

        SetCoverButtonsEnabled(!busy);
        SetRenameButtonsEnabled(!busy);
    }

    /// <summary>
    /// Returns true if the work folder is safe to wipe (or if the user
    /// confirms). Returns false to abort the operation.
    /// </summary>
    private bool ConfirmWorkFolderWipe(string workDir)
    {
        try
        {
            if (!Directory.Exists(workDir)) return true;

            var (files, dirs, bytes) = SafePathGuard.MeasureDirectory(workDir);
            if (files == 0 && dirs == 0) return true;

            var answer = MessageBox.Show(this,
                "The tool's work folder currently contains data:\n\n" +
                workDir + "\n\n" +
                $"{files} file(s), {dirs} folder(s), {Formatting.FormatBytes(bytes)} total.\n\n" +
                "This will be deleted before the next operation runs. Continue?",
                "Work folder will be cleared",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            return answer == DialogResult.Yes;
        }
        catch
        {
            return true;
        }
    }

    private void AppendLog(string line)
    {
        if (txtLog.IsDisposed) return;

        if (line == _lastLoggedLine) return;
        _lastLoggedLine = line;

        if (txtLog.InvokeRequired)
        {
            txtLog.BeginInvoke(new Action<string>(AppendLog), line);
            return;
        }

        txtLog.AppendText(line + Environment.NewLine);
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "PS4 Remarry",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void batchTab_Load(object sender, EventArgs e)
    {

    }
}