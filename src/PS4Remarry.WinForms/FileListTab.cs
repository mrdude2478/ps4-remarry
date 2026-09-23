using System.Text;
using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class FileListTab : UserControl
{
    private readonly ProcessRunner _runner = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string? _lastLoggedLine;

    private PkgFileListReader.TreeNode? _root;
    private string? _loadedPkgPath;

    // Accumulates every line of img_file_list output in memory. The txtLog
    // box gets a throttled view of this so the UI stays responsive.
    private readonly StringBuilder _logBuffer = new();

    // Throttling for the log box: only push to the UI every 200 ms.
    private System.Windows.Forms.Timer? _logFlushTimer;
    private bool _logDirty;

    private string? _lastInfoDumpedFor;

    public FileListTab()
    {
        InitializeComponent();
        WireEvents();
        SetupLogFlushTimer();
        UpdateButtonStates();
    }

    private void SetupLogFlushTimer()
    {
        _logFlushTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _logFlushTimer.Tick += (_, _) => FlushLogIfDirty();
        _logFlushTimer.Start();
    }

    private void WireEvents()
    {
        txtSourcePkg.AllowDrop = true;
        txtSourcePkg.DragEnter += PkgBox_DragEnter;
        txtSourcePkg.DragDrop += PkgBox_DragDrop;
        txtSourcePkg.TextChanged += (_, _) => UpdateButtonStates();

        btnBrowsePkg.Click += (_, _) => BrowsePkg();
        btnLoad.Click += async (_, _) => await LoadFileListAsync();
        btnExtractSelected.Click += async (_, _) => await ExtractSelectedAsync();
        btnCancel.Click += (_, _) => CancelCurrentOperation();

        treeFiles.BeforeExpand += TreeFiles_BeforeExpand;
        treeFiles.AfterSelect += TreeFiles_AfterSelect;

        btnExtractAll.Click += async (_, _) => await ExtractAllAsync();
    }

    // ---------- Browsing / drag/drop ----------

    private void BrowsePkg()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select the PKG to inspect",
            Filter = "PS4 PKG (*.pkg)|*.pkg|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            txtSourcePkg.Text = dlg.FileName;
            ClearTree();
            _lastInfoDumpedFor = null;   // allow re-dump for this new file
            _ = DumpPkgInfoAsync(dlg.FileName);
        }
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
        ClearTree();
        _lastInfoDumpedFor = null;
        _ = DumpPkgInfoAsync(pkg);
    }

    // ---------- Load ----------

    private async Task LoadFileListAsync()
    {
        if (_isBusy) return;

        var source = txtSourcePkg.Text.Trim();
        if (!File.Exists(source)) { Warn("Load a PKG first."); return; }

        StartOperation();

        // Clear both the visible log and the buffer.
        _logBuffer.Clear();
        txtLog.Clear();
        ClearTree();
        lblSummary.Text = "";
        progressBar.Value = 0;

        AppendLogToBuffer("=== img_file_list ===");
        AppendLogToBuffer($"Source: {source}");
        AppendLogToBuffer("---------------------");
        FlushLogIfDirty();

        var progress = BuildProgress();

        try
        {
            var tools = ToolLocator.Default();

            // Phase 1 — run the process, accumulate lines in _logBuffer.
            var (ok, err) = await PkgFileListReader.RunImgFileListAsync(
                source, tools, _runner, _logBuffer, progress, _cts!.Token);

            FlushLogIfDirty();

            if (!ok)
            {
                AppendLogToBuffer("ERROR: " + (err ?? "unknown"));
                FlushLogIfDirty();
                statusLog("Load failed.");
                return;
            }

            AppendLogToBuffer("");
            AppendLogToBuffer($"img_file_list finished. Parsing {_logBuffer.Length:N0} chars...");
            FlushLogIfDirty();

            progressBar.Value = 50;

            // Phase 2 — parse the accumulated lines.
            var lines = SplitBufferIntoLines(_logBuffer);
            var result = PkgFileListReader.ParseFromLines(lines);

            progressBar.Value = 80;

            if (!result.Success || result.Root is null)
            {
                AppendLogToBuffer("ERROR: " + (result.Error ?? "parse produced nothing."));
                FlushLogIfDirty();
                statusLog("Parse failed.");
                return;
            }

            _root = result.Root;
            _loadedPkgPath = source;

            PopulateTreeTopLevel(result.Root);

            lblSummary.Text =
                $"{result.FileCount:N0} files, {Formatting.FormatBytes(result.TotalBytes)} total.";
            statusLog($"Loaded {result.FileCount:N0} files.");

            progressBar.Value = 100;
        }
        catch (OperationCanceledException)
        {
            AppendLogToBuffer("Cancelled.");
            FlushLogIfDirty();
            progressBar.Value = 0;
        }
        catch (Exception ex)
        {
            AppendLogToBuffer("ERROR: " + ex.Message);
            FlushLogIfDirty();
            progressBar.Value = 0;
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task DumpPkgInfoAsync(string pkgPath)
    {
        if (string.Equals(_lastInfoDumpedFor, pkgPath, StringComparison.OrdinalIgnoreCase))
            return;

        if (!File.Exists(pkgPath)) return;

        _lastInfoDumpedFor = pkgPath;

        try
        {
            var tools = ToolLocator.Default();
            var (ok, err, lines) = await PkgFileListReader.RunImgInfoAsync(
                pkgPath, tools, _runner);

            if (!ok)
            {
                AppendLogToBuffer($"[img_info] failed: {err}");
                FlushLogIfDirty();
                return;
            }

            var paramsSection = PkgFileListReader.ExtractParamsSection(lines);

            AppendLogToBuffer("");
            AppendLogToBuffer("=== PKG info (from img_info) ===");
            AppendLogToBuffer($"File: {Path.GetFileName(pkgPath)}");
            AppendLogToBuffer("");

            if (paramsSection.Count == 0)
            {
                AppendLogToBuffer("(no [Params] section found in img_info output)");
            }
            else
            {
                foreach (var line in paramsSection)
                    AppendLogToBuffer("  " + line);
            }

            AppendLogToBuffer("===============================");
            AppendLogToBuffer("");
            FlushLogIfDirty();
        }
        catch (Exception ex)
        {
            AppendLogToBuffer($"[img_info] error: {ex.Message}");
            FlushLogIfDirty();
        }
    }

    private static IEnumerable<string> SplitBufferIntoLines(StringBuilder buffer)
    {
        // Split on \n and trim trailing \r so both Windows and Unix line
        // endings parse the same way.
        var text = buffer.ToString();
        return text.Split('\n').Select(l => l.TrimEnd('\r'));
    }

    // ---------- Extract one file ----------

    private async Task ExtractSelectedAsync()
    {
        if (_isBusy) return;

        var node = treeFiles.SelectedNode?.Tag as PkgFileListReader.TreeNode;
        if (node is null || node.IsFolder)
        {
            Warn("Select a file (not a folder) to extract.");
            return;
        }

        var source = txtSourcePkg.Text.Trim();
        if (!File.Exists(source)) { Warn("Source PKG not found."); return; }

        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where to extract the file. The PKG's internal " +
                          "folder structure will be recreated under this folder.",
            SelectedPath = Directory.Exists(Path.GetDirectoryName(source))
                ? Path.GetDirectoryName(source)!
                : AppContext.BaseDirectory,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var outputRoot = dlg.SelectedPath;

        StartOperation();
        progressBar.Value = 0;

        AppendLogToBuffer("");
        AppendLogToBuffer("=== Extract single file ===");
        AppendLogToBuffer($"File:   {node.FullPath}");
        AppendLogToBuffer($"Size:   {Formatting.FormatBytes(node.Size)}");
        AppendLogToBuffer($"Output: {outputRoot}");
        AppendLogToBuffer("---------------------------");
        FlushLogIfDirty();

        var progress = BuildProgress();

        try
        {
            var tools = ToolLocator.Default();
            var result = await PkgFileExtractor.ExtractOneAsync(
                source, node.FullPath, outputRoot, tools, _runner, progress, _cts!.Token);

            FlushLogIfDirty();

            if (!result.Success)
            {
                AppendLogToBuffer("ERROR: " + (result.Error ?? "unknown"));
                FlushLogIfDirty();
                statusLog("Extract failed.");
                return;
            }

            AppendLogToBuffer("");
            AppendLogToBuffer("Extracted: " + result.ExtractedPath);
            FlushLogIfDirty();
            statusLog("Extract complete.");

            MessageBox.Show(this,
                "File extracted to:\n\n" + result.ExtractedPath,
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            AppendLogToBuffer("Cancelled.");
            FlushLogIfDirty();
            progressBar.Value = 0;
        }
        catch (Exception ex)
        {
            AppendLogToBuffer("ERROR: " + ex.Message);
            FlushLogIfDirty();
            progressBar.Value = 0;
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task ExtractAllAsync()
    {
        if (_isBusy) return;

        var source = txtSourcePkg.Text.Trim();
        if (!File.Exists(source)) { Warn("Source PKG not found."); return; }

        // Default output folder: same directory as the source PKG, named after it.
        var sourceDir = Path.GetDirectoryName(source) ?? AppContext.BaseDirectory;
        var suggestedName = Path.GetFileNameWithoutExtension(source) + "_extracted";
        var suggestedPath = Path.Combine(sourceDir, suggestedName);

        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where to extract the PKG. The folder will be " +
                          "normalised so it can be repacked directly from the Repack tab.",
            SelectedPath = Directory.Exists(suggestedPath) ? suggestedPath : sourceDir,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var outputRoot = dlg.SelectedPath;

        // Refuse to extract into a folder that already has content, unless the
        // user agrees to delete it. RepackPipeline.ExtractAsync requires an
        // empty or non-existent target folder.
        if (Directory.Exists(outputRoot) &&
            Directory.EnumerateFileSystemEntries(outputRoot).Any())
        {
            var answer = MessageBox.Show(this,
                "The chosen folder is not empty:\n\n" + outputRoot + "\n\n" +
                "Delete its contents and extract anyway?",
                "Folder not empty", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            try { Directory.Delete(outputRoot, recursive: true); }
            catch (Exception ex) { Warn("Cannot delete existing folder: " + ex.Message); return; }
        }

        var tools = ToolLocator.Default();

        // Same safety check as the Repack tab.
        if (!ConfirmWorkFolderWipe(tools.WorkDir))
            return;

        StartOperation();
        progressBar.Value = 0;

        AppendLogToBuffer("");
        AppendLogToBuffer("=== Extract all ===");
        AppendLogToBuffer($"Source: {source}");
        AppendLogToBuffer($"Output: {outputRoot}");
        AppendLogToBuffer("-------------------");
        FlushLogIfDirty();

        var progress = BuildProgress();

        try
        {
            var pipeline = new RepackPipeline(_runner, tools);
            var opts = new RepackExtractOptions(source, outputRoot, tools.WorkDir);

            await Task.Run(() => pipeline.ExtractAsync(opts, progress, _cts!.Token),
                           _cts!.Token);

            FlushLogIfDirty();

            AppendLogToBuffer("");
            AppendLogToBuffer("Extract all complete.");
            AppendLogToBuffer("The folder is normalised and can be repacked from the Repack tab.");
            FlushLogIfDirty();
            statusLog("Extract all complete.");

            var answer = MessageBox.Show(this,
                "Extract all complete.\n\nOutput:\n" + outputRoot + "\n\n" +
                "Open the folder in Explorer?",
                "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (answer == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "explorer.exe", $"\"{outputRoot}\"")
                { UseShellExecute = true });
            }
        }
        catch (OperationCanceledException)
        {
            AppendLogToBuffer("Cancelled.");
            FlushLogIfDirty();
            progressBar.Value = 0;
        }
        catch (Exception ex)
        {
            AppendLogToBuffer("ERROR: " + ex.Message);
            FlushLogIfDirty();
            progressBar.Value = 0;
        }
        finally
        {
            EndOperation();
        }
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

    private void CancelCurrentOperation()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            progressBar.Value = 0;
            AppendLogToBuffer("Cancellation requested...");
            FlushLogIfDirty();
        }
    }

    // ---------- Tree population ----------

    private void ClearTree()
    {
        treeFiles.Nodes.Clear();
        _root = null;
        _loadedPkgPath = null;
        txtPath.Clear();
        txtType.Clear();
        btnExtractSelected.Enabled = false;
    }

    private void PopulateTreeTopLevel(PkgFileListReader.TreeNode root)
    {
        treeFiles.BeginUpdate();
        try
        {
            treeFiles.Nodes.Clear();
            foreach (var child in root.Children)
                treeFiles.Nodes.Add(CreateTreeNodeFor(child));
        }
        finally
        {
            treeFiles.EndUpdate();
        }
    }

    private TreeNode CreateTreeNodeFor(PkgFileListReader.TreeNode model)
    {
        var node = new TreeNode(model.Name)
        {
            Tag = model,
            ToolTipText = model.IsFolder
                ? $"{model.FullPath}  ({Formatting.FormatBytes(model.Size)})"
                : $"{model.FullPath}  ({Formatting.FormatBytes(model.Size)})",
        };

        if (model.IsFolder)
            node.Nodes.Add(new TreeNode("..."));

        return node;
    }

    private void TreeFiles_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        var node = e.Node;
        if (node is null) return;
        if (node.Tag is not PkgFileListReader.TreeNode model) return;
        if (!model.IsFolder) return;

        if (node.Nodes.Count == 1 && node.Nodes[0].Tag is null &&
            node.Nodes[0].Text == "...")
        {
            node.Nodes.Clear();
            foreach (var child in model.Children)
                node.Nodes.Add(CreateTreeNodeFor(child));
        }
    }

    private void TreeFiles_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        var node = e.Node?.Tag as PkgFileListReader.TreeNode;
        if (node is null)
        {
            btnExtractSelected.Enabled = false;
            return;
        }

        txtPath.Text = node.FullPath;
        txtType.Text = node.IsFolder ? "Folder" : ClassifyFile(node.Name);

        btnExtractSelected.Enabled = !node.IsFolder && !_isBusy;
    }

    private static string ClassifyFile(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext switch
        {
            ".pkg" => "PKG package",
            ".sfo" => "System metadata",
            ".png" => "Image (PNG)",
            ".dds" => "Image (DDS)",
            ".at9" => "Audio (AT9)",
            ".mp4" => "Video (MP4)",
            ".prx" => "PS4 module",
            ".bin" => "Binary data",
            ".ipak" => "Asset pack",
            ".manifest" => "Manifest",
            ".dat" => "Data file",
            ".lua" => "Lua script",
            ".xml" => "XML data",
            ".json" => "JSON data",
            ".trp" => "Trophy data",
            _ => "File"
        };
    }

    // ---------- Operation state ----------

    private void StartOperation()
    {
        _isBusy = true;
        _cts = new CancellationTokenSource();
        _lastLoggedLine = null;

        btnBrowsePkg.Enabled = false;
        btnLoad.Enabled = false;
        btnExtractSelected.Enabled = false;
        btnCancel.Enabled = true;
        btnExtractAll.Enabled = false;
        txtSourcePkg.Enabled = false;
    }

    private void EndOperation()
    {
        _isBusy = false;
        if (_cts is not null) { _cts.Dispose(); _cts = null; }

        btnBrowsePkg.Enabled = true;
        btnLoad.Enabled = true;
        btnCancel.Enabled = false;
        txtSourcePkg.Enabled = true;
        UpdateButtonStates();

        var selected = treeFiles.SelectedNode?.Tag as PkgFileListReader.TreeNode;
        btnExtractSelected.Enabled = selected is { IsFolder: false } && !_isBusy;
    }

    private void UpdateButtonStates()
    {
        if (_isBusy) return;

        bool hasSource = File.Exists(txtSourcePkg.Text.Trim());
        btnLoad.Enabled = hasSource;
        btnExtractAll.Enabled = hasSource;

        var selected = treeFiles.SelectedNode?.Tag as PkgFileListReader.TreeNode;
        btnExtractSelected.Enabled = selected is { IsFolder: false };
    }

    // ---------- Logging with throttling ----------

    private void AppendLogToBuffer(string line)
    {
        if (line == _lastLoggedLine) return;
        _lastLoggedLine = line;

        _logBuffer.AppendLine(line);
        _logDirty = true;
    }

    private void FlushLogIfDirty()
    {
        if (!_logDirty) return;
        if (txtLog.IsDisposed) return;
        _logDirty = false;

        // Push the entire buffer to the text box. For large buffers this is
        // cheaper than appending line by line, and it runs at most 5×/s.
        var text = _logBuffer.ToString();
        if (txtLog.TextLength == text.Length) return;

        // Preserve scroll-at-bottom behaviour: only autoscroll if the user
        // hasn't scrolled up.
        bool wasAtEnd = txtLog.SelectionStart >= txtLog.TextLength - 1;

        txtLog.Text = text;

        if (wasAtEnd)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }
    }

    private IProgress<ProgressReport> BuildProgress()
    {
        return new Progress<ProgressReport>(r =>
        {
            if (r.Percent is double p)
                progressBar.Value = (int)Math.Clamp(p, 0, 100);

            if (!string.IsNullOrEmpty(r.Message))
                AppendLogToBuffer(r.Message);
        });
    }

    private void statusLog(string text) => AppendLogToBuffer($"[status] {text}");

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "File List",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}