using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class ForgotPasswordForm
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
            this.pnlCard = new System.Windows.Forms.Panel();
            this.lblBrand = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblMessage = new System.Windows.Forms.Label();
            this.pnlStepEmail = new System.Windows.Forms.Panel();
            this.lblEmail = new System.Windows.Forms.Label();
            this.pnlEmail = new System.Windows.Forms.Panel();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.btnSendOtp = new System.Windows.Forms.Button();
            this.pnlStepReset = new System.Windows.Forms.Panel();
            this.lblMaskedEmail = new System.Windows.Forms.Label();
            this.lblOtp = new System.Windows.Forms.Label();
            this.pnlOtp = new System.Windows.Forms.Panel();
            this.txtOtp = new System.Windows.Forms.TextBox();
            this.lblNewPassword = new System.Windows.Forms.Label();
            this.pnlNewPassword = new System.Windows.Forms.Panel();
            this.txtNewPassword = new System.Windows.Forms.TextBox();
            this.lblConfirmPassword = new System.Windows.Forms.Label();
            this.pnlConfirmPassword = new System.Windows.Forms.Panel();
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.btnResetPassword = new System.Windows.Forms.Button();
            this.lnkBack = new System.Windows.Forms.LinkLabel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlCard.SuspendLayout();
            this.pnlStepEmail.SuspendLayout();
            this.pnlEmail.SuspendLayout();
            this.pnlStepReset.SuspendLayout();
            this.pnlOtp.SuspendLayout();
            this.pnlNewPassword.SuspendLayout();
            this.pnlConfirmPassword.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCard
            // 
            this.pnlCard.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.Controls.Add(this.lblBrand);
            this.pnlCard.Controls.Add(this.lblTitle);
            this.pnlCard.Controls.Add(this.lblSubtitle);
            this.pnlCard.Controls.Add(this.lblMessage);
            this.pnlCard.Controls.Add(this.pnlStepEmail);
            this.pnlCard.Controls.Add(this.pnlStepReset);
            this.pnlCard.Controls.Add(this.btnCancel);
            this.pnlCard.Location = new System.Drawing.Point(112, 50);
            this.pnlCard.Margin = new System.Windows.Forms.Padding(4);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(45);
            this.pnlCard.Size = new System.Drawing.Size(775, 825);
            this.pnlCard.TabIndex = 0;
            // 
            // lblBrand
            // 
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBrand.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.lblBrand.Location = new System.Drawing.Point(50, 42);
            this.lblBrand.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new System.Drawing.Size(122, 20);
            this.lblBrand.TabIndex = 0;
            this.lblBrand.Text = "MARKTOGETHER";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblTitle.Location = new System.Drawing.Point(50, 88);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(211, 37);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Quên mật khẩu";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblSubtitle.Location = new System.Drawing.Point(52, 148);
            this.lblSubtitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(675, 68);
            this.lblSubtitle.TabIndex = 2;
            this.lblSubtitle.Text = "Nhập email đã đăng ký để nhận mã OTP đặt lại mật khẩu.";
            // 
            // lblMessage
            // 
            this.lblMessage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblMessage.Location = new System.Drawing.Point(52, 222);
            this.lblMessage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblMessage.Size = new System.Drawing.Size(675, 55);
            this.lblMessage.TabIndex = 3;
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblMessage.Visible = false;
            // 
            // pnlStepEmail
            // 
            this.pnlStepEmail.Controls.Add(this.lblEmail);
            this.pnlStepEmail.Controls.Add(this.pnlEmail);
            this.pnlStepEmail.Controls.Add(this.btnSendOtp);
            this.pnlStepEmail.Location = new System.Drawing.Point(52, 300);
            this.pnlStepEmail.Margin = new System.Windows.Forms.Padding(4);
            this.pnlStepEmail.Name = "pnlStepEmail";
            this.pnlStepEmail.Size = new System.Drawing.Size(675, 244);
            this.pnlStepEmail.TabIndex = 1;
            // 
            // lblEmail
            // 
            this.lblEmail.AutoSize = true;
            this.lblEmail.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblEmail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblEmail.Location = new System.Drawing.Point(0, 0);
            this.lblEmail.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Size = new System.Drawing.Size(54, 23);
            this.lblEmail.TabIndex = 0;
            this.lblEmail.Text = "Email";
            // 
            // pnlEmail
            // 
            this.pnlEmail.BackColor = System.Drawing.Color.White;
            this.pnlEmail.Controls.Add(this.txtEmail);
            this.pnlEmail.Location = new System.Drawing.Point(0, 45);
            this.pnlEmail.Margin = new System.Windows.Forms.Padding(4);
            this.pnlEmail.Name = "pnlEmail";
            this.pnlEmail.Padding = new System.Windows.Forms.Padding(18, 15, 18, 0);
            this.pnlEmail.Size = new System.Drawing.Size(675, 68);
            this.pnlEmail.TabIndex = 0;
            // 
            // txtEmail
            // 
            this.txtEmail.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEmail.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtEmail.Location = new System.Drawing.Point(18, 15);
            this.txtEmail.Margin = new System.Windows.Forms.Padding(4);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(639, 23);
            this.txtEmail.TabIndex = 0;
            // 
            // btnSendOtp
            // 
            this.btnSendOtp.Location = new System.Drawing.Point(0, 145);
            this.btnSendOtp.Margin = new System.Windows.Forms.Padding(4);
            this.btnSendOtp.Name = "btnSendOtp";
            this.btnSendOtp.Size = new System.Drawing.Size(675, 50);
            this.btnSendOtp.TabIndex = 1;
            this.btnSendOtp.Text = "Gửi mã";
            this.btnSendOtp.Click += new System.EventHandler(this.btnSendOtp_Click);
            // 
            // pnlStepReset
            // 
            this.pnlStepReset.Controls.Add(this.lblMaskedEmail);
            this.pnlStepReset.Controls.Add(this.lblOtp);
            this.pnlStepReset.Controls.Add(this.pnlOtp);
            this.pnlStepReset.Controls.Add(this.lblNewPassword);
            this.pnlStepReset.Controls.Add(this.pnlNewPassword);
            this.pnlStepReset.Controls.Add(this.lblConfirmPassword);
            this.pnlStepReset.Controls.Add(this.pnlConfirmPassword);
            this.pnlStepReset.Controls.Add(this.btnResetPassword);
            this.pnlStepReset.Controls.Add(this.lnkBack);
            this.pnlStepReset.Location = new System.Drawing.Point(52, 288);
            this.pnlStepReset.Margin = new System.Windows.Forms.Padding(4);
            this.pnlStepReset.Name = "pnlStepReset";
            this.pnlStepReset.Size = new System.Drawing.Size(675, 450);
            this.pnlStepReset.TabIndex = 2;
            this.pnlStepReset.Visible = false;
            // 
            // lblMaskedEmail
            // 
            this.lblMaskedEmail.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMaskedEmail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblMaskedEmail.Location = new System.Drawing.Point(0, 0);
            this.lblMaskedEmail.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMaskedEmail.Name = "lblMaskedEmail";
            this.lblMaskedEmail.Size = new System.Drawing.Size(675, 45);
            this.lblMaskedEmail.TabIndex = 0;
            // 
            // lblOtp
            // 
            this.lblOtp.AutoSize = true;
            this.lblOtp.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblOtp.Location = new System.Drawing.Point(0, 55);
            this.lblOtp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOtp.Name = "lblOtp";
            this.lblOtp.Size = new System.Drawing.Size(42, 23);
            this.lblOtp.TabIndex = 1;
            this.lblOtp.Text = "OTP";
            // 
            // pnlOtp
            // 
            this.pnlOtp.BackColor = System.Drawing.Color.White;
            this.pnlOtp.Controls.Add(this.txtOtp);
            this.pnlOtp.Location = new System.Drawing.Point(0, 92);
            this.pnlOtp.Margin = new System.Windows.Forms.Padding(4);
            this.pnlOtp.Name = "pnlOtp";
            this.pnlOtp.Padding = new System.Windows.Forms.Padding(18, 15, 18, 0);
            this.pnlOtp.Size = new System.Drawing.Size(675, 62);
            this.pnlOtp.TabIndex = 0;
            // 
            // txtOtp
            // 
            this.txtOtp.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtOtp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOtp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtOtp.Location = new System.Drawing.Point(18, 15);
            this.txtOtp.Margin = new System.Windows.Forms.Padding(4);
            this.txtOtp.MaxLength = 6;
            this.txtOtp.Name = "txtOtp";
            this.txtOtp.Size = new System.Drawing.Size(639, 23);
            this.txtOtp.TabIndex = 0;
            this.txtOtp.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOtp_KeyPress);
            // 
            // lblNewPassword
            // 
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblNewPassword.Location = new System.Drawing.Point(0, 172);
            this.lblNewPassword.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNewPassword.Name = "lblNewPassword";
            this.lblNewPassword.Size = new System.Drawing.Size(123, 23);
            this.lblNewPassword.TabIndex = 2;
            this.lblNewPassword.Text = "Mật khẩu mới";
            // 
            // pnlNewPassword
            // 
            this.pnlNewPassword.BackColor = System.Drawing.Color.White;
            this.pnlNewPassword.Controls.Add(this.txtNewPassword);
            this.pnlNewPassword.Location = new System.Drawing.Point(0, 210);
            this.pnlNewPassword.Margin = new System.Windows.Forms.Padding(4);
            this.pnlNewPassword.Name = "pnlNewPassword";
            this.pnlNewPassword.Padding = new System.Windows.Forms.Padding(18, 15, 18, 0);
            this.pnlNewPassword.Size = new System.Drawing.Size(675, 62);
            this.pnlNewPassword.TabIndex = 1;
            // 
            // txtNewPassword
            // 
            this.txtNewPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtNewPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNewPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtNewPassword.Location = new System.Drawing.Point(18, 15);
            this.txtNewPassword.Margin = new System.Windows.Forms.Padding(4);
            this.txtNewPassword.Name = "txtNewPassword";
            this.txtNewPassword.PasswordChar = '•';
            this.txtNewPassword.Size = new System.Drawing.Size(639, 23);
            this.txtNewPassword.TabIndex = 0;
            // 
            // lblConfirmPassword
            // 
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblConfirmPassword.Location = new System.Drawing.Point(0, 290);
            this.lblConfirmPassword.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblConfirmPassword.Name = "lblConfirmPassword";
            this.lblConfirmPassword.Size = new System.Drawing.Size(163, 23);
            this.lblConfirmPassword.TabIndex = 3;
            this.lblConfirmPassword.Text = "Xác nhận mật khẩu";
            // 
            // pnlConfirmPassword
            // 
            this.pnlConfirmPassword.BackColor = System.Drawing.Color.White;
            this.pnlConfirmPassword.Controls.Add(this.txtConfirmPassword);
            this.pnlConfirmPassword.Location = new System.Drawing.Point(0, 328);
            this.pnlConfirmPassword.Margin = new System.Windows.Forms.Padding(4);
            this.pnlConfirmPassword.Name = "pnlConfirmPassword";
            this.pnlConfirmPassword.Padding = new System.Windows.Forms.Padding(18, 15, 18, 0);
            this.pnlConfirmPassword.Size = new System.Drawing.Size(675, 62);
            this.pnlConfirmPassword.TabIndex = 2;
            // 
            // txtConfirmPassword
            // 
            this.txtConfirmPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConfirmPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtConfirmPassword.Location = new System.Drawing.Point(18, 15);
            this.txtConfirmPassword.Margin = new System.Windows.Forms.Padding(4);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PasswordChar = '•';
            this.txtConfirmPassword.Size = new System.Drawing.Size(639, 23);
            this.txtConfirmPassword.TabIndex = 0;
            // 
            // btnResetPassword
            // 
            this.btnResetPassword.Location = new System.Drawing.Point(0, 395);
            this.btnResetPassword.Margin = new System.Windows.Forms.Padding(4);
            this.btnResetPassword.Name = "btnResetPassword";
            this.btnResetPassword.Size = new System.Drawing.Size(450, 50);
            this.btnResetPassword.TabIndex = 3;
            this.btnResetPassword.Text = "Đặt lại mật khẩu";
            this.btnResetPassword.Click += new System.EventHandler(this.btnResetPassword_Click);
            // 
            // lnkBack
            // 
            this.lnkBack.AutoSize = true;
            this.lnkBack.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lnkBack.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkBack.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.lnkBack.Location = new System.Drawing.Point(475, 407);
            this.lnkBack.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lnkBack.Name = "lnkBack";
            this.lnkBack.Size = new System.Drawing.Size(82, 23);
            this.lnkBack.TabIndex = 4;
            this.lnkBack.TabStop = true;
            this.lnkBack.Text = "Đổi email";
            this.lnkBack.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkBack_LinkClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(52, 738);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(175, 50);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Đóng";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ForgotPasswordForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1000, 925);
            this.Controls.Add(this.pnlCard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ForgotPasswordForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MarkTogether — Quên mật khẩu";
            this.pnlCard.ResumeLayout(false);
            this.pnlCard.PerformLayout();
            this.pnlStepEmail.ResumeLayout(false);
            this.pnlStepEmail.PerformLayout();
            this.pnlEmail.ResumeLayout(false);
            this.pnlEmail.PerformLayout();
            this.pnlStepReset.ResumeLayout(false);
            this.pnlStepReset.PerformLayout();
            this.pnlOtp.ResumeLayout(false);
            this.pnlOtp.PerformLayout();
            this.pnlNewPassword.ResumeLayout(false);
            this.pnlNewPassword.PerformLayout();
            this.pnlConfirmPassword.ResumeLayout(false);
            this.pnlConfirmPassword.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Panel pnlCard;
        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblMessage;

        private System.Windows.Forms.Panel pnlStepEmail;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.Panel pnlEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Button btnSendOtp;

        private System.Windows.Forms.Panel pnlStepReset;
        private System.Windows.Forms.Label lblMaskedEmail;
        private System.Windows.Forms.Label lblOtp;
        private System.Windows.Forms.Panel pnlOtp;
        private System.Windows.Forms.TextBox txtOtp;
        private System.Windows.Forms.Label lblNewPassword;
        private System.Windows.Forms.Panel pnlNewPassword;
        private System.Windows.Forms.TextBox txtNewPassword;
        private System.Windows.Forms.Label lblConfirmPassword;
        private System.Windows.Forms.Panel pnlConfirmPassword;
        private System.Windows.Forms.TextBox txtConfirmPassword;
        private System.Windows.Forms.Button btnResetPassword;
        private System.Windows.Forms.LinkLabel lnkBack;

        private System.Windows.Forms.Button btnCancel;
    }
}