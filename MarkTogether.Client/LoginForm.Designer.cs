using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class LoginForm
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
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.pnlUsername = new System.Windows.Forms.Panel();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.pnlPassword = new System.Windows.Forms.Panel();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lblErrorBanner = new System.Windows.Forms.Label();
            this.lblFooter = new System.Windows.Forms.Label();
            this.lnkRegister = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // pnlCard (canvas trắng - card chính)
            // 
            this.pnlCard.BackColor = AppTheme.Surface;
            this.pnlCard.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pnlCard.Location = new System.Drawing.Point(180, 70);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Size = new System.Drawing.Size(440, 500);
            this.pnlCard.Padding = new System.Windows.Forms.Padding(AppTheme.Space2Xl);
            // 
            // lblBrand
            // 
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = AppTheme.Caption;
            this.lblBrand.ForeColor = AppTheme.TextSecondary;
            this.lblBrand.Location = new System.Drawing.Point(36, 36);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Text = "MARKTOGETHER";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppTheme.H1;
            this.lblTitle.ForeColor = AppTheme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(36, 60);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Đăng nhập";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = AppTheme.Subtitle;
            this.lblSubtitle.ForeColor = AppTheme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(36, 110);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Text = "Đăng nhập để tiếp tục cộng tác.";
            // 
            // Username
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = AppTheme.BodyBold;
            this.lblUsername.ForeColor = AppTheme.TextPrimary;
            this.lblUsername.Location = new System.Drawing.Point(36, 162);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Text = "Tên đăng nhập";
            // 
            this.pnlUsername.BackColor = AppTheme.Surface;
            this.pnlUsername.Location = new System.Drawing.Point(36, 188);
            this.pnlUsername.Size = new System.Drawing.Size(368, AppTheme.InputHeight);
            this.pnlUsername.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlUsername.Name = "pnlUsername";
            // 
            this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsername.Font = AppTheme.Body;
            this.txtUsername.ForeColor = AppTheme.TextPrimary;
            this.txtUsername.BackColor = AppTheme.Surface;
            this.txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtUsername_KeyDown);
            this.pnlUsername.Controls.Add(this.txtUsername);
            // 
            // Password
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = AppTheme.BodyBold;
            this.lblPassword.ForeColor = AppTheme.TextPrimary;
            this.lblPassword.Location = new System.Drawing.Point(36, 244);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Text = "Mật khẩu";
            // 
            this.pnlPassword.BackColor = AppTheme.Surface;
            this.pnlPassword.Location = new System.Drawing.Point(36, 270);
            this.pnlPassword.Size = new System.Drawing.Size(368, AppTheme.InputHeight);
            this.pnlPassword.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlPassword.Name = "pnlPassword";
            // 
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPassword.Font = AppTheme.Body;
            this.txtPassword.ForeColor = AppTheme.TextPrimary;
            this.txtPassword.BackColor = AppTheme.Surface;
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.PasswordChar = '•';
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPassword_KeyDown);
            this.pnlPassword.Controls.Add(this.txtPassword);
            // 
            // Error banner
            // 
            this.lblErrorBanner.Visible = false;
            this.lblErrorBanner.BackColor = System.Drawing.Color.FromArgb(0xFE, 0xF2, 0xF2);
            this.lblErrorBanner.ForeColor = AppTheme.Error;
            this.lblErrorBanner.Font = AppTheme.Caption;
            this.lblErrorBanner.Location = new System.Drawing.Point(36, 322);
            this.lblErrorBanner.Size = new System.Drawing.Size(368, 36);
            this.lblErrorBanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblErrorBanner.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.lblErrorBanner.Name = "lblErrorBanner";
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(36, 372);
            this.btnLogin.Size = new System.Drawing.Size(368, AppTheme.ButtonHeight);
            this.btnLogin.Text = "Đăng nhập";
            this.btnLogin.Font = AppTheme.Button;
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.BackColor = AppTheme.Primary;
            this.btnLogin.ForeColor = AppTheme.TextOnPrimary;
            this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogin.UseVisualStyleBackColor = false;
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // Footer (Chưa có tài khoản? Đăng ký)
            // 
            this.lblFooter.AutoSize = true;
            this.lblFooter.Font = AppTheme.Caption;
            this.lblFooter.ForeColor = AppTheme.TextSecondary;
            this.lblFooter.Location = new System.Drawing.Point(120, 442);
            this.lblFooter.Name = "lblFooter";
            this.lblFooter.Text = "Chưa có tài khoản?";
            // 
            this.lnkRegister.AutoSize = true;
            this.lnkRegister.Font = AppTheme.BodyBold;
            this.lnkRegister.LinkColor = AppTheme.Primary;
            this.lnkRegister.ActiveLinkColor = AppTheme.PrimaryPressed;
            this.lnkRegister.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkRegister.Location = new System.Drawing.Point(245, 441);
            this.lnkRegister.Name = "lnkRegister";
            this.lnkRegister.Text = "Đăng ký ngay";
            this.lnkRegister.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkRegister_LinkClicked);
            // 
            // Add to card
            // 
            this.pnlCard.Controls.Add(this.lblBrand);
            this.pnlCard.Controls.Add(this.lblTitle);
            this.pnlCard.Controls.Add(this.lblSubtitle);
            this.pnlCard.Controls.Add(this.lblUsername);
            this.pnlCard.Controls.Add(this.pnlUsername);
            this.pnlCard.Controls.Add(this.lblPassword);
            this.pnlCard.Controls.Add(this.pnlPassword);
            this.pnlCard.Controls.Add(this.lblErrorBanner);
            this.pnlCard.Controls.Add(this.btnLogin);
            this.pnlCard.Controls.Add(this.lblFooter);
            this.pnlCard.Controls.Add(this.lnkRegister);
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Background;
            this.ClientSize = new System.Drawing.Size(800, 640);
            this.Controls.Add(this.pnlCard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AcceptButton = this.btnLogin;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarkTogether — Đăng nhập";
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
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Panel pnlPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblErrorBanner;
        private System.Windows.Forms.Label lblFooter;
        private System.Windows.Forms.LinkLabel lnkRegister;
    }
}