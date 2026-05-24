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
            this.btnSave = new System.Windows.Forms.Button();
            this.btnShare = new System.Windows.Forms.Button();
            this.btnVersions = new System.Windows.Forms.Button();
            this.btnExportPdf = new System.Windows.Forms.Button();
            this.btnAddComment = new System.Windows.Forms.Button();
            this.btnInsertImage = new System.Windows.Forms.Button();
            this.btnToggleSide = new System.Windows.Forms.Button();

            this.splitOuter = new System.Windows.Forms.SplitContainer();
            this.splitMain = new System.Windows.Forms.SplitContainer();

            this.pnlEditor = new System.Windows.Forms.Panel();
            this.lblRaw = new System.Windows.Forms.Label();
            this.txtRawMarkdown = new System.Windows.Forms.TextBox();

            this.pnlPreview = new System.Windows.Forms.Panel();
            this.lblPreview = new System.Windows.Forms.Label();
            this.webPreview = new Microsoft.Web.WebView2.WinForms.WebView2();

            this.tabSide = new System.Windows.Forms.TabControl();
            this.tabChat = new System.Windows.Forms.TabPage();
            this.lstChat = new System.Windows.Forms.ListBox();
            this.pnlChatInput = new System.Windows.Forms.Panel();
            this.txtChatInput = new System.Windows.Forms.TextBox();
            this.btnChatSend = new System.Windows.Forms.Button();

            this.tabComments = new System.Windows.Forms.TabPage();
            this.lstComments = new System.Windows.Forms.ListBox();
            this.pnlCommentBtns = new System.Windows.Forms.Panel();
            this.btnCommentResolve = new System.Windows.Forms.Button();
            this.btnCommentDelete = new System.Windows.Forms.Button();
            this.btnCommentRefresh = new System.Windows.Forms.Button();

            this.tabAi = new System.Windows.Forms.TabPage();
            this.txtAiHistory = new System.Windows.Forms.TextBox();
            this.pnlAiInput = new System.Windows.Forms.Panel();
            this.cmbAiMode = new System.Windows.Forms.ComboBox();
            this.txtAiPrompt = new System.Windows.Forms.TextBox();
            this.btnAiSend = new System.Windows.Forms.Button();
            this.btnAiSettings = new System.Windows.Forms.Button();
            this.chkAiEditMode = new System.Windows.Forms.CheckBox();
            this.btnAiUndo = new System.Windows.Forms.Button();

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
            this.SuspendLayout();
            //
            // ─── HEADER (Toolbar) ────────────────────────────────
            //
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = AppTheme.ToolbarHeight + 8;
            this.pnlHeader.BackColor = AppTheme.Surface;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, AppTheme.SpaceSm, AppTheme.SpaceLg, AppTheme.SpaceSm);
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
            this.pnlHeader.Name = "pnlHeader";
            //
            this.btnBack.Location = new System.Drawing.Point(AppTheme.SpaceLg, 12);
            this.btnBack.Size = new System.Drawing.Size(90, AppTheme.ButtonHeight);
            this.btnBack.Text = "← Trở về";
            this.btnBack.Name = "btnBack";
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            //
            this.lblDocTitle.AutoSize = true;
            this.lblDocTitle.Font = AppTheme.H3;
            this.lblDocTitle.ForeColor = AppTheme.TextPrimary;
            this.lblDocTitle.Location = new System.Drawing.Point(120, 22);
            this.lblDocTitle.Name = "lblDocTitle";
            this.lblDocTitle.Text = "Tài liệu";
            //
            this.lblPermissionBadge.AutoSize = true;
            this.lblPermissionBadge.Font = AppTheme.Caption;
            this.lblPermissionBadge.ForeColor = AppTheme.Success;
            this.lblPermissionBadge.BackColor = System.Drawing.Color.FromArgb(0xDC, 0xFC, 0xE7);
            this.lblPermissionBadge.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceSm, 4, AppTheme.SpaceSm, 4);
            this.lblPermissionBadge.Location = new System.Drawing.Point(300, 22);
            this.lblPermissionBadge.Name = "lblPermissionBadge";
            this.lblPermissionBadge.Text = "Chủ sở hữu";
            //
            this.chkPeriodicAutosave.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.chkPeriodicAutosave.AutoSize = true;
            this.chkPeriodicAutosave.Font = AppTheme.Caption;
            this.chkPeriodicAutosave.ForeColor = AppTheme.TextSecondary;
            this.chkPeriodicAutosave.Name = "chkPeriodicAutosave";
            this.chkPeriodicAutosave.Text = "Tự lưu";
            this.chkPeriodicAutosave.CheckedChanged += new System.EventHandler(this.chkPeriodicAutosave_CheckedChanged);
            //
            this.cmbAutosaveInterval.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.cmbAutosaveInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAutosaveInterval.Font = AppTheme.Caption;
            this.cmbAutosaveInterval.Name = "cmbAutosaveInterval";
            this.cmbAutosaveInterval.Size = new System.Drawing.Size(80, AppTheme.ButtonHeight);
            this.cmbAutosaveInterval.Items.AddRange(new object[] { "1 phút", "5 phút", "30 phút" });
            this.cmbAutosaveInterval.SelectedIndex = 0;
            this.cmbAutosaveInterval.Enabled = false;
            this.cmbAutosaveInterval.SelectedIndexChanged += new System.EventHandler(this.cmbAutosaveInterval_SelectedIndexChanged);
            //
            // CTA buttons (right-aligned)
            //
            int rightStart = 0; // sẽ tính khi load
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Size = new System.Drawing.Size(110, AppTheme.ButtonHeight);
            this.btnSave.Text = "Lưu";
            this.btnSave.Name = "btnSave";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.btnShare.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnShare.Size = new System.Drawing.Size(110, AppTheme.ButtonHeight);
            this.btnShare.Text = "Chia sẻ";
            this.btnShare.Name = "btnShare";
            this.btnShare.Click += new System.EventHandler(this.btnShare_Click);

            this.btnVersions.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnVersions.Size = new System.Drawing.Size(110, AppTheme.ButtonHeight);
            this.btnVersions.Text = "Lịch sử";
            this.btnVersions.Name = "btnVersions";
            this.btnVersions.Click += new System.EventHandler(this.btnVersions_Click);

            this.btnExportPdf.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnExportPdf.Size = new System.Drawing.Size(110, AppTheme.ButtonHeight);
            this.btnExportPdf.Text = "Xuất PDF";
            this.btnExportPdf.Name = "btnExportPdf";
            this.btnExportPdf.Click += new System.EventHandler(this.btnExportPdf_Click);

            this.btnAddComment.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnAddComment.Size = new System.Drawing.Size(120, AppTheme.ButtonHeight);
            this.btnAddComment.Text = "Comment";
            this.btnAddComment.Name = "btnAddComment";
            this.btnAddComment.Click += new System.EventHandler(this.btnAddComment_Click);

            this.btnInsertImage.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnInsertImage.Size = new System.Drawing.Size(120, AppTheme.ButtonHeight);
            this.btnInsertImage.Text = "Chèn ảnh";
            this.btnInsertImage.Name = "btnInsertImage";
            this.btnInsertImage.Click += new System.EventHandler(this.btnInsertImage_Click);

            this.btnToggleSide.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnToggleSide.Size = new System.Drawing.Size(40, AppTheme.ButtonHeight);
            this.btnToggleSide.Text = "≡";
            this.btnToggleSide.Name = "btnToggleSide";
            this.btnToggleSide.Click += new System.EventHandler(this.btnToggleSide_Click);
            //
            // ─── SPLIT OUTER (editor+preview | side panel) ────────
            //
            this.splitOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitOuter.Panel1.Controls.Add(this.splitMain);
            this.splitOuter.Panel2.Controls.Add(this.tabSide);
            this.splitOuter.Panel2.BackColor = AppTheme.Background;
            this.splitOuter.SplitterDistance = 1180;
            this.splitOuter.SplitterWidth = 8;
            this.splitOuter.BackColor = AppTheme.Background;
            this.splitOuter.Name = "splitOuter";
            //
            // ─── SPLIT MAIN (editor | preview) ────────────────────
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Panel1.Controls.Add(this.pnlEditor);
            this.splitMain.Panel2.Controls.Add(this.pnlPreview);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, AppTheme.SpaceMd, AppTheme.SpaceSm, AppTheme.SpaceMd);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceSm, AppTheme.SpaceMd, AppTheme.SpaceMd, AppTheme.SpaceMd);
            this.splitMain.SplitterWidth = 4;
            this.splitMain.BackColor = AppTheme.Background;
            this.splitMain.Name = "splitMain";
            //
            // ─── EDITOR card ──────────────────────────────────────
            //
            this.pnlEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlEditor.BackColor = AppTheme.Surface;
            this.pnlEditor.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, 0, AppTheme.SpaceLg, AppTheme.SpaceLg);
            this.pnlEditor.Controls.Add(this.txtRawMarkdown);
            this.pnlEditor.Controls.Add(this.lblRaw);
            this.pnlEditor.Name = "pnlEditor";
            //
            this.lblRaw.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRaw.Height = 40;
            this.lblRaw.Padding = new System.Windows.Forms.Padding(0, AppTheme.SpaceMd, 0, AppTheme.SpaceSm);
            this.lblRaw.Font = AppTheme.BodyBold;
            this.lblRaw.ForeColor = AppTheme.TextSecondary;
            this.lblRaw.Text = "MARKDOWN";
            //
            this.txtRawMarkdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRawMarkdown.Font = AppTheme.Mono;
            this.txtRawMarkdown.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtRawMarkdown.BackColor = AppTheme.Surface;
            this.txtRawMarkdown.ForeColor = AppTheme.TextPrimary;
            this.txtRawMarkdown.Multiline = true;
            this.txtRawMarkdown.AcceptsReturn = true;
            this.txtRawMarkdown.AcceptsTab = true;
            this.txtRawMarkdown.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtRawMarkdown.WordWrap = true;
            this.txtRawMarkdown.Name = "txtRawMarkdown";
            this.txtRawMarkdown.TextChanged += new System.EventHandler(this.txtRawMarkdown_TextChanged);
            //
            // ─── PREVIEW card ─────────────────────────────────────
            //
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.BackColor = AppTheme.Surface;
            this.pnlPreview.Padding = new System.Windows.Forms.Padding(0);
            this.pnlPreview.Controls.Add(this.webPreview);
            this.pnlPreview.Controls.Add(this.lblPreview);
            this.pnlPreview.Name = "pnlPreview";
            //
            this.lblPreview.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPreview.Height = 40;
            this.lblPreview.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, AppTheme.SpaceMd, AppTheme.SpaceLg, AppTheme.SpaceSm);
            this.lblPreview.Font = AppTheme.BodyBold;
            this.lblPreview.ForeColor = AppTheme.TextSecondary;
            this.lblPreview.Text = "XEM TRƯỚC";
            //
            this.webPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webPreview.DefaultBackgroundColor = AppTheme.Surface;
            this.webPreview.ZoomFactor = 1D;
            this.webPreview.Name = "webPreview";
            //
            // ─── SIDE TAB ─────────────────────────────────────────
            //
            this.tabSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSide.Font = AppTheme.Body;
            this.tabSide.Controls.Add(this.tabChat);
            this.tabSide.Controls.Add(this.tabComments);
            this.tabSide.Controls.Add(this.tabAi);
            this.tabSide.SelectedIndex = 0;
            this.tabSide.Name = "tabSide";
            //
            // Chat tab
            //
            this.tabChat.Text = "Trò chuyện";
            this.tabChat.BackColor = AppTheme.Surface;
            this.tabChat.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd);
            this.tabChat.Controls.Add(this.lstChat);
            this.tabChat.Controls.Add(this.pnlChatInput);
            //
            this.lstChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstChat.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstChat.Font = AppTheme.Body;
            this.lstChat.HorizontalScrollbar = false;
            this.lstChat.IntegralHeight = false;
            this.lstChat.Name = "lstChat";
            //
            this.pnlChatInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlChatInput.Height = AppTheme.InputHeight + AppTheme.SpaceSm * 2;
            this.pnlChatInput.BackColor = AppTheme.Surface;
            this.pnlChatInput.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceSm);
            this.pnlChatInput.Controls.Add(this.txtChatInput);
            this.pnlChatInput.Controls.Add(this.btnChatSend);
            //
            this.txtChatInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtChatInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtChatInput.Font = AppTheme.Body;
            this.txtChatInput.Name = "txtChatInput";
            this.txtChatInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtChatInput_KeyDown);
            //
            this.btnChatSend.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnChatSend.Width = 80;
            this.btnChatSend.Text = "Gửi";
            this.btnChatSend.Name = "btnChatSend";
            this.btnChatSend.Click += new System.EventHandler(this.btnChatSend_Click);
            //
            // Comments tab
            //
            this.tabComments.Text = "Bình luận";
            this.tabComments.BackColor = AppTheme.Surface;
            this.tabComments.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd);
            this.tabComments.Controls.Add(this.lstComments);
            this.tabComments.Controls.Add(this.pnlCommentBtns);
            //
            this.lstComments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstComments.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstComments.Font = AppTheme.Body;
            this.lstComments.IntegralHeight = false;
            this.lstComments.Name = "lstComments";
            this.lstComments.DoubleClick += new System.EventHandler(this.lstComments_DoubleClick);
            //
            this.pnlCommentBtns.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlCommentBtns.Height = 56;
            this.pnlCommentBtns.BackColor = AppTheme.Surface;
            this.pnlCommentBtns.Padding = new System.Windows.Forms.Padding(0, AppTheme.SpaceMd, 0, 0);
            this.pnlCommentBtns.Controls.Add(this.btnCommentRefresh);
            this.pnlCommentBtns.Controls.Add(this.btnCommentDelete);
            this.pnlCommentBtns.Controls.Add(this.btnCommentResolve);
            //
            this.btnCommentResolve.Location = new System.Drawing.Point(0, 8);
            this.btnCommentResolve.Size = new System.Drawing.Size(120, AppTheme.ButtonHeight);
            this.btnCommentResolve.Text = "Đã xử lý";
            this.btnCommentResolve.Name = "btnCommentResolve";
            this.btnCommentResolve.Click += new System.EventHandler(this.btnCommentResolve_Click);
            //
            this.btnCommentDelete.Location = new System.Drawing.Point(130, 8);
            this.btnCommentDelete.Size = new System.Drawing.Size(120, AppTheme.ButtonHeight);
            this.btnCommentDelete.Text = "Xoá";
            this.btnCommentDelete.Name = "btnCommentDelete";
            this.btnCommentDelete.Click += new System.EventHandler(this.btnCommentDelete_Click);
            //
            this.btnCommentRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnCommentRefresh.Location = new System.Drawing.Point(280, 8);
            this.btnCommentRefresh.Size = new System.Drawing.Size(100, AppTheme.ButtonHeight);
            this.btnCommentRefresh.Text = "Tải lại";
            this.btnCommentRefresh.Name = "btnCommentRefresh";
            this.btnCommentRefresh.Click += new System.EventHandler(this.btnCommentRefresh_Click);
            //
            // AI tab
            //
            this.tabAi.Text = "Trợ lý AI";
            this.tabAi.BackColor = AppTheme.Surface;
            this.tabAi.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd);
            this.tabAi.Controls.Add(this.txtAiHistory);
            this.tabAi.Controls.Add(this.pnlAiInput);
            //
            this.txtAiHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAiHistory.BackColor = AppTheme.Background;
            this.txtAiHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtAiHistory.Font = AppTheme.Body;
            this.txtAiHistory.Multiline = true;
            this.txtAiHistory.ReadOnly = true;
            this.txtAiHistory.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtAiHistory.Name = "txtAiHistory";
            //
            this.pnlAiInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlAiInput.Height = 140;
            this.pnlAiInput.BackColor = AppTheme.Surface;
            this.pnlAiInput.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceSm);
            this.pnlAiInput.Controls.Add(this.txtAiPrompt);
            this.pnlAiInput.Controls.Add(this.btnAiSettings);
            this.pnlAiInput.Controls.Add(this.btnAiUndo);
            this.pnlAiInput.Controls.Add(this.btnAiSend);
            this.pnlAiInput.Controls.Add(this.cmbAiMode);
            this.pnlAiInput.Controls.Add(this.chkAiEditMode);
            //
            this.chkAiEditMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkAiEditMode.Text = "AI có thể chỉnh sửa văn bản (Action mode)";
            this.chkAiEditMode.Font = AppTheme.Body;
            this.chkAiEditMode.AutoSize = true;
            this.chkAiEditMode.Name = "chkAiEditMode";
            this.chkAiEditMode.CheckedChanged += new System.EventHandler(this.chkAiEditMode_CheckedChanged);
            //
            this.cmbAiMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAiMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbAiMode.Items.AddRange(new object[] { "Hỏi đáp", "Tóm tắt", "Viết tiếp", "Dịch" });
            this.cmbAiMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbAiMode.Font = AppTheme.Body;
            this.cmbAiMode.Name = "cmbAiMode";
            //
            this.txtAiPrompt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAiPrompt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAiPrompt.Font = AppTheme.Body;
            this.txtAiPrompt.Multiline = true;
            this.txtAiPrompt.Name = "txtAiPrompt";
            //
            this.btnAiSend.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnAiSend.Width = 80;
            this.btnAiSend.Text = "Gửi";
            this.btnAiSend.Name = "btnAiSend";
            this.btnAiSend.Click += new System.EventHandler(this.btnAiSend_Click);
            //
            this.btnAiSettings.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnAiSettings.Width = 90;
            this.btnAiSettings.Text = "⚙ Cài đặt";
            this.btnAiSettings.Name = "btnAiSettings";
            this.btnAiSettings.Click += new System.EventHandler(this.btnAiSettings_Click);
            //
            this.btnAiUndo.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnAiUndo.Width = 100;
            this.btnAiUndo.Text = "↶ Hoàn tác AI";
            this.btnAiUndo.Name = "btnAiUndo";
            this.btnAiUndo.Enabled = false;
            this.btnAiUndo.Click += new System.EventHandler(this.btnAiUndo_Click);
            //
            // Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.splitOuter);
            this.Controls.Add(this.pnlHeader);
            this.MinimumSize = new System.Drawing.Size(1280, 720);
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
    }
}