using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class VersionHistoryForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.split = new System.Windows.Forms.SplitContainer();
            this.listVersions = new System.Windows.Forms.ListView();
            this.colTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUser = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLabel = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.txtPreview = new System.Windows.Forms.TextBox();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblCount = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // split
            // 
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 0);
            this.split.Name = "split";
            this.split.Panel1.Controls.Add(this.listVersions);
            this.split.Panel1.BackColor = AppTheme.Surface;
            this.split.Panel2.Controls.Add(this.txtPreview);
            this.split.Panel2.BackColor = AppTheme.Surface;
            this.split.BackColor = AppTheme.Border;
            this.split.Size = new System.Drawing.Size(940, 480);
            this.split.SplitterDistance = 380;
            this.split.SplitterWidth = 2;
            // 
            // listVersions
            // 
            this.listVersions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colTime, this.colUser, this.colLabel });
            this.listVersions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listVersions.FullRowSelect = true;
            this.listVersions.HideSelection = false;
            this.listVersions.MultiSelect = false;
            this.listVersions.Name = "listVersions";
            this.listVersions.UseCompatibleStateImageBehavior = false;
            this.listVersions.View = System.Windows.Forms.View.Details;
            this.listVersions.SelectedIndexChanged += new System.EventHandler(this.listVersions_SelectedIndexChanged);
            // 
            this.colTime.Text = "Thời gian"; this.colTime.Width = 160;
            this.colUser.Text = "Người lưu"; this.colUser.Width = 110;
            this.colLabel.Text = "Ghi chú"; this.colLabel.Width = 100;
            // 
            // txtPreview
            // 
            this.txtPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPreview.Font = AppTheme.Mono;
            this.txtPreview.ForeColor = AppTheme.TextPrimary;
            this.txtPreview.BackColor = AppTheme.Surface;
            this.txtPreview.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPreview.Multiline = true;
            this.txtPreview.Name = "txtPreview";
            this.txtPreview.ReadOnly = true;
            this.txtPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtPreview.WordWrap = false;
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.lblCount);
            this.pnlButtons.Controls.Add(this.btnRefresh);
            this.pnlButtons.Controls.Add(this.btnDelete);
            this.pnlButtons.Controls.Add(this.btnRestore);
            this.pnlButtons.Controls.Add(this.btnClose);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.BackColor = AppTheme.Surface;
            this.pnlButtons.Location = new System.Drawing.Point(0, 480);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(940, 60);
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, AppTheme.SpaceMd, AppTheme.SpaceLg, AppTheme.SpaceMd);
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = true;
            this.lblCount.Font = AppTheme.Body;
            this.lblCount.ForeColor = AppTheme.TextSecondary;
            this.lblCount.Location = new System.Drawing.Point(AppTheme.SpaceLg, 20);
            this.lblCount.Name = "lblCount";
            this.lblCount.Text = "Tổng: 0";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRefresh.Location = new System.Drawing.Point(480, 14);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, AppTheme.ButtonHeightSmall);
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnRestore
            // 
            this.btnRestore.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRestore.Location = new System.Drawing.Point(590, 14);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(110, AppTheme.ButtonHeightSmall);
            this.btnRestore.Text = "Khôi phục";
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnDelete.Location = new System.Drawing.Point(710, 14);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(100, AppTheme.ButtonHeightSmall);
            this.btnDelete.Text = "Xoá";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.Location = new System.Drawing.Point(820, 14);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, AppTheme.ButtonHeightSmall);
            this.btnClose.Text = "Đóng";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // VersionHistoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(940, 540);
            this.Controls.Add(this.split);
            this.Controls.Add(this.pnlButtons);
            this.MinimumSize = new System.Drawing.Size(700, 400);
            this.Name = "VersionHistoryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Lịch sử phiên bản";
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            this.split.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            this.pnlButtons.ResumeLayout(false);
            this.pnlButtons.PerformLayout();
            this.ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.ListView listVersions;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ColumnHeader colUser;
        private System.Windows.Forms.ColumnHeader colLabel;
        private System.Windows.Forms.TextBox txtPreview;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblCount;
    }
}