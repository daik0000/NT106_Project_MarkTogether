using System;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.UI;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            Load += LoginForm_Load;
            Resize += (s, e) => CenterCard();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            CenterCard();

            // Apply rounded styling cho card / button
            UiFactory.StyleAsCard(pnlCard);
            UiFactory.StylePrimaryButton(btnLogin);
            UiFactory.ApplyRoundedRegion(lblErrorBanner, AppTheme.CornerRadius);
            lblErrorBanner.Resize += (s, ev) => UiFactory.ApplyRoundedRegion(lblErrorBanner, AppTheme.CornerRadius);

            // Input panels: chỉ vẽ border, KHÔNG clip Region (tránh mất nét)
            UiFactory.StyleInputPanel(pnlUsername);
            UiFactory.StyleInputPanel(pnlPassword);

            // Focus highlight cho input
            WireInputFocus(pnlUsername, txtUsername);
            WireInputFocus(pnlPassword, txtPassword);
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

        private void ClearError()
        {
            lblErrorBanner.Visible = false;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return;
            }

            try
            {
                btnLogin.Enabled = false;
                btnLogin.Text = "Đang đăng nhập...";
                ClearError();

                SocketClient.Instance.Connect("159.203.184.87", 5000);
                Payload_AUTH_RESPONSE result = SocketClient.Instance.Login(username, password);

                if (result.Success)
                {
                    this.Hide();
                    var homeForm = new HomeForm();
                    homeForm.FormClosed += (s, args) =>
                    {
                        SocketClient.Instance.Disconnect();
                        this.Close();
                    };
                    homeForm.Show();
                }
                else
                {
                    ShowError(result.Message ?? "Tên đăng nhập hoặc mật khẩu không đúng.");
                    SocketClient.Instance.Disconnect();
                }
            }
            catch (Exception ex)
            {
                ShowError("Không thể kết nối đến server. " + ex.Message);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Đăng nhập";
            }
        }

        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var registerForm = new RegisterForm())
            {
                registerForm.ShowDialog(this);
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtPassword.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

    }
}