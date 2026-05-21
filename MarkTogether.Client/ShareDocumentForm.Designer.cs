using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class ShareDocumentForm
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
            this.pnlShareCode = new System.Windows.Forms.Panel();
            this.lblShareCodeTitle = new System.Windows.Forms.Label();
            this.lblShareCode = new System.Windows.Forms.Label();
            this.txtShareCode = new System.Windows.Forms.TextBox();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnRegen = new System.Windows.Forms.Button();
            this.grpInvite = new System.Windows.Forms.GroupBox();
            this.cmbPermission = new System.Windows.Forms.ComboBox();
            this.lblPerm = new System.Windows.Forms.Label();
            this.btnInvite = new System.Windows.Forms.Button();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.grpCollab = new System.Windows.Forms.GroupBox();
            this.btnRevoke = new System.Windows.Forms.Button();
            this.btnUpdatePerm = new System.Windows.Forms.Button();
            this.listCollab = new System.Windows.Forms.ListView();
            this.colUser = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colEmail = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPerm = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colInvited = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnClose = new System.Windows.Forms.Button();
            this.grpInvite.SuspendLayout();
            this.grpCollab.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlShareCode — card chứa mã chia sẻ
            // 
            this.pnlShareCode = new System.Windows.Forms.Panel();
            this.pnlShareCode.BackColor = AppTheme.PrimaryLight;
            this.pnlShareCode.Location = new System.Drawing.Point(20, 20);
            this.pnlShareCode.Size = new System.Drawing.Size(570, 70);
            this.pnlShareCode.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg);
            this.pnlShareCode.Name = "pnlShareCode";
            // 
            // lblShareCodeTitle
            // 
            this.lblShareCodeTitle = new System.Windows.Forms.Label();
            this.lblShareCodeTitle.AutoSize = true;
            this.lblShareCodeTitle.Font = AppTheme.CaptionBold;
            this.lblShareCodeTitle.ForeColor = AppTheme.Primary;
            this.lblShareCodeTitle.Location = new System.Drawing.Point(AppTheme.SpaceLg, 8);
            this.lblShareCodeTitle.Text = "MÃ CHIA SẺ";
            this.pnlShareCode.Controls.Add(this.lblShareCodeTitle);
            // 
            // lblShareCode
            // 
            this.lblShareCode.AutoSize = true;
            this.lblShareCode.Font = AppTheme.H3;
            this.lblShareCode.ForeColor = AppTheme.TextPrimary;
            this.lblShareCode.Location = new System.Drawing.Point(AppTheme.SpaceLg, 10);
            this.lblShareCode.Name = "lblShareCode";
            this.lblShareCode.Size = new System.Drawing.Size(116, 20);
            this.lblShareCode.Text = "Mã chia sẻ:";
            this.lblShareCode.Visible = false;
            // 
            // txtShareCode
            // 
            this.txtShareCode.Font = AppTheme.MonoLarge;
            this.txtShareCode.ForeColor = AppTheme.TextPrimary;
            this.txtShareCode.BackColor = AppTheme.Surface;
            this.txtShareCode.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtShareCode.Location = new System.Drawing.Point(AppTheme.SpaceLg, 32);
            this.txtShareCode.Name = "txtShareCode";
            this.txtShareCode.ReadOnly = true;
            this.txtShareCode.Size = new System.Drawing.Size(220, 28);
            this.pnlShareCode.Controls.Add(this.txtShareCode);
            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(370, 28);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(90, AppTheme.ButtonHeightSmall);
            this.btnCopy.Text = "📋 Copy";
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            this.pnlShareCode.Controls.Add(this.btnCopy);
            // 
            // btnRegen
            // 
            this.btnRegen.Location = new System.Drawing.Point(468, 28);
            this.btnRegen.Name = "btnRegen";
            this.btnRegen.Size = new System.Drawing.Size(90, AppTheme.ButtonHeightSmall);
            this.btnRegen.Text = "🔄 Tạo lại";
            this.btnRegen.Click += new System.EventHandler(this.btnRegen_Click);
            this.pnlShareCode.Controls.Add(this.btnRegen);
            // 
            // grpInvite
            // 
            this.grpInvite.Controls.Add(this.cmbPermission);
            this.grpInvite.Controls.Add(this.lblPerm);
            this.grpInvite.Controls.Add(this.btnInvite);
            this.grpInvite.Controls.Add(this.txtUsername);
            this.grpInvite.Controls.Add(this.lblUsername);
            this.grpInvite.Font = AppTheme.BodyBold;
            this.grpInvite.ForeColor = AppTheme.TextPrimary;
            this.grpInvite.BackColor = AppTheme.Surface;
            this.grpInvite.Location = new System.Drawing.Point(20, 100);
            this.grpInvite.Name = "grpInvite";
            this.grpInvite.Size = new System.Drawing.Size(570, 90);
            this.grpInvite.TabStop = false;
            this.grpInvite.Text = "Mời theo username";
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = AppTheme.Body;
            this.lblUsername.ForeColor = AppTheme.TextSecondary;
            this.lblUsername.Location = new System.Drawing.Point(15, 38);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            this.txtUsername.Font = AppTheme.BodyLarge;
            this.txtUsername.ForeColor = AppTheme.TextPrimary;
            this.txtUsername.BackColor = AppTheme.Surface;
            this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUsername.Location = new System.Drawing.Point(95, 34);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(180, 27);
            // 
            // lblPerm
            // 
            this.lblPerm.AutoSize = true;
            this.lblPerm.Font = AppTheme.Body;
            this.lblPerm.ForeColor = AppTheme.TextSecondary;
            this.lblPerm.Location = new System.Drawing.Point(285, 38);
            this.lblPerm.Name = "lblPerm";
            this.lblPerm.Text = "Quyền:";
            // 
            // cmbPermission
            // 
            this.cmbPermission.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPermission.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbPermission.Font = AppTheme.Body;
            this.cmbPermission.ForeColor = AppTheme.TextPrimary;
            this.cmbPermission.BackColor = AppTheme.Surface;
            this.cmbPermission.Items.AddRange(new object[] { "viewer", "editor" });
            this.cmbPermission.Location = new System.Drawing.Point(340, 34);
            this.cmbPermission.Name = "cmbPermission";
            this.cmbPermission.Size = new System.Drawing.Size(120, 27);
            // 
            // btnInvite
            // 
            this.btnInvite.Location = new System.Drawing.Point(470, 32);
            this.btnInvite.Name = "btnInvite";
            this.btnInvite.Size = new System.Drawing.Size(90, AppTheme.ButtonHeightSmall);
            this.btnInvite.Text = "Mời";
            this.btnInvite.Click += new System.EventHandler(this.btnInvite_Click);
            // 
            // grpCollab
            // 
            this.grpCollab.Controls.Add(this.btnRevoke);
            this.grpCollab.Controls.Add(this.btnUpdatePerm);
            this.grpCollab.Controls.Add(this.listCollab);
            this.grpCollab.Font = AppTheme.BodyBold;
            this.grpCollab.ForeColor = AppTheme.TextPrimary;
            this.grpCollab.BackColor = AppTheme.Surface;
            this.grpCollab.Location = new System.Drawing.Point(20, 200);
            this.grpCollab.Name = "grpCollab";
            this.grpCollab.Size = new System.Drawing.Size(570, 280);
            this.grpCollab.TabStop = false;
            this.grpCollab.Text = "Người cộng tác";
            // 
            // listCollab
            // 
            this.listCollab.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colUser, this.colEmail, this.colPerm, this.colInvited });
            this.listCollab.FullRowSelect = true;
            this.listCollab.HideSelection = false;
            this.listCollab.Location = new System.Drawing.Point(10, 30);
            this.listCollab.MultiSelect = false;
            this.listCollab.Name = "listCollab";
            this.listCollab.Size = new System.Drawing.Size(550, 195);
            this.listCollab.UseCompatibleStateImageBehavior = false;
            this.listCollab.View = System.Windows.Forms.View.Details;
            // 
            this.colUser.Text = "Username"; this.colUser.Width = 130;
            this.colEmail.Text = "Email"; this.colEmail.Width = 170;
            this.colPerm.Text = "Quyền"; this.colPerm.Width = 100;
            this.colInvited.Text = "Mời lúc"; this.colInvited.Width = 140;
            // 
            // btnUpdatePerm
            // 
            this.btnUpdatePerm.Location = new System.Drawing.Point(10, 235);
            this.btnUpdatePerm.Name = "btnUpdatePerm";
            this.btnUpdatePerm.Size = new System.Drawing.Size(140, AppTheme.ButtonHeightSmall);
            this.btnUpdatePerm.Text = "Đổi quyền...";
            this.btnUpdatePerm.Click += new System.EventHandler(this.btnUpdatePerm_Click);
            // 
            // btnRevoke
            // 
            this.btnRevoke.Location = new System.Drawing.Point(160, 235);
            this.btnRevoke.Name = "btnRevoke";
            this.btnRevoke.Size = new System.Drawing.Size(140, AppTheme.ButtonHeightSmall);
            this.btnRevoke.Text = "Thu hồi";
            this.btnRevoke.Click += new System.EventHandler(this.btnRevoke_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(490, 492);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, AppTheme.ButtonHeightSmall);
            this.btnClose.Text = "Đóng";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ShareDocumentForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(610, 540);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.grpCollab);
            this.Controls.Add(this.grpInvite);
            this.Controls.Add(this.pnlShareCode);
            this.Controls.Add(this.lblShareCode);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShareDocumentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Chia sẻ tài liệu";
            this.grpInvite.ResumeLayout(false);
            this.grpInvite.PerformLayout();
            this.grpCollab.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Panel pnlShareCode;
        private System.Windows.Forms.Label lblShareCodeTitle;
        private System.Windows.Forms.Label lblShareCode;
        private System.Windows.Forms.TextBox txtShareCode;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnRegen;
        private System.Windows.Forms.GroupBox grpInvite;
        private System.Windows.Forms.ComboBox cmbPermission;
        private System.Windows.Forms.Label lblPerm;
        private System.Windows.Forms.Button btnInvite;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.GroupBox grpCollab;
        private System.Windows.Forms.Button btnRevoke;
        private System.Windows.Forms.Button btnUpdatePerm;
        private System.Windows.Forms.ListView listCollab;
        private System.Windows.Forms.ColumnHeader colUser;
        private System.Windows.Forms.ColumnHeader colEmail;
        private System.Windows.Forms.ColumnHeader colPerm;
        private System.Windows.Forms.ColumnHeader colInvited;
        private System.Windows.Forms.Button btnClose;
    }
}