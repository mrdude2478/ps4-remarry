namespace PS4Remarry.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.TabControl tabControl;
    private System.Windows.Forms.TabPage    tabSingle;
    private System.Windows.Forms.TabPage    tabBatch;
    private PS4Remarry.WinForms.BatchTab    batchTab;
    private System.Windows.Forms.TabPage    tabVerify;
    private PS4Remarry.WinForms.VerifyTab   verifyTab;
    private System.Windows.Forms.TabPage    tabRepack;
    private PS4Remarry.WinForms.RepackTab   repackTab;
    
    private System.Windows.Forms.TabPage    tabFileList;
    private PS4Remarry.WinForms.FileListTab fileListTab;

    private System.Windows.Forms.Label   lblGame;
    private System.Windows.Forms.TextBox txtGame;
    private System.Windows.Forms.Button  btnBrowseGame;
    private System.Windows.Forms.Button  btnRenameGame;

    private System.Windows.Forms.Label   lblUpdate;
    private System.Windows.Forms.TextBox txtUpdate;
    private System.Windows.Forms.Button  btnBrowseUpdate;
    private System.Windows.Forms.Button  btnRenameUpdate;

    private System.Windows.Forms.Label   lblOutput;
    private System.Windows.Forms.TextBox txtOutput;
    private System.Windows.Forms.Button  btnBrowseOutput;

    private System.Windows.Forms.Button  btnRun;
    private System.Windows.Forms.Button  btnMerge;
    private System.Windows.Forms.Button  btnCancel;
    private System.Windows.Forms.Button  btnOpenOutput;
    private System.Windows.Forms.Button  btnVerifyOutput;

    private System.Windows.Forms.Button  btnShowIcon;
    private System.Windows.Forms.Button  btnShowBackground;

    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.TextBox     txtLog;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel statusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        tabControl = new TabControl();
        tabSingle = new TabPage();
        lblGame = new Label();
        txtGame = new TextBox();
        btnBrowseGame = new Button();
        btnRenameGame = new Button();
        lblUpdate = new Label();
        txtUpdate = new TextBox();
        btnBrowseUpdate = new Button();
        btnRenameUpdate = new Button();
        lblOutput = new Label();
        txtOutput = new TextBox();
        btnBrowseOutput = new Button();
        btnRun = new Button();
        btnMerge = new Button();
        btnCancel = new Button();
        btnOpenOutput = new Button();
        btnVerifyOutput = new Button();
        btnShowIcon = new Button();
        btnShowBackground = new Button();
        progressBar = new ProgressBar();
        txtLog = new TextBox();
        tabBatch = new TabPage();
        batchTab = new BatchTab();
        tabVerify = new TabPage();
        verifyTab = new VerifyTab();
        tabRepack = new TabPage();
        repackTab = new RepackTab();
        tabFileList = new TabPage();
        fileListTab = new FileListTab();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        tabControl.SuspendLayout();
        tabSingle.SuspendLayout();
        tabBatch.SuspendLayout();
        tabVerify.SuspendLayout();
        tabRepack.SuspendLayout();
        tabFileList.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // tabControl
        // 
        tabControl.Controls.Add(tabSingle);
        tabControl.Controls.Add(tabBatch);
        tabControl.Controls.Add(tabVerify);
        tabControl.Controls.Add(tabRepack);
        tabControl.Controls.Add(tabFileList);
        tabControl.Dock = DockStyle.Fill;
        tabControl.Location = new Point(0, 0);
        tabControl.Name = "tabControl";
        tabControl.SelectedIndex = 0;
        tabControl.Size = new Size(811, 519);
        tabControl.TabIndex = 0;
        // 
        // tabSingle
        // 
        tabSingle.Controls.Add(lblGame);
        tabSingle.Controls.Add(txtGame);
        tabSingle.Controls.Add(btnBrowseGame);
        tabSingle.Controls.Add(btnRenameGame);
        tabSingle.Controls.Add(lblUpdate);
        tabSingle.Controls.Add(txtUpdate);
        tabSingle.Controls.Add(btnBrowseUpdate);
        tabSingle.Controls.Add(btnRenameUpdate);
        tabSingle.Controls.Add(lblOutput);
        tabSingle.Controls.Add(txtOutput);
        tabSingle.Controls.Add(btnBrowseOutput);
        tabSingle.Controls.Add(btnRun);
        tabSingle.Controls.Add(btnMerge);
        tabSingle.Controls.Add(btnCancel);
        tabSingle.Controls.Add(btnOpenOutput);
        tabSingle.Controls.Add(btnVerifyOutput);
        tabSingle.Controls.Add(btnShowIcon);
        tabSingle.Controls.Add(btnShowBackground);
        tabSingle.Controls.Add(progressBar);
        tabSingle.Controls.Add(txtLog);
        tabSingle.Location = new Point(4, 24);
        tabSingle.Name = "tabSingle";
        tabSingle.Padding = new Padding(3);
        tabSingle.Size = new Size(803, 491);
        tabSingle.TabIndex = 0;
        tabSingle.Text = "Single";
        tabSingle.UseVisualStyleBackColor = true;
        // 
        // lblGame
        // 
        lblGame.AutoSize = true;
        lblGame.Location = new Point(7, 10);
        lblGame.Name = "lblGame";
        lblGame.Size = new Size(65, 15);
        lblGame.TabIndex = 0;
        lblGame.Text = "Game PKG:";
        // 
        // txtGame
        // 
        txtGame.AllowDrop = true;
        txtGame.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtGame.Location = new Point(113, 5);
        txtGame.Name = "txtGame";
        txtGame.Size = new Size(552, 23);
        txtGame.TabIndex = 1;
        txtGame.DragDrop += PkgBox_DragDrop;
        txtGame.DragEnter += PkgBox_DragEnter;
        // 
        // btnBrowseGame
        // 
        btnBrowseGame.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseGame.Location = new Point(671, 5);
        btnBrowseGame.Name = "btnBrowseGame";
        btnBrowseGame.Size = new Size(90, 25);
        btnBrowseGame.TabIndex = 2;
        btnBrowseGame.Text = "Browse...";
        btnBrowseGame.UseVisualStyleBackColor = true;
        btnBrowseGame.Click += btnBrowseGame_Click;
        // 
        // btnRenameGame
        // 
        btnRenameGame.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRenameGame.Location = new Point(767, 5);
        btnRenameGame.Name = "btnRenameGame";
        btnRenameGame.Size = new Size(30, 25);
        btnRenameGame.TabIndex = 3;
        btnRenameGame.Text = "R";
        btnRenameGame.UseVisualStyleBackColor = true;
        btnRenameGame.Click += btnRenameGame_Click;
        // 
        // lblUpdate
        // 
        lblUpdate.AutoSize = true;
        lblUpdate.Location = new Point(7, 44);
        lblUpdate.Name = "lblUpdate";
        lblUpdate.Size = new Size(72, 15);
        lblUpdate.TabIndex = 4;
        lblUpdate.Text = "Update PKG:";
        // 
        // txtUpdate
        // 
        txtUpdate.AllowDrop = true;
        txtUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUpdate.Location = new Point(113, 39);
        txtUpdate.Name = "txtUpdate";
        txtUpdate.Size = new Size(552, 23);
        txtUpdate.TabIndex = 5;
        txtUpdate.DragDrop += PkgBox_DragDrop;
        txtUpdate.DragEnter += PkgBox_DragEnter;
        // 
        // btnBrowseUpdate
        // 
        btnBrowseUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseUpdate.Location = new Point(671, 39);
        btnBrowseUpdate.Name = "btnBrowseUpdate";
        btnBrowseUpdate.Size = new Size(90, 25);
        btnBrowseUpdate.TabIndex = 6;
        btnBrowseUpdate.Text = "Browse...";
        btnBrowseUpdate.UseVisualStyleBackColor = true;
        btnBrowseUpdate.Click += btnBrowseUpdate_Click;
        // 
        // btnRenameUpdate
        // 
        btnRenameUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRenameUpdate.Location = new Point(767, 39);
        btnRenameUpdate.Name = "btnRenameUpdate";
        btnRenameUpdate.Size = new Size(30, 25);
        btnRenameUpdate.TabIndex = 7;
        btnRenameUpdate.Text = "R";
        btnRenameUpdate.UseVisualStyleBackColor = true;
        btnRenameUpdate.Click += btnRenameUpdate_Click;
        // 
        // lblOutput
        // 
        lblOutput.AutoSize = true;
        lblOutput.Location = new Point(7, 76);
        lblOutput.Name = "lblOutput";
        lblOutput.Size = new Size(82, 15);
        lblOutput.TabIndex = 8;
        lblOutput.Text = "Output folder:";
        // 
        // txtOutput
        // 
        txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOutput.Location = new Point(113, 73);
        txtOutput.Name = "txtOutput";
        txtOutput.Size = new Size(552, 23);
        txtOutput.TabIndex = 9;
        // 
        // btnBrowseOutput
        // 
        btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseOutput.Location = new Point(671, 73);
        btnBrowseOutput.Name = "btnBrowseOutput";
        btnBrowseOutput.Size = new Size(90, 25);
        btnBrowseOutput.TabIndex = 10;
        btnBrowseOutput.Text = "Browse...";
        btnBrowseOutput.UseVisualStyleBackColor = true;
        btnBrowseOutput.Click += btnBrowseOutput_Click;
        // 
        // btnRun
        // 
        btnRun.Location = new Point(7, 117);
        btnRun.Name = "btnRun";
        btnRun.Size = new Size(100, 32);
        btnRun.TabIndex = 11;
        btnRun.Text = "Run Remarry";
        btnRun.UseVisualStyleBackColor = true;
        btnRun.Click += btnRun_Click;
        // 
        // btnMerge
        // 
        btnMerge.Location = new Point(113, 117);
        btnMerge.Name = "btnMerge";
        btnMerge.Size = new Size(100, 32);
        btnMerge.TabIndex = 19;
        btnMerge.Text = "Merge";
        btnMerge.UseVisualStyleBackColor = true;
        btnMerge.Click += btnMerge_Click;
        // 
        // btnCancel
        // 
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(219, 117);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(100, 32);
        btnCancel.TabIndex = 12;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        // 
        // btnOpenOutput
        // 
        btnOpenOutput.Location = new Point(325, 117);
        btnOpenOutput.Name = "btnOpenOutput";
        btnOpenOutput.Size = new Size(100, 32);
        btnOpenOutput.TabIndex = 13;
        btnOpenOutput.Text = "Open Output";
        btnOpenOutput.UseVisualStyleBackColor = true;
        btnOpenOutput.Click += btnOpenOutput_Click;
        // 
        // btnVerifyOutput
        // 
        btnVerifyOutput.Enabled = false;
        btnVerifyOutput.Location = new Point(431, 117);
        btnVerifyOutput.Name = "btnVerifyOutput";
        btnVerifyOutput.Size = new Size(100, 32);
        btnVerifyOutput.TabIndex = 14;
        btnVerifyOutput.Text = "Verify Output";
        btnVerifyOutput.UseVisualStyleBackColor = true;
        btnVerifyOutput.Click += btnVerifyOutput_Click;
        // 
        // btnShowIcon
        // 
        btnShowIcon.Enabled = false;
        btnShowIcon.Location = new Point(671, 121);
        btnShowIcon.Name = "btnShowIcon";
        btnShowIcon.Size = new Size(58, 32);
        btnShowIcon.TabIndex = 15;
        btnShowIcon.Text = "Icon";
        btnShowIcon.UseVisualStyleBackColor = true;
        btnShowIcon.Click += btnShowIcon_Click;
        // 
        // btnShowBackground
        // 
        btnShowBackground.Enabled = false;
        btnShowBackground.Location = new Point(735, 121);
        btnShowBackground.Name = "btnShowBackground";
        btnShowBackground.Size = new Size(62, 32);
        btnShowBackground.TabIndex = 16;
        btnShowBackground.Text = "BG";
        btnShowBackground.UseVisualStyleBackColor = true;
        btnShowBackground.Click += btnShowBackground_Click;
        // 
        // progressBar
        // 
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(7, 159);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(790, 22);
        progressBar.TabIndex = 17;
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(7, 189);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.Size = new Size(790, 293);
        txtLog.TabIndex = 18;
        // 
        // tabBatch
        // 
        tabBatch.Controls.Add(batchTab);
        tabBatch.Location = new Point(4, 24);
        tabBatch.Name = "tabBatch";
        tabBatch.Padding = new Padding(3);
        tabBatch.Size = new Size(803, 491);
        tabBatch.TabIndex = 1;
        tabBatch.Text = "Batch";
        tabBatch.UseVisualStyleBackColor = true;
        // 
        // batchTab
        // 
        batchTab.Anchor = AnchorStyles.None;
        batchTab.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        batchTab.Location = new Point(0, 2);
        batchTab.Name = "batchTab";
        batchTab.Padding = new Padding(8);
        batchTab.Size = new Size(805, 486);
        batchTab.TabIndex = 0;
        batchTab.Load += batchTab_Load;
        // 
        // tabVerify
        // 
        tabVerify.Controls.Add(verifyTab);
        tabVerify.Location = new Point(4, 24);
        tabVerify.Name = "tabVerify";
        tabVerify.Padding = new Padding(3);
        tabVerify.Size = new Size(803, 491);
        tabVerify.TabIndex = 2;
        tabVerify.Text = "Verify";
        tabVerify.UseVisualStyleBackColor = true;
        // 
        // verifyTab
        // 
        verifyTab.Location = new Point(3, 3);
        verifyTab.Name = "verifyTab";
        verifyTab.Padding = new Padding(8);
        verifyTab.Size = new Size(797, 482);
        verifyTab.TabIndex = 0;
        // 
        // tabRepack
        // 
        tabRepack.Controls.Add(repackTab);
        tabRepack.Location = new Point(4, 24);
        tabRepack.Name = "tabRepack";
        tabRepack.Padding = new Padding(3);
        tabRepack.Size = new Size(803, 491);
        tabRepack.TabIndex = 3;
        tabRepack.Text = "Repack";
        tabRepack.UseVisualStyleBackColor = true;
        // 
        // repackTab
        // 
        repackTab.Dock = DockStyle.Fill;
        repackTab.Location = new Point(3, 3);
        repackTab.Name = "repackTab";
        repackTab.Size = new Size(797, 485);
        repackTab.TabIndex = 0;
        // 
        // tabFileList
        // 
        tabFileList.Controls.Add(fileListTab);
        tabFileList.Location = new Point(4, 24);
        tabFileList.Name = "tabFileList";
        tabFileList.Padding = new Padding(3);
        tabFileList.Size = new Size(803, 491);
        tabFileList.TabIndex = 4;
        tabFileList.Text = "File List";
        tabFileList.UseVisualStyleBackColor = true;
        // 
        // fileListTab
        // 
        fileListTab.Dock = DockStyle.Fill;
        fileListTab.Location = new Point(3, 3);
        fileListTab.Name = "fileListTab";
        fileListTab.Size = new Size(797, 485);
        fileListTab.TabIndex = 0;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 519);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(811, 22);
        statusStrip.TabIndex = 0;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(42, 17);
        statusLabel.Text = "Ready.";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(811, 541);
        Controls.Add(tabControl);
        Controls.Add(statusStrip);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(760, 580);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "PS4 Remarry — Game + Update";
        tabControl.ResumeLayout(false);
        tabSingle.ResumeLayout(false);
        tabSingle.PerformLayout();
        tabBatch.ResumeLayout(false);
        tabVerify.ResumeLayout(false);
        tabRepack.ResumeLayout(false);
        tabFileList.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}