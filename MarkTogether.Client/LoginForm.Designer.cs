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
            this.pnlUsername = new System.Windows.Forms.Panel();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.pnlPassword = new System.Windows.Forms.Panel();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblErrorBanner = new System.Windows.Forms.Label();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lblFooter = new System.Windows.Forms.Label();
            this.lnkRegister = new System.Windows.Forms.LinkLabel();
            this.pnlCard.SuspendLayout();
            this.pnlUsername.SuspendLayout();
            this.pnlPassword.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCard
            // 
            this.pnlCard.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pnlCard.BackColor = System.Drawing.Color.White;
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
            this.pnlCard.Location = new System.Drawing.Point(360, 140);
            this.pnlCard.Margin = new System.Windows.Forms.Padding(6);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Padding = new System.Windows.Forms.Padding(64);
            this.pnlCard.Size = new System.Drawing.Size(880, 1000);
            this.pnlCard.TabIndex = 0;
            // 
            // lblBrand
            // 
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = new System.Drawing.Font("Inter", 9F);
            this.lblBrand.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblBrand.Location = new System.Drawing.Point(72, 72);
            this.lblBrand.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new System.Drawing.Size(207, 29);
            this.lblBrand.TabIndex = 0;
            this.lblBrand.Text = "MARKTOGETHER";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Inter", 22F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblTitle.Location = new System.Drawing.Point(72, 120);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(340, 71);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Đăng nhập";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Inter", 11F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblSubtitle.Location = new System.Drawing.Point(72, 220);
            this.lblSubtitle.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(467, 36);
            this.lblSubtitle.TabIndex = 2;
            this.lblSubtitle.Text = "Đăng nhập để tiếp tục cộng tác.";
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Font = new System.Drawing.Font("Inter", 10F, System.Drawing.FontStyle.Bold);
            this.lblUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblUsername.Location = new System.Drawing.Point(72, 324);
            this.lblUsername.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(210, 33);
            this.lblUsername.TabIndex = 3;
            this.lblUsername.Text = "Tên đăng nhập";
            // 
            // pnlUsername
            // 
            this.pnlUsername.BackColor = System.Drawing.Color.White;
            this.pnlUsername.Controls.Add(this.txtUsername);
            this.pnlUsername.Location = new System.Drawing.Point(70, 375);
            this.pnlUsername.Margin = new System.Windows.Forms.Padding(6);
            this.pnlUsername.Name = "pnlUsername";
            this.pnlUsername.Padding = new System.Windows.Forms.Padding(24, 20, 24, 0);
            this.pnlUsername.Size = new System.Drawing.Size(736, 76);
            this.pnlUsername.TabIndex = 4;
            // 
            // txtUsername
            // 
            this.txtUsername.BackColor = System.Drawing.Color.White;
            this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUsername.Font = new System.Drawing.Font("Inter", 10F);
            this.txtUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.txtUsername.Location = new System.Drawing.Point(24, 20);
            this.txtUsername.Margin = new System.Windows.Forms.Padding(6);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(688, 33);
            this.txtUsername.TabIndex = 0;
            this.txtUsername.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtUsername_KeyDown);
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Inter", 10F, System.Drawing.FontStyle.Bold);
            this.lblPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblPassword.Location = new System.Drawing.Point(72, 488);
            this.lblPassword.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(138, 33);
            this.lblPassword.TabIndex = 5;
            this.lblPassword.Text = "Mật khẩu";
            // 
            // pnlPassword
            // 
            this.pnlPassword.BackColor = System.Drawing.Color.White;
            this.pnlPassword.Controls.Add(this.txtPassword);
            this.pnlPassword.Location = new System.Drawing.Point(72, 540);
            this.pnlPassword.Margin = new System.Windows.Forms.Padding(6);
            this.pnlPassword.Name = "pnlPassword";
            this.pnlPassword.Padding = new System.Windows.Forms.Padding(24, 20, 24, 0);
            this.pnlPassword.Size = new System.Drawing.Size(736, 76);
            this.pnlPassword.TabIndex = 6;
            // 
            // txtPassword
            // 
            this.txtPassword.BackColor = System.Drawing.Color.White;
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.Font = new System.Drawing.Font("Inter", 10F);
            this.txtPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.txtPassword.Location = new System.Drawing.Point(24, 20);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(6);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '•';
            this.txtPassword.Size = new System.Drawing.Size(688, 33);
            this.txtPassword.TabIndex = 0;
            this.txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPassword_KeyDown);
            // 
            // lblErrorBanner
            // 
            this.lblErrorBanner.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(242)))), ((int)(((byte)(242)))));
            this.lblErrorBanner.Font = new System.Drawing.Font("Inter", 9F);
            this.lblErrorBanner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(68)))), ((int)(((byte)(68)))));
            this.lblErrorBanner.Location = new System.Drawing.Point(72, 644);
            this.lblErrorBanner.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblErrorBanner.Name = "lblErrorBanner";
            this.lblErrorBanner.Padding = new System.Windows.Forms.Padding(24, 0, 24, 0);
            this.lblErrorBanner.Size = new System.Drawing.Size(736, 72);
            this.lblErrorBanner.TabIndex = 7;
            this.lblErrorBanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblErrorBanner.Visible = false;
            // 
            // btnLogin
            // 
            this.btnLogin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.Font = new System.Drawing.Font("Inter", 10F, System.Drawing.FontStyle.Bold);
            this.btnLogin.ForeColor = System.Drawing.Color.White;
            this.btnLogin.Location = new System.Drawing.Point(72, 744);
            this.btnLogin.Margin = new System.Windows.Forms.Padding(6);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(736, 80);
            this.btnLogin.TabIndex = 8;
            this.btnLogin.Text = "Đăng nhập";
            this.btnLogin.UseVisualStyleBackColor = false;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // lblFooter
            // 
            this.lblFooter.AutoSize = true;
            this.lblFooter.Font = new System.Drawing.Font("Inter", 9F);
            this.lblFooter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblFooter.Location = new System.Drawing.Point(240, 884);
            this.lblFooter.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblFooter.Name = "lblFooter";
            this.lblFooter.Size = new System.Drawing.Size(231, 29);
            this.lblFooter.TabIndex = 9;
            this.lblFooter.Text = "Chưa có tài khoản?";
            // 
            // lnkRegister
            // 
            this.lnkRegister.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(64)))), ((int)(((byte)(175)))));
            this.lnkRegister.AutoSize = true;
            this.lnkRegister.Font = new System.Drawing.Font("Inter", 10F, System.Drawing.FontStyle.Bold);
            this.lnkRegister.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkRegister.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.lnkRegister.Location = new System.Drawing.Point(490, 882);
            this.lnkRegister.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lnkRegister.Name = "lnkRegister";
            this.lnkRegister.Size = new System.Drawing.Size(194, 33);
            this.lnkRegister.TabIndex = 10;
            this.lnkRegister.TabStop = true;
            this.lnkRegister.Text = "Đăng ký ngay";
            this.lnkRegister.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkRegister_LinkClicked);
            // 
            // LoginForm
            // 
            this.AcceptButton = this.btnLogin;
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(1600, 1280);
            this.Controls.Add(this.pnlCard);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MaximizeBox = true;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarkTogether — Đăng nhập";
            this.pnlCard.ResumeLayout(false);
            this.pnlCard.PerformLayout();
            this.pnlUsername.ResumeLayout(false);
            this.pnlUsername.PerformLayout();
            this.pnlPassword.ResumeLayout(false);
            this.pnlPassword.PerformLayout();
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