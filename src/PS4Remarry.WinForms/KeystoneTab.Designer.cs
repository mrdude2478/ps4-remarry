namespace PS4Remarry.WinForms;

partial class KeystoneTab
{
    private System.ComponentModel.IContainer components = null;

    // Generate section
    private System.Windows.Forms.Label      lblPasscode;
    private System.Windows.Forms.TextBox    txtPasscode;
    private System.Windows.Forms.Button     btnRandom;
    private System.Windows.Forms.Button     btnGenerate;
    private System.Windows.Forms.Button     btnSave;
    private System.Windows.Forms.Button     btnCopy;
    private System.Windows.Forms.RichTextBox txtHex;

    // Verify section
    private System.Windows.Forms.Label      lblVerifyHeader;
    private System.Windows.Forms.Button     btnLoadKeystone;
    private System.Windows.Forms.Label      lblVerifyPasscode;
    private System.Windows.Forms.TextBox    txtVerifyPasscode;
    private System.Windows.Forms.Button     btnVerify;
    private System.Windows.Forms.Label      lblVerifyResult;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblPasscode = new Label();
        txtPasscode = new TextBox();
        btnRandom = new Button();
        btnGenerate = new Button();
        btnSave = new Button();
        btnCopy = new Button();
        txtHex = new RichTextBox();
        lblVerifyHeader = new Label();
        btnLoadKeystone = new Button();
        lblVerifyPasscode = new Label();
        txtVerifyPasscode = new TextBox();
        btnVerify = new Button();
        lblVerifyResult = new Label();
        SuspendLayout();
        // 
        // lblPasscode
        // 
        lblPasscode.AutoSize = true;
        lblPasscode.Location = new Point(8, 12);
        lblPasscode.Name = "lblPasscode";
        lblPasscode.Size = new Size(56, 15);
        lblPasscode.TabIndex = 0;
        lblPasscode.Text = "Passcode";
        // 
        // txtPasscode
        // 
        txtPasscode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtPasscode.Location = new Point(70, 9);
        txtPasscode.MaxLength = 32;
        txtPasscode.Name = "txtPasscode";
        txtPasscode.Size = new Size(297, 23);
        txtPasscode.TabIndex = 1;
        txtPasscode.Text = "00000000000000000000000000000000";
        // 
        // btnRandom
        // 
        btnRandom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRandom.Location = new Point(373, 8);
        btnRandom.Name = "btnRandom";
        btnRandom.Size = new Size(90, 25);
        btnRandom.TabIndex = 2;
        btnRandom.Text = "Random";
        // 
        // btnGenerate
        // 
        btnGenerate.Location = new Point(469, 8);
        btnGenerate.Name = "btnGenerate";
        btnGenerate.Size = new Size(113, 25);
        btnGenerate.TabIndex = 3;
        btnGenerate.Text = "Generate Keystone";
        // 
        // btnSave
        // 
        btnSave.Enabled = false;
        btnSave.Location = new Point(588, 8);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(113, 25);
        btnSave.TabIndex = 4;
        btnSave.Text = "Save keystone...";
        // 
        // btnCopy
        // 
        btnCopy.Enabled = false;
        btnCopy.Location = new Point(707, 8);
        btnCopy.Name = "btnCopy";
        btnCopy.Size = new Size(90, 25);
        btnCopy.TabIndex = 5;
        btnCopy.Text = "Copy Hex";
        // 
        // txtHex
        // 
        txtHex.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtHex.BackColor = Color.Black;
        txtHex.Font = new Font("Consolas", 9F);
        txtHex.ForeColor = Color.Gainsboro;
        txtHex.Location = new Point(8, 38);
        txtHex.Name = "txtHex";
        txtHex.ReadOnly = true;
        txtHex.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtHex.Size = new Size(789, 230);
        txtHex.TabIndex = 6;
        txtHex.Text = "";
        txtHex.WordWrap = false;
        // 
        // lblVerifyHeader
        // 
        lblVerifyHeader.AutoSize = true;
        lblVerifyHeader.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        lblVerifyHeader.Location = new Point(8, 272);
        lblVerifyHeader.Name = "lblVerifyHeader";
        lblVerifyHeader.Size = new Size(268, 17);
        lblVerifyHeader.TabIndex = 7;
        lblVerifyHeader.Text = "Verify Passcode Against Existing Keystone";
        // 
        // btnLoadKeystone
        // 
        btnLoadKeystone.Location = new Point(8, 296);
        btnLoadKeystone.Name = "btnLoadKeystone";
        btnLoadKeystone.Size = new Size(160, 27);
        btnLoadKeystone.TabIndex = 8;
        btnLoadKeystone.Text = "Load keystone...";
        // 
        // lblVerifyPasscode
        // 
        lblVerifyPasscode.AutoSize = true;
        lblVerifyPasscode.Location = new Point(8, 332);
        lblVerifyPasscode.Name = "lblVerifyPasscode";
        lblVerifyPasscode.Size = new Size(201, 15);
        lblVerifyPasscode.TabIndex = 9;
        lblVerifyPasscode.Text = "Candidate passcode (32 ASCII chars):";
        // 
        // txtVerifyPasscode
        // 
        txtVerifyPasscode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtVerifyPasscode.Location = new Point(211, 327);
        txtVerifyPasscode.MaxLength = 32;
        txtVerifyPasscode.Name = "txtVerifyPasscode";
        txtVerifyPasscode.Size = new Size(490, 23);
        txtVerifyPasscode.TabIndex = 10;
        // 
        // btnVerify
        // 
        btnVerify.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnVerify.Enabled = false;
        btnVerify.Location = new Point(707, 326);
        btnVerify.Name = "btnVerify";
        btnVerify.Size = new Size(90, 25);
        btnVerify.TabIndex = 11;
        btnVerify.Text = "Verify";
        // 
        // lblVerifyResult
        // 
        lblVerifyResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblVerifyResult.Font = new Font("Consolas", 9F);
        lblVerifyResult.Location = new Point(8, 360);
        lblVerifyResult.Name = "lblVerifyResult";
        lblVerifyResult.Size = new Size(789, 110);
        lblVerifyResult.TabIndex = 12;
        lblVerifyResult.Text = "No keystone loaded.";
        // 
        // KeystoneTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(lblPasscode);
        Controls.Add(txtPasscode);
        Controls.Add(btnRandom);
        Controls.Add(btnGenerate);
        Controls.Add(btnSave);
        Controls.Add(btnCopy);
        Controls.Add(txtHex);
        Controls.Add(lblVerifyHeader);
        Controls.Add(btnLoadKeystone);
        Controls.Add(lblVerifyPasscode);
        Controls.Add(txtVerifyPasscode);
        Controls.Add(btnVerify);
        Controls.Add(lblVerifyResult);
        Name = "KeystoneTab";
        Size = new Size(797, 485);
        ResumeLayout(false);
        PerformLayout();
    }
}