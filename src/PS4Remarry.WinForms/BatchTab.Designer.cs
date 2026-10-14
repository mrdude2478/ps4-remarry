namespace PS4Remarry.WinForms;

partial class BatchTab
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Button           btnAdd;
    private System.Windows.Forms.Button           btnAddFromFiles;
    private System.Windows.Forms.Button           btnRemove;
    private System.Windows.Forms.Button           btnClear;
    private System.Windows.Forms.Button           btnRun;
    private System.Windows.Forms.Button           btnCancel;
    private System.Windows.Forms.Label            lblParallel;
    private System.Windows.Forms.NumericUpDown    numParallel;
    private System.Windows.Forms.DataGridView     grid;
    private System.Windows.Forms.RichTextBox          txtLog;

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
        btnAdd = new Button();
        btnAddFromFiles = new Button();
        btnRemove = new Button();
        btnClear = new Button();
        btnRun = new Button();
        btnCancel = new Button();
        lblParallel = new Label();
        numParallel = new NumericUpDown();
        grid = new DataGridView();
        txtLog = new RichTextBox();
        ((System.ComponentModel.ISupportInitialize)numParallel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        SuspendLayout();
        // 
        // btnAdd
        // 
        btnAdd.Anchor = AnchorStyles.None;
        btnAdd.Location = new Point(0, 216);
        btnAdd.Margin = new Padding(0);
        btnAdd.Name = "btnAdd";
        btnAdd.Padding = new Padding(8, 2, 8, 2);
        btnAdd.Size = new Size(90, 27);
        btnAdd.TabIndex = 0;
        btnAdd.Text = "Add Single";
        btnAdd.UseVisualStyleBackColor = true;
        // 
        // btnAddFromFiles
        // 
        btnAddFromFiles.Anchor = AnchorStyles.None;
        btnAddFromFiles.Location = new Point(96, 216);
        btnAddFromFiles.Margin = new Padding(0);
        btnAddFromFiles.Name = "btnAddFromFiles";
        btnAddFromFiles.Padding = new Padding(8, 2, 8, 2);
        btnAddFromFiles.Size = new Size(90, 27);
        btnAddFromFiles.TabIndex = 1;
        btnAddFromFiles.Text = "Add Pair";
        btnAddFromFiles.UseVisualStyleBackColor = true;
        // 
        // btnRemove
        // 
        btnRemove.Anchor = AnchorStyles.None;
        btnRemove.Location = new Point(192, 216);
        btnRemove.Margin = new Padding(0);
        btnRemove.Name = "btnRemove";
        btnRemove.Padding = new Padding(8, 2, 8, 2);
        btnRemove.Size = new Size(90, 27);
        btnRemove.TabIndex = 2;
        btnRemove.Text = "Remove";
        btnRemove.UseVisualStyleBackColor = true;
        // 
        // btnClear
        // 
        btnClear.Anchor = AnchorStyles.None;
        btnClear.Location = new Point(288, 216);
        btnClear.Margin = new Padding(0);
        btnClear.Name = "btnClear";
        btnClear.Padding = new Padding(8, 2, 8, 2);
        btnClear.Size = new Size(90, 27);
        btnClear.TabIndex = 3;
        btnClear.Text = "Clear All";
        btnClear.UseVisualStyleBackColor = true;
        // 
        // btnRun
        // 
        btnRun.Anchor = AnchorStyles.None;
        btnRun.Location = new Point(384, 216);
        btnRun.Margin = new Padding(0);
        btnRun.Name = "btnRun";
        btnRun.Padding = new Padding(8, 2, 8, 2);
        btnRun.Size = new Size(90, 27);
        btnRun.TabIndex = 5;
        btnRun.Text = "Run All";
        btnRun.UseVisualStyleBackColor = true;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.None;
        btnCancel.Location = new Point(480, 216);
        btnCancel.Margin = new Padding(0);
        btnCancel.Name = "btnCancel";
        btnCancel.Padding = new Padding(8, 2, 8, 2);
        btnCancel.Size = new Size(90, 27);
        btnCancel.TabIndex = 6;
        btnCancel.Text = "Cancel All";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // lblParallel
        // 
        lblParallel.Anchor = AnchorStyles.None;
        lblParallel.Location = new Point(668, 216);
        lblParallel.Name = "lblParallel";
        lblParallel.Padding = new Padding(0, 6, 0, 0);
        lblParallel.Size = new Size(73, 21);
        lblParallel.TabIndex = 8;
        lblParallel.Text = "Parallel jobs:";
        // 
        // numParallel
        // 
        numParallel.Anchor = AnchorStyles.None;
        numParallel.BorderStyle = BorderStyle.FixedSingle;
        numParallel.Location = new Point(747, 218);
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
        grid.Anchor = AnchorStyles.None;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = SystemColors.ButtonFace;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Location = new Point(0, 0);
        grid.Margin = new Padding(0);
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.Size = new Size(797, 211);
        grid.TabIndex = 1;
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.None;
        txtLog.BackColor = Color.Black;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.ForeColor = Color.Gainsboro;
        txtLog.Location = new Point(0, 249);
        txtLog.Margin = new Padding(0);
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtLog.Size = new Size(797, 236);
        txtLog.TabIndex = 2;
        txtLog.Text = "";
        // 
        // BatchTab
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(btnCancel);
        Controls.Add(numParallel);
        Controls.Add(lblParallel);
        Controls.Add(btnRun);
        Controls.Add(btnClear);
        Controls.Add(btnRemove);
        Controls.Add(btnAddFromFiles);
        Controls.Add(btnAdd);
        Controls.Add(grid);
        Controls.Add(txtLog);
        Name = "BatchTab";
        Padding = new Padding(8);
        Size = new Size(797, 485);
        ((System.ComponentModel.ISupportInitialize)numParallel).EndInit();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        ResumeLayout(false);
    }
}