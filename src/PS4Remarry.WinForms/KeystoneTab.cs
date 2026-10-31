using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

public partial class KeystoneTab : UserControl
{
    private byte[]? _currentKeystone;
    private byte[]? _loadedKeystone;

    public KeystoneTab()
    {
        InitializeComponent();
        WireEvents();
    }

    private void WireEvents()
    {
        btnRandom.Click   += (_, _) => txtPasscode.Text = KeystoneGenerator.GenerateRandomPasscode();
        btnGenerate.Click += (_, _) => Generate();
        btnSave.Click     += (_, _) => Save();
        btnCopy.Click     += (_, _) => CopyHex();
        btnLoadKeystone.Click += (_, _) => LoadKeystone();
        btnVerify.Click       += (_, _) => Verify();
    }

    // ---------- Generate ----------

    private void Generate()
    {
        try
        {
            KeystoneGenerator.ValidatePasscode(txtPasscode.Text);
        }
        catch (Exception ex)
        {
            Warn(ex.Message);
            return;
        }

        try
        {
            _currentKeystone = KeystoneGenerator.Build(txtPasscode.Text);
            txtHex.Text = KeystoneGenerator.FormatHexDump(_currentKeystone);
            btnSave.Enabled = true;
            btnCopy.Enabled = true;
        }
        catch (Exception ex)
        {
            Warn("Error generating keystone:\n" + ex.Message);
        }
    }

    private void Save()
    {
        if (_currentKeystone is null)
        {
            Warn("Generate a keystone first.");
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Title  = "Save keystone",
            FileName = "keystone",
            Filter = "Keystone file (keystone)|keystone|All files (*.*)|*.*",
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllBytes(sfd.FileName, _currentKeystone);
            MessageBox.Show(this,
                "Saved:\n" + sfd.FileName,
                "Keystone", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Warn("Save failed:\n" + ex.Message);
        }
    }

    private void CopyHex()
    {
        if (string.IsNullOrEmpty(txtHex.Text)) return;
        try { Clipboard.SetText(txtHex.Text); } catch { }
    }

    // ---------- Verify ----------

    private void LoadKeystone()
    {
        using var ofd = new OpenFileDialog
        {
            Title  = "Open keystone",
            Filter = "Keystone file (keystone)|keystone|All files (*.*)|*.*",
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var data = File.ReadAllBytes(ofd.FileName);
            var problem = KeystoneGenerator.DescribeFile(data);
            if (problem is not null)
            {
                _loadedKeystone = null;
                btnVerify.Enabled = false;
                lblVerifyResult.ForeColor = Color.DarkRed;
                lblVerifyResult.Text = "Load failed: " + problem;
                return;
            }

            _loadedKeystone = data;
            btnVerify.Enabled = true;

            lblVerifyResult.ForeColor = SystemColors.ControlText;
            lblVerifyResult.Text =
                $"Loaded keystone: {Path.GetFileName(ofd.FileName)}\n" +
                $"Fingerprint: {Convert.ToHexString(data.AsSpan(32, 32))}";
        }
        catch (Exception ex)
        {
            _loadedKeystone = null;
            btnVerify.Enabled = false;
            Warn("Error loading file:\n" + ex.Message);
        }
    }

    private void Verify()
    {
        if (_loadedKeystone is null)
        {
            Warn("Load a keystone file first.");
            return;
        }

        var candidate = txtVerifyPasscode.Text;
        try
        {
            KeystoneGenerator.ValidatePasscode(candidate);
        }
        catch (Exception ex)
        {
            Warn(ex.Message);
            return;
        }

        if (KeystoneGenerator.Matches(candidate, _loadedKeystone))
        {
            lblVerifyResult.ForeColor = Color.DarkGreen;
            lblVerifyResult.Text =
                "✔ MATCH — this passcode produced the fingerprint in the keystone.\n" +
                "Fingerprint: " + Convert.ToHexString(_loadedKeystone.AsSpan(32, 32));
        }
        else
        {
            var expected = _loadedKeystone.AsSpan(32, 32).ToArray();
            var actual   = KeystoneGenerator.ComputeFingerprint(candidate);

            lblVerifyResult.ForeColor = Color.DarkRed;
            lblVerifyResult.Text =
                "✘ NO MATCH — this passcode does not match the keystone.\n" +
                "Expected: " + Convert.ToHexString(expected) + "\n" +
                "Computed: " + Convert.ToHexString(actual);
        }
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "Keystone",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}