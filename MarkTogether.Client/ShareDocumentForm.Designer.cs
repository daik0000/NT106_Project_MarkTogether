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
            this.pnlShareCode = new System.Windows.Forms.Panel();
            this.lblShareCodeTitle = new System.Windows.Forms.Label();
            this.grpInvite.SuspendLayout();
            this.grpCollab.SuspendLayout();
            this.pnlShareCode.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtShareCode
            // 
            this.txtShareCode.BackColor = System.Drawing.Color.White;
            this.txtShareCode.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtShareCode.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this.txtShareCode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.txtShareCode.Location = new System.Drawing.Point(20, 40);
            this.txtShareCode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtShareCode.Name = "txtShareCode";
            this.txtShareCode.ReadOnly = true;
            this.txtShareCode.Size = new System.Drawing.Size(275, 24);
            this.txtShareCode.TabIndex = 1;
            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(462, 35);
            this.btnCopy.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(112, 40);
            this.btnCopy.TabIndex = 2;
            this.btnCopy.Text = "📋 Copy";
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // btnRegen
            // 
            this.btnRegen.Location = new System.Drawing.Point(585, 35);
            this.btnRegen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRegen.Name = "btnRegen";
            this.btnRegen.Size = new System.Drawing.Size(112, 40);
            this.btnRegen.TabIndex = 3;
            this.btnRegen.Text = "🔄 Tạo lại";
            this.btnRegen.Click += new System.EventHandler(this.btnRegen_Click);
            // 
            // grpInvite
            // 
            this.grpInvite.BackColor = System.Drawing.Color.White;
            this.grpInvite.Controls.Add(this.cmbPermission);
            this.grpInvite.Controls.Add(this.lblPerm);
            this.grpInvite.Controls.Add(this.btnInvite);
            this.grpInvite.Controls.Add(this.txtUsername);
            this.grpInvite.Controls.Add(this.lblUsername);
            this.grpInvite.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpInvite.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.grpInvite.Location = new System.Drawing.Point(25, 125);
            this.grpInvite.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grpInvite.Name = "grpInvite";
            this.grpInvite.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grpInvite.Size = new System.Drawing.Size(712, 112);
            this.grpInvite.TabIndex = 2;
            this.grpInvite.TabStop = false;
            this.grpInvite.Text = "Mời theo username";
            // 
            // cmbPermission
            // 
            this.cmbPermission.BackColor = System.Drawing.Color.White;
            this.cmbPermission.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPermission.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbPermission.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbPermission.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.cmbPermission.Items.AddRange(new object[] {
            "viewer",
            "editor"});
            this.cmbPermission.Location = new System.Drawing.Point(425, 42);
            this.cmbPermission.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbPermission.Name = "cmbPermission";
            this.cmbPermission.Size = new System.Drawing.Size(149, 31);
            this.cmbPermission.TabIndex = 0;
            // 
            // lblPerm
            // 
            this.lblPerm.AutoSize = true;
            this.lblPerm.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPerm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblPerm.Location = new System.Drawing.Point(356, 48);
            this.lblPerm.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPerm.Name = "lblPerm";
            this.lblPerm.Size = new System.Drawing.Size(64, 23);
            this.lblPerm.TabIndex = 1;
            this.lblPerm.Text = "Quyền:";
            // 
            // btnInvite
            // 
            this.btnInvite.Location = new System.Drawing.Point(588, 40);
            this.btnInvite.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnInvite.Name = "btnInvite";
            this.btnInvite.Size = new System.Drawing.Size(112, 40);
            this.btnInvite.TabIndex = 2;
            this.btnInvite.Text = "Mời";
            this.btnInvite.Click += new System.EventHandler(this.btnInvite_Click);
            // 
            // txtUsername
            // 
            this.txtUsername.BackColor = System.Drawing.Color.White;
            this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUsername.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.txtUsername.Location = new System.Drawing.Point(119, 42);
            this.txtUsername.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(224, 32);
            this.txtUsername.TabIndex = 3;
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblUsername.Location = new System.Drawing.Point(19, 48);
            this.lblUsername.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(91, 23);
            this.lblUsername.TabIndex = 4;
            this.lblUsername.Text = "Username:";
            // 
            // grpCollab
            // 
            this.grpCollab.BackColor = System.Drawing.Color.White;
            this.grpCollab.Controls.Add(this.btnRevoke);
            this.grpCollab.Controls.Add(this.btnUpdatePerm);
            this.grpCollab.Controls.Add(this.listCollab);
            this.grpCollab.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpCollab.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.grpCollab.Location = new System.Drawing.Point(25, 250);
            this.grpCollab.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grpCollab.Name = "grpCollab";
            this.grpCollab.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grpCollab.Size = new System.Drawing.Size(712, 350);
            this.grpCollab.TabIndex = 1;
            this.grpCollab.TabStop = false;
            this.grpCollab.Text = "Người cộng tác";
            // 
            // btnRevoke
            // 
            this.btnRevoke.Location = new System.Drawing.Point(200, 294);
            this.btnRevoke.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRevoke.Name = "btnRevoke";
            this.btnRevoke.Size = new System.Drawing.Size(175, 40);
            this.btnRevoke.TabIndex = 0;
            this.btnRevoke.Text = "Thu hồi";
            this.btnRevoke.Click += new System.EventHandler(this.btnRevoke_Click);
            // 
            // btnUpdatePerm
            // 
            this.btnUpdatePerm.Location = new System.Drawing.Point(12, 294);
            this.btnUpdatePerm.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUpdatePerm.Name = "btnUpdatePerm";
            this.btnUpdatePerm.Size = new System.Drawing.Size(175, 40);
            this.btnUpdatePerm.TabIndex = 1;
            this.btnUpdatePerm.Text = "Đổi quyền...";
            this.btnUpdatePerm.Click += new System.EventHandler(this.btnUpdatePerm_Click);
            // 
            // listCollab
            // 
            this.listCollab.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colUser,
            this.colEmail,
            this.colPerm,
            this.colInvited});
            this.listCollab.FullRowSelect = true;
            this.listCollab.HideSelection = false;
            this.listCollab.Location = new System.Drawing.Point(12, 38);
            this.listCollab.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listCollab.MultiSelect = false;
            this.listCollab.Name = "listCollab";
            this.listCollab.Size = new System.Drawing.Size(686, 243);
            this.listCollab.TabIndex = 2;
            this.listCollab.UseCompatibleStateImageBehavior = false;
            this.listCollab.View = System.Windows.Forms.View.Details;
            // 
            // colUser
            // 
            this.colUser.Text = "Username";
            this.colUser.Width = 130;
            // 
            // colEmail
            // 
            this.colEmail.Text = "Email";
            this.colEmail.Width = 170;
            // 
            // colPerm
            // 
            this.colPerm.Text = "Quyền";
            this.colPerm.Width = 100;
            // 
            // colInvited
            // 
            this.colInvited.Text = "Mời lúc";
            this.colInvited.Width = 140;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(612, 615);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(125, 40);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Đóng";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // pnlShareCode
            // 
            this.pnlShareCode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(234)))), ((int)(((byte)(254)))));
            this.pnlShareCode.Controls.Add(this.lblShareCodeTitle);
            this.pnlShareCode.Controls.Add(this.txtShareCode);
            this.pnlShareCode.Controls.Add(this.btnCopy);
            this.pnlShareCode.Controls.Add(this.btnRegen);
            this.pnlShareCode.Location = new System.Drawing.Point(25, 25);
            this.pnlShareCode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlShareCode.Name = "pnlShareCode";
            this.pnlShareCode.Padding = new System.Windows.Forms.Padding(20, 20, 20, 20);
            this.pnlShareCode.Size = new System.Drawing.Size(712, 88);
            this.pnlShareCode.TabIndex = 3;
            // 
            // lblShareCodeTitle
            // 
            this.lblShareCodeTitle.AutoSize = true;
            this.lblShareCodeTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblShareCodeTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.lblShareCodeTitle.Location = new System.Drawing.Point(20, 10);
            this.lblShareCodeTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblShareCodeTitle.Name = "lblShareCodeTitle";
            this.lblShareCodeTitle.Size = new System.Drawing.Size(94, 20);
            this.lblShareCodeTitle.TabIndex = 0;
            this.lblShareCodeTitle.Text = "MÃ CHIA SẺ";
            // 
            // ShareDocumentForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(762, 675);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.grpCollab);
            this.Controls.Add(this.grpInvite);
            this.Controls.Add(this.pnlShareCode);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimizeBox = false;
            this.Name = "ShareDocumentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Chia sẻ tài liệu";
            this.grpInvite.ResumeLayout(false);
            this.grpInvite.PerformLayout();
            this.grpCollab.ResumeLayout(false);
            this.pnlShareCode.ResumeLayout(false);
            this.pnlShareCode.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Panel pnlShareCode;
        private System.Windows.Forms.Label lblShareCodeTitle;
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