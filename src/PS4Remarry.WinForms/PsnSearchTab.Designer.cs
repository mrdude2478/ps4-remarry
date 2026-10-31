namespace PS4Remarry.WinForms;

partial class PsnSearchTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label       lblQuery;
    private System.Windows.Forms.TextBox     txtQuery;
    private System.Windows.Forms.Label       lblRegion;
    private System.Windows.Forms.ComboBox    cmbRegion;
    private System.Windows.Forms.Button      btnSearch;
    private System.Windows.Forms.Button      btnCancel;
    private System.Windows.Forms.Button      btnCopy;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.RichTextBox rtbResults;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblQuery    = new Label();
        txtQuery    = new TextBox();
        lblRegion   = new Label();
        cmbRegion   = new ComboBox();
        btnSearch   = new Button();
        btnCancel   = new Button();
        btnCopy     = new Button();
        progressBar = new ProgressBar();
        rtbResults  = new RichTextBox();
        SuspendLayout();

        // lblQuery
        lblQuery.AutoSize = true;
        lblQuery.Location = new Point(8, 12);
        lblQuery.Size     = new Size(47, 15);
        lblQuery.Text     = "Search:";

        // txtQuery
        txtQuery.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtQuery.Location = new Point(60, 9);
        txtQuery.Size     = new Size(380, 23);

        // lblRegion
        lblRegion.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        lblRegion.AutoSize = true;
        lblRegion.Location = new Point(448, 12);
        lblRegion.Size     = new Size(47, 15);
        lblRegion.Text     = "Region:";

        // cmbRegion
        cmbRegion.Anchor        = AnchorStyles.Top | AnchorStyles.Right;
        cmbRegion.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbRegion.Location      = new Point(500, 9);
        cmbRegion.Size          = new Size(100, 23);

        // btnSearch
        btnSearch.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        btnSearch.Location = new Point(608, 8);
        btnSearch.Size     = new Size(90, 25);
        btnSearch.Text     = "Search";

        // btnCancel
        btnCancel.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.Location = new Point(704, 8);
        btnCancel.Size     = new Size(90, 25);
        btnCancel.Text     = "Cancel";
        btnCancel.Enabled  = false;

        // btnCopy
        btnCopy.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        btnCopy.Location = new Point(704, 40);
        btnCopy.Size     = new Size(90, 25);
        btnCopy.Text     = "Copy";
        btnCopy.Enabled  = false;

        // progressBar — indeterminate while a search is running.
        progressBar.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(8, 40);
        progressBar.Size     = new Size(690, 22);
        progressBar.Style    = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 30;
        progressBar.Visible  = false;

        // rtbResults
        rtbResults.Anchor     = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        rtbResults.BackColor  = Color.Black;
        rtbResults.ForeColor  = Color.Gainsboro;
        rtbResults.Font       = new Font("Consolas", 9F);
        rtbResults.Location   = new Point(8, 75);
        rtbResults.Size       = new Size(786, 400);
        rtbResults.ReadOnly   = true;
        rtbResults.WordWrap   = false;
        rtbResults.ScrollBars = RichTextBoxScrollBars.Vertical;

        // PsnSearchTab
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode       = AutoScaleMode.Font;
        Controls.Add(lblQuery);
        Controls.Add(txtQuery);
        Controls.Add(lblRegion);
        Controls.Add(cmbRegion);
        Controls.Add(btnSearch);
        Controls.Add(btnCancel);
        Controls.Add(btnCopy);
        Controls.Add(progressBar);
        Controls.Add(rtbResults);
        Name = "PsnSearchTab";
        Size = new Size(797, 485);
        ResumeLayout(false);
        PerformLayout();
    }
}