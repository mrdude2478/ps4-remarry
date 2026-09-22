using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class VerifyTab : UserControl
{
    public VerifyTab()
    {
        InitializeComponent();
        WireEvents();
    }

    private void WireEvents()
    {
        txtGame.DragEnter += PkgBox_DragEnter;
        txtGame.DragDrop += PkgBox_DragDrop;
        txtUpdate.DragEnter += PkgBox_DragEnter;
        txtUpdate.DragDrop += PkgBox_DragDrop;

        btnBrowseGame.Click += (_, _) => BrowseInto(txtGame, "Select the base GAME pkg");
        btnBrowseUpdate.Click += (_, _) => BrowseInto(txtUpdate, "Select the UPDATE pkg");
        btnCheck.Click += async (_, _) => await CheckAsync();
        btnClear.Click += (_, _) => Clear();
    }

    private void BrowseInto(TextBox target, string title)
    {
        using var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = "PS4 PKG (*.pkg)|*.pkg|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            target.Text = dlg.FileName;
    }

    private void PkgBox_DragEnter(object? sender, DragEventArgs e)
    {
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

        if (sender == txtGame) txtGame.Text = pkg;
        else if (sender == txtUpdate) txtUpdate.Text = pkg;
    }

    private void Clear()
    {
        txtGame.Clear();
        txtUpdate.Clear();
        txtGameDigest.Clear();
        txtUpdateDigest.Clear();
        lblStatus.Text = "Pick both files and click Check.";
        lblStatus.ForeColor = SystemColors.ControlText;
        txtLog.Clear();
    }

    private async Task CheckAsync()
    {
        txtGameDigest.Clear();
        txtUpdateDigest.Clear();
        txtLog.Clear();
        lblStatus.ForeColor = SystemColors.ControlText;
        lblStatus.Text = "Working...";

        string game = txtGame.Text.Trim();
        string update = txtUpdate.Text.Trim();

        if (!File.Exists(game)) { SetStatus("Game PKG not found.", isError: true); return; }
        if (!File.Exists(update)) { SetStatus("Update PKG not found.", isError: true); return; }

        btnCheck.Enabled = false;
        try
        {
            var gameDigest = await Task.Run(() => MarryDigestReader.GetChecksum(game));
            var updateDigest = await Task.Run(() => MarryDigestReader.GetChecksum(update));

            txtGameDigest.Text = gameDigest == "-" ? "(couldn't read)" : gameDigest;
            txtUpdateDigest.Text = updateDigest == "-" ? "(couldn't read)" : updateDigest;

            AppendLog($"Game:   {Path.GetFileName(game)}");
            AppendLog($"  digest = {gameDigest}");
            AppendLog("");
            AppendLog($"Update: {Path.GetFileName(update)}");
            AppendLog($"  digest = {updateDigest}");
            AppendLog("");

            if (gameDigest == "-" || updateDigest == "-")
            {
                SetStatus("One or both files couldn't be read as base-game-class PKGs.", isWarn: true);
                return;
            }

            bool match = string.Equals(gameDigest, updateDigest, StringComparison.OrdinalIgnoreCase);
            if (match)
            {
                SetStatus("✓ Compatible — the update was built for this exact game dump.", isError: false);
                AppendLog("Result: MATCH.");
            }
            else
            {
                SetStatus("✗ NOT compatible — the update was built for a different base game dump.", isError: true);
                AppendLog("Result: MISMATCH.");
            }
        }
        catch (Exception ex)
        {
            SetStatus("Error: " + ex.Message, isError: true);
            AppendLog(ex.ToString());
        }
        finally
        {
            btnCheck.Enabled = true;
        }
    }

    private void SetStatus(string text, bool isError = false, bool isWarn = false)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = isError ? Color.Firebrick
                            : isWarn ? Color.DarkGoldenrod
                            : Color.ForestGreen;
    }

    private void AppendLog(string line)
    {
        if (txtLog.IsDisposed) return;
        txtLog.AppendText(line + Environment.NewLine);
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
    }

    private void txtUpdateDigest_TextChanged(object sender, EventArgs e)
    {

    }

    private void btnClear_Click(object sender, EventArgs e)
    {

    }
}