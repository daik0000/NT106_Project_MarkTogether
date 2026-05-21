namespace MarkTogether.Client
{
    partial class ShareManagementForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblShareCodeHeader = new System.Windows.Forms.Label();
            this.lblShareCode = new System.Windows.Forms.Label();
            this.btnCopyCode = new System.Windows.Forms.Button();
            this.grpPublic = new System.Windows.Forms.GroupBox();
            this.btnSavePublic = new System.Windows.Forms.Button();
            this.rdoPublicEditor = new System.Windows.Forms.RadioButton();
            this.rdoPublicViewer = new System.Windows.Forms.RadioButton();
            this.lblPublicPerm = new System.Windows.Forms.Label();
            this.chkPublic = new System.Windows.Forms.CheckBox();
            this.grpNewShare = new System.Windows.Forms.GroupBox();
            this.btnShare = new System.Windows.Forms.Button();
            this.cmbNewPerm = new System.Windows.Forms.ComboBox();
            this.lblNewPerm = new System.Windows.Forms.Label();
            this.txtNewUsername = new System.Windows.Forms.TextBox();
            this.lblNewUser = new System.Windows.Forms.Label();
            this.lblCollabs = new System.Windows.Forms.Label();
            this.dgvCollabs = new System.Windows.Forms.DataGridView();
            this.colUsername = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEmail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPermission = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colAction = new System.Windows.Forms.DataGridViewButtonColumn();
            this.btnClose = new System.Windows.Forms.Button();
            this.grpPublic.SuspendLayout();
            this.grpNewShare.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCollabs)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.DodgerBlue;
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(200, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Quản lý chia sẻ";
            // 
            // lblShareCodeHeader
            // 
            this.lblShareCodeHeader.AutoSize = true;
            this.lblShareCodeHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblShareCodeHeader.Location = new System.Drawing.Point(21, 65);
            this.lblShareCodeHeader.Name = "lblShareCodeHeader";
            this.lblShareCodeHeader.Size = new System.Drawing.Size(117, 25);
            this.lblShareCodeHeader.TabIndex = 1;
            this.lblShareCodeHeader.Text = "Share Code:";
            // 
            // lblShareCode
            // 
            this.lblShareCode.AutoSize = true;
            this.lblShareCode.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Bold);
            this.lblShareCode.ForeColor = System.Drawing.Color.DodgerBlue;
            this.lblShareCode.Location = new System.Drawing.Point(144, 63);
            this.lblShareCode.Name = "lblShareCode";
            this.lblShareCode.Size = new System.Drawing.Size(116, 28);
            this.lblShareCode.TabIndex = 2;
            this.lblShareCode.Text = "ABCD1234";
            // 
            // btnCopyCode
            // 
            this.btnCopyCode.Location = new System.Drawing.Point(270, 62);
            this.btnCopyCode.Name = "btnCopyCode";
            this.btnCopyCode.Size = new System.Drawing.Size(75, 30);
            this.btnCopyCode.TabIndex = 3;
            this.btnCopyCode.Text = "Copy";
            this.btnCopyCode.UseVisualStyleBackColor = true;
            this.btnCopyCode.Click += new System.EventHandler(this.btnCopyCode_Click);
            // 
            // grpPublic
            // 
            this.grpPublic.Controls.Add(this.btnSavePublic);
            this.grpPublic.Controls.Add(this.rdoPublicEditor);
            this.grpPublic.Controls.Add(this.rdoPublicViewer);
            this.grpPublic.Controls.Add(this.lblPublicPerm);
            this.grpPublic.Controls.Add(this.chkPublic);
            this.grpPublic.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.grpPublic.Location = new System.Drawing.Point(26, 110);
            this.grpPublic.Name = "grpPublic";
            this.grpPublic.Size = new System.Drawing.Size(648, 100);
            this.grpPublic.TabIndex = 4;
            this.grpPublic.TabStop = false;
            this.grpPublic.Text = "Share with Everyone";
            // 
            // btnSavePublic
            // 
            this.btnSavePublic.Location = new System.Drawing.Point(500, 55);
            this.btnSavePublic.Name = "btnSavePublic";
            this.btnSavePublic.Size = new System.Drawing.Size(130, 35);
            this.btnSavePublic.TabIndex = 4;
            this.btnSavePublic.Text = "Lưu cài đặt";
            this.btnSavePublic.UseVisualStyleBackColor = true;
            this.btnSavePublic.Click += new System.EventHandler(this.btnSavePublic_Click);
            // 
            // rdoPublicEditor
            // 
            this.rdoPublicEditor.AutoSize = true;
            this.rdoPublicEditor.Location = new System.Drawing.Point(230, 60);
            this.rdoPublicEditor.Name = "rdoPublicEditor";
            this.rdoPublicEditor.Size = new System.Drawing.Size(76, 27);
            this.rdoPublicEditor.TabIndex = 3;
            this.rdoPublicEditor.Text = "Editor";
            this.rdoPublicEditor.UseVisualStyleBackColor = true;
            // 
            // rdoPublicViewer
            // 
            this.rdoPublicViewer.AutoSize = true;
            this.rdoPublicViewer.Checked = true;
            this.rdoPublicViewer.Location = new System.Drawing.Point(140, 60);
            this.rdoPublicViewer.Name = "rdoPublicViewer";
            this.rdoPublicViewer.Size = new System.Drawing.Size(82, 27);
            this.rdoPublicViewer.TabIndex = 2;
            this.rdoPublicViewer.TabStop = true;
            this.rdoPublicViewer.Text = "Viewer";
            this.rdoPublicViewer.UseVisualStyleBackColor = true;
            // 
            // lblPublicPerm
            // 
            this.lblPublicPerm.AutoSize = true;
            this.lblPublicPerm.Location = new System.Drawing.Point(6, 62);
            this.lblPublicPerm.Name = "lblPublicPerm";
            this.lblPublicPerm.Size = new System.Drawing.Size(128, 23);
            this.lblPublicPerm.TabIndex = 1;
            this.lblPublicPerm.Text = "Quyền mặc định:";
            // 
            // chkPublic
            // 
            this.chkPublic.AutoSize = true;
            this.chkPublic.Location = new System.Drawing.Point(10, 30);
            this.chkPublic.Name = "chkPublic";
            this.chkPublic.Size = new System.Drawing.Size(225, 27);
            this.chkPublic.TabIndex = 0;
            this.chkPublic.Text = "Cho phép tham gia bằng mã";
            this.chkPublic.UseVisualStyleBackColor = true;
            this.chkPublic.CheckedChanged += new System.EventHandler(this.chkPublic_CheckedChanged);
            // 
            // grpNewShare
            // 
            this.grpNewShare.Controls.Add(this.btnShare);
            this.grpNewShare.Controls.Add(this.cmbNewPerm);
            this.grpNewShare.Controls.Add(this.lblNewPerm);
            this.grpNewShare.Controls.Add(this.txtNewUsername);
            this.grpNewShare.Controls.Add(this.lblNewUser);
            this.grpNewShare.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.grpNewShare.Location = new System.Drawing.Point(26, 220);
            this.grpNewShare.Name = "grpNewShare";
            this.grpNewShare.Size = new System.Drawing.Size(648, 100);
            this.grpNewShare.TabIndex = 5;
            this.grpNewShare.TabStop = false;
            this.grpNewShare.Text = "Chia sẻ với người dùng cụ thể";
            // 
            // btnShare
            // 
            this.btnShare.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnShare.ForeColor = System.Drawing.Color.White;
            this.btnShare.Location = new System.Drawing.Point(500, 45);
            this.btnShare.Name = "btnShare";
            this.btnShare.Size = new System.Drawing.Size(130, 35);
            this.btnShare.TabIndex = 4;
            this.btnShare.Text = "Chia sẻ";
            this.btnShare.UseVisualStyleBackColor = false;
            this.btnShare.Click += new System.EventHandler(this.btnShare_Click);
            // 
            // cmbNewPerm
            // 
            this.cmbNewPerm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNewPerm.FormattingEnabled = true;
            this.cmbNewPerm.Items.AddRange(new object[] {
            "Viewer",
            "Editor"});
            this.cmbNewPerm.Location = new System.Drawing.Point(350, 48);
            this.cmbNewPerm.Name = "cmbNewPerm";
            this.cmbNewPerm.Size = new System.Drawing.Size(130, 31);
            this.cmbNewPerm.TabIndex = 3;
            // 
            // lblNewPerm
            // 
            this.lblNewPerm.AutoSize = true;
            this.lblNewPerm.Location = new System.Drawing.Point(280, 51);
            this.lblNewPerm.Name = "lblNewPerm";
            this.lblNewPerm.Size = new System.Drawing.Size(64, 23);
            this.lblNewPerm.TabIndex = 2;
            this.lblNewPerm.Text = "Quyền:";
            // 
            // txtNewUsername
            // 
            this.txtNewUsername.Location = new System.Drawing.Point(100, 48);
            this.txtNewUsername.Name = "txtNewUsername";
            this.txtNewUsername.Size = new System.Drawing.Size(160, 30);
            this.txtNewUsername.TabIndex = 1;
            // 
            // lblNewUser
            // 
            this.lblNewUser.AutoSize = true;
            this.lblNewUser.Location = new System.Drawing.Point(6, 51);
            this.lblNewUser.Name = "lblNewUser";
            this.lblNewUser.Size = new System.Drawing.Size(91, 23);
            this.lblNewUser.TabIndex = 0;
            this.lblNewUser.Text = "Username:";
            // 
            // lblCollabs
            // 
            this.lblCollabs.AutoSize = true;
            this.lblCollabs.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblCollabs.Location = new System.Drawing.Point(21, 335);
            this.lblCollabs.Name = "lblCollabs";
            this.lblCollabs.Size = new System.Drawing.Size(280, 25);
            this.lblCollabs.TabIndex = 6;
            this.lblCollabs.Text = "Danh sách người có quyền truy cập:";
            // 
            // dgvCollabs
            // 
            this.dgvCollabs.AllowUserToAddRows = false;
            this.dgvCollabs.AllowUserToDeleteRows = false;
            this.dgvCollabs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvCollabs.BackgroundColor = System.Drawing.Color.WhiteSmoke;
            this.dgvCollabs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCollabs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colUsername,
            this.colEmail,
            this.colPermission,
            this.colAction});
            this.dgvCollabs.Location = new System.Drawing.Point(26, 365);
            this.dgvCollabs.MultiSelect = false;
            this.dgvCollabs.Name = "dgvCollabs";
            this.dgvCollabs.RowHeadersVisible = false;
            this.dgvCollabs.RowHeadersWidth = 51;
            this.dgvCollabs.RowTemplate.Height = 30;
            this.dgvCollabs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCollabs.Size = new System.Drawing.Size(648, 180);
            this.dgvCollabs.TabIndex = 7;
            this.dgvCollabs.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCollabs_CellValueChanged);
            this.dgvCollabs.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCollabs_CellContentClick);
            // 
            // colUsername
            // 
            this.colUsername.HeaderText = "Username";
            this.colUsername.MinimumWidth = 6;
            this.colUsername.Name = "colUsername";
            this.colUsername.ReadOnly = true;
            // 
            // colEmail
            // 
            this.colEmail.HeaderText = "Email";
            this.colEmail.MinimumWidth = 6;
            this.colEmail.Name = "colEmail";
            this.colEmail.ReadOnly = true;
            // 
            // colPermission
            // 
            this.colPermission.HeaderText = "Quyền";
            this.colPermission.Items.AddRange(new object[] {
            "viewer",
            "editor"});
            this.colPermission.MinimumWidth = 6;
            this.colPermission.Name = "colPermission";
            // 
            // colAction
            // 
            this.colAction.HeaderText = "Hành động";
            this.colAction.MinimumWidth = 6;
            this.colAction.Name = "colAction";
            this.colAction.Text = "Xóa quyền";
            this.colAction.UseColumnTextForButtonValue = true;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(544, 555);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(130, 35);
            this.btnClose.TabIndex = 8;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ShareManagementForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(700, 600);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.dgvCollabs);
            this.Controls.Add(this.lblCollabs);
            this.Controls.Add(this.grpNewShare);
            this.Controls.Add(this.grpPublic);
            this.Controls.Add(this.btnCopyCode);
            this.Controls.Add(this.lblShareCode);
            this.Controls.Add(this.lblShareCodeHeader);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShareManagementForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Quản lý chia sẻ";
            this.grpPublic.ResumeLayout(false);
            this.grpPublic.PerformLayout();
            this.grpNewShare.ResumeLayout(false);
            this.grpNewShare.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCollabs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblShareCodeHeader;
        private System.Windows.Forms.Label lblShareCode;
        private System.Windows.Forms.Button btnCopyCode;
        private System.Windows.Forms.GroupBox grpPublic;
        private System.Windows.Forms.CheckBox chkPublic;
        private System.Windows.Forms.RadioButton rdoPublicEditor;
        private System.Windows.Forms.RadioButton rdoPublicViewer;
        private System.Windows.Forms.Label lblPublicPerm;
        private System.Windows.Forms.Button btnSavePublic;
        private System.Windows.Forms.GroupBox grpNewShare;
        private System.Windows.Forms.Label lblNewUser;
        private System.Windows.Forms.TextBox txtNewUsername;
        private System.Windows.Forms.Label lblNewPerm;
        private System.Windows.Forms.ComboBox cmbNewPerm;
        private System.Windows.Forms.Button btnShare;
        private System.Windows.Forms.Label lblCollabs;
        private System.Windows.Forms.DataGridView dgvCollabs;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUsername;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEmail;
        private System.Windows.Forms.DataGridViewComboBoxColumn colPermission;
        private System.Windows.Forms.DataGridViewButtonColumn colAction;
    }
}
