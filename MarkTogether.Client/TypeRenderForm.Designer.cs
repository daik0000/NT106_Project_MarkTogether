using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class TypeRenderForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.btnBack = new System.Windows.Forms.Button();
            this.lblDocTitle = new System.Windows.Forms.Label();
            this.lblPermissionBadge = new System.Windows.Forms.Label();
            this.chkPeriodicAutosave = new System.Windows.Forms.CheckBox();
            this.cmbAutosaveInterval = new System.Windows.Forms.ComboBox();
            this.btnInsertImage = new System.Windows.Forms.Button();
            this.btnAddComment = new System.Windows.Forms.Button();
            this.btnExportPdf = new System.Windows.Forms.Button();
            this.btnVersions = new System.Windows.Forms.Button();
            this.btnShare = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnToggleSide = new System.Windows.Forms.Button();
            this.splitOuter = new System.Windows.Forms.SplitContainer();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlEditor = new System.Windows.Forms.Panel();
            this.txtRawMarkdown = new System.Windows.Forms.TextBox();
            this.lblRaw = new System.Windows.Forms.Label();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.webPreview = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.lblPreview = new System.Windows.Forms.Label();
            this.tabSide = new System.Windows.Forms.TabControl();
            this.tabChat = new System.Windows.Forms.TabPage();
            this.lstChat = new System.Windows.Forms.ListBox();
            this.pnlChatInput = new System.Windows.Forms.Panel();
            this.txtChatInput = new System.Windows.Forms.TextBox();
            this.btnChatSend = new System.Windows.Forms.Button();
            this.tabComments = new System.Windows.Forms.TabPage();
            this.lstComments = new System.Windows.Forms.ListBox();
            this.pnlCommentBtns = new System.Windows.Forms.Panel();
            this.btnCommentRefresh = new System.Windows.Forms.Button();
            this.btnCommentDelete = new System.Windows.Forms.Button();
            this.btnCommentResolve = new System.Windows.Forms.Button();
            this.tabAi = new System.Windows.Forms.TabPage();
            this.txtAiHistory = new System.Windows.Forms.TextBox();
            this.pnlAiInput = new System.Windows.Forms.Panel();
            this.pnlAiPrompt = new System.Windows.Forms.Panel();
            this.txtAiPrompt = new System.Windows.Forms.TextBox();
            this.btnAiSend = new System.Windows.Forms.Button();
            this.pnlAiActions = new System.Windows.Forms.Panel();
            this.btnAiSettings = new System.Windows.Forms.Button();
            this.btnAiUndo = new System.Windows.Forms.Button();
            this.cmbAiMode = new System.Windows.Forms.ComboBox();
            this.chkAiEditMode = new System.Windows.Forms.CheckBox();
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitOuter)).BeginInit();
            this.splitOuter.Panel1.SuspendLayout();
            this.splitOuter.Panel2.SuspendLayout();
            this.splitOuter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlEditor.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webPreview)).BeginInit();
            this.tabSide.SuspendLayout();
            this.tabChat.SuspendLayout();
            this.pnlChatInput.SuspendLayout();
            this.tabComments.SuspendLayout();
            this.pnlCommentBtns.SuspendLayout();
            this.tabAi.SuspendLayout();
            this.pnlAiInput.SuspendLayout();
            this.pnlAiPrompt.SuspendLayout();
            this.pnlAiActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.btnBack);
            this.pnlHeader.Controls.Add(this.lblDocTitle);
            this.pnlHeader.Controls.Add(this.lblPermissionBadge);
            this.pnlHeader.Controls.Add(this.chkPeriodicAutosave);
            this.pnlHeader.Controls.Add(this.cmbAutosaveInterval);
            this.pnlHeader.Controls.Add(this.btnInsertImage);
            this.pnlHeader.Controls.Add(this.btnAddComment);
            this.pnlHeader.Controls.Add(this.btnExportPdf);
            this.pnlHeader.Controls.Add(this.btnVersions);
            this.pnlHeader.Controls.Add(this.btnShare);
            this.pnlHeader.Controls.Add(this.btnSave);
            this.pnlHeader.Controls.Add(this.btnToggleSide);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Margin = new System.Windows.Forms.Padding(4);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.pnlHeader.Size = new System.Drawing.Size(1924, 70);
            this.pnlHeader.TabIndex = 1;
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(20, 15);
            this.btnBack.Margin = new System.Windows.Forms.Padding(4);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(112, 50);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "← Trở về";
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // lblDocTitle
            // 
            this.lblDocTitle.AutoSize = true;
            this.lblDocTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblDocTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblDocTitle.Location = new System.Drawing.Point(150, 28);
            this.lblDocTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDocTitle.Name = "lblDocTitle";
            this.lblDocTitle.Size = new System.Drawing.Size(85, 30);
            this.lblDocTitle.TabIndex = 1;
            this.lblDocTitle.Text = "Tài liệu";
            // 
            // lblPermissionBadge
            // 
            this.lblPermissionBadge.AutoSize = true;
            this.lblPermissionBadge.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(252)))), ((int)(((byte)(231)))));
            this.lblPermissionBadge.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPermissionBadge.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(197)))), ((int)(((byte)(94)))));
            this.lblPermissionBadge.Location = new System.Drawing.Point(454, 28);
            this.lblPermissionBadge.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPermissionBadge.Name = "lblPermissionBadge";
            this.lblPermissionBadge.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblPermissionBadge.Size = new System.Drawing.Size(102, 30);
            this.lblPermissionBadge.TabIndex = 2;
            this.lblPermissionBadge.Text = "Chủ sở hữu";
            // 
            // chkPeriodicAutosave
            // 
            this.chkPeriodicAutosave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkPeriodicAutosave.AutoSize = true;
            this.chkPeriodicAutosave.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkPeriodicAutosave.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.chkPeriodicAutosave.Location = new System.Drawing.Point(1731, 0);
            this.chkPeriodicAutosave.Margin = new System.Windows.Forms.Padding(4);
            this.chkPeriodicAutosave.Name = "chkPeriodicAutosave";
            this.chkPeriodicAutosave.Size = new System.Drawing.Size(73, 24);
            this.chkPeriodicAutosave.TabIndex = 3;
            this.chkPeriodicAutosave.Text = "Tự lưu";
            this.chkPeriodicAutosave.CheckedChanged += new System.EventHandler(this.chkPeriodicAutosave_CheckedChanged);
            // 
            // cmbAutosaveInterval
            // 
            this.cmbAutosaveInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbAutosaveInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAutosaveInterval.Enabled = false;
            this.cmbAutosaveInterval.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbAutosaveInterval.Items.AddRange(new object[] {
            "1 phút",
            "5 phút",
            "30 phút"});
            this.cmbAutosaveInterval.Location = new System.Drawing.Point(1674, 0);
            this.cmbAutosaveInterval.Margin = new System.Windows.Forms.Padding(4);
            this.cmbAutosaveInterval.Name = "cmbAutosaveInterval";
            this.cmbAutosaveInterval.Size = new System.Drawing.Size(99, 28);
            this.cmbAutosaveInterval.TabIndex = 4;
            this.cmbAutosaveInterval.SelectedIndexChanged += new System.EventHandler(this.cmbAutosaveInterval_SelectedIndexChanged);
            // 
            // btnInsertImage
            // 
            this.btnInsertImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInsertImage.Location = new System.Drawing.Point(1674, 0);
            this.btnInsertImage.Margin = new System.Windows.Forms.Padding(4);
            this.btnInsertImage.Name = "btnInsertImage";
            this.btnInsertImage.Size = new System.Drawing.Size(150, 50);
            this.btnInsertImage.TabIndex = 5;
            this.btnInsertImage.Text = "Chèn ảnh";
            this.btnInsertImage.Click += new System.EventHandler(this.btnInsertImage_Click);
            // 
            // btnAddComment
            // 
            this.btnAddComment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddComment.Location = new System.Drawing.Point(1674, 0);
            this.btnAddComment.Margin = new System.Windows.Forms.Padding(4);
            this.btnAddComment.Name = "btnAddComment";
            this.btnAddComment.Size = new System.Drawing.Size(150, 50);
            this.btnAddComment.TabIndex = 6;
            this.btnAddComment.Text = "Comment";
            this.btnAddComment.Click += new System.EventHandler(this.btnAddComment_Click);
            // 
            // btnExportPdf
            // 
            this.btnExportPdf.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportPdf.Location = new System.Drawing.Point(1674, 0);
            this.btnExportPdf.Margin = new System.Windows.Forms.Padding(4);
            this.btnExportPdf.Name = "btnExportPdf";
            this.btnExportPdf.Size = new System.Drawing.Size(138, 50);
            this.btnExportPdf.TabIndex = 7;
            this.btnExportPdf.Text = "Xuất PDF";
            this.btnExportPdf.Click += new System.EventHandler(this.btnExportPdf_Click);
            // 
            // btnVersions
            // 
            this.btnVersions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVersions.Location = new System.Drawing.Point(1674, 0);
            this.btnVersions.Margin = new System.Windows.Forms.Padding(4);
            this.btnVersions.Name = "btnVersions";
            this.btnVersions.Size = new System.Drawing.Size(138, 50);
            this.btnVersions.TabIndex = 8;
            this.btnVersions.Text = "Lịch sử";
            this.btnVersions.Click += new System.EventHandler(this.btnVersions_Click);
            // 
            // btnShare
            // 
            this.btnShare.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnShare.Location = new System.Drawing.Point(1674, 0);
            this.btnShare.Margin = new System.Windows.Forms.Padding(4);
            this.btnShare.Name = "btnShare";
            this.btnShare.Size = new System.Drawing.Size(138, 50);
            this.btnShare.TabIndex = 9;
            this.btnShare.Text = "Chia sẻ";
            this.btnShare.Click += new System.EventHandler(this.btnShare_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(1674, 0);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(138, 50);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Lưu";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnToggleSide
            // 
            this.btnToggleSide.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnToggleSide.Location = new System.Drawing.Point(1674, 0);
            this.btnToggleSide.Margin = new System.Windows.Forms.Padding(4);
            this.btnToggleSide.Name = "btnToggleSide";
            this.btnToggleSide.Size = new System.Drawing.Size(50, 50);
            this.btnToggleSide.TabIndex = 11;
            this.btnToggleSide.Text = "≡";
            this.btnToggleSide.Click += new System.EventHandler(this.btnToggleSide_Click);
            // 
            // splitOuter
            // 
            this.splitOuter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.splitOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitOuter.Location = new System.Drawing.Point(0, 70);
            this.splitOuter.Margin = new System.Windows.Forms.Padding(4);
            this.splitOuter.Name = "splitOuter";
            // 
            // splitOuter.Panel1
            // 
            this.splitOuter.Panel1.Controls.Add(this.splitMain);
            // 
            // splitOuter.Panel2
            // 
            this.splitOuter.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.splitOuter.Panel2.Controls.Add(this.tabSide);
            this.splitOuter.Size = new System.Drawing.Size(1924, 985);
            this.splitOuter.SplitterDistance = 1552;
            this.splitOuter.SplitterWidth = 10;
            this.splitOuter.TabIndex = 0;
            // 
            // splitMain
            // 
            this.splitMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Margin = new System.Windows.Forms.Padding(4);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.pnlEditor);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(15, 15, 10, 15);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.pnlPreview);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(10, 15, 15, 15);
            this.splitMain.Size = new System.Drawing.Size(1552, 985);
            this.splitMain.SplitterDistance = 517;
            this.splitMain.SplitterWidth = 5;
            this.splitMain.TabIndex = 0;
            // 
            // pnlEditor
            // 
            this.pnlEditor.BackColor = System.Drawing.Color.White;
            this.pnlEditor.Controls.Add(this.txtRawMarkdown);
            this.pnlEditor.Controls.Add(this.lblRaw);
            this.pnlEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlEditor.Location = new System.Drawing.Point(15, 15);
            this.pnlEditor.Margin = new System.Windows.Forms.Padding(4);
            this.pnlEditor.Name = "pnlEditor";
            this.pnlEditor.Padding = new System.Windows.Forms.Padding(20, 0, 20, 20);
            this.pnlEditor.Size = new System.Drawing.Size(492, 955);
            this.pnlEditor.TabIndex = 0;
            // 
            // txtRawMarkdown
            // 
            this.txtRawMarkdown.AcceptsReturn = true;
            this.txtRawMarkdown.AcceptsTab = true;
            this.txtRawMarkdown.BackColor = System.Drawing.Color.White;
            this.txtRawMarkdown.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtRawMarkdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRawMarkdown.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtRawMarkdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.txtRawMarkdown.Location = new System.Drawing.Point(20, 50);
            this.txtRawMarkdown.Margin = new System.Windows.Forms.Padding(4);
            this.txtRawMarkdown.Multiline = true;
            this.txtRawMarkdown.Name = "txtRawMarkdown";
            this.txtRawMarkdown.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtRawMarkdown.Size = new System.Drawing.Size(452, 885);
            this.txtRawMarkdown.TabIndex = 0;
            this.txtRawMarkdown.TextChanged += new System.EventHandler(this.txtRawMarkdown_TextChanged);
            // 
            // lblRaw
            // 
            this.lblRaw.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRaw.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblRaw.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblRaw.Location = new System.Drawing.Point(20, 0);
            this.lblRaw.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRaw.Name = "lblRaw";
            this.lblRaw.Padding = new System.Windows.Forms.Padding(0, 15, 0, 10);
            this.lblRaw.Size = new System.Drawing.Size(452, 50);
            this.lblRaw.TabIndex = 1;
            this.lblRaw.Text = "MARKDOWN";
            // 
            // pnlPreview
            // 
            this.pnlPreview.BackColor = System.Drawing.Color.White;
            this.pnlPreview.Controls.Add(this.webPreview);
            this.pnlPreview.Controls.Add(this.lblPreview);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Location = new System.Drawing.Point(10, 15);
            this.pnlPreview.Margin = new System.Windows.Forms.Padding(4);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(1005, 955);
            this.pnlPreview.TabIndex = 0;
            // 
            // webPreview
            // 
            this.webPreview.AllowExternalDrop = true;
            this.webPreview.CreationProperties = null;
            this.webPreview.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webPreview.Location = new System.Drawing.Point(0, 50);
            this.webPreview.Margin = new System.Windows.Forms.Padding(4);
            this.webPreview.Name = "webPreview";
            this.webPreview.Size = new System.Drawing.Size(1005, 905);
            this.webPreview.TabIndex = 0;
            this.webPreview.ZoomFactor = 1D;
            // 
            // lblPreview
            // 
            this.lblPreview.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPreview.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblPreview.Location = new System.Drawing.Point(0, 0);
            this.lblPreview.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Padding = new System.Windows.Forms.Padding(20, 15, 20, 10);
            this.lblPreview.Size = new System.Drawing.Size(1005, 50);
            this.lblPreview.TabIndex = 1;
            this.lblPreview.Text = "XEM TRƯỚC";
            // 
            // tabSide
            // 
            this.tabSide.Controls.Add(this.tabChat);
            this.tabSide.Controls.Add(this.tabComments);
            this.tabSide.Controls.Add(this.tabAi);
            this.tabSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSide.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabSide.Location = new System.Drawing.Point(0, 0);
            this.tabSide.Margin = new System.Windows.Forms.Padding(4);
            this.tabSide.Name = "tabSide";
            this.tabSide.SelectedIndex = 0;
            this.tabSide.Size = new System.Drawing.Size(362, 985);
            this.tabSide.TabIndex = 0;
            // 
            // tabChat
            // 
            this.tabChat.BackColor = System.Drawing.Color.White;
            this.tabChat.Controls.Add(this.lstChat);
            this.tabChat.Controls.Add(this.pnlChatInput);
            this.tabChat.Location = new System.Drawing.Point(4, 32);
            this.tabChat.Margin = new System.Windows.Forms.Padding(4);
            this.tabChat.Name = "tabChat";
            this.tabChat.Padding = new System.Windows.Forms.Padding(15);
            this.tabChat.Size = new System.Drawing.Size(354, 949);
            this.tabChat.TabIndex = 0;
            this.tabChat.Text = "Trò chuyện";
            // 
            // lstChat
            // 
            this.lstChat.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstChat.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstChat.IntegralHeight = false;
            this.lstChat.ItemHeight = 23;
            this.lstChat.Location = new System.Drawing.Point(15, 15);
            this.lstChat.Margin = new System.Windows.Forms.Padding(4);
            this.lstChat.Name = "lstChat";
            this.lstChat.Size = new System.Drawing.Size(324, 849);
            this.lstChat.TabIndex = 0;
            // 
            // pnlChatInput
            // 
            this.pnlChatInput.BackColor = System.Drawing.Color.White;
            this.pnlChatInput.Controls.Add(this.txtChatInput);
            this.pnlChatInput.Controls.Add(this.btnChatSend);
            this.pnlChatInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlChatInput.Location = new System.Drawing.Point(15, 864);
            this.pnlChatInput.Margin = new System.Windows.Forms.Padding(4);
            this.pnlChatInput.Name = "pnlChatInput";
            this.pnlChatInput.Padding = new System.Windows.Forms.Padding(10);
            this.pnlChatInput.Size = new System.Drawing.Size(324, 70);
            this.pnlChatInput.TabIndex = 1;
            // 
            // txtChatInput
            // 
            this.txtChatInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtChatInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtChatInput.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtChatInput.Location = new System.Drawing.Point(10, 10);
            this.txtChatInput.Margin = new System.Windows.Forms.Padding(4);
            this.txtChatInput.Name = "txtChatInput";
            this.txtChatInput.Size = new System.Drawing.Size(204, 30);
            this.txtChatInput.TabIndex = 0;
            this.txtChatInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtChatInput_KeyDown);
            // 
            // btnChatSend
            // 
            this.btnChatSend.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnChatSend.Location = new System.Drawing.Point(214, 10);
            this.btnChatSend.Margin = new System.Windows.Forms.Padding(4);
            this.btnChatSend.Name = "btnChatSend";
            this.btnChatSend.Size = new System.Drawing.Size(100, 50);
            this.btnChatSend.TabIndex = 1;
            this.btnChatSend.Text = "Gửi";
            this.btnChatSend.Click += new System.EventHandler(this.btnChatSend_Click);
            // 
            // tabComments
            // 
            this.tabComments.BackColor = System.Drawing.Color.White;
            this.tabComments.Controls.Add(this.lstComments);
            this.tabComments.Controls.Add(this.pnlCommentBtns);
            this.tabComments.Location = new System.Drawing.Point(4, 32);
            this.tabComments.Margin = new System.Windows.Forms.Padding(4);
            this.tabComments.Name = "tabComments";
            this.tabComments.Padding = new System.Windows.Forms.Padding(15);
            this.tabComments.Size = new System.Drawing.Size(354, 949);
            this.tabComments.TabIndex = 1;
            this.tabComments.Text = "Bình luận";
            // 
            // lstComments
            // 
            this.lstComments.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstComments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstComments.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstComments.IntegralHeight = false;
            this.lstComments.ItemHeight = 23;
            this.lstComments.Location = new System.Drawing.Point(15, 15);
            this.lstComments.Margin = new System.Windows.Forms.Padding(4);
            this.lstComments.Name = "lstComments";
            this.lstComments.Size = new System.Drawing.Size(324, 849);
            this.lstComments.TabIndex = 0;
            this.lstComments.DoubleClick += new System.EventHandler(this.lstComments_DoubleClick);
            // 
            // pnlCommentBtns
            // 
            this.pnlCommentBtns.BackColor = System.Drawing.Color.White;
            this.pnlCommentBtns.Controls.Add(this.btnCommentRefresh);
            this.pnlCommentBtns.Controls.Add(this.btnCommentDelete);
            this.pnlCommentBtns.Controls.Add(this.btnCommentResolve);
            this.pnlCommentBtns.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlCommentBtns.Location = new System.Drawing.Point(15, 864);
            this.pnlCommentBtns.Margin = new System.Windows.Forms.Padding(4);
            this.pnlCommentBtns.Name = "pnlCommentBtns";
            this.pnlCommentBtns.Padding = new System.Windows.Forms.Padding(0, 15, 0, 0);
            this.pnlCommentBtns.Size = new System.Drawing.Size(324, 70);
            this.pnlCommentBtns.TabIndex = 1;
            // 
            // btnCommentRefresh
            // 
            this.btnCommentRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCommentRefresh.Location = new System.Drawing.Point(424, 10);
            this.btnCommentRefresh.Margin = new System.Windows.Forms.Padding(4);
            this.btnCommentRefresh.Name = "btnCommentRefresh";
            this.btnCommentRefresh.Size = new System.Drawing.Size(125, 50);
            this.btnCommentRefresh.TabIndex = 0;
            this.btnCommentRefresh.Text = "Tải lại";
            this.btnCommentRefresh.Click += new System.EventHandler(this.btnCommentRefresh_Click);
            // 
            // btnCommentDelete
            // 
            this.btnCommentDelete.Location = new System.Drawing.Point(162, 10);
            this.btnCommentDelete.Margin = new System.Windows.Forms.Padding(4);
            this.btnCommentDelete.Name = "btnCommentDelete";
            this.btnCommentDelete.Size = new System.Drawing.Size(150, 50);
            this.btnCommentDelete.TabIndex = 1;
            this.btnCommentDelete.Text = "Xoá";
            this.btnCommentDelete.Click += new System.EventHandler(this.btnCommentDelete_Click);
            // 
            // btnCommentResolve
            // 
            this.btnCommentResolve.Location = new System.Drawing.Point(0, 10);
            this.btnCommentResolve.Margin = new System.Windows.Forms.Padding(4);
            this.btnCommentResolve.Name = "btnCommentResolve";
            this.btnCommentResolve.Size = new System.Drawing.Size(150, 50);
            this.btnCommentResolve.TabIndex = 2;
            this.btnCommentResolve.Text = "Đã xử lý";
            this.btnCommentResolve.Click += new System.EventHandler(this.btnCommentResolve_Click);
            // 
            // tabAi
            // 
            this.tabAi.BackColor = System.Drawing.Color.White;
            this.tabAi.Controls.Add(this.txtAiHistory);
            this.tabAi.Controls.Add(this.pnlAiInput);
            this.tabAi.Location = new System.Drawing.Point(4, 32);
            this.tabAi.Margin = new System.Windows.Forms.Padding(4);
            this.tabAi.Name = "tabAi";
            this.tabAi.Padding = new System.Windows.Forms.Padding(15);
            this.tabAi.Size = new System.Drawing.Size(354, 949);
            this.tabAi.TabIndex = 2;
            this.tabAi.Text = "Trợ lý AI";
            // 
            // txtAiHistory
            // 
            this.txtAiHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtAiHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtAiHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAiHistory.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtAiHistory.Location = new System.Drawing.Point(15, 15);
            this.txtAiHistory.Margin = new System.Windows.Forms.Padding(4);
            this.txtAiHistory.Multiline = true;
            this.txtAiHistory.Name = "txtAiHistory";
            this.txtAiHistory.ReadOnly = true;
            this.txtAiHistory.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtAiHistory.Size = new System.Drawing.Size(324, 744);
            this.txtAiHistory.TabIndex = 0;
            // 
            // pnlAiInput
            // 
            this.pnlAiInput.BackColor = System.Drawing.Color.White;
            this.pnlAiInput.Controls.Add(this.pnlAiPrompt);
            this.pnlAiInput.Controls.Add(this.pnlAiActions);
            this.pnlAiInput.Controls.Add(this.cmbAiMode);
            this.pnlAiInput.Controls.Add(this.chkAiEditMode);
            this.pnlAiInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlAiInput.Location = new System.Drawing.Point(15, 759);
            this.pnlAiInput.Margin = new System.Windows.Forms.Padding(4);
            this.pnlAiInput.Name = "pnlAiInput";
            this.pnlAiInput.Padding = new System.Windows.Forms.Padding(10);
            this.pnlAiInput.Size = new System.Drawing.Size(324, 175);
            this.pnlAiInput.TabIndex = 1;
            // 
            // pnlAiPrompt
            // 
            this.pnlAiPrompt.BackColor = System.Drawing.Color.White;
            this.pnlAiPrompt.Controls.Add(this.txtAiPrompt);
            this.pnlAiPrompt.Controls.Add(this.btnAiSend);
            this.pnlAiPrompt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAiPrompt.Location = new System.Drawing.Point(10, 110);
            this.pnlAiPrompt.Margin = new System.Windows.Forms.Padding(4);
            this.pnlAiPrompt.Name = "pnlAiPrompt";
            this.pnlAiPrompt.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.pnlAiPrompt.Size = new System.Drawing.Size(304, 55);
            this.pnlAiPrompt.TabIndex = 7;
            // 
            // txtAiPrompt
            // 
            this.txtAiPrompt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAiPrompt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAiPrompt.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtAiPrompt.Location = new System.Drawing.Point(0, 6);
            this.txtAiPrompt.Margin = new System.Windows.Forms.Padding(4);
            this.txtAiPrompt.Multiline = true;
            this.txtAiPrompt.Name = "txtAiPrompt";
            this.txtAiPrompt.Size = new System.Drawing.Size(214, 49);
            this.txtAiPrompt.TabIndex = 0;
            // 
            // btnAiSend
            // 
            this.btnAiSend.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnAiSend.Location = new System.Drawing.Point(214, 6);
            this.btnAiSend.Margin = new System.Windows.Forms.Padding(4);
            this.btnAiSend.Name = "btnAiSend";
            this.btnAiSend.Size = new System.Drawing.Size(90, 49);
            this.btnAiSend.TabIndex = 3;
            this.btnAiSend.Text = "Gửi";
            this.btnAiSend.Click += new System.EventHandler(this.btnAiSend_Click);
            // 
            // pnlAiActions
            // 
            this.pnlAiActions.BackColor = System.Drawing.Color.White;
            this.pnlAiActions.Controls.Add(this.btnAiSettings);
            this.pnlAiActions.Controls.Add(this.btnAiUndo);
            this.pnlAiActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAiActions.Location = new System.Drawing.Point(10, 68);
            this.pnlAiActions.Margin = new System.Windows.Forms.Padding(4);
            this.pnlAiActions.Name = "pnlAiActions";
            this.pnlAiActions.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.pnlAiActions.Size = new System.Drawing.Size(304, 42);
            this.pnlAiActions.TabIndex = 6;
            // 
            // btnAiSettings
            // 
            this.btnAiSettings.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAiSettings.Location = new System.Drawing.Point(0, 4);
            this.btnAiSettings.Margin = new System.Windows.Forms.Padding(4);
            this.btnAiSettings.Name = "btnAiSettings";
            this.btnAiSettings.Size = new System.Drawing.Size(157, 34);
            this.btnAiSettings.TabIndex = 1;
            this.btnAiSettings.Text = "⚙ Cài đặt";
            this.btnAiSettings.Click += new System.EventHandler(this.btnAiSettings_Click);
            // 
            // btnAiUndo
            // 
            this.btnAiUndo.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnAiUndo.Enabled = false;
            this.btnAiUndo.Location = new System.Drawing.Point(155, 4);
            this.btnAiUndo.Margin = new System.Windows.Forms.Padding(4);
            this.btnAiUndo.Name = "btnAiUndo";
            this.btnAiUndo.Size = new System.Drawing.Size(149, 34);
            this.btnAiUndo.TabIndex = 2;
            this.btnAiUndo.Text = "↶ Hoàn tác AI";
            this.btnAiUndo.Click += new System.EventHandler(this.btnAiUndo_Click);
            // 
            // cmbAiMode
            // 
            this.cmbAiMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbAiMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAiMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbAiMode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbAiMode.Items.AddRange(new object[] {
            "Hỏi đáp",
            "Tóm tắt",
            "Viết tiếp",
            "Dịch"});
            this.cmbAiMode.Location = new System.Drawing.Point(10, 37);
            this.cmbAiMode.Margin = new System.Windows.Forms.Padding(4);
            this.cmbAiMode.Name = "cmbAiMode";
            this.cmbAiMode.Size = new System.Drawing.Size(304, 31);
            this.cmbAiMode.TabIndex = 4;
            // 
            // chkAiEditMode
            // 
            this.chkAiEditMode.AutoSize = true;
            this.chkAiEditMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkAiEditMode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkAiEditMode.Location = new System.Drawing.Point(10, 10);
            this.chkAiEditMode.Margin = new System.Windows.Forms.Padding(4);
            this.chkAiEditMode.Name = "chkAiEditMode";
            this.chkAiEditMode.Size = new System.Drawing.Size(304, 27);
            this.chkAiEditMode.TabIndex = 5;
            this.chkAiEditMode.Text = "AI có thể chỉnh sửa văn bản (Action mode)";
            this.chkAiEditMode.CheckedChanged += new System.EventHandler(this.chkAiEditMode_CheckedChanged);
            // 
            // TypeRenderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(1924, 1055);
            this.Controls.Add(this.splitOuter);
            this.Controls.Add(this.pnlHeader);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(1596, 888);
            this.Name = "TypeRenderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarkTogether — Editor";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.splitOuter.Panel1.ResumeLayout(false);
            this.splitOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitOuter)).EndInit();
            this.splitOuter.ResumeLayout(false);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlEditor.ResumeLayout(false);
            this.pnlEditor.PerformLayout();
            this.pnlPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webPreview)).EndInit();
            this.tabSide.ResumeLayout(false);
            this.tabChat.ResumeLayout(false);
            this.pnlChatInput.ResumeLayout(false);
            this.pnlChatInput.PerformLayout();
            this.tabComments.ResumeLayout(false);
            this.pnlCommentBtns.ResumeLayout(false);
            this.tabAi.ResumeLayout(false);
            this.tabAi.PerformLayout();
            this.pnlAiInput.ResumeLayout(false);
            this.pnlAiInput.PerformLayout();
            this.pnlAiPrompt.ResumeLayout(false);
            this.pnlAiPrompt.PerformLayout();
            this.pnlAiActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        // Header
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Label lblDocTitle;
        private System.Windows.Forms.Label lblPermissionBadge;
        private System.Windows.Forms.CheckBox chkPeriodicAutosave;
        private System.Windows.Forms.ComboBox cmbAutosaveInterval;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnShare;
        private System.Windows.Forms.Button btnVersions;
        private System.Windows.Forms.Button btnExportPdf;
        private System.Windows.Forms.Button btnAddComment;
        private System.Windows.Forms.Button btnInsertImage;
        private System.Windows.Forms.Button btnToggleSide;

        // Body
        private System.Windows.Forms.SplitContainer splitOuter;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlEditor;
        private System.Windows.Forms.Label lblRaw;
        private System.Windows.Forms.TextBox txtRawMarkdown;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.Label lblPreview;
        private Microsoft.Web.WebView2.WinForms.WebView2 webPreview;

        // Side panel
        private System.Windows.Forms.TabControl tabSide;
        private System.Windows.Forms.TabPage tabChat;
        private System.Windows.Forms.ListBox lstChat;
        private System.Windows.Forms.Panel pnlChatInput;
        private System.Windows.Forms.TextBox txtChatInput;
        private System.Windows.Forms.Button btnChatSend;

        private System.Windows.Forms.TabPage tabComments;
        private System.Windows.Forms.ListBox lstComments;
        private System.Windows.Forms.Panel pnlCommentBtns;
        private System.Windows.Forms.Button btnCommentResolve;
        private System.Windows.Forms.Button btnCommentDelete;
        private System.Windows.Forms.Button btnCommentRefresh;

        private System.Windows.Forms.TabPage tabAi;
        private System.Windows.Forms.TextBox txtAiHistory;
        private System.Windows.Forms.Panel pnlAiInput;
        private System.Windows.Forms.ComboBox cmbAiMode;
        private System.Windows.Forms.TextBox txtAiPrompt;
        private System.Windows.Forms.Button btnAiSend;
        private System.Windows.Forms.Button btnAiSettings;
        private System.Windows.Forms.CheckBox chkAiEditMode;
        private System.Windows.Forms.Button btnAiUndo;
        private System.Windows.Forms.Panel pnlAiActions;
        private System.Windows.Forms.Panel pnlAiPrompt;
    }
}