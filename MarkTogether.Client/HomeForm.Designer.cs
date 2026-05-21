using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class HomeForm
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
            this.lblBrand = new System.Windows.Forms.Label();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.btnLogout = new System.Windows.Forms.Button();

            this.pnlBody = new System.Windows.Forms.Panel();

            this.pnlActionBar = new System.Windows.Forms.Panel();
            this.lblPageTitle = new System.Windows.Forms.Label();
            this.lblPageSubtitle = new System.Windows.Forms.Label();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.pnlJoin = new System.Windows.Forms.Panel();
            this.txtJoinCode = new System.Windows.Forms.TextBox();
            this.btnJoinCode = new System.Windows.Forms.Button();

            this.pnlFilter = new System.Windows.Forms.Panel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.cmbSortMode = new System.Windows.Forms.ComboBox();
            this.lblCount = new System.Windows.Forms.Label();

            this.pnlListContainer = new System.Windows.Forms.Panel();
            this.listDocuments = new System.Windows.Forms.ListView();
            this.colTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUpdatedAt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPermission = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));

            this.pnlHeader.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlActionBar.SuspendLayout();
            this.pnlJoin.SuspendLayout();
            this.pnlFilter.SuspendLayout();
            this.pnlListContainer.SuspendLayout();
            this.SuspendLayout();
            //
            // ─── HEADER ─────────────────────────────────────────
            //
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.BackColor = AppTheme.Surface;
            this.pnlHeader.Height = 64;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceXl, 0, AppTheme.SpaceXl, 0);
            this.pnlHeader.Controls.Add(this.lblBrand);
            this.pnlHeader.Controls.Add(this.lblWelcome);
            this.pnlHeader.Controls.Add(this.btnLogout);
            this.pnlHeader.Name = "pnlHeader";
            //
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = AppTheme.H2;
            this.lblBrand.ForeColor = AppTheme.Primary;
            this.lblBrand.Location = new System.Drawing.Point(AppTheme.SpaceXl, 18);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Text = "MarkTogether";
            //
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = AppTheme.Body;
            this.lblWelcome.ForeColor = AppTheme.TextSecondary;
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Text = "Xin chào, user";
            //
            this.btnLogout.Size = new System.Drawing.Size(110, 36);
            this.btnLogout.Text = "Đăng xuất";
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            //
            // ─── BODY ───────────────────────────────────────────
            //
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.BackColor = AppTheme.Background;
            this.pnlBody.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceXl);
            this.pnlBody.Controls.Add(this.pnlListContainer);
            this.pnlBody.Controls.Add(this.pnlFilter);
            this.pnlBody.Controls.Add(this.pnlActionBar);
            this.pnlBody.Name = "pnlBody";
            //
            // ─── ACTION BAR (Title + CTA) ───────────────────────
            //
            this.pnlActionBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlActionBar.BackColor = AppTheme.Background;
            this.pnlActionBar.Height = 168;
            this.pnlActionBar.Controls.Add(this.lblPageTitle);
            this.pnlActionBar.Controls.Add(this.lblPageSubtitle);
            this.pnlActionBar.Controls.Add(this.pnlJoin);
            this.pnlActionBar.Controls.Add(this.btnImport);
            this.pnlActionBar.Controls.Add(this.btnNew);
            this.pnlActionBar.Name = "pnlActionBar";
            //
            this.lblPageTitle.AutoSize = true;
            this.lblPageTitle.Font = AppTheme.H1;
            this.lblPageTitle.ForeColor = AppTheme.TextPrimary;
            this.lblPageTitle.Location = new System.Drawing.Point(0, 8);
            this.lblPageTitle.Name = "lblPageTitle";
            this.lblPageTitle.Text = "Tài liệu của bạn";
            //
            this.lblPageSubtitle.AutoSize = true;
            this.lblPageSubtitle.Font = AppTheme.Subtitle;
            this.lblPageSubtitle.ForeColor = AppTheme.TextSecondary;
            this.lblPageSubtitle.Location = new System.Drawing.Point(0, 56);
            this.lblPageSubtitle.Name = "lblPageSubtitle";
            this.lblPageSubtitle.Text = "Tạo, mở, chia sẻ và cộng tác trên các tài liệu Markdown.";
            //
            this.btnNew.Size = new System.Drawing.Size(160, AppTheme.ButtonHeight);
            this.btnNew.Text = "+ Tài liệu mới";
            this.btnNew.Name = "btnNew";
            this.btnNew.Click += new System.EventHandler(this.btnCreateDocument_Click);
            //
            this.btnImport.Size = new System.Drawing.Size(150, AppTheme.ButtonHeight);
            this.btnImport.Text = "Import .md";
            this.btnImport.Name = "btnImport";
            this.btnImport.Click += new System.EventHandler(this.btnImportMd_Click);
            //
            // Join code panel
            //
            this.pnlJoin.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.pnlJoin.Location = new System.Drawing.Point(0, 100);
            this.pnlJoin.Size = new System.Drawing.Size(640, AppTheme.InputHeight + 24);
            this.pnlJoin.Name = "pnlJoin";
            this.pnlJoin.BackColor = AppTheme.Surface;
            this.pnlJoin.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd);
            //
            this.txtJoinCode.Location = new System.Drawing.Point(AppTheme.SpaceLg, AppTheme.SpaceMd);
            this.txtJoinCode.Size = new System.Drawing.Size(360, AppTheme.InputHeight);
            this.txtJoinCode.Font = AppTheme.MonoLarge;
            this.txtJoinCode.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtJoinCode.BackColor = AppTheme.Surface;
            this.txtJoinCode.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtJoinCode.MaxLength = 16;
            this.txtJoinCode.Name = "txtJoinCode";
            this.pnlJoin.Controls.Add(this.txtJoinCode);
            //
            this.btnJoinCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnJoinCode.Location = new System.Drawing.Point(420, AppTheme.SpaceMd);
            this.btnJoinCode.Size = new System.Drawing.Size(180, AppTheme.ButtonHeight);
            this.btnJoinCode.Text = "Tham gia bằng mã";
            this.btnJoinCode.Name = "btnJoinCode";
            this.btnJoinCode.Click += new System.EventHandler(this.btnJoinCode_Click);
            this.pnlJoin.Controls.Add(this.btnJoinCode);
            //
            // ─── FILTER (Sort + count) ──────────────────────────
            //
            this.pnlFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilter.BackColor = AppTheme.Background;
            this.pnlFilter.Height = 60;
            this.pnlFilter.Controls.Add(this.lblFilter);
            this.pnlFilter.Controls.Add(this.cmbSortMode);
            this.pnlFilter.Controls.Add(this.lblCount);
            this.pnlFilter.Name = "pnlFilter";
            //
            this.lblFilter.AutoSize = true;
            this.lblFilter.Font = AppTheme.BodyBold;
            this.lblFilter.ForeColor = AppTheme.TextPrimary;
            this.lblFilter.Location = new System.Drawing.Point(0, 18);
            this.lblFilter.Text = "Sắp xếp:";
            //
            this.cmbSortMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbSortMode.Font = AppTheme.Body;
            this.cmbSortMode.Items.AddRange(new object[] {
                "Mới nhất",
                "Cũ nhất",
                "A → Z",
                "Z → A"});
            this.cmbSortMode.Location = new System.Drawing.Point(76, 14);
            this.cmbSortMode.Size = new System.Drawing.Size(180, 28);
            this.cmbSortMode.Name = "cmbSortMode";
            this.cmbSortMode.SelectedIndexChanged += new System.EventHandler(this.cmbSortMode_SelectedIndexChanged);
            //
            this.lblCount.AutoSize = true;
            this.lblCount.Font = AppTheme.Caption;
            this.lblCount.ForeColor = AppTheme.TextSecondary;
            this.lblCount.Name = "lblCount";
            this.lblCount.Text = "0 tài liệu";
            //
            // ─── LIST CONTAINER (card) ──────────────────────────
            //
            this.pnlListContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlListContainer.BackColor = AppTheme.Surface;
            this.pnlListContainer.Padding = new System.Windows.Forms.Padding(0);
            this.pnlListContainer.Controls.Add(this.listDocuments);
            this.pnlListContainer.Name = "pnlListContainer";
            //
            this.listDocuments.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colTitle, this.colUpdatedAt, this.colPermission });
            this.listDocuments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listDocuments.View = System.Windows.Forms.View.Details;
            this.listDocuments.FullRowSelect = true;
            this.listDocuments.HideSelection = false;
            this.listDocuments.MultiSelect = false;
            this.listDocuments.OwnerDraw = true;
            this.listDocuments.Name = "listDocuments";
            this.listDocuments.UseCompatibleStateImageBehavior = false;
            this.listDocuments.ItemActivate += new System.EventHandler(this.listDocuments_ItemActivate);
            //
            this.colTitle.Text = "Tiêu đề"; this.colTitle.Width = 480;
            this.colUpdatedAt.Text = "Cập nhật"; this.colUpdatedAt.Width = 220;
            this.colPermission.Text = "Quyền"; this.colPermission.Width = 160;
            //
            // HomeForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(1200, 760);
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlHeader);
            this.MinimumSize = new System.Drawing.Size(960, 600);
            this.Name = "HomeForm";
            this.Text = "MarkTogether";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlBody.ResumeLayout(false);
            this.pnlActionBar.ResumeLayout(false);
            this.pnlActionBar.PerformLayout();
            this.pnlJoin.ResumeLayout(false);
            this.pnlFilter.ResumeLayout(false);
            this.pnlFilter.PerformLayout();
            this.pnlListContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Button btnLogout;

        private System.Windows.Forms.Panel pnlBody;

        private System.Windows.Forms.Panel pnlActionBar;
        private System.Windows.Forms.Label lblPageTitle;
        private System.Windows.Forms.Label lblPageSubtitle;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Panel pnlJoin;
        private System.Windows.Forms.TextBox txtJoinCode;
        private System.Windows.Forms.Button btnJoinCode;

        private System.Windows.Forms.Panel pnlFilter;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.ComboBox cmbSortMode;
        private System.Windows.Forms.Label lblCount;

        private System.Windows.Forms.Panel pnlListContainer;
        private System.Windows.Forms.ListView listDocuments;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colUpdatedAt;
        private System.Windows.Forms.ColumnHeader colPermission;
    }
}