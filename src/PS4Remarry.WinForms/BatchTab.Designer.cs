namespace PS4Remarry.WinForms;

partial class BatchTab
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.FlowLayoutPanel  toolbarPanel;
    private System.Windows.Forms.Button           btnAdd;
    private System.Windows.Forms.Button           btnAddFromFiles;
    private System.Windows.Forms.Button           btnRemove;
    private System.Windows.Forms.Button           btnClear;
    private System.Windows.Forms.Button           btnRun;
    private System.Windows.Forms.Button           btnCancel;
    private System.Windows.Forms.Label            lblSpacer2;
    private System.Windows.Forms.Label            lblParallel;
    private System.Windows.Forms.NumericUpDown    numParallel;
    private System.Windows.Forms.DataGridView     grid;
    private System.Windows.Forms.TextBox          txtLog;

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
        toolbarPanel = new FlowLayoutPanel();
        btnAdd = new Button();
        btnAddFromFiles = new Button();
        btnRemove = new Button();
        btnClear = new Button();
        btnRun = new Button();
        btnCancel = new Button();
        lblSpacer2 = new Label();
        lblParallel = new Label();
        numParallel = new NumericUpDown();
        grid = new DataGridView();
        txtLog = new TextBox();
        toolbarPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numParallel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();
        // 
        // toolbarPanel
        // 
        toolbarPanel.AutoSize = true;
        toolbarPanel.Controls.Add(btnAdd);
        toolbarPanel.Controls.Add(btnAddFromFiles);
        toolbarPanel.Controls.Add(btnRemove);
        toolbarPanel.Controls.Add(btnClear);
        toolbarPanel.Controls.Add(btnRun);
        toolbarPanel.Controls.Add(btnCancel);
        toolbarPanel.Controls.Add(lblSpacer2);
        toolbarPanel.Controls.Add(lblParallel);
        toolbarPanel.Controls.Add(numParallel);
        toolbarPanel.Dock = DockStyle.Top;
        toolbarPanel.Location = new Point(8, 8);
        toolbarPanel.Name = "toolbarPanel";
        toolbarPanel.Padding = new Padding(0, 0, 0, 6);
        toolbarPanel.Size = new Size(781, 41);
        toolbarPanel.TabIndex = 0;
        // 
        // btnAdd
        // 
        btnAdd.Location = new Point(3, 3);
        btnAdd.Name = "btnAdd";
        btnAdd.Padding = new Padding(8, 2, 8, 2);
        btnAdd.Size = new Size(81, 29);
        btnAdd.TabIndex = 0;
        btnAdd.Text = "Add Row";
        btnAdd.UseVisualStyleBackColor = true;
        // 
        // btnAddFromFiles
        // 
        btnAddFromFiles.Location = new Point(90, 3);
        btnAddFromFiles.Name = "btnAddFromFiles";
        btnAddFromFiles.Padding = new Padding(8, 2, 8, 2);
        btnAddFromFiles.Size = new Size(119, 29);
        btnAddFromFiles.TabIndex = 1;
        btnAddFromFiles.Text = "Add from Files...";
        btnAddFromFiles.UseVisualStyleBackColor = true;
        // 
        // btnRemove
        // 
        btnRemove.Location = new Point(215, 3);
        btnRemove.Name = "btnRemove";
        btnRemove.Padding = new Padding(8, 2, 8, 2);
        btnRemove.Size = new Size(108, 29);
        btnRemove.TabIndex = 2;
        btnRemove.Text = "Remove Selected";
        btnRemove.UseVisualStyleBackColor = true;
        // 
        // btnClear
        // 
        btnClear.Location = new Point(329, 3);
        btnClear.Name = "btnClear";
        btnClear.Padding = new Padding(8, 2, 8, 2);
        btnClear.Size = new Size(77, 29);
        btnClear.TabIndex = 3;
        btnClear.Text = "Clear All";
        btnClear.UseVisualStyleBackColor = true;
        // 
        // btnRun
        // 
        btnRun.Location = new Point(412, 3);
        btnRun.Name = "btnRun";
        btnRun.Padding = new Padding(8, 2, 8, 2);
        btnRun.Size = new Size(71, 29);
        btnRun.TabIndex = 5;
        btnRun.Text = "Run All";
        btnRun.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Location = new Point(489, 3);
        btnCancel.Name = "btnCancel";
        btnCancel.Padding = new Padding(8, 2, 8, 2);
        btnCancel.Size = new Size(87, 29);
        btnCancel.TabIndex = 6;
        btnCancel.Text = "Cancel All";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // lblSpacer2
        // 
        lblSpacer2.Location = new Point(582, 0);
        lblSpacer2.Name = "lblSpacer2";
        lblSpacer2.Size = new Size(61, 30);
        lblSpacer2.TabIndex = 7;
        // 
        // lblParallel
        // 
        lblParallel.Location = new Point(649, 0);
        lblParallel.Name = "lblParallel";
        lblParallel.Padding = new Padding(0, 6, 0, 0);
        lblParallel.Size = new Size(73, 21);
        lblParallel.TabIndex = 8;
        lblParallel.Text = "Parallel jobs:";
        // 
        // numParallel
        // 
        numParallel.Location = new Point(728, 3);
        numParallel.Name = "numParallel";
        numParallel.Size = new Size(50, 23);
        numParallel.TabIndex = 9;
        // 
        // grid
        // 
        grid.AllowDrop = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = SystemColors.ButtonFace;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Dock = DockStyle.Fill;
        grid.Location = new Point(8, 49);
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.Size = new Size(781, 197);
        grid.TabIndex = 1;
        // 
        // txtLog
        // 
        txtLog.BackColor = Color.Black;
        txtLog.Dock = DockStyle.Bottom;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(8, 246);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.Size = new Size(781, 231);
        txtLog.TabIndex = 2;
        txtLog.WordWrap = false;
        // 
        // BatchTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(grid);
        Controls.Add(txtLog);
        Controls.Add(toolbarPanel);
        Name = "BatchTab";
        Padding = new Padding(8);
        Size = new Size(797, 485);
        toolbarPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)numParallel).EndInit();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}