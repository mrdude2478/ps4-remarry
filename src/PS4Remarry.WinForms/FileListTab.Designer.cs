namespace PS4Remarry.WinForms;

partial class FileListTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label        lblSource;
    private System.Windows.Forms.TextBox      txtSourcePkg;
    private System.Windows.Forms.Button       btnBrowsePkg;
    private System.Windows.Forms.Button       btnLoad;
    private System.Windows.Forms.Button       btnExtractSelected;
    private System.Windows.Forms.Button       btnCancel;
    private System.Windows.Forms.ProgressBar  progressBar;
    private System.Windows.Forms.SplitContainer splitContainer;
    private System.Windows.Forms.TreeView     treeFiles;
    private System.Windows.Forms.Label        lblDetailsHeader;
    private System.Windows.Forms.Label        lblPathLabel;
    private System.Windows.Forms.TextBox      txtPath;
    private System.Windows.Forms.Label        lblTypeLabel;
    private System.Windows.Forms.TextBox      txtType;
    private System.Windows.Forms.TextBox      txtLog;
    private System.Windows.Forms.Label        lblSummary;

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
        btnLoad = new Button();
        btnExtractSelected = new Button();
        btnCancel = new Button();
        progressBar = new ProgressBar();
        splitContainer = new SplitContainer();
        treeFiles = new TreeView();
        lblDetailsHeader = new Label();
        lblPathLabel = new Label();
        txtPath = new TextBox();
        lblTypeLabel = new Label();
        txtType = new TextBox();
        btnExtractAll = new Button();
        txtLog = new TextBox();
        lblSummary = new Label();
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        SuspendLayout();
        // 
        // lblSource
        // 
        lblSource.AutoSize = true;
        lblSource.Location = new Point(12, 18);
        lblSource.Name = "lblSource";
        lblSource.Size = new Size(46, 15);
        lblSource.TabIndex = 0;
        lblSource.Text = "Source:";
        // 
        // txtSourcePkg
        // 
        txtSourcePkg.AllowDrop = true;
        txtSourcePkg.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSourcePkg.Location = new Point(64, 14);
        txtSourcePkg.Name = "txtSourcePkg";
        txtSourcePkg.Size = new Size(564, 23);
        txtSourcePkg.TabIndex = 1;
        // 
        // btnBrowsePkg
        // 
        btnBrowsePkg.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowsePkg.Location = new Point(634, 13);
        btnBrowsePkg.Name = "btnBrowsePkg";
        btnBrowsePkg.Size = new Size(90, 25);
        btnBrowsePkg.TabIndex = 2;
        btnBrowsePkg.Text = "Browse...";
        btnBrowsePkg.UseVisualStyleBackColor = true;
        // 
        // btnLoad
        // 
        btnLoad.Location = new Point(12, 47);
        btnLoad.Name = "btnLoad";
        btnLoad.Size = new Size(120, 32);
        btnLoad.TabIndex = 3;
        btnLoad.Text = "Load File List";
        btnLoad.UseVisualStyleBackColor = true;
        // 
        // btnExtractSelected
        // 
        btnExtractSelected.AutoSize = true;
        btnExtractSelected.Enabled = false;
        btnExtractSelected.Location = new Point(234, 47);
        btnExtractSelected.Name = "btnExtractSelected";
        btnExtractSelected.Size = new Size(149, 32);
        btnExtractSelected.TabIndex = 4;
        btnExtractSelected.Text = "Extract Selected File...";
        btnExtractSelected.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(138, 47);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 32);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // progressBar
        // 
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(12, 90);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(712, 22);
        progressBar.TabIndex = 6;
        // 
        // splitContainer
        // 
        splitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainer.Location = new Point(12, 120);
        splitContainer.Name = "splitContainer";
        // 
        // splitContainer.Panel1
        // 
        splitContainer.Panel1.Controls.Add(treeFiles);
        // 
        // splitContainer.Panel2
        // 
        splitContainer.Panel2.Controls.Add(lblDetailsHeader);
        splitContainer.Panel2.Controls.Add(lblPathLabel);
        splitContainer.Panel2.Controls.Add(txtPath);
        splitContainer.Panel2.Controls.Add(lblTypeLabel);
        splitContainer.Panel2.Controls.Add(txtType);
        splitContainer.Size = new Size(712, 300);
        splitContainer.SplitterDistance = 470;
        splitContainer.TabIndex = 7;
        // 
        // treeFiles
        // 
        treeFiles.HideSelection = false;
        treeFiles.Location = new Point(0, 0);
        treeFiles.Name = "treeFiles";
        treeFiles.Size = new Size(470, 272);
        treeFiles.TabIndex = 0;
        // 
        // lblDetailsHeader
        // 
        lblDetailsHeader.AutoSize = true;
        lblDetailsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblDetailsHeader.Location = new Point(12, 12);
        lblDetailsHeader.Name = "lblDetailsHeader";
        lblDetailsHeader.Size = new Size(65, 15);
        lblDetailsHeader.TabIndex = 0;
        lblDetailsHeader.Text = "File details";
        // 
        // lblPathLabel
        // 
        lblPathLabel.AutoSize = true;
        lblPathLabel.Location = new Point(12, 42);
        lblPathLabel.Name = "lblPathLabel";
        lblPathLabel.Size = new Size(34, 15);
        lblPathLabel.TabIndex = 1;
        lblPathLabel.Text = "Path:";
        // 
        // txtPath
        // 
        txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtPath.Location = new Point(60, 39);
        txtPath.Name = "txtPath";
        txtPath.ReadOnly = true;
        txtPath.Size = new Size(170, 23);
        txtPath.TabIndex = 2;
        // 
        // lblTypeLabel
        // 
        lblTypeLabel.AutoSize = true;
        lblTypeLabel.Location = new Point(12, 74);
        lblTypeLabel.Name = "lblTypeLabel";
        lblTypeLabel.Size = new Size(34, 15);
        lblTypeLabel.TabIndex = 5;
        lblTypeLabel.Text = "Type:";
        // 
        // txtType
        // 
        txtType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtType.Location = new Point(60, 71);
        txtType.Name = "txtType";
        txtType.ReadOnly = true;
        txtType.Size = new Size(170, 23);
        txtType.TabIndex = 6;
        // 
        // btnExtractAll
        // 
        btnExtractAll.AutoSize = true;
        btnExtractAll.Location = new Point(389, 47);
        btnExtractAll.Name = "btnExtractAll";
        btnExtractAll.Size = new Size(172, 32);
        btnExtractAll.TabIndex = 7;
        btnExtractAll.Text = "Extract All";
        btnExtractAll.UseVisualStyleBackColor = true;
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(12, 430);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(712, 130);
        txtLog.TabIndex = 8;
        // 
        // lblSummary
        // 
        lblSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblSummary.AutoSize = true;
        lblSummary.Location = new Point(12, 410);
        lblSummary.Name = "lblSummary";
        lblSummary.Size = new Size(0, 15);
        lblSummary.TabIndex = 9;
        // 
        // FileListTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(btnExtractAll);
        Controls.Add(lblSource);
        Controls.Add(txtSourcePkg);
        Controls.Add(btnBrowsePkg);
        Controls.Add(btnLoad);
        Controls.Add(btnCancel);
        Controls.Add(btnExtractSelected);
        Controls.Add(progressBar);
        Controls.Add(splitContainer);
        Controls.Add(lblSummary);
        Controls.Add(txtLog);
        Name = "FileListTab";
        Size = new Size(736, 570);
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        splitContainer.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private Button btnExtractAll;
}