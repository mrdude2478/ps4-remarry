namespace PS4Remarry.WinForms;

partial class DlcTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label        lblContentId;
    private System.Windows.Forms.TextBox      txtContentId;
    private System.Windows.Forms.Label        lblTitleIdPreview;
    private System.Windows.Forms.Label        lblTitle;
    private System.Windows.Forms.TextBox      txtTitle;
    private System.Windows.Forms.Label        lblIcon;
    private System.Windows.Forms.TextBox      txtIcon;
    private System.Windows.Forms.Button       btnBrowseIcon;
    private System.Windows.Forms.Button       btnClearIcon;
    private System.Windows.Forms.Label        lblOutput;
    private System.Windows.Forms.TextBox      txtOutput;
    private System.Windows.Forms.Button       btnBrowseOutput;
    private System.Windows.Forms.Button       btnCreate;
    private System.Windows.Forms.Button       btnOpenOutput;
    private System.Windows.Forms.Button       btnCancel;
    private System.Windows.Forms.ProgressBar  progressBar;
    private System.Windows.Forms.TextBox      txtLog;

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
        lblContentId = new Label();
        txtContentId = new TextBox();
        lblTitleIdPreview = new Label();
        lblTitle = new Label();
        txtTitle = new TextBox();
        lblIcon = new Label();
        txtIcon = new TextBox();
        btnBrowseIcon = new Button();
        btnClearIcon = new Button();
        lblOutput = new Label();
        txtOutput = new TextBox();
        btnBrowseOutput = new Button();
        btnCreate = new Button();
        btnOpenOutput = new Button();
        btnCancel = new Button();
        progressBar = new ProgressBar();
        txtLog = new TextBox();
        SuspendLayout();
        // 
        // lblContentId
        // 
        lblContentId.AutoSize = true;
        lblContentId.Location = new Point(12, 18);
        lblContentId.Name = "lblContentId";
        lblContentId.Size = new Size(67, 15);
        lblContentId.TabIndex = 0;
        lblContentId.Text = "Content ID:";
        // 
        // txtContentId
        // 
        txtContentId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtContentId.Location = new Point(110, 15);
        txtContentId.Name = "txtContentId";
        txtContentId.Size = new Size(610, 23);
        txtContentId.TabIndex = 1;
        // 
        // lblTitleIdPreview
        // 
        lblTitleIdPreview.AutoSize = true;
        lblTitleIdPreview.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblTitleIdPreview.ForeColor = SystemColors.ControlDarkDark;
        lblTitleIdPreview.Location = new Point(502, 139);
        lblTitleIdPreview.Name = "lblTitleIdPreview";
        lblTitleIdPreview.Size = new Size(35, 17);
        lblTitleIdPreview.TabIndex = 2;
        lblTitleIdPreview.Text = "(TID)";
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Location = new Point(12, 47);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(32, 15);
        lblTitle.TabIndex = 3;
        lblTitle.Text = "Title:";
        // 
        // txtTitle
        // 
        txtTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtTitle.Location = new Point(110, 44);
        txtTitle.Name = "txtTitle";
        txtTitle.Size = new Size(610, 23);
        txtTitle.TabIndex = 4;
        // 
        // lblIcon
        // 
        lblIcon.AutoSize = true;
        lblIcon.Location = new Point(12, 76);
        lblIcon.Name = "lblIcon";
        lblIcon.Size = new Size(62, 15);
        lblIcon.TabIndex = 5;
        lblIcon.Text = "Icon (opt):";
        // 
        // txtIcon
        // 
        txtIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtIcon.Location = new Point(110, 73);
        txtIcon.Name = "txtIcon";
        txtIcon.Size = new Size(440, 23);
        txtIcon.TabIndex = 6;
        // 
        // btnBrowseIcon
        // 
        btnBrowseIcon.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseIcon.Location = new Point(558, 72);
        btnBrowseIcon.Name = "btnBrowseIcon";
        btnBrowseIcon.Size = new Size(80, 25);
        btnBrowseIcon.TabIndex = 7;
        btnBrowseIcon.Text = "Browse...";
        btnBrowseIcon.UseVisualStyleBackColor = true;
        // 
        // btnClearIcon
        // 
        btnClearIcon.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearIcon.Location = new Point(644, 72);
        btnClearIcon.Name = "btnClearIcon";
        btnClearIcon.Size = new Size(76, 25);
        btnClearIcon.TabIndex = 8;
        btnClearIcon.Text = "Clear";
        btnClearIcon.UseVisualStyleBackColor = true;
        // 
        // lblOutput
        // 
        lblOutput.AutoSize = true;
        lblOutput.Location = new Point(12, 106);
        lblOutput.Name = "lblOutput";
        lblOutput.Size = new Size(82, 15);
        lblOutput.TabIndex = 9;
        lblOutput.Text = "Output folder:";
        // 
        // txtOutput
        // 
        txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOutput.Location = new Point(110, 103);
        txtOutput.Name = "txtOutput";
        txtOutput.Size = new Size(520, 23);
        txtOutput.TabIndex = 10;
        // 
        // btnBrowseOutput
        // 
        btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseOutput.Location = new Point(638, 102);
        btnBrowseOutput.Name = "btnBrowseOutput";
        btnBrowseOutput.Size = new Size(82, 25);
        btnBrowseOutput.TabIndex = 11;
        btnBrowseOutput.Text = "Browse...";
        btnBrowseOutput.UseVisualStyleBackColor = true;
        // 
        // btnCreate
        // 
        btnCreate.Location = new Point(110, 132);
        btnCreate.Name = "btnCreate";
        btnCreate.Size = new Size(160, 32);
        btnCreate.TabIndex = 12;
        btnCreate.Text = "Create DLC Unlocker";
        btnCreate.UseVisualStyleBackColor = true;
        // 
        // btnOpenOutput
        // 
        btnOpenOutput.Location = new Point(278, 132);
        btnOpenOutput.Name = "btnOpenOutput";
        btnOpenOutput.Size = new Size(120, 32);
        btnOpenOutput.TabIndex = 13;
        btnOpenOutput.Text = "Open Output";
        btnOpenOutput.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(406, 132);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 32);
        btnCancel.TabIndex = 14;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // progressBar
        // 
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(12, 170);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(712, 22);
        progressBar.TabIndex = 15;
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(12, 198);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(712, 287);
        txtLog.TabIndex = 16;
        // 
        // DlcTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(lblContentId);
        Controls.Add(txtContentId);
        Controls.Add(lblTitleIdPreview);
        Controls.Add(lblTitle);
        Controls.Add(txtTitle);
        Controls.Add(lblIcon);
        Controls.Add(txtIcon);
        Controls.Add(btnBrowseIcon);
        Controls.Add(btnClearIcon);
        Controls.Add(lblOutput);
        Controls.Add(txtOutput);
        Controls.Add(btnBrowseOutput);
        Controls.Add(btnCreate);
        Controls.Add(btnOpenOutput);
        Controls.Add(btnCancel);
        Controls.Add(progressBar);
        Controls.Add(txtLog);
        Name = "DlcTab";
        Size = new Size(736, 500);
        ResumeLayout(false);
        PerformLayout();
    }
}