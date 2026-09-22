using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class RepackTab : UserControl
{
    private readonly ProcessRunner _runner = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string? _lastLoggedLine;

    public RepackTab()
    {
        InitializeComponent();
        WireEvents();
        UpdateButtonStates();
    }

    private void WireEvents()
    {
        txtSourcePkg.DragEnter += PkgBox_DragEnter;
        txtSourcePkg.DragDrop += PkgBox_DragDrop;
        txtSourcePkg.TextChanged += (_, _) => UpdateButtonStates();

        txtExtractFolder.TextChanged += (_, _) => UpdateButtonStates();

        btnBrowsePkg.Click += (_, _) => BrowsePkg();
        btnBrowseExtract.Click += (_, _) => BrowseExtractFolder();
        btnExtract.Click += async (_, _) => await ExtractAsync();
        btnOpenFolder.Click += (_, _) => OpenExtractFolder();
        btnCheck.Click += (_, _) => RunIntegrityCheck();
        btnRepack.Click += async (_, _) => await RepackAsync();
        btnCleanFolder.Click += (_, _) => CleanExtractFolder();
        btnCancel.Click += (_, _) => CancelCurrentOperation();
    }

    private void BrowsePkg()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select the PKG to extract",
            Filter = "PS4 PKG (*.pkg)|*.pkg|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            txtSourcePkg.Text = dlg.FileName;

            if (string.IsNullOrWhiteSpace(txtExtractFolder.Text))
            {
                var stem = Path.GetFileNameWithoutExtension(dlg.FileName);
                var dir = Path.GetDirectoryName(dlg.FileName) ?? AppContext.BaseDirectory;
                txtExtractFolder.Text = Path.Combine(dir, stem + "_extracted");
            }
        }
    }

    private void BrowseExtractFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where to extract the PKG. The folder must be empty.",
        };
        if (Directory.Exists(txtExtractFolder.Text))
            dlg.SelectedPath = txtExtractFolder.Text;
        else if (Directory.Exists(Path.GetDirectoryName(txtExtractFolder.Text)))
            dlg.SelectedPath = Path.GetDirectoryName(txtExtractFolder.Text)!;

        if (dlg.ShowDialog(this) == DialogResult.OK)
            txtExtractFolder.Text = dlg.SelectedPath;
    }

    private void PkgBox_DragEnter(object? sender, DragEventArgs e)
    {
        if (_isBusy) { e.Effect = DragDropEffects.None; return; }
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void PkgBox_DragDrop(object? sender, DragEventArgs e)
    {
        if (_isBusy) return;
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;

        var pkg = files.FirstOrDefault(f =>
            f.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase)) ?? files[0];

        txtSourcePkg.Text = pkg;

        if (string.IsNullOrWhiteSpace(txtExtractFolder.Text))
        {
            var stem = Path.GetFileNameWithoutExtension(pkg);
            var dir = Path.GetDirectoryName(pkg) ?? AppContext.BaseDirectory;
            txtExtractFolder.Text = Path.Combine(dir, stem + "_extracted");
        }

        AppendLog($"Loaded source: {pkg}");
    }

    private async Task ExtractAsync()
    {
        if (_isBusy) return;

        var source = txtSourcePkg.Text.Trim();
        var extract = txtExtractFolder.Text.Trim();

        if (!File.Exists(source)) { Warn("Source PKG not found."); return; }
        if (string.IsNullOrWhiteSpace(extract)) { Warn("Pick an extract folder."); return; }

        if (Directory.Exists(extract) && Directory.EnumerateFileSystemEntries(extract).Any())
        {
            var answer = MessageBox.Show(this,
                "The extract folder already exists and is not empty:\n\n" +
                extract + "\n\n" +
                "Delete its contents and extract anyway?",
                "Folder not empty", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            try { Directory.Delete(extract, recursive: true); }
            catch (Exception ex) { Warn("Cannot delete existing folder: " + ex.Message); return; }
        }

        var tools = ToolLocator.Default();

        if (!ConfirmWorkFolderWipe(tools.WorkDir))
            return;

        StartOperation();
        progressBar.Value = 0;
        txtLog.Clear();
        AppendLog("=== Extract ===");
        AppendLog($"Source:  {source}");
        AppendLog($"Extract: {extract}");
        AppendLog("---------------");

        var progress = BuildProgress();

        try
        {
            var pipeline = new RepackPipeline(_runner, tools);
            var opts = new RepackExtractOptions(source, extract, tools.WorkDir);

            await Task.Run(() => pipeline.ExtractAsync(opts, progress, _cts!.Token), _cts!.Token);

            AppendLog("[status] Extract complete.");
        }
        catch (OperationCanceledException)
        {
            progressBar.Value = 0;
            AppendLog("Extract cancelled.");
        }
        catch (Exception ex)
        {
            progressBar.Value = 0;
            AppendLog("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Extract error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task RepackAsync()
    {
        if (_isBusy) return;

        var extract = txtExtractFolder.Text.Trim();
        if (!Directory.Exists(extract)) { Warn("Extract folder not found. Extract a PKG first."); return; }

        var paramSfo = Path.Combine(extract, "sce_sys", "param.sfo");
        if (!File.Exists(paramSfo))
        {
            Warn("param.sfo is missing from the extract folder's sce_sys subfolder.\n\n" +
                 "Nothing to repack.");
            return;
        }

        var report = PkgIntegrityChecker.Check(extract);

        AppendLog("");
        AppendLog("=== Integrity check (pre-repack) ===");
        foreach (var item in report.Items)
        {
            var prefix = item.Level switch
            {
                IntegrityLevel.Ok => "[  ok  ]",
                IntegrityLevel.Warn => "[ warn ]",
                IntegrityLevel.Error => "[ERROR ]",
                _ => "[      ]",
            };
            AppendLog($"{prefix} {item.Message}");
        }
        AppendLog($"Summary: {report.OkCount} ok, {report.WarnCount} warning(s), {report.ErrorCount} error(s).");
        AppendLog("====================================");
        AppendLog("");

        if (report.HasErrors)
        {
            var errorLines = report.Items
                .Where(i => i.Level == IntegrityLevel.Error)
                .Select(i => "  - " + i.Message);

            var msg = "The extract folder has errors and may not produce a valid PKG:\n\n" +
                      string.Join("\n", errorLines) +
                      "\n\nFull report is in the log. Continue anyway?";

            var answer = MessageBox.Show(this, msg,
                "Integrity check failed",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        var log = RepackExtractLog.TryRead(extract);
        bool keystoneNeeded;

        if (log is not null)
        {
            keystoneNeeded = log.KeystoneNeeded;
        }
        else
        {
            string cat = "";
            try
            {
                var sfo = SfoParser.ParseToDictionary(paramSfo);
                if (sfo.TryGetValue("CATEGORY", out var c)) cat = c.Trim();
            }
            catch { }
            keystoneNeeded = cat.Equals("gd", StringComparison.OrdinalIgnoreCase);
        }

        var keystonePath = Path.Combine(extract, "sce_sys", "keystone");
        bool keystonePresent = File.Exists(keystonePath);

        if (keystoneNeeded && !keystonePresent)
        {
            var answer = MessageBox.Show(this,
                "This is a base game but sce_sys\\keystone is MISSING.\n\n" +
                "Repacking without a keystone will produce a PKG that installs\n" +
                "but cannot save. Are you sure you want to continue?",
                "Keystone missing",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        var source = txtSourcePkg.Text.Trim();
        var defaultOut = File.Exists(source)
            ? Path.GetDirectoryName(source)!
            : AppContext.BaseDirectory;

        using var outDlg = new FolderBrowserDialog
        {
            Description = "Where should the repacked PKG go?",
            SelectedPath = Directory.Exists(defaultOut) ? defaultOut : AppContext.BaseDirectory,
        };
        if (outDlg.ShowDialog(this) != DialogResult.OK) return;
        var outputFolder = outDlg.SelectedPath;

        var tools = ToolLocator.Default();

        if (!ConfirmWorkFolderWipe(tools.WorkDir))
            return;

        StartOperation();
        progressBar.Value = 0;
        AppendLog("=== Repack ===");
        AppendLog($"Extract: {extract}");
        AppendLog($"Output:  {outputFolder}");
        AppendLog("---------------");

        var progress = BuildProgress();

        try
        {
            var pipeline = new RepackPipeline(_runner, tools);
            var opts = new RepackBuildOptions(extract, outputFolder, tools.WorkDir);

            var outPkg = await Task.Run(() => pipeline.RepackAsync(opts, progress, _cts!.Token), _cts!.Token);

            AppendLog("");
            AppendLog($"Repacked: {outPkg}");

            MessageBox.Show(this,
                "Repack complete.\n\nOutput:\n" + outPkg,
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            progressBar.Value = 0;
            AppendLog("Repack cancelled.");
        }
        catch (Exception ex)
        {
            progressBar.Value = 0;
            AppendLog("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Repack error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            EndOperation();
        }
    }

    private void RunIntegrityCheck()
    {
        var extract = txtExtractFolder.Text.Trim();
        if (!Directory.Exists(extract)) { Warn("Extract folder not found."); return; }

        AppendLog("");
        AppendLog("=== Integrity check ===");
        AppendLog($"Folder: {extract}");
        AppendLog("");

        var report = PkgIntegrityChecker.Check(extract);

        foreach (var item in report.Items)
        {
            var prefix = item.Level switch
            {
                IntegrityLevel.Ok => "[  ok  ]",
                IntegrityLevel.Warn => "[ warn ]",
                IntegrityLevel.Error => "[ERROR ]",
                _ => "[      ]",
            };
            AppendLog($"{prefix} {item.Message}");
        }

        AppendLog("");
        AppendLog($"Summary: {report.OkCount} ok, {report.WarnCount} warning(s), {report.ErrorCount} error(s).");

        if (report.HasErrors)
        {
            AppendLog("");
            AppendLog("Errors must be fixed before repacking.");
        }
        else if (report.HasWarnings)
        {
            AppendLog("Warnings are usually safe to ignore but worth a look.");
        }
        else
        {
            AppendLog("No problems found — safe to repack.");
        }
        AppendLog("=======================");
    }

    private void OpenExtractFolder()
    {
        var extract = txtExtractFolder.Text.Trim();
        if (!Directory.Exists(extract)) { Warn("Extract folder not found."); return; }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "explorer.exe", $"\"{extract}\"")
        { UseShellExecute = true });
    }

    private void CleanExtractFolder()
    {
        var extract = txtExtractFolder.Text.Trim();
        if (!Directory.Exists(extract)) { Warn("Extract folder not found."); return; }

        var answer = MessageBox.Show(this,
            "Delete the extract folder and everything in it?\n\n" + extract,
            "Delete extract folder", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes) return;

        try
        {
            Directory.Delete(extract, recursive: true);
            AppendLog($"Deleted: {extract}");
        }
        catch (Exception ex)
        {
            Warn("Delete failed: " + ex.Message);
        }
    }

    private void StartOperation()
    {
        _isBusy = true;
        _cts = new CancellationTokenSource();
        _lastLoggedLine = null;

        btnBrowsePkg.Enabled = false;
        btnBrowseExtract.Enabled = false;
        btnExtract.Enabled = false;
        btnOpenFolder.Enabled = false;
        btnCheck.Enabled = false;
        btnRepack.Enabled = false;
        btnCleanFolder.Enabled = false;
        btnCancel.Enabled = true;
    }

    private void EndOperation()
    {
        _isBusy = false;
        if (_cts is not null) { _cts.Dispose(); _cts = null; }

        btnBrowsePkg.Enabled = true;
        btnBrowseExtract.Enabled = true;
        btnCancel.Enabled = false;
        UpdateButtonStates();
    }

    private void CancelCurrentOperation()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            progressBar.Value = 0;
            AppendLog("Cancellation requested...");
        }
    }

    private void UpdateButtonStates()
    {
        if (_isBusy) return;

        bool hasSource = File.Exists(txtSourcePkg.Text.Trim());
        bool hasExtract = !string.IsNullOrWhiteSpace(txtExtractFolder.Text.Trim());
        bool extractExists = hasExtract && Directory.Exists(txtExtractFolder.Text.Trim());
        bool extractHasParam = extractExists &&
            File.Exists(Path.Combine(txtExtractFolder.Text.Trim(), "sce_sys", "param.sfo"));

        btnExtract.Enabled = hasSource && hasExtract;
        btnOpenFolder.Enabled = extractExists;
        btnCheck.Enabled = extractExists;
        btnRepack.Enabled = extractHasParam;
        btnCleanFolder.Enabled = extractExists;
    }

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

    private IProgress<ProgressReport> BuildProgress()
    {
        return new Progress<ProgressReport>(r =>
        {
            if (r.Percent is double p)
                progressBar.Value = (int)Math.Clamp(p, 0, 100);

            if (!string.IsNullOrEmpty(r.Message))
                AppendLog(r.Message);
        });
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
        txtLog.SelectionLength = 0;

        txtLog.BeginInvoke(new Action(() =>
        {
            try
            {
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
            catch { }
        }));
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "Repack",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void btnBrowsePkg_Click(object sender, EventArgs e)
    {

    }
}