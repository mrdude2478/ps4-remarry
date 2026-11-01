using System.Diagnostics;
using System.Text;
using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

/// <summary>
/// PKG library: a searchable SQLite database of your game and update PKGs.
/// Drop a single PKG to review it before adding, or drop one or more folders
/// (sub-folders included) to import everything automatically.
/// </summary>
public partial class PkgDbTab : UserControl
{
    private static readonly PkgSort[] SortModes =
    {
        PkgSort.TitleAsc, PkgSort.TitleDesc, PkgSort.TitleIdAsc,
        PkgSort.TitleIdDesc, PkgSort.GameThenUpdate, PkgSort.UpdateThenGame,
    };

    private PkgCatalogDb? _db;
    private PkgMeta? _loadedMeta;      // the PKG currently shown in the boxes (single-file drop)
    private CancellationTokenSource? _cts;
    private bool _busy;
    private bool _updatingList;

    public PkgDbTab()
    {
        InitializeComponent();

        cboSort.Items.AddRange(new object[]
        {
            "Title (A to Z)", "Title (Z to A)", "Title ID (A to Z)",
            "Title ID (Z to A)", "Game - Update", "Update - Game",
        });
        cboSort.SelectedIndex = 0;

        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ActivateTab();
    }

    /// <summary>
    /// Opens pkg.db (creating it if needed) and reloads the list. Called when the tab is
    /// first shown and every time the user switches back to it.
    /// </summary>
    public void ActivateTab()
    {
        if (DesignMode || _busy) return;

        try
        {
            if (_db is null)
            {
                _db = new PkgCatalogDb(ResolveDbPath());
            }

            RefreshList();
            SetStatus($"Database: {_db.DbPath}   -   drop a PKG to review it, or a folder to import it.");
        }
        catch (Exception ex)
        {
            SetStatus("Database unavailable.");
            MessageBox.Show(this, "Could not open the PKG database:\n\n" + ex.Message,
                "PKG Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetBusy(true);   // leave everything disabled
        }
    }

    /// <summary>
    /// pkg.db lives in the same tools folder the rest of the app uses (the one ToolLocator
    /// finds: beside the exe, or up to six parent folders above it), so saving and loading
    /// always agree.
    /// </summary>
    private static string ResolveDbPath()
    {
        string toolsDir;
        try { toolsDir = ToolLocator.Default().ToolsDir; }
        catch { toolsDir = Path.Combine(AppContext.BaseDirectory, "tools"); }
        return Path.Combine(toolsDir, "pkg.db");
    }

    private void WireEvents()
    {
        btnAdd.Click += (_, _) => AddFromBoxes();
        btnUpdate.Click += (_, _) => UpdateSelected();
        btnRemove.Click += (_, _) => RemoveSelected();
        btnScanFolder.Click += (_, _) => BrowseFolder();
        btnAddFiles.Click += (_, _) => BrowseFiles();
        btnExport.Click += (_, _) => ExportList();
        btnCancel.Click += (_, _) => _cts?.Cancel();

        txtSearch.TextChanged += (_, _) => RefreshList();
        txtSearch.MouseDown += (_, e) => { if (e.Button == MouseButtons.Middle) txtSearch.Clear(); };
        cboSort.SelectedIndexChanged += (_, _) => RefreshList();
        listView.SelectedIndexChanged += (_, _) => ShowSelected();

        mnuGoogle.Click += (_, _) => SearchWeb("https://www.google.com/search?q=", " PS4");
        mnuYoutube.Click += (_, _) => SearchWeb("https://www.youtube.com/results?search_query=", " PS4 gameplay no commentary");
        mnuCopyTitleId.Click += (_, _) =>
        {
            if (SelectedRecord() is { } r && r.TitleId.Length > 0) Clipboard.SetText(r.TitleId);
        };

        mnuCopy.Click += (_, _) => rtbInfo.Copy();
        mnuPaste.Click += (_, _) => rtbInfo.Paste();
        mnuClear.Click += (_, _) => rtbInfo.Clear();
        mnuB64Enc.Click += (_, _) => Base64Transform(encode: true);
        mnuB64Dec.Click += (_, _) => Base64Transform(encode: false);
        rtbInfo.LinkClicked += (_, e) =>
        {
            try
            {
                var link = e.LinkText;
                if (!string.IsNullOrEmpty(link) &&
                    (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
            catch { }
        };

        EnableDrop(this);
    }

    // ---------- Drag & drop ----------

    private void EnableDrop(Control root)
    {
        if (root is not ComboBox)
        {
            root.AllowDrop = true;
            root.DragEnter += OnDragEnterFiles;
            root.DragDrop += OnDragDropFiles;
        }
        foreach (Control child in root.Controls)
            EnableDrop(child);
    }

    private void OnDragEnterFiles(object? sender, DragEventArgs e)
    {
        e.Effect = !_busy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDragDropFiles(object? sender, DragEventArgs e)
    {
        if (_busy) return;
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0) return;

        // Return to Explorer immediately; do the real work afterwards.
        BeginInvoke(new Action(() => HandleDroppedPaths(paths)));
    }

    private void HandleDroppedPaths(string[] paths)
    {
        if (_db is null || _busy) return;

        // One file: load it into the boxes so it can be reviewed first (like the old tool).
        if (paths.Length == 1 && File.Exists(paths[0]))
        {
            LoadSingleFile(paths[0]);
            return;
        }

        // A folder, or several items: import everything automatically.
        _ = ImportAsync(paths);
    }

    // ---------- Single file: review before adding ----------

    private void LoadSingleFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is not (".pkg" or ".sfo"))
        {
            SetStatus("File not supported - drop a .pkg, a param.sfo, or a folder.");
            return;
        }

        try
        {
            var meta = PkgMetaReader.Read(path);

            ClearBoxes();
            listView.SelectedItems.Clear();
            _loadedMeta = meta;

            txtTitle.Text = meta.Title;
            txtTitleId.Text = meta.TitleId;
            txtCategory.Text = PkgMetaReader.DisplayCategory(meta.RawCategory);
            txtVersion.Text = meta.Version;
            txtAppVersion.Text = meta.AppVersion;
            txtContentId.Text = meta.ContentId;

            // Tell the user what Add would do.
            var plan = PkgImportPlanner.Plan(_db!.GetAll(), new[] { meta });
            if (plan.Duplicates.Count > 0)
                SetStatus($"{meta.FileName}: already in the database.");
            else if (plan.UnmatchedDlc.Count > 0)
                SetStatus($"{meta.FileName}: DLC, but its game ({meta.ParentTitleId}) isn't in the database yet.");
            else if (meta.Kind == PkgKind.Dlc)
                SetStatus($"{meta.FileName}: DLC - click Add to list it under its game.");
            else if (meta.Kind == PkgKind.Other)
                SetStatus($"{meta.FileName}: category '{meta.RawCategory}' isn't stored (only games and updates).");
            else
                SetStatus($"{meta.FileName}: loaded - click Add to store it.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read '{Path.GetFileName(path)}':\n\n{ex.Message}",
                "PKG Database", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ---------- Folder / multi-file import ----------

    private async Task ImportAsync(string[] paths)
    {
        var db = _db;
        if (db is null || _busy) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        SetBusy(true);   // after _cts exists, so the progress bar and Cancel button show

        progressBar.Style = ProgressBarStyle.Marquee;
        SetStatus("Searching for .pkg files...");

        var progress = new Progress<PkgScanProgress>(p =>
        {
            if (p.Total == 0) return;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Maximum = p.Total;
            progressBar.Value = Math.Min(p.Done, p.Total);
            lblStatus.Text = p.CurrentFile.Length == 0
                ? "Saving to database..."
                : $"Reading {p.Done + 1} of {p.Total}:  {p.CurrentFile}";
        });

        try
        {
            var (scan, plan) = await Task.Run(() =>
            {
                var s = PkgFolderScanner.Scan(paths, progress, ct);
                var p = PkgImportPlanner.Plan(db.GetAll(), s.Metas);
                db.Apply(p);
                return (s, p);
            });

            ClearBoxes();
            RefreshList();

            if (scan.FilesFound == 0)
            {
                SetStatus("No .pkg files found.");
                MessageBox.Show(this, "No .pkg files were found in what you dropped.",
                    "PKG Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(
                $"Imported: {plan.GamesAdded} game(s), {plan.UpdatesAdded} update(s), " +
                $"{plan.DlcAttached} DLC listed, {plan.DuplicatesSkipped + plan.DlcDuplicates} duplicate(s) skipped" +
                (scan.Cancelled ? "  (cancelled)" : ""));
            ShowReport(BuildReport(scan, plan));
        }
        catch (Exception ex)
        {
            SetStatus("Import failed.");
            MessageBox.Show(this, "Import failed:\n\n" + ex.Message,
                "PKG Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private static string BuildReport(PkgScanResult scan, PkgImportPlan plan)
    {
        var nl = Environment.NewLine;
        var sb = new StringBuilder();

        sb.AppendLine($"PKG files found:            {scan.FilesFound}" + (scan.Cancelled ? "   (scan was cancelled)" : ""));
        sb.AppendLine($"Games added:                {plan.GamesAdded}");
        sb.AppendLine($"Updates added:              {plan.UpdatesAdded}");
        sb.AppendLine($"DLC listed under games:     {plan.DlcAttached}");
        sb.AppendLine($"Duplicates skipped:         {plan.DuplicatesSkipped + plan.DlcDuplicates}");
        sb.AppendLine($"DLC with no game in DB:     {plan.UnmatchedDlc.Count}");
        sb.AppendLine($"Not game/update/DLC:        {plan.OtherSkipped}");
        sb.AppendLine($"Could not be read:          {scan.Failed.Count}");

        void Section(string title, List<string> items)
        {
            if (items.Count == 0) return;
            sb.Append(nl).AppendLine(title);
            foreach (var s in items.Take(200)) sb.AppendLine("  " + s);
            if (items.Count > 200) sb.AppendLine($"  ...and {items.Count - 200} more");
        }

        Section("DLC whose game is not in the database (drop the game PKG, then re-scan):", plan.UnmatchedDlc);
        Section("Could not be read:", scan.Failed);
        Section("Skipped (only games and updates are stored):", plan.SkippedOther);
        Section("Duplicates skipped:", plan.Duplicates);

        return sb.ToString();
    }

    private void ShowReport(string text)
    {
        using var f = new Form
        {
            Text = "PKG import results",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(760, 520),
            MinimizeBox = false,
            ShowInTaskbar = false,
        };
        var tb = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9F),
            Text = text,
        };
        var btn = new Button { Text = "Close", Dock = DockStyle.Bottom, Height = 30, DialogResult = DialogResult.OK };
        f.Controls.Add(tb);
        f.Controls.Add(btn);
        f.AcceptButton = btn;
        tb.Select(0, 0);
        f.ShowDialog(FindForm());
    }

    // ---------- Browse buttons ----------

    private void BrowseFolder()
    {
        if (_busy || _db is null) return;
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose a folder. All .pkg files in it and its sub-folders will be scanned.",
            UseDescriptionForTitle = true,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _ = ImportAsync(new[] { dlg.SelectedPath });
    }

    private void BrowseFiles()
    {
        if (_busy || _db is null) return;
        using var dlg = new OpenFileDialog
        {
            Title = "Choose PKG file(s)",
            Filter = "PS4 files (*.pkg;*.sfo)|*.pkg;*.sfo|All files (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            HandleDroppedPaths(dlg.FileNames);
    }

    // ---------- Add / Update / Remove ----------

    private bool ValidateBoxes(out PkgKind kind)
    {
        kind = PkgMetaReader.KindFromDisplay(txtCategory.Text);

        if (string.IsNullOrWhiteSpace(txtTitle.Text)) return Warn("Please enter a value in the Title box.");
        if (string.IsNullOrWhiteSpace(txtTitleId.Text)) return Warn("Please enter a value in the Title ID box.");
        if (string.IsNullOrWhiteSpace(txtCategory.Text)) return Warn("Please enter a value in the Category box.");
        if (string.IsNullOrWhiteSpace(txtVersion.Text)) return Warn("Please enter a value in the Version box.");
        if (string.IsNullOrWhiteSpace(txtContentId.Text)) return Warn("Please enter a value in the Content ID box.");

        if (string.IsNullOrWhiteSpace(txtAppVersion.Text))
            txtAppVersion.Text = "01.00";
        return true;
    }

    private PkgRecord RecordFromBoxes(PkgKind kind, int id = 0) => new()
    {
        Id = id,
        Title = txtTitle.Text.Trim(),   // stored exactly as typed (e.g. "Black Ops II"); no auto Title Case here
        TitleId = txtTitleId.Text.Trim().ToUpperInvariant(),
        Category = kind == PkgKind.Game ? "Game" : "Update",
        Version = txtVersion.Text.Trim(),
        AppVersion = txtAppVersion.Text.Trim(),
        ContentId = txtContentId.Text.Trim(),
        Information = rtbInfo.Text,
    };

    private void AddFromBoxes()
    {
        if (_db is null || _busy) return;
        if (!ValidateBoxes(out var kind)) return;

        try
        {
            if (kind == PkgKind.Dlc)
            {
                // DLC never gets its own row: it is listed under its game.
                if (_loadedMeta is not { Kind: PkgKind.Dlc } meta)
                {
                    Warn("DLC PKGs are listed inside their game's Information box rather than stored as an entry.\n\n" +
                         "Drop the DLC .pkg file (or a folder containing it) and I'll attach it to its game.");
                    return;
                }

                var plan = PkgImportPlanner.Plan(_db.GetAll(), new[] { meta });
                _db.Apply(plan);
                if (plan.DlcAttached > 0) SetStatus($"{meta.FileName} listed under its game.");
                else if (plan.DlcDuplicates > 0) SetStatus($"{meta.FileName} is already listed.");
                else SetStatus($"{meta.FileName}: its game ({meta.ParentTitleId}) isn't in the database yet.");

                ClearBoxes();
                RefreshList();
                return;
            }

            if (kind == PkgKind.Other)
            {
                Warn("Only Game and Update entries are stored. Set the Category to Game (gd) or Update (gp).");
                return;
            }

            var rec = RecordFromBoxes(kind);
            if (PkgImportPlanner.IsDuplicate(_db.GetAll(), rec))
            {
                Warn($"{rec.Title} ({rec.TitleId}, {rec.Category} {rec.Version} / {rec.AppVersion}) is already in the database.");
                return;
            }

            _db.Insert(rec);
            SetStatus($"{rec.Title} added to the database.");
            ClearBoxes();
            RefreshList();
        }
        catch (Exception ex)
        {
            Warn("Error: " + ex.Message);
        }
    }

    private void UpdateSelected()
    {
        if (_db is null || _busy) return;
        if (SelectedRecord() is not { } sel)
        {
            Warn("Select an entry in the list first.");
            return;
        }
        if (!ValidateBoxes(out var kind)) return;
        if (kind is not (PkgKind.Game or PkgKind.Update))
        {
            Warn("Category must be Game (gd) or Update (gp).");
            return;
        }

        var rec = RecordFromBoxes(kind, sel.Id);
        if (MessageBox.Show(this, $"Update {rec.Title} in the database?", "Update",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            _db.Update(rec);
            SetStatus($"{rec.Title} updated.");
            RefreshList(rec.Id);
        }
        catch (Exception ex)
        {
            Warn("Error: " + ex.Message);
        }
    }

    private void RemoveSelected()
    {
        if (_db is null || _busy) return;
        if (SelectedRecord() is not { } sel)
        {
            Warn("Select an entry in the list first.");
            return;
        }

        if (MessageBox.Show(this, $"Remove {sel.Title} ({sel.Category} {sel.Version}) from the database?",
                "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            _db.Delete(sel.Id);
            SetStatus($"{sel.Title} removed.");
            ClearBoxes();
            RefreshList();
        }
        catch (Exception ex)
        {
            Warn("Error: " + ex.Message);
        }
    }

    // ---------- List ----------

    private PkgRecord? SelectedRecord() =>
        listView.SelectedItems.Count > 0 ? listView.SelectedItems[0].Tag as PkgRecord : null;

    private void RefreshList(int? reselectId = null)
    {
        if (_db is null) return;

        try
        {
            var idx = cboSort.SelectedIndex;
            var sort = idx >= 0 && idx < SortModes.Length ? SortModes[idx] : PkgSort.TitleAsc;
            var rows = _db.Search(txtSearch.Text, sort);

            _updatingList = true;
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                ListViewItem? toSelect = null;
                foreach (var r in rows)
                {
                    var item = new ListViewItem(new[]
                    {
                        r.Title, r.TitleId, r.Category, r.Version, r.AppVersion, r.ContentId,
                    }) { Tag = r };
                    listView.Items.Add(item);
                    if (reselectId == r.Id) toSelect = item;
                }

                listView.AutoResizeColumns(rows.Count > 0
                    ? ColumnHeaderAutoResizeStyle.ColumnContent
                    : ColumnHeaderAutoResizeStyle.HeaderSize);

                if (toSelect != null) toSelect.Selected = true;
            }
            finally
            {
                listView.EndUpdate();
                _updatingList = false;
            }

            int games = rows.Count(r => r.Category.Equals("Game", StringComparison.OrdinalIgnoreCase));
            int updates = rows.Count(r => r.Category.Equals("Update", StringComparison.OrdinalIgnoreCase));
            lblCount.Text = $"{rows.Count} shown  -  {games} game(s), {updates} update(s)";
        }
        catch (Exception ex)
        {
            _updatingList = false;
            SetStatus("Could not load the list: " + ex.Message);
        }
    }

    private void ShowSelected()
    {
        if (_updatingList) return;
        if (SelectedRecord() is not { } r) return;

        _loadedMeta = null;
        txtTitle.Text = r.Title;
        txtTitleId.Text = r.TitleId;
        txtCategory.Text = r.Category;
        txtVersion.Text = r.Version;
        txtAppVersion.Text = r.AppVersion;
        txtContentId.Text = r.ContentId;
        rtbInfo.Text = r.Information;
    }

    private void ClearBoxes()
    {
        _loadedMeta = null;
        txtTitle.Clear();
        txtTitleId.Clear();
        txtCategory.Clear();
        txtVersion.Clear();
        txtAppVersion.Clear();
        txtContentId.Clear();
        rtbInfo.Clear();
    }

    // ---------- Export / web search / misc ----------

    private void ExportList()
    {
        if (_db is null || _busy) return;

        using var sfd = new SaveFileDialog
        {
            Title = "Export game list",
            FileName = "list.txt",
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var lines = _db.GetAll()
                .Where(r => r.Category.Equals("Game", StringComparison.OrdinalIgnoreCase))
                .Select(r => $"{r.Title} - ({r.TitleId})")
                .OrderBy(l => l, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            File.WriteAllLines(sfd.FileName, lines);
            SetStatus($"Exported {lines.Count} game(s) to {sfd.FileName}");
        }
        catch (Exception ex)
        {
            Warn("Export failed:\n" + ex.Message);
        }
    }

    private void SearchWeb(string baseUrl, string suffix)
    {
        if (SelectedRecord() is not { } r) return;
        try
        {
            var url = baseUrl + Uri.EscapeDataString(r.Title + suffix);
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            Warn("Unable to open the browser for " + r.Title);
        }
    }

    private void Base64Transform(bool encode)
    {
        try
        {
            var all = rtbInfo.Text;
            var sel = rtbInfo.SelectedText;
            string Convert1(string s) => encode
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(s))
                : Encoding.UTF8.GetString(Convert.FromBase64String(s));

            if (sel.Length > 0)
            {
                var start = rtbInfo.SelectionStart;
                rtbInfo.SelectedText = Convert1(sel);
                rtbInfo.Select(start, 0);
            }
            else
            {
                rtbInfo.Text = Convert1(all);
            }
        }
        catch
        {
            Warn(encode ? "Unable to encode the text." : "That isn't a valid Base64 string.");
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        btnAdd.Enabled = btnUpdate.Enabled = btnRemove.Enabled = !busy;
        btnScanFolder.Enabled = btnAddFiles.Enabled = btnExport.Enabled = !busy;
        progressBar.Visible = busy && _cts != null;
        btnCancel.Visible = busy && _cts != null;
        if (!busy)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
        }
    }

    private void SetStatus(string text) => lblStatus.Text = text;

    private bool Warn(string message)
    {
        MessageBox.Show(this, message, "PKG Database", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }
}
