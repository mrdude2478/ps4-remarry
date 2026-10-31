using System.Diagnostics;
using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class PsnSearchTab : UserControl
{
    private readonly PsnSearchService _service;
    private CancellationTokenSource? _cts;
    private bool _busy;

    private const int MaxResultLines = 5000;
    private const int TrimToLines    = 4000;

    public PsnSearchTab()
    {
        InitializeComponent();

        _service = new PsnSearchService(AppendLine);

        cmbRegion.Items.AddRange(new object[]
        {
            "en-us", "en-gb", "en-ca", "en-au",
            "de-de", "fr-fr", "es-es", "it-it", "ja-jp"
        });
        cmbRegion.SelectedIndex = 0;

        txtQuery.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && btnSearch.Enabled)
            {
                e.SuppressKeyPress = true;
                btnSearch.PerformClick();
            }
        };

        btnSearch.Click       += async (_, _) => await SearchAsync();
        btnCancel.Click       += (_, _) => _cts?.Cancel();
        btnCopy.Click         += (_, _) => CopyResults();
        rtbResults.DoubleClick += RtbResults_DoubleClick;
    }

    // ---------- Search ----------

    private async Task SearchAsync()
    {
        if (_busy) return;

        var query = txtQuery.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            Warn("Enter a game name first.");
            return;
        }

        var region = cmbRegion.SelectedItem?.ToString() ?? "en-us";

        _cts = new CancellationTokenSource();
        SetBusy(true);
        rtbResults.Clear();
        AppendLine($"Searching for: \"{query}\" (region: {region})...");
        AppendLine();

        // Phase log for the scraper — surfaces "Opening store page..."
        // etc. so the user can see the search hasn't hung.
        var phaseLog = new Progress<string>(msg =>
        {
            // Drop duplicate consecutive phase messages.
            var last = rtbResults.Lines.Length > 0
                ? rtbResults.Lines[^1]
                : "";
            if (last == msg) return;
            AppendLine("[phase] " + msg);
        });

        try
        {
            var results = await _service.SearchAsync(
                query, region, phaseLog, _cts.Token);

            if (results.Count == 0)
            {
                AppendLine();
                AppendLine("No results. The store returned a valid page but no products matched.");
                AppendLine("Try a different query or region.");
                return;
            }

            var baseGame = PsnScraper.FindBaseGame(results, query, out var baseReason);
            var dlc      = PsnScraper.GetDlc(results, query);
            var groups   = PsnScraper.GroupByTitle(results);

            AppendLine();
            AppendLine($"Total results: {results.Count}");
            AppendLine();

            if (baseGame is not null)
            {
                AppendLine("Base game:");
                AppendLine($"  {baseGame.Name}");
                AppendLine($"    {baseGame.Href}");
                AppendLine($"    titleId={baseGame.TitleId}  id={baseGame.Id}");
                AppendLine($"    (matched by: {baseReason})");
                AppendLine();
            }

            AppendLine($"DLC for base game ({dlc.Count}):");
            if (dlc.Count == 0)
                AppendLine("  (none — no other products share the base game's titleId)");
            else
                foreach (var r in dlc)
                {
                    AppendLine($"  {r.Name}");
                    AppendLine($"    {r.Href}");
                }
            AppendLine();

            AppendLine("All groups by titleId:");
            foreach (var kv in groups)
            {
                AppendLine();
                AppendLine($"  === {kv.Key} ===");
                foreach (var r in kv.Value)
                {
                    AppendLine($"    {r.Name}");
                    AppendLine($"      {r.Href}");
                }
            }

            btnCopy.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            AppendLine();
            AppendLine("Cancelled.");
        }
        catch (PsnScrapeException ex)
        {
            // Scraper broken, not empty result. Show clearly.
            AppendLine();
            AppendLine("SCRAPER ERROR: " + ex.Message);
            AppendLine("The store page loaded but did not look like a search result page.");
            AppendLine("This usually means Sony changed the store layout.");
            if (!string.IsNullOrEmpty(ex.PageSnippet))
            {
                AppendLine();
                AppendLine("Page snippet:");
                foreach (var line in ex.PageSnippet!.Split('\n'))
                    AppendLine("  " + line.Trim());
            }
        }
        catch (Exception ex)
        {
            AppendLine();
            AppendLine("Error: " + ex.GetType().FullName);
            AppendLine(ex.Message);
            AppendLine();
            AppendLine(ex.StackTrace ?? "(no stack trace)");
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    // ---------- Busy / clipboard / links ----------

    private void SetBusy(bool busy)
    {
        _busy              = busy;
        btnSearch.Enabled  = !busy;
        btnCancel.Enabled  = busy;
        txtQuery.Enabled   = !busy;
        cmbRegion.Enabled  = !busy;
        progressBar.Visible = busy;
        Cursor             = busy ? Cursors.AppStarting : Cursors.Default;
    }

    private void CopyResults()
    {
        if (string.IsNullOrEmpty(rtbResults.Text)) return;
        try { Clipboard.SetText(rtbResults.Text); } catch { }
    }

    private void RtbResults_DoubleClick(object? sender, EventArgs e)
    {
        var pos       = rtbResults.PointToClient(Cursor.Position);
        var charIndex = rtbResults.GetCharIndexFromPosition(pos);
        var lineIndex = rtbResults.GetLineFromCharIndex(charIndex);
        if (lineIndex < 0 || lineIndex >= rtbResults.Lines.Length) return;

        var line = rtbResults.Lines[lineIndex].Trim();
        if (!line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(line) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Warn("Could not open link: " + ex.Message);
        }
    }

    // ---------- Helpers ----------

    private void AppendLine(string text = "")
    {
        if (rtbResults.IsDisposed) return;
        if (rtbResults.InvokeRequired)
        {
            rtbResults.BeginInvoke(new Action<string>(AppendLine), text);
            return;
        }

        // Trim from the top if the buffer gets too large.
        if (rtbResults.Lines.Length > MaxResultLines)
            TrimLog();

        rtbResults.AppendText(text + Environment.NewLine);
        rtbResults.SelectionStart = rtbResults.TextLength;
        rtbResults.ScrollToCaret();
    }

    private void TrimLog()
    {
        var lines = rtbResults.Lines;
        var keep  = lines.Skip(lines.Length - TrimToLines);
        rtbResults.Text = string.Join(Environment.NewLine, keep) + Environment.NewLine;
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "PSN Search",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>
    /// Called by MainForm when the app is closing so the headless browser
    /// doesn't outlive the process.
    /// </summary>
    public void Shutdown() => _ = _service.DisposeAsync().AsTask();
}