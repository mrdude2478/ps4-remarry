using System.ComponentModel;
using System.Diagnostics;
using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class BatchTab : UserControl
{
    private readonly ProcessRunner _runner = new();
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    private readonly BindingList<BatchJob> _jobs = new();
    private int _nextId = 1;

    public BatchTab()
    {
        InitializeComponent();
        SetupGridColumns();
        ConfigureNumericUpDown();
        WireEvents();
        UpdateButtonStates();
    }

    private void ConfigureNumericUpDown()
    {
        numParallel.Minimum = 1;
        numParallel.Maximum = Math.Max(1, Environment.ProcessorCount * 2);
        numParallel.Value   = Math.Max(1, Environment.ProcessorCount / 2);
    }

    private void SetupGridColumns()
    {
        // Disable auto-generation BEFORE the data source is assigned. Setting
        // it after, or relying on the designer to preserve it, doesn't work —
        // AutoGenerateColumns is hidden from the designer and it will reset
        // the property on save.
        grid.AutoGenerateColumns = false;

        var colId = new DataGridViewTextBoxColumn
        {
            HeaderText = "#",
            DataPropertyName = nameof(BatchJob.Id),
            FillWeight = 4,
            MinimumWidth = 32,
            ReadOnly = true
        };
        var colGame = new DataGridViewTextBoxColumn
        {
            HeaderText = "Game PKG",
            DataPropertyName = nameof(BatchJob.GamePkg),
            FillWeight = 28,
            ReadOnly = true
        };
        var colUpdate = new DataGridViewTextBoxColumn
        {
            HeaderText = "Update PKG",
            DataPropertyName = nameof(BatchJob.UpdatePkg),
            FillWeight = 28,
            ReadOnly = true
        };
        var colOutput = new DataGridViewTextBoxColumn
        {
            HeaderText = "Output",
            DataPropertyName = nameof(BatchJob.OutputDir),
            FillWeight = 20,
            ReadOnly = true
        };
        var colState = new DataGridViewTextBoxColumn
        {
            HeaderText = "State",
            DataPropertyName = nameof(BatchJob.StateText),
            FillWeight = 6,
            MinimumWidth = 70,
            ReadOnly = true
        };
        var colDigest = new DataGridViewColoredTextColumn
        {
            HeaderText = "Digest",
            DataPropertyName = nameof(BatchJob.DigestText),
            ColorPropertyName = nameof(BatchJob.DigestColorArgb),
            FillWeight = 14,
            MinimumWidth = 90,
            ReadOnly = true
        };
        var colProgress = new DataGridViewProgressColumn
        {
            HeaderText = "Progress",
            DataPropertyName = nameof(BatchJob.Progress),
            FillWeight = 10,
            MinimumWidth = 90,
            ReadOnly = true
        };

        grid.Columns.AddRange(colId, colGame, colUpdate, colOutput,
                              colState, colDigest, colProgress);

        grid.DataSource = _jobs;
    }

    private void WireEvents()
    {
        grid.DragEnter += Grid_DragEnter;
        grid.DragDrop  += Grid_DragDrop;

        btnAdd.Click          += (_, _) => AddRowManually();
        btnAddFromFiles.Click += (_, _) => AddFromFilesDialog();
        btnRemove.Click       += (_, _) => RemoveSelected();
        btnClear.Click        += (_, _) => ClearAll();
        btnRun.Click          += async (_, _) => await RunAllAsync();
        btnCancel.Click       += (_, _) => CancelAll();
    }

    // ---------- Row management ----------

    private void AddRowManually()
    {
        using var gameDlg = new OpenFileDialog
        {
            Title = "Select Game PKG",
            Filter = "PS4 PKG (*.pkg)|*.pkg"
        };
        if (gameDlg.ShowDialog(this) != DialogResult.OK) return;

        using var updateDlg = new OpenFileDialog
        {
            Title = "Select Update PKG",
            Filter = "PS4 PKG (*.pkg)|*.pkg"
        };
        if (updateDlg.ShowDialog(this) != DialogResult.OK) return;

        using var outDlg = new FolderBrowserDialog
        {
            Description = "Output folder for this job"
        };
        if (outDlg.ShowDialog(this) != DialogResult.OK) return;

        AddJob(gameDlg.FileName, updateDlg.FileName, outDlg.SelectedPath);
    }

    private void AddFromFilesDialog()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select PKG files (game + update pairs)",
            Filter = "PS4 PKG (*.pkg)|*.pkg",
            Multiselect = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var files = dlg.FileNames;
        var games   = files.Where(IsLikelyGamePkg).ToList();
        var updates = files.Where(IsLikelyUpdatePkg).ToList();

        int pairs = Math.Min(games.Count, updates.Count);
        if (pairs == 0)
        {
            MessageBox.Show(this,
                "Couldn't identify any Game/Update pairs in the selection.\n\n" +
                "Filenames usually contain 'Game' and 'Update'.",
                "Batch Add", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string outputRoot = Path.Combine(AppContext.BaseDirectory, "Finished");

        for (int i = 0; i < pairs; i++)
            AddJob(games[i], updates[i], outputRoot);
    }

    private static bool IsLikelyGamePkg(string path) =>
        Path.GetFileName(path).IndexOf("game", StringComparison.OrdinalIgnoreCase) >= 0 &&
        Path.GetFileName(path).IndexOf("update", StringComparison.OrdinalIgnoreCase) < 0;

    private static bool IsLikelyUpdatePkg(string path) =>
        Path.GetFileName(path).IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0 ||
        Path.GetFileName(path).IndexOf("patch", StringComparison.OrdinalIgnoreCase) >= 0;

    private void AddJob(string game, string update, string output)
    {
        int id = _nextId++;
        var jobOut = Path.Combine(output, $"Job{id}-{Path.GetFileNameWithoutExtension(game)}");
        Directory.CreateDirectory(jobOut);

        _jobs.Add(new BatchJob(id, game, update, jobOut));
        UpdateButtonStates();
    }

    private void RemoveSelected()
    {
        if (_isRunning) return;
        var toRemove = grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as BatchJob)
            .Where(j => j is not null)
            .ToList();
        foreach (var j in toRemove) _jobs.Remove(j!);
        UpdateButtonStates();
    }

    private void ClearAll()
    {
        if (_isRunning) return;
        _jobs.Clear();
        _nextId = 1;
        UpdateButtonStates();
    }

    // ---------- Drag & drop onto grid ----------

    private void Grid_DragEnter(object? sender, DragEventArgs e)
    {
        if (_isRunning) { e.Effect = DragDropEffects.None; return; }
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void Grid_DragDrop(object? sender, DragEventArgs e)
    {
        if (_isRunning) return;
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;

        var pkgs = files.Where(f => f.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase)).ToList();
        if (pkgs.Count == 0) return;

        var games   = pkgs.Where(IsLikelyGamePkg).ToList();
        var updates = pkgs.Where(IsLikelyUpdatePkg).ToList();
        int pairs = Math.Min(games.Count, updates.Count);

        if (pairs > 0)
        {
            var outputRoot = Path.Combine(AppContext.BaseDirectory, "Finished");
            for (int i = 0; i < pairs; i++)
                AddJob(games[i], updates[i], outputRoot);
            AppendLog($"Added {pairs} pair(s) from dropped files.");
        }
        else if (pkgs.Count == 1)
        {
            AppendLog($"Dropped '{Path.GetFileName(pkgs[0])}' — need both game and update to form a pair.");
        }
        else
        {
            AppendLog("Couldn't identify Game/Update pairs from dropped files.");
        }
    }

    // ---------- Run / Cancel ----------

    private async Task RunAllAsync()
    {
        if (_isRunning || _jobs.Count == 0) return;

        foreach (var j in _jobs)
        {
            if (j.State != BatchJobState.Completed)
            {
                j.State = BatchJobState.Queued;
                j.Progress = 0;
                j.Status = "Queued";
                j.DigestStatus = DigestStatus.Unknown;
                j.BuiltPkgPath = "";
            }
        }

        _cts = new CancellationTokenSource();
        SetRunning(true);
        txtLog.Clear();
        AppendLog($"Starting batch of {_jobs.Count} job(s), parallelism = {numParallel.Value}.");

        var tools  = ToolLocator.Default();
        var runner = new BatchRunner(_runner, tools);

        int maxParallel = (int)numParallel.Value;

        try
        {
            await Task.Run(() => runner.RunAllAsync(
                _jobs.ToList(),
                maxParallel,
                perJobProgress: (id, report) =>
                {
                    if (IsHandleCreated && !IsDisposed)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            if (!string.IsNullOrEmpty(report.Message))
                                AppendLog($"[Job {id}] {report.Message}");
                        }));
                    }
                },
                ct: _cts.Token));
        }
        catch (OperationCanceledException)
        {
            AppendLog("Batch cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog("Batch error: " + ex.Message);
        }
        finally
        {
            SetRunning(false);

            int ok  = _jobs.Count(j => j.State == BatchJobState.Completed);
            int bad = _jobs.Count(j => j.State == BatchJobState.Failed);
            int cx  = _jobs.Count(j => j.State == BatchJobState.Cancelled);
            AppendLog($"Batch complete: {ok} ok, {bad} failed, {cx} cancelled.");

            _cts.Dispose();
            _cts = null;
        }
    }

    private void CancelAll()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            AppendLog("Cancellation requested for all jobs...");
        }
    }

    // ---------- State ----------

    private void SetRunning(bool running)
    {
        _isRunning = running;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool has = _jobs.Count > 0;
        btnAdd.Enabled          = !_isRunning;
        btnAddFromFiles.Enabled = !_isRunning;
        btnRemove.Enabled       = !_isRunning && has;
        btnClear.Enabled        = !_isRunning && has;
        btnRun.Enabled          = !_isRunning && has;
        btnCancel.Enabled       = _isRunning;
        numParallel.Enabled     = !_isRunning;
    }

    private void AppendLog(string line)
    {
        if (txtLog.IsDisposed) return;
        if (txtLog.InvokeRequired)
        {
            txtLog.BeginInvoke(new Action<string>(AppendLog), line);
            return;
        }
        txtLog.AppendText(line + Environment.NewLine);
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
    }
}