using System.Drawing;
using System.Windows.Forms;

namespace PS4Remarry.WinForms;

public sealed class ImageViewerForm : Form
{
    private const int MaxViewW = 1000;
    private const int MaxViewH = 700;

    private readonly byte[] _pngBytes;
    private readonly string _suggestedFileName;
    private readonly PictureBox? _picture;

    public ImageViewerForm(string title, byte[] pngBytes, string status, string suggestedFileName)
    {
        _pngBytes = pngBytes;
        _suggestedFileName = string.IsNullOrWhiteSpace(suggestedFileName)
            ? "image.png"
            : suggestedFileName;

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowIcon = false;
        BackColor = Color.FromArgb(32, 32, 32);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;

        Image? img = null;
        try
        {
            using var ms = new MemoryStream(pngBytes);
            img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
        }
        catch { }

        if (img is null)
        {
            var errLabel = new Label
            {
                Text = "Couldn't decode the image data.\n\n" + status,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.OrangeRed,
                Padding = new Padding(20),
            };
            Controls.Add(errLabel);
            ClientSize = new Size(480, 160);
            return;
        }

        int w = img.Width, h = img.Height;
        double scale = Math.Min(1.0, Math.Min((double)MaxViewW / w, (double)MaxViewH / h));
        int displayW = (int)(w * scale);
        int displayH = (int)(h * scale);

        _picture = new PictureBox
        {
            Image = img,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
        };
        _picture.DoubleClick += (_, _) => SaveAs();
        _picture.MouseUp += Picture_MouseUp;

        var ctx = new ContextMenuStrip();
        ctx.Items.Add(new ToolStripMenuItem("Save As...", null, (_, _) => SaveAs()));
        ctx.Items.Add(new ToolStripMenuItem("Copy to clipboard", null, (_, _) => CopyToClipboard()));
        _picture.ContextMenuStrip = ctx;

        var infoLabel = new Label
        {
            Text = $"{w} × {h} px   —   {pngBytes.Length:N0} bytes",
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0),
            BackColor = Color.FromArgb(48, 48, 48),
            ForeColor = Color.Gainsboro,
        };

        var statusLabel = new Label
        {
            Text = status,
            Dock = DockStyle.Bottom,
            Height = string.IsNullOrWhiteSpace(status) ? 0 : 40,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0),
            BackColor = Color.FromArgb(48, 48, 48),
            ForeColor = Color.Gainsboro,
        };

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40,
            Padding = new Padding(6),
            BackColor = Color.FromArgb(48, 48, 48),
        };

        var btnSave = new Button
        {
            Text = "Save As...",
            AutoSize = true,
            Padding = new Padding(10, 2, 10, 2),
            Margin = new Padding(4),
        };
        btnSave.Click += (_, _) => SaveAs();

        var btnCopy = new Button
        {
            Text = "Copy",
            AutoSize = true,
            Padding = new Padding(10, 2, 10, 2),
            Margin = new Padding(4),
        };
        btnCopy.Click += (_, _) => CopyToClipboard();

        buttonBar.Controls.Add(btnSave);
        buttonBar.Controls.Add(btnCopy);

        Controls.Add(_picture);
        Controls.Add(infoLabel);
        Controls.Add(statusLabel);
        Controls.Add(buttonBar);
        Controls.SetChildIndex(_picture, 0);
        Controls.SetChildIndex(infoLabel, 1);
        Controls.SetChildIndex(statusLabel, 2);
        Controls.SetChildIndex(buttonBar, 3);

        var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
        int capW = Math.Min(displayW, Math.Min(MaxViewW, wa.Width - 80));
        int capH = Math.Min(displayH, Math.Min(MaxViewH, wa.Height - 160));

        ClientSize = new Size(
            Math.Max(capW, 360),
            Math.Max(capH + infoLabel.Height + statusLabel.Height + buttonBar.Height, 260));

        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveAs();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopyToClipboard();
                e.Handled = true;
            }
        };
    }

    private void Picture_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && _picture?.ContextMenuStrip is ContextMenuStrip ctx)
            ctx.Show(_picture, e.Location);
    }

    private void SaveAs()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Save image",
            Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
            DefaultExt = "png",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = _suggestedFileName,
        };

        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                dlg.InitialDirectory = desktop;
        }
        catch { }

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllBytes(dlg.FileName, _pngBytes);
            MessageBox.Show(this,
                "Saved:\n" + dlg.FileName,
                "Save image", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Couldn't save the file:\n" + ex.Message,
                "Save image", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyToClipboard()
    {
        if (_picture?.Image is not Image img) return;

        try
        {
            Clipboard.SetImage(img);
            MessageBox.Show(this, "Image copied to clipboard.",
                "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Couldn't copy the image:\n" + ex.Message,
                "Copy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);

        if (_picture is not null)
        {
            var img = _picture.Image;
            _picture.Image = null;
            img?.Dispose();
        }
    }
}