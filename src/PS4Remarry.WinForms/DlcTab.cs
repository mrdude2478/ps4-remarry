using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class DlcTab : UserControl
{
    private readonly ProcessRunner _runner = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string? _lastLoggedLine;

    public DlcTab()
    {
        InitializeComponent();
        WireEvents();
        LoadDefaults();
        UpdateButtonStates();
    }

    private void LoadDefaults()
    {
        txtOutput.Text = Path.Combine(AppContext.BaseDirectory, "Finished");
    }

    private void WireEvents()
    {
        txtContentId.TextChanged += (_, _) => OnContentIdChanged();
        txtTitle.TextChanged += (_, _) => UpdateButtonStates();
        txtIcon.TextChanged += (_, _) => UpdateButtonStates();
        txtOutput.TextChanged += (_, _) => UpdateButtonStates();

        btnBrowseIcon.Click += (_, _) => BrowseIcon();
        btnClearIcon.Click += (_, _) => txtIcon.Clear();
        btnBrowseOutput.Click += (_, _) => BrowseOutput();
        btnCreate.Click += async (_, _) => await CreateAsync();
        btnOpenOutput.Click += (_, _) => OpenOutputFolder();
        btnCancel.Click += (_, _) => CancelCurrentOperation();
    }

    // ---------- Live title ID preview ----------

    private void OnContentIdChanged()
    {
        var contentId = txtContentId.Text.Trim();

        if (contentId.Length < 16)
        {
            lblTitleIdPreview.Text = contentId.Length == 0
                ? ""
                : $"(too short to derive title ID — need at least 16 characters)";
        }
        else
        {
            var titleId = contentId.Substring(7, 9);
            lblTitleIdPreview.Text = $"(derived Title ID: {titleId})";
        }

        UpdateButtonStates();
    }

    // ---------- Browsing ----------

    private void BrowseIcon()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select icon (512x512 PNG recommended)",
            Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            txtIcon.Text = dlg.FileName;
    }

    private void BrowseOutput()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where the DLC unlocker PKG will be saved",
            SelectedPath = Directory.Exists(txtOutput.Text) ? txtOutput.Text : AppContext.BaseDirectory,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var chosen = dlg.SelectedPath;

        // SAFETY: reject protected roots (Desktop itself, Documents, ...).
        // Subfolders are fine.
        if (SafePathGuard.IsProtectedRoot(chosen))
        {
            var suggestion = Path.Combine(chosen, "DLC");
            Warn(
                "You chose a protected Windows folder:" + Environment.NewLine + Environment.NewLine +
                chosen + Environment.NewLine + Environment.NewLine +
                "This folder contains your personal files and the tool will not " +
                "use it as an output location." + Environment.NewLine + Environment.NewLine +
                "Please create a subfolder (for example, " + suggestion + ") " +
                "and select that instead.");
            return;
        }

        txtOutput.Text = chosen;
    }

    private void OpenOutputFolder()
    {
        var outDir = txtOutput.Text.Trim();
        if (!Directory.Exists(outDir)) { Warn("Output folder does not exist yet."); return; }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "explorer.exe", $"\"{outDir}\"")
        { UseShellExecute = true });
    }

    // ---------- Create ----------

    private async Task CreateAsync()
    {
        if (_isBusy) return;

        var contentId = txtContentId.Text.Trim();
        var title = txtTitle.Text.Trim();
        var icon = txtIcon.Text.Trim();
        var output = txtOutput.Text.Trim();

        if (string.IsNullOrEmpty(contentId)) { Warn("Enter a Content ID."); return; }
        if (contentId.Length < 16) { Warn("Content ID is too short."); return; }
        if (string.IsNullOrEmpty(title)) { Warn("Enter a Title."); return; }
        if (string.IsNullOrWhiteSpace(output)) { Warn("Pick an output folder."); return; }

        // SAFETY: refuse a protected root as output, even if typed by hand.
        if (SafePathGuard.IsProtectedRoot(output))
        {
            var suggestion = Path.Combine(output, "DLC");
            Warn(
                "The output folder is a protected Windows folder:" + Environment.NewLine + Environment.NewLine +
                output + Environment.NewLine + Environment.NewLine +
                "This folder contains your personal files and cannot be used as " +
                "an output location." + Environment.NewLine + Environment.NewLine +
                "Please create a subfolder (for example, " + suggestion + ") " +
                "and use that instead.");
            return;
        }

        var answer = MessageBox.Show(this,
            "Create DLC unlocker?" + Environment.NewLine + Environment.NewLine +
            $"Content ID:  {contentId}" + Environment.NewLine +
            $"Title:       {title}" + Environment.NewLine +
            $"Icon:        {(string.IsNullOrEmpty(icon) ? "(none)" : Path.GetFileName(icon))}" + Environment.NewLine +
            $"Output:      {output}",
            "Create DLC Unlocker", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        if (answer != DialogResult.OK) return;

        StartOperation();

        _lastLoggedLine = null;
        txtLog.Clear();
        progressBar.Value = 0;

        AppendLog("=== Create DLC Unlocker ===");
        AppendLog($"Content ID: {contentId}");
        AppendLog($"Title:      {title}");
        AppendLog($"Icon:       {(string.IsNullOrEmpty(icon) ? "(none)" : icon)}");
        AppendLog($"Output:     {output}");
        AppendLog("---------------------------");

        var progress = BuildProgress();

        try
        {
            var tools = ToolLocator.Default();
            var pipeline = new DlcUnlockerPipeline(_runner, tools);
            var opts = new DlcUnlockerOptions(
                contentId,
                title,
                string.IsNullOrWhiteSpace(icon) ? null : icon,
                output,
                tools.WorkDir);

            var result = await Task.Run(
                () => pipeline.RunAsync(opts, progress, _cts!.Token),
                _cts!.Token);

            if (!result.Success)
            {
                AppendLog("ERROR: " + (result.Error ?? "unknown"));
                MessageBox.Show(this,
                    result.Error ?? "Unknown error.",
                    "DLC Unlocker failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppendLog("");
            AppendLog("Created: " + result.OutputPath);

            MessageBox.Show(this,
                "DLC unlocker created:" + Environment.NewLine + Environment.NewLine +
                result.OutputPath,
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var open = MessageBox.Show(this,
                "Open the output folder?", "Success",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (open == DialogResult.Yes)
                OpenOutputFolder();
        }
        catch (OperationCanceledException)
        {
            AppendLog("Cancelled.");
            progressBar.Value = 0;
        }
        catch (Exception ex)
        {
            AppendLog("ERROR: " + ex.Message);
            progressBar.Value = 0;
            MessageBox.Show(this, ex.Message, "DLC Unlocker failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            EndOperation();
        }
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

    // ---------- Operation state ----------

    private void StartOperation()
    {
        _isBusy = true;
        _cts = new CancellationTokenSource();

        txtContentId.Enabled = false;
        txtTitle.Enabled = false;
        txtIcon.Enabled = false;
        txtOutput.Enabled = false;
        btnBrowseIcon.Enabled = false;
        btnClearIcon.Enabled = false;
        btnBrowseOutput.Enabled = false;
        btnCreate.Enabled = false;
        btnOpenOutput.Enabled = false;
        btnCancel.Enabled = true;
    }

    private void EndOperation()
    {
        _isBusy = false;
        if (_cts is not null) { _cts.Dispose(); _cts = null; }

        txtContentId.Enabled = true;
        txtTitle.Enabled = true;
        txtIcon.Enabled = true;
        txtOutput.Enabled = true;
        btnBrowseIcon.Enabled = true;
        btnClearIcon.Enabled = true;
        btnBrowseOutput.Enabled = true;
        btnOpenOutput.Enabled = true;
        btnCancel.Enabled = false;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (_isBusy) return;

        var contentId = txtContentId.Text.Trim();
        var title = txtTitle.Text.Trim();

        bool ready = contentId.Length >= 16 && !string.IsNullOrWhiteSpace(title);
        btnCreate.Enabled = ready;
    }

    // ---------- Logging ----------

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
        txtLog.ScrollToCaret();
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "DLC Unlocker",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}