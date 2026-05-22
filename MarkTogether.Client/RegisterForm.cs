using System;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.UI;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
            Load += RegisterForm_Load;
            Resize += (s, e) => CenterCard();
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {
            CenterCard();

            UiFactory.StyleAsCard(pnlCard);
            UiFactory.StylePrimaryButton(btnRegister);
            UiFactory.ApplyRoundedRegion(lblErrorBanner, AppTheme.CornerRadius);
            lblErrorBanner.Resize += (s, ev) => UiFactory.ApplyRoundedRegion(lblErrorBanner, AppTheme.CornerRadius);

            // Input panels: chỉ vẽ border, KHÔNG clip Region (tránh mất nét)
            UiFactory.StyleInputPanel(pnlUsername);
            UiFactory.StyleInputPanel(pnlEmail);
            UiFactory.StyleInputPanel(pnlPassword);
            UiFactory.StyleInputPanel(pnlConfirmPassword);

            WireInputFocus(pnlUsername, txtUsername);
            WireInputFocus(pnlEmail, txtEmail);
            WireInputFocus(pnlPassword, txtPassword);
            WireInputFocus(pnlConfirmPassword, txtConfirmPassword);
        }

        private void CenterCard()
        {
            pnlCard.Left = Math.Max(0, (ClientSize.Width - pnlCard.Width) / 2);
            pnlCard.Top = Math.Max(0, (ClientSize.Height - pnlCard.Height) / 2);
        }

        private void WireInputFocus(Panel panel, TextBox tb)
        {
            tb.GotFocus += (s, e) => { panel.Tag = "focus"; panel.Invalidate(); };
            tb.LostFocus += (s, e) => { panel.Tag = null; panel.Invalidate(); };
            panel.Paint += (s, e) =>
            {
                bool focused = panel.Tag as string == "focus";
                UiFactory.DrawBorder(e.Graphics, panel.ClientRectangle,
                    focused ? AppTheme.BorderFocus : AppTheme.Border,
                    AppTheme.CornerRadius);
            };
        }

        private void ShowError(string message)
        {
            lblErrorBanner.Text = "  " + message;
            lblErrorBanner.Visible = true;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Tên đăng nhập và mật khẩu không được để trống.");
                return;
            }
            if (password.Length < 6)
            {
                ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
                return;
            }
            if (password != confirmPassword)
            {
                ShowError("Mật khẩu xác nhận không khớp.");
                return;
            }

            try
            {
                btnRegister.Enabled = false;
                btnRegister.Text = "Đang đăng ký...";
                lblErrorBanner.Visible = false;

                // Disconnect nếu đang có connection cũ (từ LoginForm fail trước đó)
                if (SocketClient.Instance.IsLoggedIn)
                    SocketClient.Instance.Disconnect();
                SocketClient.Instance.Connect("159.203.184.87", 5000);
                Payload_AUTH_RESPONSE result = SocketClient.Instance.Register(username, email, password);

                if (result.Success)
                {
                    MessageBox.Show("Đăng ký thành công. Bạn có thể đăng nhập ngay.",
                        "Thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    SocketClient.Instance.Disconnect();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowError(result.Message ?? "Không thể tạo tài khoản.");
                    SocketClient.Instance.Disconnect();
                }
            }
            catch (Exception ex)
            {
                ShowError("Không thể kết nối đến server. " + ex.Message);
            }
            finally
            {
                btnRegister.Enabled = true;
                btnRegister.Text = "Tạo tài khoản";
            }
        }

        private void lnkBackToLogin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
        }

        private void txtConfirmPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnRegister_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }
}