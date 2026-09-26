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
    private System.Windows.Forms.TreeView     treeFiles;
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
        treeFiles = new TreeView();
        lblPathLabel = new Label();
        txtPath = new TextBox();
        lblTypeLabel = new Label();
        txtType = new TextBox();
        btnExtractAll = new Button();
        txtLog = new TextBox();
        lblSummary = new Label();
        SuspendLayout();
        // 
        // lblSource
        // 
        lblSource.Location = new Point(9, 11);
        lblSource.Name = "lblSource";
        lblSource.Size = new Size(46, 15);
        lblSource.TabIndex = 0;
        lblSource.Text = "Source:";
        // 
        // txtSourcePkg
        // 
        txtSourcePkg.AllowDrop = true;
        txtSourcePkg.Location = new Point(61, 8);
        txtSourcePkg.Name = "txtSourcePkg";
        txtSourcePkg.Size = new Size(632, 23);
        txtSourcePkg.TabIndex = 1;
        // 
        // btnBrowsePkg
        // 
        btnBrowsePkg.Location = new Point(699, 7);
        btnBrowsePkg.Name = "btnBrowsePkg";
        btnBrowsePkg.Size = new Size(90, 25);
        btnBrowsePkg.TabIndex = 2;
        btnBrowsePkg.Text = "Browse...";
        btnBrowsePkg.UseVisualStyleBackColor = true;
        // 
        // btnLoad
        // 
        btnLoad.Location = new Point(9, 37);
        btnLoad.Name = "btnLoad";
        btnLoad.Size = new Size(90, 25);
        btnLoad.TabIndex = 3;
        btnLoad.Text = "Load File List";
        btnLoad.UseVisualStyleBackColor = true;
        // 
        // btnExtractSelected
        // 
        btnExtractSelected.Enabled = false;
        btnExtractSelected.Location = new Point(201, 37);
        btnExtractSelected.Name = "btnExtractSelected";
        btnExtractSelected.Size = new Size(90, 25);
        btnExtractSelected.TabIndex = 4;
        btnExtractSelected.Text = "Extract File";
        btnExtractSelected.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(105, 37);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 25);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // progressBar
        // 
        progressBar.Location = new Point(393, 37);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(396, 25);
        progressBar.TabIndex = 6;
        // 
        // treeFiles
        // 
        treeFiles.BorderStyle = BorderStyle.FixedSingle;
        treeFiles.HideSelection = false;
        treeFiles.Location = new Point(7, 320);
        treeFiles.Name = "treeFiles";
        treeFiles.Size = new Size(782, 159);
        treeFiles.TabIndex = 0;
        // 
        // lblPathLabel
        // 
        lblPathLabel.Location = new Point(146, 295);
        lblPathLabel.Name = "lblPathLabel";
        lblPathLabel.Size = new Size(34, 15);
        lblPathLabel.TabIndex = 1;
        lblPathLabel.Text = "Path:";
        // 
        // txtPath
        // 
        txtPath.Location = new Point(181, 292);
        txtPath.Name = "txtPath";
        txtPath.ReadOnly = true;
        txtPath.Size = new Size(608, 23);
        txtPath.TabIndex = 2;
        // 
        // lblTypeLabel
        // 
        lblTypeLabel.Location = new Point(7, 295);
        lblTypeLabel.Name = "lblTypeLabel";
        lblTypeLabel.Size = new Size(34, 15);
        lblTypeLabel.TabIndex = 5;
        lblTypeLabel.Text = "Type:";
        // 
        // txtType
        // 
        txtType.Location = new Point(42, 292);
        txtType.Name = "txtType";
        txtType.ReadOnly = true;
        txtType.Size = new Size(101, 23);
        txtType.TabIndex = 6;
        // 
        // btnExtractAll
        // 
        btnExtractAll.Location = new Point(297, 37);
        btnExtractAll.Name = "btnExtractAll";
        btnExtractAll.Size = new Size(90, 25);
        btnExtractAll.TabIndex = 7;
        btnExtractAll.Text = "Extract All";
        btnExtractAll.UseVisualStyleBackColor = true;
        // 
        // txtLog
        // 
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(9, 68);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(780, 220);
        txtLog.TabIndex = 8;
        // 
        // lblSummary
        // 
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
        Controls.Add(treeFiles);
        Controls.Add(txtLog);
        Controls.Add(txtPath);
        Controls.Add(lblPathLabel);
        Controls.Add(btnExtractAll);
        Controls.Add(lblSource);
        Controls.Add(txtType);
        Controls.Add(lblTypeLabel);
        Controls.Add(txtSourcePkg);
        Controls.Add(btnBrowsePkg);
        Controls.Add(btnLoad);
        Controls.Add(btnCancel);
        Controls.Add(btnExtractSelected);
        Controls.Add(progressBar);
        Controls.Add(lblSummary);
        Name = "FileListTab";
        Size = new Size(797, 485);
        ResumeLayout(false);
        PerformLayout();
    }

    private Button btnExtractAll;
}