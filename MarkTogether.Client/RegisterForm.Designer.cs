using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class RegisterForm
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
            this.lblUsername = new System.Windows.Forms.Label();
            this.pnlUsername = new System.Windows.Forms.Panel();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblEmail = new System.Windows.Forms.Label();
            this.pnlEmail = new System.Windows.Forms.Panel();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.pnlPassword = new System.Windows.Forms.Panel();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblConfirmPassword = new System.Windows.Forms.Label();
            this.pnlConfirmPassword = new System.Windows.Forms.Panel();
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.lblErrorBanner = new System.Windows.Forms.Label();
            this.btnRegister = new System.Windows.Forms.Button();
            this.lblFooter = new System.Windows.Forms.Label();
            this.lnkBackToLogin = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            //
            // Card
            //
            this.pnlCard.BackColor = AppTheme.Surface;
            this.pnlCard.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pnlCard.Location = new System.Drawing.Point(180, 30);
            this.pnlCard.Size = new System.Drawing.Size(440, 660);
            this.pnlCard.Padding = new System.Windows.Forms.Padding(AppTheme.Space2Xl);
            this.pnlCard.Name = "pnlCard";
            //
            // Brand label
            //
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = AppTheme.Caption;
            this.lblBrand.ForeColor = AppTheme.TextSecondary;
            this.lblBrand.Location = new System.Drawing.Point(36, 36);
            this.lblBrand.Text = "MARKTOGETHER";
            //
            // Title
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppTheme.H1;
            this.lblTitle.ForeColor = AppTheme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(36, 60);
            this.lblTitle.Text = "Tạo tài khoản";
            //
            // Subtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = AppTheme.Subtitle;
            this.lblSubtitle.ForeColor = AppTheme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(36, 110);
            this.lblSubtitle.Text = "Điền thông tin để bắt đầu cộng tác.";
            //
            // Username
            //
            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = AppTheme.BodyBold;
            this.lblUsername.ForeColor = AppTheme.TextPrimary;
            this.lblUsername.Location = new System.Drawing.Point(36, 158);
            this.lblUsername.Text = "Tên đăng nhập";
            //
            this.pnlUsername.BackColor = AppTheme.Surface;
            this.pnlUsername.Location = new System.Drawing.Point(36, 184);
            this.pnlUsername.Size = new System.Drawing.Size(368, AppTheme.InputHeight);
            this.pnlUsername.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlUsername.Name = "pnlUsername";
            //
            this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsername.Font = AppTheme.Body;
            this.txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUsername.Name = "txtUsername";
            this.pnlUsername.Controls.Add(this.txtUsername);
            //
            // Email
            //
            this.lblEmail.AutoSize = true;
            this.lblEmail.Font = AppTheme.BodyBold;
            this.lblEmail.ForeColor = AppTheme.TextPrimary;
            this.lblEmail.Location = new System.Drawing.Point(36, 240);
            this.lblEmail.Text = "Email (tuỳ chọn)";
            //
            this.pnlEmail.BackColor = AppTheme.Surface;
            this.pnlEmail.Location = new System.Drawing.Point(36, 266);
            this.pnlEmail.Size = new System.Drawing.Size(368, AppTheme.InputHeight);
            this.pnlEmail.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlEmail.Name = "pnlEmail";
            //
            this.txtEmail.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtEmail.Font = AppTheme.Body;
            this.txtEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEmail.Name = "txtEmail";
            this.pnlEmail.Controls.Add(this.txtEmail);
            //
            // Password
            //
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = AppTheme.BodyBold;
            this.lblPassword.ForeColor = AppTheme.TextPrimary;
            this.lblPassword.Location = new System.Drawing.Point(36, 322);
            this.lblPassword.Text = "Mật khẩu";
            //
            this.pnlPassword.BackColor = AppTheme.Surface;
            this.pnlPassword.Location = new System.Drawing.Point(36, 348);
            this.pnlPassword.Size = new System.Drawing.Size(368, AppTheme.InputHeight);
            this.pnlPassword.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlPassword.Name = "pnlPassword";
            //
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPassword.Font = AppTheme.Body;
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.PasswordChar = '•';
            this.txtPassword.Name = "txtPassword";
            this.pnlPassword.Controls.Add(this.txtPassword);
            //
            // Confirm password
            //
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Font = AppTheme.BodyBold;
            this.lblConfirmPassword.ForeColor = AppTheme.TextPrimary;
            this.lblConfirmPassword.Location = new System.Drawing.Point(36, 404);
            this.lblConfirmPassword.Text = "Xác nhận mật khẩu";
            //
            this.pnlConfirmPassword.BackColor = AppTheme.Surface;
            this.pnlConfirmPassword.Location = new System.Drawing.Point(36, 430);
            this.pnlConfirmPassword.Size = new System.Drawing.Size(368, AppTheme.InputHeight);
            this.pnlConfirmPassword.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlConfirmPassword.Name = "pnlConfirmPassword";
            //
            this.txtConfirmPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConfirmPassword.Font = AppTheme.Body;
            this.txtConfirmPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConfirmPassword.PasswordChar = '•';
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtConfirmPassword_KeyDown);
            this.pnlConfirmPassword.Controls.Add(this.txtConfirmPassword);
            //
            // Error banner
            //
            this.lblErrorBanner.Visible = false;
            this.lblErrorBanner.BackColor = System.Drawing.Color.FromArgb(0xFE, 0xF2, 0xF2);
            this.lblErrorBanner.ForeColor = AppTheme.Error;
            this.lblErrorBanner.Font = AppTheme.Caption;
            this.lblErrorBanner.Location = new System.Drawing.Point(36, 482);
            this.lblErrorBanner.Size = new System.Drawing.Size(368, 36);
            this.lblErrorBanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblErrorBanner.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.lblErrorBanner.Name = "lblErrorBanner";
            //
            // Register button
            //
            this.btnRegister.Location = new System.Drawing.Point(36, 530);
            this.btnRegister.Size = new System.Drawing.Size(368, AppTheme.ButtonHeight);
            this.btnRegister.Text = "Tạo tài khoản";
            this.btnRegister.Font = AppTheme.Button;
            this.btnRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegister.BackColor = AppTheme.Primary;
            this.btnRegister.ForeColor = AppTheme.TextOnPrimary;
            this.btnRegister.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRegister.UseVisualStyleBackColor = false;
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            //
            // Footer
            //
            this.lblFooter.AutoSize = true;
            this.lblFooter.Font = AppTheme.Caption;
            this.lblFooter.ForeColor = AppTheme.TextSecondary;
            this.lblFooter.Location = new System.Drawing.Point(120, 600);
            this.lblFooter.Text = "Đã có tài khoản?";

            this.lnkBackToLogin.AutoSize = true;
            this.lnkBackToLogin.Font = AppTheme.BodyBold;
            this.lnkBackToLogin.LinkColor = AppTheme.Primary;
            this.lnkBackToLogin.ActiveLinkColor = AppTheme.PrimaryPressed;
            this.lnkBackToLogin.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkBackToLogin.Location = new System.Drawing.Point(238, 599);
            this.lnkBackToLogin.Name = "lnkBackToLogin";
            this.lnkBackToLogin.Text = "Đăng nhập";
            this.lnkBackToLogin.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkBackToLogin_LinkClicked);
            //
            // Add to card
            //
            this.pnlCard.Controls.Add(this.lblBrand);
            this.pnlCard.Controls.Add(this.lblTitle);
            this.pnlCard.Controls.Add(this.lblSubtitle);
            this.pnlCard.Controls.Add(this.lblUsername);
            this.pnlCard.Controls.Add(this.pnlUsername);
            this.pnlCard.Controls.Add(this.lblEmail);
            this.pnlCard.Controls.Add(this.pnlEmail);
            this.pnlCard.Controls.Add(this.lblPassword);
            this.pnlCard.Controls.Add(this.pnlPassword);
            this.pnlCard.Controls.Add(this.lblConfirmPassword);
            this.pnlCard.Controls.Add(this.pnlConfirmPassword);
            this.pnlCard.Controls.Add(this.lblErrorBanner);
            this.pnlCard.Controls.Add(this.btnRegister);
            this.pnlCard.Controls.Add(this.lblFooter);
            this.pnlCard.Controls.Add(this.lnkBackToLogin);
            //
            // Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(800, 720);
            this.Controls.Add(this.pnlCard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AcceptButton = this.btnRegister;
            this.Name = "RegisterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MarkTogether — Đăng ký";
            this.ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.Panel pnlCard;
        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Panel pnlUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.Panel pnlEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Panel pnlPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblConfirmPassword;
        private System.Windows.Forms.Panel pnlConfirmPassword;
        private System.Windows.Forms.TextBox txtConfirmPassword;
        private System.Windows.Forms.Label lblErrorBanner;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Label lblFooter;
        private System.Windows.Forms.LinkLabel lnkBackToLogin;
    }
}