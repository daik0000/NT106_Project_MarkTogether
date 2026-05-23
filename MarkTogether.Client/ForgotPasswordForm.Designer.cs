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
            this.pnlCard.Location = new System.Drawing.Point(90, 40);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(36);
            this.pnlCard.Size = new System.Drawing.Size(620, 660);
            this.pnlCard.TabIndex = 0;
            //
            // lblBrand
            //
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = AppTheme.Caption;
            this.lblBrand.ForeColor = AppTheme.TextMuted;
            this.lblBrand.Location = new System.Drawing.Point(40, 34);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Text = "MARKTOGETHER";
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppTheme.H2;
            this.lblTitle.ForeColor = AppTheme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(40, 70);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Quên mật khẩu";
            //
            // lblSubtitle
            //
            this.lblSubtitle.Font = AppTheme.Body;
            this.lblSubtitle.ForeColor = AppTheme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(42, 118);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(540, 54);
            this.lblSubtitle.Text = "Nhập email đã đăng ký để nhận mã OTP đặt lại mật khẩu.";
            //
            // lblMessage
            //
            this.lblMessage.Font = AppTheme.Body;
            this.lblMessage.Location = new System.Drawing.Point(42, 178);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.lblMessage.Size = new System.Drawing.Size(540, 44);
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblMessage.Visible = false;
            //
            // pnlStepEmail
            //
            this.pnlStepEmail.Controls.Add(this.lblEmail);
            this.pnlStepEmail.Controls.Add(this.pnlEmail);
            this.pnlStepEmail.Controls.Add(this.btnSendOtp);
            this.pnlStepEmail.Location = new System.Drawing.Point(42, 240);
            this.pnlStepEmail.Name = "pnlStepEmail";
            this.pnlStepEmail.Size = new System.Drawing.Size(540, 230);
            this.pnlStepEmail.TabIndex = 1;
            //
            // lblEmail
            //
            this.lblEmail.AutoSize = true;
            this.lblEmail.Font = AppTheme.BodyBold;
            this.lblEmail.ForeColor = AppTheme.TextPrimary;
            this.lblEmail.Location = new System.Drawing.Point(0, 0);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Text = "Email";
            //
            // pnlEmail
            //
            this.pnlEmail.BackColor = System.Drawing.Color.White;
            this.pnlEmail.Controls.Add(this.txtEmail);
            this.pnlEmail.Location = new System.Drawing.Point(0, 36);
            this.pnlEmail.Name = "pnlEmail";
            this.pnlEmail.Padding = new System.Windows.Forms.Padding(14, 12, 14, 0);
            this.pnlEmail.Size = new System.Drawing.Size(540, 54);
            this.pnlEmail.TabIndex = 0;
            //
            // txtEmail
            //
            this.txtEmail.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEmail.Font = AppTheme.Body;
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(512, 27);
            this.txtEmail.TabIndex = 0;
            //
            // btnSendOtp
            //
            this.btnSendOtp.Location = new System.Drawing.Point(0, 116);
            this.btnSendOtp.Name = "btnSendOtp";
            this.btnSendOtp.Size = new System.Drawing.Size(540, AppTheme.ButtonHeight);
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
            this.pnlStepReset.Location = new System.Drawing.Point(42, 230);
            this.pnlStepReset.Name = "pnlStepReset";
            this.pnlStepReset.Size = new System.Drawing.Size(540, 360);
            this.pnlStepReset.TabIndex = 2;
            this.pnlStepReset.Visible = false;
            //
            // lblMaskedEmail
            //
            this.lblMaskedEmail.Font = AppTheme.Caption;
            this.lblMaskedEmail.ForeColor = AppTheme.TextSecondary;
            this.lblMaskedEmail.Location = new System.Drawing.Point(0, 0);
            this.lblMaskedEmail.Name = "lblMaskedEmail";
            this.lblMaskedEmail.Size = new System.Drawing.Size(540, 36);
            //
            // lblOtp
            //
            this.lblOtp.AutoSize = true;
            this.lblOtp.Font = AppTheme.BodyBold;
            this.lblOtp.Location = new System.Drawing.Point(0, 44);
            this.lblOtp.Name = "lblOtp";
            this.lblOtp.Text = "OTP";
            //
            // pnlOtp
            //
            this.pnlOtp.BackColor = System.Drawing.Color.White;
            this.pnlOtp.Controls.Add(this.txtOtp);
            this.pnlOtp.Location = new System.Drawing.Point(0, 74);
            this.pnlOtp.Name = "pnlOtp";
            this.pnlOtp.Padding = new System.Windows.Forms.Padding(14, 12, 14, 0);
            this.pnlOtp.Size = new System.Drawing.Size(540, 50);
            this.pnlOtp.TabIndex = 0;
            //
            // txtOtp
            //
            this.txtOtp.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtOtp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOtp.Font = AppTheme.Body;
            this.txtOtp.MaxLength = 6;
            this.txtOtp.Name = "txtOtp";
            this.txtOtp.Size = new System.Drawing.Size(512, 27);
            this.txtOtp.TabIndex = 0;
            this.txtOtp.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOtp_KeyPress);
            //
            // lblNewPassword
            //
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Font = AppTheme.BodyBold;
            this.lblNewPassword.Location = new System.Drawing.Point(0, 138);
            this.lblNewPassword.Name = "lblNewPassword";
            this.lblNewPassword.Text = "Mật khẩu mới";
            //
            // pnlNewPassword
            //
            this.pnlNewPassword.BackColor = System.Drawing.Color.White;
            this.pnlNewPassword.Controls.Add(this.txtNewPassword);
            this.pnlNewPassword.Location = new System.Drawing.Point(0, 168);
            this.pnlNewPassword.Name = "pnlNewPassword";
            this.pnlNewPassword.Padding = new System.Windows.Forms.Padding(14, 12, 14, 0);
            this.pnlNewPassword.Size = new System.Drawing.Size(540, 50);
            this.pnlNewPassword.TabIndex = 1;
            //
            // txtNewPassword
            //
            this.txtNewPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtNewPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNewPassword.Font = AppTheme.Body;
            this.txtNewPassword.Name = "txtNewPassword";
            this.txtNewPassword.PasswordChar = '•';
            this.txtNewPassword.Size = new System.Drawing.Size(512, 27);
            this.txtNewPassword.TabIndex = 0;
            //
            // lblConfirmPassword
            //
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Font = AppTheme.BodyBold;
            this.lblConfirmPassword.Location = new System.Drawing.Point(0, 232);
            this.lblConfirmPassword.Name = "lblConfirmPassword";
            this.lblConfirmPassword.Text = "Xác nhận mật khẩu";
            //
            // pnlConfirmPassword
            //
            this.pnlConfirmPassword.BackColor = System.Drawing.Color.White;
            this.pnlConfirmPassword.Controls.Add(this.txtConfirmPassword);
            this.pnlConfirmPassword.Location = new System.Drawing.Point(0, 262);
            this.pnlConfirmPassword.Name = "pnlConfirmPassword";
            this.pnlConfirmPassword.Padding = new System.Windows.Forms.Padding(14, 12, 14, 0);
            this.pnlConfirmPassword.Size = new System.Drawing.Size(540, 50);
            this.pnlConfirmPassword.TabIndex = 2;
            //
            // txtConfirmPassword
            //
            this.txtConfirmPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConfirmPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConfirmPassword.Font = AppTheme.Body;
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PasswordChar = '•';
            this.txtConfirmPassword.Size = new System.Drawing.Size(512, 27);
            this.txtConfirmPassword.TabIndex = 0;
            //
            // btnResetPassword
            //
            this.btnResetPassword.Location = new System.Drawing.Point(0, 326);
            this.btnResetPassword.Name = "btnResetPassword";
            this.btnResetPassword.Size = new System.Drawing.Size(360, AppTheme.ButtonHeight);
            this.btnResetPassword.TabIndex = 3;
            this.btnResetPassword.Text = "Đặt lại mật khẩu";
            this.btnResetPassword.Click += new System.EventHandler(this.btnResetPassword_Click);
            //
            // lnkBack
            //
            this.lnkBack.AutoSize = true;
            this.lnkBack.Font = AppTheme.Body;
            this.lnkBack.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkBack.LinkColor = AppTheme.Primary;
            this.lnkBack.Location = new System.Drawing.Point(380, 338);
            this.lnkBack.Name = "lnkBack";
            this.lnkBack.Size = new System.Drawing.Size(91, 25);
            this.lnkBack.TabIndex = 4;
            this.lnkBack.Text = "Đổi email";
            this.lnkBack.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkBack_LinkClicked);
            //
            // btnCancel
            //
            this.btnCancel.Location = new System.Drawing.Point(42, 590);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(140, AppTheme.ButtonHeight);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Đóng";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // ForgotPasswordForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(800, 740);
            this.Controls.Add(this.pnlCard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
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