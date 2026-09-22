namespace PS4Remarry.WinForms;

partial class VerifyTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label   lblGame;
    private System.Windows.Forms.TextBox txtGame;
    private System.Windows.Forms.Button  btnBrowseGame;
    private System.Windows.Forms.Label   lblUpdate;
    private System.Windows.Forms.TextBox txtUpdate;
    private System.Windows.Forms.Button  btnBrowseUpdate;
    private System.Windows.Forms.Button  btnCheck;
    private System.Windows.Forms.Button  btnClear;
    private System.Windows.Forms.Label   lblGameDigest;
    private System.Windows.Forms.TextBox txtGameDigest;
    private System.Windows.Forms.Label   lblUpdateDigest;
    private System.Windows.Forms.TextBox txtUpdateDigest;
    private System.Windows.Forms.Label   lblStatus;
    private System.Windows.Forms.TextBox txtLog;

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
        lblGame = new Label();
        txtGame = new TextBox();
        btnBrowseGame = new Button();
        lblUpdate = new Label();
        txtUpdate = new TextBox();
        btnBrowseUpdate = new Button();
        btnCheck = new Button();
        btnClear = new Button();
        lblGameDigest = new Label();
        txtGameDigest = new TextBox();
        lblUpdateDigest = new Label();
        txtUpdateDigest = new TextBox();
        lblStatus = new Label();
        txtLog = new TextBox();
        SuspendLayout();
        // 
        // lblGame
        // 
        lblGame.Anchor = AnchorStyles.None;
        lblGame.AutoSize = true;
        lblGame.Location = new Point(3, 25);
        lblGame.Name = "lblGame";
        lblGame.Size = new Size(65, 15);
        lblGame.TabIndex = 0;
        lblGame.Text = "Game PKG:";
        // 
        // txtGame
        // 
        txtGame.AllowDrop = true;
        txtGame.Anchor = AnchorStyles.None;
        txtGame.Location = new Point(92, 17);
        txtGame.Name = "txtGame";
        txtGame.Size = new Size(598, 23);
        txtGame.TabIndex = 1;
        // 
        // btnBrowseGame
        // 
        btnBrowseGame.Anchor = AnchorStyles.None;
        btnBrowseGame.Location = new Point(696, 16);
        btnBrowseGame.Name = "btnBrowseGame";
        btnBrowseGame.Size = new Size(98, 25);
        btnBrowseGame.TabIndex = 2;
        btnBrowseGame.Text = "Browse...";
        btnBrowseGame.UseVisualStyleBackColor = true;
        // 
        // lblUpdate
        // 
        lblUpdate.Anchor = AnchorStyles.None;
        lblUpdate.AutoSize = true;
        lblUpdate.Location = new Point(3, 59);
        lblUpdate.Name = "lblUpdate";
        lblUpdate.Size = new Size(72, 15);
        lblUpdate.TabIndex = 3;
        lblUpdate.Text = "Update PKG:";
        // 
        // txtUpdate
        // 
        txtUpdate.AllowDrop = true;
        txtUpdate.Anchor = AnchorStyles.None;
        txtUpdate.Location = new Point(92, 51);
        txtUpdate.Name = "txtUpdate";
        txtUpdate.Size = new Size(598, 23);
        txtUpdate.TabIndex = 4;
        // 
        // btnBrowseUpdate
        // 
        btnBrowseUpdate.Anchor = AnchorStyles.None;
        btnBrowseUpdate.Location = new Point(696, 50);
        btnBrowseUpdate.Name = "btnBrowseUpdate";
        btnBrowseUpdate.Size = new Size(98, 25);
        btnBrowseUpdate.TabIndex = 5;
        btnBrowseUpdate.Text = "Browse...";
        btnBrowseUpdate.UseVisualStyleBackColor = true;
        // 
        // btnCheck
        // 
        btnCheck.Anchor = AnchorStyles.None;
        btnCheck.Location = new Point(92, 86);
        btnCheck.Name = "btnCheck";
        btnCheck.Size = new Size(131, 32);
        btnCheck.TabIndex = 6;
        btnCheck.Text = "Check Compatibility";
        btnCheck.UseVisualStyleBackColor = true;
        // 
        // btnClear
        // 
        btnClear.Anchor = AnchorStyles.None;
        btnClear.Location = new Point(238, 86);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(80, 32);
        btnClear.TabIndex = 7;
        btnClear.Text = "Clear";
        btnClear.UseVisualStyleBackColor = true;
        btnClear.Click += btnClear_Click;
        // 
        // lblGameDigest
        // 
        lblGameDigest.Anchor = AnchorStyles.None;
        lblGameDigest.AutoSize = true;
        lblGameDigest.Location = new Point(3, 137);
        lblGameDigest.Name = "lblGameDigest";
        lblGameDigest.Size = new Size(76, 15);
        lblGameDigest.TabIndex = 8;
        lblGameDigest.Text = "Game digest:";
        // 
        // txtGameDigest
        // 
        txtGameDigest.Anchor = AnchorStyles.None;
        txtGameDigest.Font = new Font("Consolas", 9F);
        txtGameDigest.Location = new Point(92, 130);
        txtGameDigest.Name = "txtGameDigest";
        txtGameDigest.ReadOnly = true;
        txtGameDigest.Size = new Size(702, 22);
        txtGameDigest.TabIndex = 9;
        // 
        // lblUpdateDigest
        // 
        lblUpdateDigest.Anchor = AnchorStyles.None;
        lblUpdateDigest.AutoSize = true;
        lblUpdateDigest.Location = new Point(3, 177);
        lblUpdateDigest.Name = "lblUpdateDigest";
        lblUpdateDigest.Size = new Size(83, 15);
        lblUpdateDigest.TabIndex = 10;
        lblUpdateDigest.Text = "Update digest:";
        // 
        // txtUpdateDigest
        // 
        txtUpdateDigest.Anchor = AnchorStyles.None;
        txtUpdateDigest.Font = new Font("Consolas", 9F);
        txtUpdateDigest.Location = new Point(92, 170);
        txtUpdateDigest.Name = "txtUpdateDigest";
        txtUpdateDigest.ReadOnly = true;
        txtUpdateDigest.Size = new Size(702, 22);
        txtUpdateDigest.TabIndex = 11;
        txtUpdateDigest.TextChanged += txtUpdateDigest_TextChanged;
        // 
        // lblStatus
        // 
        lblStatus.Anchor = AnchorStyles.None;
        lblStatus.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblStatus.Location = new Point(92, 199);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(702, 32);
        lblStatus.TabIndex = 12;
        lblStatus.Text = "Pick both files and click Check.";
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.None;
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(3, 234);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.Size = new Size(791, 245);
        txtLog.TabIndex = 13;
        txtLog.WordWrap = false;
        // 
        // VerifyTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(lblGame);
        Controls.Add(txtGame);
        Controls.Add(btnBrowseGame);
        Controls.Add(lblUpdate);
        Controls.Add(txtUpdate);
        Controls.Add(btnBrowseUpdate);
        Controls.Add(btnCheck);
        Controls.Add(btnClear);
        Controls.Add(lblGameDigest);
        Controls.Add(txtGameDigest);
        Controls.Add(lblUpdateDigest);
        Controls.Add(txtUpdateDigest);
        Controls.Add(lblStatus);
        Controls.Add(txtLog);
        Name = "VerifyTab";
        Size = new Size(797, 482);
        ResumeLayout(false);
        PerformLayout();
    }
}