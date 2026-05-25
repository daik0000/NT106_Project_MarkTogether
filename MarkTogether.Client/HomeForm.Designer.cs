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
            this.pnlListContainer = new System.Windows.Forms.Panel();
            this.listDocuments = new System.Windows.Forms.ListView();
            this.colTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUpdatedAt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPermission = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pnlFilter = new System.Windows.Forms.Panel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.cmbSortMode = new System.Windows.Forms.ComboBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.pnlActionBar = new System.Windows.Forms.Panel();
            this.lblPageTitle = new System.Windows.Forms.Label();
            this.lblPageSubtitle = new System.Windows.Forms.Label();
            this.pnlJoin = new System.Windows.Forms.Panel();
            this.txtJoinCode = new System.Windows.Forms.TextBox();
            this.btnJoinCode = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.pnlHeader.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlListContainer.SuspendLayout();
            this.pnlFilter.SuspendLayout();
            this.pnlActionBar.SuspendLayout();
            this.pnlJoin.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.lblBrand);
            this.pnlHeader.Controls.Add(this.lblWelcome);
            this.pnlHeader.Controls.Add(this.btnLogout);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.pnlHeader.Size = new System.Drawing.Size(1500, 80);
            this.pnlHeader.TabIndex = 1;
            // 
            // lblBrand
            // 
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblBrand.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.lblBrand.Location = new System.Drawing.Point(30, 22);
            this.lblBrand.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new System.Drawing.Size(200, 37);
            this.lblBrand.TabIndex = 0;
            this.lblBrand.Text = "MarkTogether";
            // 
            // lblWelcome
            // 
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblWelcome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblWelcome.Location = new System.Drawing.Point(0, 0);
            this.lblWelcome.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(117, 23);
            this.lblWelcome.TabIndex = 1;
            this.lblWelcome.Text = "Xin chào, user";
            // 
            // btnLogout
            // 
            this.btnLogout.Location = new System.Drawing.Point(0, 0);
            this.btnLogout.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(138, 45);
            this.btnLogout.TabIndex = 2;
            this.btnLogout.Text = "Đăng xuất";
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // pnlBody
            // 
            this.pnlBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlBody.Controls.Add(this.pnlListContainer);
            this.pnlBody.Controls.Add(this.pnlFilter);
            this.pnlBody.Controls.Add(this.pnlActionBar);
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.Location = new System.Drawing.Point(0, 80);
            this.pnlBody.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlBody.Name = "pnlBody";
            this.pnlBody.Padding = new System.Windows.Forms.Padding(30, 30, 30, 30);
            this.pnlBody.Size = new System.Drawing.Size(1500, 870);
            this.pnlBody.TabIndex = 0;
            // 
            // pnlListContainer
            // 
            this.pnlListContainer.BackColor = System.Drawing.Color.White;
            this.pnlListContainer.Controls.Add(this.listDocuments);
            this.pnlListContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlListContainer.Location = new System.Drawing.Point(30, 290);
            this.pnlListContainer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlListContainer.Name = "pnlListContainer";
            this.pnlListContainer.Size = new System.Drawing.Size(1440, 550);
            this.pnlListContainer.TabIndex = 0;
            // 
            // listDocuments
            // 
            this.listDocuments.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTitle,
            this.colUpdatedAt,
            this.colPermission});
            this.listDocuments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listDocuments.FullRowSelect = true;
            this.listDocuments.HideSelection = false;
            this.listDocuments.Location = new System.Drawing.Point(0, 0);
            this.listDocuments.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listDocuments.MultiSelect = false;
            this.listDocuments.Name = "listDocuments";
            this.listDocuments.OwnerDraw = true;
            this.listDocuments.Size = new System.Drawing.Size(1440, 550);
            this.listDocuments.TabIndex = 0;
            this.listDocuments.UseCompatibleStateImageBehavior = false;
            this.listDocuments.View = System.Windows.Forms.View.Details;
            this.listDocuments.ItemActivate += new System.EventHandler(this.listDocuments_ItemActivate);
            // 
            // colTitle
            // 
            this.colTitle.Text = "Tiêu đề";
            this.colTitle.Width = 620;
            // 
            // colUpdatedAt
            // 
            this.colUpdatedAt.Text = "Cập nhật";
            this.colUpdatedAt.Width = 260;
            // 
            // colPermission
            // 
            this.colPermission.Text = "Quyền";
            this.colPermission.Width = 180;
            // 
            // pnlFilter
            // 
            this.pnlFilter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlFilter.Controls.Add(this.lblFilter);
            this.pnlFilter.Controls.Add(this.cmbSortMode);
            this.pnlFilter.Controls.Add(this.lblCount);
            this.pnlFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilter.Location = new System.Drawing.Point(30, 215);
            this.pnlFilter.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlFilter.Name = "pnlFilter";
            this.pnlFilter.Size = new System.Drawing.Size(1440, 75);
            this.pnlFilter.TabIndex = 1;
            // 
            // lblFilter
            // 
            this.lblFilter.AutoSize = true;
            this.lblFilter.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblFilter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblFilter.Location = new System.Drawing.Point(0, 22);
            this.lblFilter.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(79, 23);
            this.lblFilter.TabIndex = 0;
            this.lblFilter.Text = "Sắp xếp:";
            // 
            // cmbSortMode
            // 
            this.cmbSortMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbSortMode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbSortMode.Items.AddRange(new object[] {
            "Mới nhất",
            "Cũ nhất",
            "A → Z",
            "Z → A"});
            this.cmbSortMode.Location = new System.Drawing.Point(95, 18);
            this.cmbSortMode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbSortMode.Name = "cmbSortMode";
            this.cmbSortMode.Size = new System.Drawing.Size(224, 31);
            this.cmbSortMode.TabIndex = 1;
            this.cmbSortMode.SelectedIndexChanged += new System.EventHandler(this.cmbSortMode_SelectedIndexChanged);
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = true;
            this.lblCount.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblCount.Location = new System.Drawing.Point(0, 0);
            this.lblCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(66, 20);
            this.lblCount.TabIndex = 2;
            this.lblCount.Text = "0 tài liệu";
            // 
            // pnlActionBar
            // 
            this.pnlActionBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.pnlActionBar.Controls.Add(this.lblPageTitle);
            this.pnlActionBar.Controls.Add(this.lblPageSubtitle);
            this.pnlActionBar.Controls.Add(this.pnlJoin);
            this.pnlActionBar.Controls.Add(this.btnImport);
            this.pnlActionBar.Controls.Add(this.btnNew);
            this.pnlActionBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlActionBar.Location = new System.Drawing.Point(30, 30);
            this.pnlActionBar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlActionBar.Name = "pnlActionBar";
            this.pnlActionBar.Size = new System.Drawing.Size(1440, 185);
            this.pnlActionBar.TabIndex = 2;
            // 
            // lblPageTitle
            // 
            this.lblPageTitle.AutoSize = true;
            this.lblPageTitle.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblPageTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblPageTitle.Location = new System.Drawing.Point(0, 10);
            this.lblPageTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPageTitle.Name = "lblPageTitle";
            this.lblPageTitle.Size = new System.Drawing.Size(291, 50);
            this.lblPageTitle.TabIndex = 0;
            this.lblPageTitle.Text = "Tài liệu của bạn";
            // 
            // lblPageSubtitle
            // 
            this.lblPageSubtitle.AutoSize = true;
            this.lblPageSubtitle.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblPageSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblPageSubtitle.Location = new System.Drawing.Point(0, 70);
            this.lblPageSubtitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPageSubtitle.Name = "lblPageSubtitle";
            this.lblPageSubtitle.Size = new System.Drawing.Size(480, 25);
            this.lblPageSubtitle.TabIndex = 1;
            this.lblPageSubtitle.Text = "Tạo, mở, chia sẻ và cộng tác trên các tài liệu Markdown.";
            // 
            // pnlJoin
            // 
            this.pnlJoin.BackColor = System.Drawing.Color.White;
            this.pnlJoin.Controls.Add(this.txtJoinCode);
            this.pnlJoin.Controls.Add(this.btnJoinCode);
            this.pnlJoin.Location = new System.Drawing.Point(0, 110);
            this.pnlJoin.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlJoin.Name = "pnlJoin";
            this.pnlJoin.Padding = new System.Windows.Forms.Padding(15, 15, 15, 15);
            this.pnlJoin.Size = new System.Drawing.Size(673, 48);
            this.pnlJoin.TabIndex = 2;
            // 
            // txtJoinCode
            // 
            this.txtJoinCode.BackColor = System.Drawing.Color.White;
            this.txtJoinCode.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtJoinCode.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtJoinCode.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this.txtJoinCode.Location = new System.Drawing.Point(20, 15);
            this.txtJoinCode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtJoinCode.MaxLength = 16;
            this.txtJoinCode.Name = "txtJoinCode";
            this.txtJoinCode.Size = new System.Drawing.Size(299, 24);
            this.txtJoinCode.TabIndex = 0;
            // 
            // btnJoinCode
            // 
            this.btnJoinCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnJoinCode.Location = new System.Drawing.Point(398, 0);
            this.btnJoinCode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnJoinCode.Name = "btnJoinCode";
            this.btnJoinCode.Size = new System.Drawing.Size(225, 50);
            this.btnJoinCode.TabIndex = 1;
            this.btnJoinCode.Text = "Tham gia bằng mã";
            this.btnJoinCode.Click += new System.EventHandler(this.btnJoinCode_Click);
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(0, 0);
            this.btnImport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(188, 50);
            this.btnImport.TabIndex = 3;
            this.btnImport.Text = "Import .md";
            this.btnImport.Click += new System.EventHandler(this.btnImportMd_Click);
            // 
            // btnNew
            // 
            this.btnNew.Location = new System.Drawing.Point(0, 0);
            this.btnNew.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(200, 50);
            this.btnNew.TabIndex = 4;
            this.btnNew.Text = "+ Tài liệu mới";
            this.btnNew.Click += new System.EventHandler(this.btnCreateDocument_Click);
            // 
            // HomeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(1500, 950);
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlHeader);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(1196, 738);
            this.Name = "HomeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarkTogether";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlBody.ResumeLayout(false);
            this.pnlListContainer.ResumeLayout(false);
            this.pnlFilter.ResumeLayout(false);
            this.pnlFilter.PerformLayout();
            this.pnlActionBar.ResumeLayout(false);
            this.pnlActionBar.PerformLayout();
            this.pnlJoin.ResumeLayout(false);
            this.pnlJoin.PerformLayout();
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