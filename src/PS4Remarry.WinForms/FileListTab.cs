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

    private readonly StringBuilder _logBuffer = new();

    private System.Windows.Forms.Timer? _logFlushTimer;
    private bool _logDirty;

    private string? _lastInfoDumpedFor;

    private ContextMenuStrip? _treeContextMenu;
    private ToolStripMenuItem? _miExtractFolder;
    private ToolStripMenuItem? _miExtractFile;

    private ContextMenuStrip? _logContextMenu;

    public FileListTab()
    {
        InitializeComponent();
        WireEvents();
        BuildTreeContextMenu();
        BuildLogContextMenu();
        SetupLogFlushTimer();
        UpdateButtonStates();
    }

    private void SetupLogFlushTimer()
    {
        _logFlushTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _logFlushTimer.Tick += (_, _) => FlushLogIfDirty();
        _logFlushTimer.Start();
    }

    private void BuildTreeContextMenu()
    {
        _treeContextMenu = new ContextMenuStrip();

        _miExtractFolder = new ToolStripMenuItem("Extract Folder...");
        _miExtractFolder.Click += async (_, _) =>
        {
            var node = treeFiles.SelectedNode?.Tag as PkgFileListReader.TreeNode;
            if (node is null || !node.IsFolder) return;
            await ExtractFolderAsync(node);
        };

        _miExtractFile = new ToolStripMenuItem("Extract File...");
        _miExtractFile.Click += async (_, _) => await ExtractSelectedAsync();

        _treeContextMenu.Items.Add(_miExtractFolder);
        _treeContextMenu.Items.Add(_miExtractFile);
        _treeContextMenu.Items.Add(new ToolStripSeparator());
        _treeContextMenu.Items.Add(new ToolStripMenuItem("Copy Path", null, (_, _) =>
        {
            var node = treeFiles.SelectedNode?.Tag as PkgFileListReader.TreeNode;
            if (node is not null && !string.IsNullOrEmpty(node.FullPath))
            {
                try { Clipboard.SetText(node.FullPath); } catch { }
            }
        }));
    }

    private void BuildLogContextMenu()
    {
        _logContextMenu = new ContextMenuStrip();

        _logContextMenu.Items.Add(new ToolStripMenuItem("Clear Log", null, (_, _) =>
        {
            _logBuffer.Clear();
            txtLog.Clear();
            _lastLoggedLine = null;
            _logDirty = false;
            _lastInfoDumpedFor = null;
        }));

        _logContextMenu.Items.Add(new ToolStripSeparator());

        _logContextMenu.Items.Add(new ToolStripMenuItem("Copy All", null, (_, _) =>
        {
            try
            {
                if (!string.IsNullOrEmpty(txtLog.Text))
                    Clipboard.SetText(txtLog.Text);
            }
            catch { }
        }));

        _logContextMenu.Items.Add(new ToolStripMenuItem("Copy Selected", null, (_, _) =>
        {
            try
            {
                if (!string.IsNullOrEmpty(txtLog.SelectedText))
                    Clipboard.SetText(txtLog.SelectedText);
            }
            catch { }
        }));

        _logContextMenu.Items.Add(new ToolStripMenuItem("Save Log As...", null, (_, _) =>
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Save log",
                Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = "filelist-log.txt",
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(dlg.FileName, txtLog.Text); } catch { }
        }));

        txtLog.ContextMenuStrip = _logContextMenu;
    }

    private void WireEvents()
    {
        txtSourcePkg.AllowDrop = true;
        txtSourcePkg.DragEnter += PkgBox_DragEnter;
        txtSourcePkg.DragDrop  += PkgBox_DragDrop;
        txtSourcePkg.TextChanged += (_, _) => UpdateButtonStates();

        btnBrowsePkg.Click       += (_, _) => BrowsePkg();
        btnLoad.Click            += async (_, _) => await LoadFileListAsync();
        btnExtractSelected.Click += async (_, _) => await ExtractSelectedAsync();
        btnExtractAll.Click      += async (_, _) => await ExtractAllAsync();
        btnCancel.Click          += (_, _) => CancelCurrentOperation();

        treeFiles.BeforeExpand += TreeFiles_BeforeExpand;
        treeFiles.AfterSelect  += TreeFiles_AfterSelect;
        treeFiles.MouseUp      += TreeFiles_MouseUp;
    }

    private void TreeFiles_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        if (_treeContextMenu is null) return;

        var hit = treeFiles.GetNodeAt(e.X, e.Y);
        if (hit is not null)
            treeFiles.SelectedNode = hit;

        var node = treeFiles.SelectedNode?.Tag as PkgFileListReader.TreeNode;
        bool hasNode = node is not null;
        bool isFolder = node?.IsFolder == true;

        if (_miExtractFolder is not null) _miExtractFolder.Enabled = hasNode && isFolder && !_isBusy;
        if (_miExtractFile is not null)   _miExtractFile.Enabled   = hasNode && !isFolder && !_isBusy;

        if (hasNode)
            _treeContextMenu.Show(treeFiles, e.Location);
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
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        txtSourcePkg.Text = dlg.FileName;
        ResetForNewPkg();
        _ = DumpPkgInfoAsync(dlg.FileName);
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
        ResetForNewPkg();
        _ = DumpPkgInfoAsync(pkg);
    }

    private void ResetForNewPkg()
    {
        ClearTree();
        _logBuffer.Clear();
        txtLog.Clear();
        _lastLoggedLine = null;
        _logDirty = false;
        _lastInfoDumpedFor = null;
        lblSummary.Text = "";
    }

    // ---------- Load ----------

    private async Task LoadFileListAsync()
    {
        if (_isBusy) return;

        var source = txtSourcePkg.Text.Trim();
        if (!File.Exists(source)) { Warn("Load a PKG first."); return; }

        StartOperation();

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

            lblSummary.Text = $"{result.FileCount:N0} files.";
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

        await ExtractManyAsync(new[] { node }, $"File: {node.FullPath}");
    }

    // ---------- Extract a folder and all its contents ----------

    private async Task ExtractFolderAsync(PkgFileListReader.TreeNode folder)
    {
        if (_isBusy) return;

        var files = CollectLeaves(folder).ToList();
        if (files.Count == 0)
        {
            Warn("The selected folder contains no files.");
            return;
        }

        var leafName = folder.FullPath.Split('/').Last();

        await ExtractManyAsync(
            files,
            $"Folder: {folder.FullPath} ({files.Count} file(s))",
            stripPrefix: folder.FullPath,
            folderName: leafName);
    }

    private static IEnumerable<PkgFileListReader.TreeNode> CollectLeaves(PkgFileListReader.TreeNode folder)
    {
        if (!folder.IsFolder) { yield return folder; yield break; }

        foreach (var child in folder.Children)
        {
            if (child.IsFolder)
            {
                foreach (var leaf in CollectLeaves(child))
                    yield return leaf;
            }
            else
            {
                yield return child;
            }
        }
    }

    // ---------- Shared extraction core ----------

    private async Task ExtractManyAsync(
        IReadOnlyList<PkgFileListReader.TreeNode> files,
        string label,
        string? stripPrefix = null,
        string? folderName = null)
    {
        if (_isBusy) return;
        if (files.Count == 0) return;

        var source = txtSourcePkg.Text.Trim();
        if (!File.Exists(source)) { Warn("Source PKG not found."); return; }

        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where to extract. The PKG's internal folder " +
                          "structure will be recreated under this folder.",
            SelectedPath = Directory.Exists(Path.GetDirectoryName(source))
                ? Path.GetDirectoryName(source)!
                : AppContext.BaseDirectory,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var outputRoot = dlg.SelectedPath;

        if (SafePathGuard.IsProtectedRoot(outputRoot))
        {
            var suggestion = Path.Combine(outputRoot, "PS4Extract");
            Warn(
                "You chose a protected Windows folder:" + Environment.NewLine + Environment.NewLine +
                outputRoot + Environment.NewLine + Environment.NewLine +
                "Please create a subfolder (for example, " + suggestion + ") " +
                "and select that instead.");
            return;
        }

        var effectiveOutput = folderName is not null
            ? Path.Combine(outputRoot, folderName)
            : outputRoot;

        if (folderName is not null && Directory.Exists(effectiveOutput))
        {
            var answer = MessageBox.Show(this,
                "A folder with the same name already exists at the destination:" +
                Environment.NewLine + Environment.NewLine +
                effectiveOutput + Environment.NewLine + Environment.NewLine +
                "Existing files with the same names will be overwritten. Continue?",
                "Folder already exists",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        StartOperation();
        progressBar.Value = 0;

        AppendLogToBuffer("");
        AppendLogToBuffer("=== Extract ===");
        AppendLogToBuffer(label);
        AppendLogToBuffer($"Output: {effectiveOutput}");
        AppendLogToBuffer("-------------------");
        FlushLogIfDirty();

        var tools = ToolLocator.Default();
        var progress = BuildProgress();

        int succeeded = 0;
        int failed = 0;

        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (_cts!.Token.IsCancellationRequested) break;

                var file = files[i];
                var pct = 100.0 * i / files.Count;
                progressBar.Value = (int)Math.Clamp(pct, 0, 100);

                AppendLogToBuffer($"[{i + 1}/{files.Count}] {file.FullPath}");
                FlushLogIfDirty();

                try
                {
                    var result = await PkgFileExtractor.ExtractOneAsync(
                        source, file.FullPath, effectiveOutput, tools, _runner,
                        progress, _cts.Token,
                        stripPrefix: stripPrefix);

                    if (result.Success)
                    {
                        succeeded++;
                    }
                    else
                    {
                        failed++;
                        AppendLogToBuffer("  ERROR: " + (result.Error ?? "unknown"));
                        FlushLogIfDirty();
                    }
                }
                catch (OperationCanceledException)
                {
                    AppendLogToBuffer("  Cancelled.");
                    FlushLogIfDirty();
                    break;
                }
                catch (Exception ex)
                {
                    failed++;
                    AppendLogToBuffer("  ERROR: " + ex.Message);
                    FlushLogIfDirty();
                }
            }

            progressBar.Value = 100;
        }
        finally
        {
            AppendLogToBuffer("");
            AppendLogToBuffer($"Extracted {succeeded} file(s); {failed} failed.");
            FlushLogIfDirty();

            MessageBox.Show(this,
                $"Extracted {succeeded} file(s); {failed} failed." + Environment.NewLine + Environment.NewLine +
                "Output:" + Environment.NewLine + effectiveOutput,
                "Extract complete", MessageBoxButtons.OK,
                failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            EndOperation();
        }
    }

    // ---------- Extract all ----------

    private async Task ExtractAllAsync()
    {
        if (_isBusy) return;

        var source = txtSourcePkg.Text.Trim();
        if (!File.Exists(source)) { Warn("Source PKG not found."); return; }

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

        if (SafePathGuard.IsProtectedRoot(outputRoot))
        {
            var suggestion = Path.Combine(outputRoot, "PS4Extract");
            Warn(
                "You chose a protected Windows folder:" + Environment.NewLine + Environment.NewLine +
                outputRoot + Environment.NewLine + Environment.NewLine +
                "Please create a subfolder (for example, " + suggestion + ") " +
                "and select that instead.");
            return;
        }

        // SAFETY: warn the user about exactly what will happen, especially
        // when the target folder already exists. The pipeline will only
        // delete the folder on failure if WE created it, but making the
        // behaviour explicit here avoids surprises either way.
        if (Directory.Exists(outputRoot) &&
            Directory.EnumerateFileSystemEntries(outputRoot).Any())
        {
            var answer = MessageBox.Show(this,
                "The chosen folder is not empty:" + Environment.NewLine + Environment.NewLine +
                outputRoot + Environment.NewLine + Environment.NewLine +
                "Delete its contents and extract anyway?" + Environment.NewLine + Environment.NewLine +
                "(Only do this if you're sure the folder contains nothing you want to keep.)",
                "Folder not empty", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            try { Directory.Delete(outputRoot, recursive: true); }
            catch (Exception ex) { Warn("Cannot delete existing folder: " + ex.Message); return; }
        }

        var tools = ToolLocator.Default();

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
                "Extract all complete." + Environment.NewLine + Environment.NewLine +
                "Output:" + Environment.NewLine + outputRoot + Environment.NewLine + Environment.NewLine +
                "Open the folder in Explorer?",
                "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (answer == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "explorer.exe", $"\"{outputRoot}\"") { UseShellExecute = true });
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
            // The pipeline itself now handles the "was this folder ours?"
            // cleanup correctly, so we just report what happened.
            AppendLogToBuffer("ERROR: " + ex.Message);
            FlushLogIfDirty();
            progressBar.Value = 0;

            MessageBox.Show(this,
                "Extract failed:" + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "The extract folder " +
                (Directory.Exists(outputRoot) ? "was left in place." : "was cleaned up."),
                "Extract all failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            ToolTipText = $"{model.FullPath}",
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
            ".pkg"      => "PKG package",
            ".sfo"      => "System metadata",
            ".png"      => "Image (PNG)",
            ".dds"      => "Image (DDS)",
            ".at9"      => "Audio (AT9)",
            ".mp4"      => "Video (MP4)",
            ".prx"      => "PS4 module",
            ".bin"      => "Binary data",
            ".ipak"     => "Asset pack",
            ".manifest" => "Manifest",
            ".dat"      => "Data file",
            ".lua"      => "Lua script",
            ".xml"      => "XML data",
            ".json"     => "JSON data",
            ".trp"      => "Trophy data",
            _           => "File"
        };
    }

    // ---------- Operation state ----------

    private void StartOperation()
    {
        _isBusy = true;
        _cts = new CancellationTokenSource();
        _lastLoggedLine = null;

        btnBrowsePkg.Enabled       = false;
        btnLoad.Enabled            = false;
        btnExtractSelected.Enabled = false;
        btnExtractAll.Enabled      = false;
        btnCancel.Enabled          = true;
        txtSourcePkg.Enabled       = false;
    }

    private void EndOperation()
    {
        _isBusy = false;
        if (_cts is not null) { _cts.Dispose(); _cts = null; }

        btnBrowsePkg.Enabled = true;
        btnLoad.Enabled      = true;
        btnCancel.Enabled    = false;
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

        var text = _logBuffer.ToString();
        if (txtLog.TextLength == text.Length) return;

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

    private bool ConfirmWorkFolderWipe(string workDir)
    {
        return WorkFolderPrompt.ConfirmWipe(this, workDir);
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "File List",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}