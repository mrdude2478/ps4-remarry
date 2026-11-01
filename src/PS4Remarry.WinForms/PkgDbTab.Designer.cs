namespace PS4Remarry.WinForms;

partial class PkgDbTab
{
    private System.ComponentModel.IContainer? components = null;

    // DB entry
    private System.Windows.Forms.GroupBox entryGroup;
    private System.Windows.Forms.TableLayoutPanel entryTable;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.TextBox txtTitle;
    private System.Windows.Forms.Label lblCategory;
    private System.Windows.Forms.TextBox txtCategory;
    private System.Windows.Forms.Label lblTitleId;
    private System.Windows.Forms.TextBox txtTitleId;
    private System.Windows.Forms.Label lblVersion;
    private System.Windows.Forms.TextBox txtVersion;
    private System.Windows.Forms.Label lblContentId;
    private System.Windows.Forms.TextBox txtContentId;
    private System.Windows.Forms.Label lblAppVersion;
    private System.Windows.Forms.TextBox txtAppVersion;

    // buttons / search
    private System.Windows.Forms.FlowLayoutPanel flowButtons;
    private System.Windows.Forms.Button btnAdd;
    private System.Windows.Forms.Button btnUpdate;
    private System.Windows.Forms.Button btnRemove;
    private System.Windows.Forms.Button btnScanFolder;
    private System.Windows.Forms.Button btnAddFiles;
    private System.Windows.Forms.Button btnExport;
    private System.Windows.Forms.FlowLayoutPanel flowSearch;
    private System.Windows.Forms.Label lblSearch;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Label lblSort;
    private System.Windows.Forms.ComboBox cboSort;
    private System.Windows.Forms.Label lblCount;

    // list
    private System.Windows.Forms.ListView listView;
    private System.Windows.Forms.ColumnHeader colTitle;
    private System.Windows.Forms.ColumnHeader colTitleId;
    private System.Windows.Forms.ColumnHeader colCategory;
    private System.Windows.Forms.ColumnHeader colVersion;
    private System.Windows.Forms.ColumnHeader colAppVersion;
    private System.Windows.Forms.ColumnHeader colContentId;
    private System.Windows.Forms.ContextMenuStrip ctxList;
    private System.Windows.Forms.ToolStripMenuItem mnuGoogle;
    private System.Windows.Forms.ToolStripMenuItem mnuYoutube;
    private System.Windows.Forms.ToolStripMenuItem mnuCopyTitleId;

    // information
    private System.Windows.Forms.GroupBox infoGroup;
    private System.Windows.Forms.RichTextBox rtbInfo;
    private System.Windows.Forms.ContextMenuStrip ctxInfo;
    private System.Windows.Forms.ToolStripMenuItem mnuCopy;
    private System.Windows.Forms.ToolStripMenuItem mnuPaste;
    private System.Windows.Forms.ToolStripMenuItem mnuClear;
    private System.Windows.Forms.ToolStripMenuItem mnuB64Enc;
    private System.Windows.Forms.ToolStripMenuItem mnuB64Dec;

    // status
    private System.Windows.Forms.Panel statusPanel;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        entryGroup = new GroupBox();
        entryTable = new TableLayoutPanel();
        lblTitle = new Label();
        txtTitle = new TextBox();
        lblCategory = new Label();
        txtCategory = new TextBox();
        lblTitleId = new Label();
        txtTitleId = new TextBox();
        lblVersion = new Label();
        txtVersion = new TextBox();
        lblContentId = new Label();
        txtContentId = new TextBox();
        lblAppVersion = new Label();
        txtAppVersion = new TextBox();
        flowButtons = new FlowLayoutPanel();
        btnAdd = new Button();
        btnUpdate = new Button();
        btnRemove = new Button();
        btnScanFolder = new Button();
        btnAddFiles = new Button();
        btnExport = new Button();
        flowSearch = new FlowLayoutPanel();
        lblSearch = new Label();
        txtSearch = new TextBox();
        lblSort = new Label();
        cboSort = new ComboBox();
        lblCount = new Label();
        listView = new ListView();
        colTitle = new ColumnHeader();
        colTitleId = new ColumnHeader();
        colCategory = new ColumnHeader();
        colVersion = new ColumnHeader();
        colAppVersion = new ColumnHeader();
        colContentId = new ColumnHeader();
        ctxList = new ContextMenuStrip(components);
        mnuGoogle = new ToolStripMenuItem();
        mnuYoutube = new ToolStripMenuItem();
        mnuCopyTitleId = new ToolStripMenuItem();
        infoGroup = new GroupBox();
        rtbInfo = new RichTextBox();
        ctxInfo = new ContextMenuStrip(components);
        mnuCopy = new ToolStripMenuItem();
        mnuPaste = new ToolStripMenuItem();
        mnuClear = new ToolStripMenuItem();
        mnuB64Enc = new ToolStripMenuItem();
        mnuB64Dec = new ToolStripMenuItem();
        statusPanel = new Panel();
        lblStatus = new Label();
        btnCancel = new Button();
        progressBar = new ProgressBar();
        entryGroup.SuspendLayout();
        entryTable.SuspendLayout();
        flowButtons.SuspendLayout();
        flowSearch.SuspendLayout();
        ctxList.SuspendLayout();
        infoGroup.SuspendLayout();
        ctxInfo.SuspendLayout();
        statusPanel.SuspendLayout();
        SuspendLayout();
        // 
        // entryGroup
        // 
        entryGroup.Controls.Add(entryTable);
        entryGroup.Dock = DockStyle.Top;
        entryGroup.Location = new Point(0, 0);
        entryGroup.Name = "entryGroup";
        entryGroup.Padding = new Padding(6, 2, 6, 6);
        entryGroup.Size = new Size(797, 112);
        entryGroup.TabIndex = 0;
        entryGroup.TabStop = false;
        entryGroup.Text = "DB Entry";
        // 
        // entryTable
        // 
        entryTable.ColumnCount = 4;
        entryTable.ColumnStyles.Add(new ColumnStyle());
        entryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        entryTable.ColumnStyles.Add(new ColumnStyle());
        entryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        entryTable.Controls.Add(lblTitle, 0, 0);
        entryTable.Controls.Add(txtTitle, 1, 0);
        entryTable.Controls.Add(lblCategory, 2, 0);
        entryTable.Controls.Add(txtCategory, 3, 0);
        entryTable.Controls.Add(lblTitleId, 0, 1);
        entryTable.Controls.Add(txtTitleId, 1, 1);
        entryTable.Controls.Add(lblVersion, 2, 1);
        entryTable.Controls.Add(txtVersion, 3, 1);
        entryTable.Controls.Add(lblContentId, 0, 2);
        entryTable.Controls.Add(txtContentId, 1, 2);
        entryTable.Controls.Add(lblAppVersion, 2, 2);
        entryTable.Controls.Add(txtAppVersion, 3, 2);
        entryTable.Dock = DockStyle.Fill;
        entryTable.Location = new Point(6, 18);
        entryTable.Name = "entryTable";
        entryTable.RowCount = 3;
        entryTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        entryTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        entryTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        entryTable.Size = new Size(785, 88);
        entryTable.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.Anchor = AnchorStyles.Left;
        lblTitle.AutoSize = true;
        lblTitle.Location = new Point(3, 6);
        lblTitle.Margin = new Padding(3, 0, 6, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(29, 15);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Title";
        // 
        // txtTitle
        // 
        txtTitle.Dock = DockStyle.Fill;
        txtTitle.Location = new Point(76, 3);
        txtTitle.Margin = new Padding(3, 3, 12, 3);
        txtTitle.Name = "txtTitle";
        txtTitle.Size = new Size(301, 23);
        txtTitle.TabIndex = 0;
        // 
        // lblCategory
        // 
        lblCategory.Anchor = AnchorStyles.Left;
        lblCategory.AutoSize = true;
        lblCategory.Location = new Point(392, 6);
        lblCategory.Margin = new Padding(3, 0, 6, 0);
        lblCategory.Name = "lblCategory";
        lblCategory.Size = new Size(55, 15);
        lblCategory.TabIndex = 1;
        lblCategory.Text = "Category";
        // 
        // txtCategory
        // 
        txtCategory.Dock = DockStyle.Fill;
        txtCategory.Location = new Point(471, 3);
        txtCategory.Margin = new Padding(3, 3, 12, 3);
        txtCategory.Name = "txtCategory";
        txtCategory.Size = new Size(302, 23);
        txtCategory.TabIndex = 1;
        // 
        // lblTitleId
        // 
        lblTitleId.Anchor = AnchorStyles.Left;
        lblTitleId.AutoSize = true;
        lblTitleId.Location = new Point(3, 34);
        lblTitleId.Margin = new Padding(3, 0, 6, 0);
        lblTitleId.Name = "lblTitleId";
        lblTitleId.Size = new Size(43, 15);
        lblTitleId.TabIndex = 2;
        lblTitleId.Text = "Title ID";
        // 
        // txtTitleId
        // 
        txtTitleId.Dock = DockStyle.Fill;
        txtTitleId.Location = new Point(76, 31);
        txtTitleId.Margin = new Padding(3, 3, 12, 3);
        txtTitleId.Name = "txtTitleId";
        txtTitleId.Size = new Size(301, 23);
        txtTitleId.TabIndex = 2;
        // 
        // lblVersion
        // 
        lblVersion.Anchor = AnchorStyles.Left;
        lblVersion.AutoSize = true;
        lblVersion.Location = new Point(392, 34);
        lblVersion.Margin = new Padding(3, 0, 6, 0);
        lblVersion.Name = "lblVersion";
        lblVersion.Size = new Size(45, 15);
        lblVersion.TabIndex = 3;
        lblVersion.Text = "Version";
        // 
        // txtVersion
        // 
        txtVersion.Dock = DockStyle.Fill;
        txtVersion.Location = new Point(471, 31);
        txtVersion.Margin = new Padding(3, 3, 12, 3);
        txtVersion.Name = "txtVersion";
        txtVersion.Size = new Size(302, 23);
        txtVersion.TabIndex = 3;
        // 
        // lblContentId
        // 
        lblContentId.Anchor = AnchorStyles.Left;
        lblContentId.AutoSize = true;
        lblContentId.Location = new Point(3, 64);
        lblContentId.Margin = new Padding(3, 0, 6, 0);
        lblContentId.Name = "lblContentId";
        lblContentId.Size = new Size(64, 15);
        lblContentId.TabIndex = 4;
        lblContentId.Text = "Content ID";
        // 
        // txtContentId
        // 
        txtContentId.Dock = DockStyle.Fill;
        txtContentId.Location = new Point(76, 59);
        txtContentId.Margin = new Padding(3, 3, 12, 3);
        txtContentId.Name = "txtContentId";
        txtContentId.Size = new Size(301, 23);
        txtContentId.TabIndex = 4;
        // 
        // lblAppVersion
        // 
        lblAppVersion.Anchor = AnchorStyles.Left;
        lblAppVersion.AutoSize = true;
        lblAppVersion.Location = new Point(392, 64);
        lblAppVersion.Margin = new Padding(3, 0, 6, 0);
        lblAppVersion.Name = "lblAppVersion";
        lblAppVersion.Size = new Size(70, 15);
        lblAppVersion.TabIndex = 5;
        lblAppVersion.Text = "App Version";
        // 
        // txtAppVersion
        // 
        txtAppVersion.Dock = DockStyle.Fill;
        txtAppVersion.Location = new Point(471, 59);
        txtAppVersion.Margin = new Padding(3, 3, 12, 3);
        txtAppVersion.Name = "txtAppVersion";
        txtAppVersion.Size = new Size(302, 23);
        txtAppVersion.TabIndex = 5;
        // 
        // flowButtons
        // 
        flowButtons.AutoSize = true;
        flowButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flowButtons.Controls.Add(btnAdd);
        flowButtons.Controls.Add(btnUpdate);
        flowButtons.Controls.Add(btnRemove);
        flowButtons.Controls.Add(btnScanFolder);
        flowButtons.Controls.Add(btnAddFiles);
        flowButtons.Controls.Add(btnExport);
        flowButtons.Dock = DockStyle.Top;
        flowButtons.Location = new Point(0, 112);
        flowButtons.Name = "flowButtons";
        flowButtons.Padding = new Padding(3, 3, 3, 0);
        flowButtons.Size = new Size(797, 37);
        flowButtons.TabIndex = 1;
        // 
        // btnAdd
        // 
        btnAdd.Location = new Point(6, 6);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(80, 28);
        btnAdd.TabIndex = 0;
        btnAdd.Text = "Add";
        btnAdd.UseVisualStyleBackColor = true;
        // 
        // btnUpdate
        // 
        btnUpdate.Location = new Point(92, 6);
        btnUpdate.Name = "btnUpdate";
        btnUpdate.Size = new Size(80, 28);
        btnUpdate.TabIndex = 1;
        btnUpdate.Text = "Update";
        btnUpdate.UseVisualStyleBackColor = true;
        // 
        // btnRemove
        // 
        btnRemove.Location = new Point(178, 6);
        btnRemove.Name = "btnRemove";
        btnRemove.Size = new Size(80, 28);
        btnRemove.TabIndex = 2;
        btnRemove.Text = "Remove";
        btnRemove.UseVisualStyleBackColor = true;
        // 
        // btnScanFolder
        // 
        btnScanFolder.Location = new Point(285, 6);
        btnScanFolder.Margin = new Padding(24, 3, 3, 3);
        btnScanFolder.Name = "btnScanFolder";
        btnScanFolder.Size = new Size(110, 28);
        btnScanFolder.TabIndex = 3;
        btnScanFolder.Text = "Scan Folder...";
        btnScanFolder.UseVisualStyleBackColor = true;
        // 
        // btnAddFiles
        // 
        btnAddFiles.Location = new Point(401, 6);
        btnAddFiles.Name = "btnAddFiles";
        btnAddFiles.Size = new Size(110, 28);
        btnAddFiles.TabIndex = 4;
        btnAddFiles.Text = "Add PKG(s)...";
        btnAddFiles.UseVisualStyleBackColor = true;
        // 
        // btnExport
        // 
        btnExport.Location = new Point(517, 6);
        btnExport.Name = "btnExport";
        btnExport.Size = new Size(110, 28);
        btnExport.TabIndex = 5;
        btnExport.Text = "Export List...";
        btnExport.UseVisualStyleBackColor = true;
        // 
        // flowSearch
        // 
        flowSearch.AutoSize = true;
        flowSearch.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flowSearch.Controls.Add(lblSearch);
        flowSearch.Controls.Add(txtSearch);
        flowSearch.Controls.Add(lblSort);
        flowSearch.Controls.Add(cboSort);
        flowSearch.Controls.Add(lblCount);
        flowSearch.Dock = DockStyle.Top;
        flowSearch.Location = new Point(0, 149);
        flowSearch.Name = "flowSearch";
        flowSearch.Padding = new Padding(3, 0, 3, 3);
        flowSearch.Size = new Size(797, 33);
        flowSearch.TabIndex = 2;
        // 
        // lblSearch
        // 
        lblSearch.AutoSize = true;
        lblSearch.Location = new Point(6, 8);
        lblSearch.Margin = new Padding(3, 8, 3, 0);
        lblSearch.Name = "lblSearch";
        lblSearch.Size = new Size(45, 15);
        lblSearch.TabIndex = 0;
        lblSearch.Text = "Search:";
        // 
        // txtSearch
        // 
        txtSearch.Location = new Point(57, 4);
        txtSearch.Margin = new Padding(3, 4, 3, 3);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new Size(220, 23);
        txtSearch.TabIndex = 0;
        // 
        // lblSort
        // 
        lblSort.AutoSize = true;
        lblSort.Location = new Point(298, 8);
        lblSort.Margin = new Padding(18, 8, 3, 0);
        lblSort.Name = "lblSort";
        lblSort.Size = new Size(47, 15);
        lblSort.TabIndex = 1;
        lblSort.Text = "Sort by:";
        // 
        // cboSort
        // 
        cboSort.DropDownStyle = ComboBoxStyle.DropDownList;
        cboSort.Location = new Point(351, 4);
        cboSort.Margin = new Padding(3, 4, 3, 3);
        cboSort.Name = "cboSort";
        cboSort.Size = new Size(150, 23);
        cboSort.TabIndex = 1;
        // 
        // lblCount
        // 
        lblCount.AutoSize = true;
        lblCount.Location = new Point(522, 8);
        lblCount.Margin = new Padding(18, 8, 3, 0);
        lblCount.Name = "lblCount";
        lblCount.Size = new Size(0, 15);
        lblCount.TabIndex = 2;
        // 
        // listView
        // 
        listView.Columns.AddRange(new ColumnHeader[] { colTitle, colTitleId, colCategory, colVersion, colAppVersion, colContentId });
        listView.ContextMenuStrip = ctxList;
        listView.Dock = DockStyle.Fill;
        listView.FullRowSelect = true;
        listView.GridLines = true;
        listView.Location = new Point(0, 182);
        listView.MultiSelect = false;
        listView.Name = "listView";
        listView.Size = new Size(797, 147);
        listView.TabIndex = 3;
        listView.UseCompatibleStateImageBehavior = false;
        listView.View = View.Details;
        // 
        // colTitle
        // 
        colTitle.Text = "Title";
        colTitle.Width = 260;
        // 
        // colTitleId
        // 
        colTitleId.Text = "Title ID";
        colTitleId.Width = 90;
        // 
        // colCategory
        // 
        colCategory.Text = "Category";
        colCategory.Width = 70;
        // 
        // colVersion
        // 
        colVersion.Text = "Version";
        colVersion.Width = 65;
        // 
        // colAppVersion
        // 
        colAppVersion.Text = "App Version";
        colAppVersion.Width = 80;
        // 
        // colContentId
        // 
        colContentId.Text = "Content ID";
        colContentId.Width = 260;
        // 
        // ctxList
        // 
        ctxList.Items.AddRange(new ToolStripItem[] { mnuGoogle, mnuYoutube, mnuCopyTitleId });
        ctxList.Name = "ctxList";
        ctxList.Size = new Size(222, 70);
        // 
        // mnuGoogle
        // 
        mnuGoogle.Name = "mnuGoogle";
        mnuGoogle.Size = new Size(221, 22);
        mnuGoogle.Text = "Search Google";
        // 
        // mnuYoutube
        // 
        mnuYoutube.Name = "mnuYoutube";
        mnuYoutube.Size = new Size(221, 22);
        mnuYoutube.Text = "Search YouTube (gameplay)";
        // 
        // mnuCopyTitleId
        // 
        mnuCopyTitleId.Name = "mnuCopyTitleId";
        mnuCopyTitleId.Size = new Size(221, 22);
        mnuCopyTitleId.Text = "Copy Title ID";
        // 
        // infoGroup
        // 
        infoGroup.Controls.Add(rtbInfo);
        infoGroup.Dock = DockStyle.Bottom;
        infoGroup.Location = new Point(0, 329);
        infoGroup.Name = "infoGroup";
        infoGroup.Padding = new Padding(6, 2, 6, 6);
        infoGroup.Size = new Size(797, 130);
        infoGroup.TabIndex = 4;
        infoGroup.TabStop = false;
        infoGroup.Text = "Information  (DLC PKG names are listed here, one per line)";
        // 
        // rtbInfo
        // 
        rtbInfo.ContextMenuStrip = ctxInfo;
        rtbInfo.Dock = DockStyle.Fill;
        rtbInfo.Location = new Point(6, 18);
        rtbInfo.Name = "rtbInfo";
        rtbInfo.Size = new Size(785, 106);
        rtbInfo.TabIndex = 0;
        rtbInfo.Text = "";
        // 
        // ctxInfo
        // 
        ctxInfo.Items.AddRange(new ToolStripItem[] { mnuCopy, mnuPaste, mnuClear, mnuB64Enc, mnuB64Dec });
        ctxInfo.Name = "ctxInfo";
        ctxInfo.Size = new Size(154, 114);
        // 
        // mnuCopy
        // 
        mnuCopy.Name = "mnuCopy";
        mnuCopy.Size = new Size(153, 22);
        mnuCopy.Text = "Copy";
        // 
        // mnuPaste
        // 
        mnuPaste.Name = "mnuPaste";
        mnuPaste.Size = new Size(153, 22);
        mnuPaste.Text = "Paste";
        // 
        // mnuClear
        // 
        mnuClear.Name = "mnuClear";
        mnuClear.Size = new Size(153, 22);
        mnuClear.Text = "Clear";
        // 
        // mnuB64Enc
        // 
        mnuB64Enc.Name = "mnuB64Enc";
        mnuB64Enc.Size = new Size(153, 22);
        mnuB64Enc.Text = "Base64 Encode";
        // 
        // mnuB64Dec
        // 
        mnuB64Dec.Name = "mnuB64Dec";
        mnuB64Dec.Size = new Size(153, 22);
        mnuB64Dec.Text = "Base64 Decode";
        // 
        // statusPanel
        // 
        statusPanel.Controls.Add(lblStatus);
        statusPanel.Controls.Add(btnCancel);
        statusPanel.Controls.Add(progressBar);
        statusPanel.Dock = DockStyle.Bottom;
        statusPanel.Location = new Point(0, 459);
        statusPanel.Name = "statusPanel";
        statusPanel.Size = new Size(797, 26);
        statusPanel.TabIndex = 5;
        // 
        // lblStatus
        // 
        lblStatus.AutoEllipsis = true;
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Location = new Point(180, 0);
        lblStatus.Name = "lblStatus";
        lblStatus.Padding = new Padding(4, 0, 0, 0);
        lblStatus.Size = new Size(542, 26);
        lblStatus.TabIndex = 0;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnCancel
        // 
        btnCancel.Dock = DockStyle.Right;
        btnCancel.Location = new Point(722, 0);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(75, 26);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Visible = false;
        // 
        // progressBar
        // 
        progressBar.Dock = DockStyle.Left;
        progressBar.Location = new Point(0, 0);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(180, 26);
        progressBar.TabIndex = 2;
        progressBar.Visible = false;
        // 
        // PkgDbTab
        // 
        Controls.Add(listView);
        Controls.Add(infoGroup);
        Controls.Add(statusPanel);
        Controls.Add(flowSearch);
        Controls.Add(flowButtons);
        Controls.Add(entryGroup);
        Name = "PkgDbTab";
        Size = new Size(797, 485);
        entryGroup.ResumeLayout(false);
        entryTable.ResumeLayout(false);
        entryTable.PerformLayout();
        flowButtons.ResumeLayout(false);
        flowSearch.ResumeLayout(false);
        flowSearch.PerformLayout();
        ctxList.ResumeLayout(false);
        infoGroup.ResumeLayout(false);
        ctxInfo.ResumeLayout(false);
        statusPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
