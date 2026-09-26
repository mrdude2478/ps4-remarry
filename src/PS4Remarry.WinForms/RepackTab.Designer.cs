namespace PS4Remarry.WinForms;

partial class RepackTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label      lblSource;
    private System.Windows.Forms.TextBox    txtSourcePkg;
    private System.Windows.Forms.Button     btnBrowsePkg;
    private System.Windows.Forms.Label      lblExtract;
    private System.Windows.Forms.TextBox    txtExtractFolder;
    private System.Windows.Forms.Button     btnBrowseExtract;
    private System.Windows.Forms.Button     btnExtract;
    private System.Windows.Forms.Button     btnOpenFolder;
    private System.Windows.Forms.Button     btnCheck;
    private System.Windows.Forms.Button     btnRepack;
    private System.Windows.Forms.Button     btnCleanFolder;
    private System.Windows.Forms.Button     btnCancel;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.RichTextBox txtLog;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblSource = new Label();
        txtSourcePkg = new TextBox();
        btnBrowsePkg = new Button();
        lblExtract = new Label();
        txtExtractFolder = new TextBox();
        btnBrowseExtract = new Button();
        btnExtract = new Button();
        btnOpenFolder = new Button();
        btnCheck = new Button();
        btnRepack = new Button();
        btnCleanFolder = new Button();
        btnCancel = new Button();
        progressBar = new ProgressBar();
        txtLog = new RichTextBox();
        SuspendLayout();
        // 
        // lblSource
        // 
        lblSource.Anchor = AnchorStyles.None;
        lblSource.AutoSize = true;
        lblSource.Location = new Point(4, 8);
        lblSource.Name = "lblSource";
        lblSource.Size = new Size(46, 15);
        lblSource.TabIndex = 0;
        lblSource.Text = "Source:";
        // 
        // txtSourcePkg
        // 
        txtSourcePkg.AllowDrop = true;
        txtSourcePkg.Anchor = AnchorStyles.None;
        txtSourcePkg.Location = new Point(56, 5);
        txtSourcePkg.Name = "txtSourcePkg";
        txtSourcePkg.Size = new Size(642, 23);
        txtSourcePkg.TabIndex = 1;
        // 
        // btnBrowsePkg
        // 
        btnBrowsePkg.Anchor = AnchorStyles.None;
        btnBrowsePkg.Location = new Point(704, 4);
        btnBrowsePkg.Name = "btnBrowsePkg";
        btnBrowsePkg.Size = new Size(90, 25);
        btnBrowsePkg.TabIndex = 2;
        btnBrowsePkg.Text = "Browse...";
        btnBrowsePkg.UseVisualStyleBackColor = true;
        btnBrowsePkg.Click += btnBrowsePkg_Click;
        // 
        // lblExtract
        // 
        lblExtract.Anchor = AnchorStyles.None;
        lblExtract.AutoSize = true;
        lblExtract.Location = new Point(4, 37);
        lblExtract.Name = "lblExtract";
        lblExtract.Size = new Size(46, 15);
        lblExtract.TabIndex = 3;
        lblExtract.Text = "Extract:";
        // 
        // txtExtractFolder
        // 
        txtExtractFolder.Anchor = AnchorStyles.None;
        txtExtractFolder.Location = new Point(56, 34);
        txtExtractFolder.Name = "txtExtractFolder";
        txtExtractFolder.Size = new Size(642, 23);
        txtExtractFolder.TabIndex = 4;
        // 
        // btnBrowseExtract
        // 
        btnBrowseExtract.Anchor = AnchorStyles.None;
        btnBrowseExtract.Location = new Point(704, 33);
        btnBrowseExtract.Name = "btnBrowseExtract";
        btnBrowseExtract.Size = new Size(90, 25);
        btnBrowseExtract.TabIndex = 5;
        btnBrowseExtract.Text = "Browse...";
        btnBrowseExtract.UseVisualStyleBackColor = true;
        // 
        // btnExtract
        // 
        btnExtract.Anchor = AnchorStyles.None;
        btnExtract.Location = new Point(4, 63);
        btnExtract.Name = "btnExtract";
        btnExtract.Size = new Size(90, 25);
        btnExtract.TabIndex = 6;
        btnExtract.Text = "Extract";
        btnExtract.UseVisualStyleBackColor = true;
        // 
        // btnOpenFolder
        // 
        btnOpenFolder.Anchor = AnchorStyles.None;
        btnOpenFolder.Enabled = false;
        btnOpenFolder.Location = new Point(608, 63);
        btnOpenFolder.Name = "btnOpenFolder";
        btnOpenFolder.Size = new Size(90, 25);
        btnOpenFolder.TabIndex = 7;
        btnOpenFolder.Text = "Open Folder";
        btnOpenFolder.UseVisualStyleBackColor = true;
        // 
        // btnCheck
        // 
        btnCheck.Anchor = AnchorStyles.None;
        btnCheck.Enabled = false;
        btnCheck.Location = new Point(292, 63);
        btnCheck.Name = "btnCheck";
        btnCheck.Size = new Size(90, 25);
        btnCheck.TabIndex = 8;
        btnCheck.Text = "Check";
        btnCheck.UseVisualStyleBackColor = true;
        // 
        // btnRepack
        // 
        btnRepack.Anchor = AnchorStyles.None;
        btnRepack.Enabled = false;
        btnRepack.Location = new Point(100, 63);
        btnRepack.Name = "btnRepack";
        btnRepack.Size = new Size(90, 25);
        btnRepack.TabIndex = 9;
        btnRepack.Text = "Repack";
        btnRepack.UseVisualStyleBackColor = true;
        // 
        // btnCleanFolder
        // 
        btnCleanFolder.Anchor = AnchorStyles.None;
        btnCleanFolder.Enabled = false;
        btnCleanFolder.Location = new Point(704, 63);
        btnCleanFolder.Name = "btnCleanFolder";
        btnCleanFolder.Size = new Size(90, 25);
        btnCleanFolder.TabIndex = 10;
        btnCleanFolder.Text = "Delete Folder";
        btnCleanFolder.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.None;
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(196, 63);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 25);
        btnCancel.TabIndex = 11;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // progressBar
        // 
        progressBar.Anchor = AnchorStyles.None;
        progressBar.Location = new Point(4, 94);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(790, 22);
        progressBar.TabIndex = 12;
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.None;
        txtLog.BackColor = Color.Black;
        txtLog.DetectUrls = false;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.HideSelection = false;
        txtLog.Location = new Point(4, 122);
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtLog.Size = new Size(790, 360);
        txtLog.TabIndex = 13;
        txtLog.Text = "";
        // 
        // RepackTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(lblSource);
        Controls.Add(txtSourcePkg);
        Controls.Add(btnBrowsePkg);
        Controls.Add(lblExtract);
        Controls.Add(txtExtractFolder);
        Controls.Add(btnBrowseExtract);
        Controls.Add(btnExtract);
        Controls.Add(btnOpenFolder);
        Controls.Add(btnCheck);
        Controls.Add(btnRepack);
        Controls.Add(btnCleanFolder);
        Controls.Add(btnCancel);
        Controls.Add(progressBar);
        Controls.Add(txtLog);
        Name = "RepackTab";
        Size = new Size(797, 485);
        ResumeLayout(false);
        PerformLayout();
    }
}