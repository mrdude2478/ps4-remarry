namespace PS4Remarry.WinForms;

partial class DlcTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label        lblContentId;
    private System.Windows.Forms.RichTextBox      txtContentId;
    private System.Windows.Forms.Label        lblTitleIdPreview;
    private System.Windows.Forms.Label        lblTitle;
    private System.Windows.Forms.RichTextBox      txtTitle;
    private System.Windows.Forms.Label        lblIcon;
    private System.Windows.Forms.RichTextBox      txtIcon;
    private System.Windows.Forms.Button       btnBrowseIcon;
    private System.Windows.Forms.Button       btnClearIcon;
    private System.Windows.Forms.Label        lblOutput;
    private System.Windows.Forms.RichTextBox      txtOutput;
    private System.Windows.Forms.Button       btnBrowseOutput;
    private System.Windows.Forms.Button       btnCreate;
    private System.Windows.Forms.Button       btnOpenOutput;
    private System.Windows.Forms.Button       btnCancel;
    private System.Windows.Forms.ProgressBar  progressBar;
    private System.Windows.Forms.RichTextBox      txtLog;

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
        txtContentId = new RichTextBox();
        lblTitleIdPreview = new Label();
        lblTitle = new Label();
        txtTitle = new RichTextBox();
        lblIcon = new Label();
        txtIcon = new RichTextBox();
        btnBrowseIcon = new Button();
        btnClearIcon = new Button();
        lblOutput = new Label();
        txtOutput = new RichTextBox();
        btnBrowseOutput = new Button();
        btnCreate = new Button();
        btnOpenOutput = new Button();
        btnCancel = new Button();
        progressBar = new ProgressBar();
        txtLog = new RichTextBox();
        SuspendLayout();
        // 
        // lblContentId
        // 
        lblContentId.AutoSize = true;
        lblContentId.Location = new Point(5, 9);
        lblContentId.Name = "lblContentId";
        lblContentId.Size = new Size(67, 15);
        lblContentId.TabIndex = 0;
        lblContentId.Text = "Content ID:";
        // 
        // txtContentId
        // 
        txtContentId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtContentId.Location = new Point(101, 6);
        txtContentId.Name = "txtContentId";
        txtContentId.ScrollBars = RichTextBoxScrollBars.None;
        txtContentId.Size = new Size(630, 23);
        txtContentId.TabIndex = 1;
        txtContentId.Text = "";
        txtContentId.WordWrap = false;
        // 
        // lblTitleIdPreview
        // 
        lblTitleIdPreview.AutoSize = true;
        lblTitleIdPreview.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblTitleIdPreview.ForeColor = SystemColors.ControlDarkDark;
        lblTitleIdPreview.Location = new Point(293, 128);
        lblTitleIdPreview.Name = "lblTitleIdPreview";
        lblTitleIdPreview.Size = new Size(35, 17);
        lblTitleIdPreview.TabIndex = 2;
        lblTitleIdPreview.Text = "(TID)";
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Location = new Point(5, 38);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(32, 15);
        lblTitle.TabIndex = 3;
        lblTitle.Text = "Title:";
        // 
        // txtTitle
        // 
        txtTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtTitle.Location = new Point(101, 35);
        txtTitle.Name = "txtTitle";
        txtTitle.ScrollBars = RichTextBoxScrollBars.None;
        txtTitle.Size = new Size(630, 23);
        txtTitle.TabIndex = 4;
        txtTitle.Text = "";
        txtTitle.WordWrap = false;
        // 
        // lblIcon
        // 
        lblIcon.AutoSize = true;
        lblIcon.Location = new Point(5, 68);
        lblIcon.Name = "lblIcon";
        lblIcon.Size = new Size(62, 15);
        lblIcon.TabIndex = 5;
        lblIcon.Text = "Icon (opt):";
        // 
        // txtIcon
        // 
        txtIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtIcon.Location = new Point(101, 64);
        txtIcon.Name = "txtIcon";
        txtIcon.ScrollBars = RichTextBoxScrollBars.None;
        txtIcon.Size = new Size(438, 23);
        txtIcon.TabIndex = 6;
        txtIcon.Text = "";
        txtIcon.WordWrap = false;
        // 
        // btnBrowseIcon
        // 
        btnBrowseIcon.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseIcon.Location = new Point(546, 63);
        btnBrowseIcon.Name = "btnBrowseIcon";
        btnBrowseIcon.Size = new Size(90, 25);
        btnBrowseIcon.TabIndex = 7;
        btnBrowseIcon.Text = "Browse...";
        btnBrowseIcon.UseVisualStyleBackColor = true;
        // 
        // btnClearIcon
        // 
        btnClearIcon.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearIcon.Location = new Point(641, 63);
        btnClearIcon.Name = "btnClearIcon";
        btnClearIcon.Size = new Size(90, 25);
        btnClearIcon.TabIndex = 8;
        btnClearIcon.Text = "Clear";
        btnClearIcon.UseVisualStyleBackColor = true;
        // 
        // lblOutput
        // 
        lblOutput.AutoSize = true;
        lblOutput.Location = new Point(5, 98);
        lblOutput.Name = "lblOutput";
        lblOutput.Size = new Size(82, 15);
        lblOutput.TabIndex = 9;
        lblOutput.Text = "Output folder:";
        // 
        // txtOutput
        // 
        txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOutput.Location = new Point(101, 94);
        txtOutput.Name = "txtOutput";
        txtOutput.ScrollBars = RichTextBoxScrollBars.None;
        txtOutput.Size = new Size(535, 23);
        txtOutput.TabIndex = 10;
        txtOutput.Text = "";
        txtOutput.WordWrap = false;
        // 
        // btnBrowseOutput
        // 
        btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseOutput.Location = new Point(641, 93);
        btnBrowseOutput.Name = "btnBrowseOutput";
        btnBrowseOutput.Size = new Size(90, 25);
        btnBrowseOutput.TabIndex = 11;
        btnBrowseOutput.Text = "Browse...";
        btnBrowseOutput.UseVisualStyleBackColor = true;
        // 
        // btnCreate
        // 
        btnCreate.Location = new Point(5, 125);
        btnCreate.Name = "btnCreate";
        btnCreate.Size = new Size(90, 25);
        btnCreate.TabIndex = 12;
        btnCreate.Text = "Make DLC";
        btnCreate.UseVisualStyleBackColor = true;
        // 
        // btnOpenOutput
        // 
        btnOpenOutput.Location = new Point(101, 125);
        btnOpenOutput.Name = "btnOpenOutput";
        btnOpenOutput.Size = new Size(90, 25);
        btnOpenOutput.TabIndex = 13;
        btnOpenOutput.Text = "Open Output";
        btnOpenOutput.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(197, 125);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 25);
        btnCancel.TabIndex = 14;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // progressBar
        // 
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(5, 156);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(726, 22);
        progressBar.TabIndex = 15;
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(5, 184);
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtLog.Size = new Size(726, 311);
        txtLog.TabIndex = 16;
        txtLog.Text = "";
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