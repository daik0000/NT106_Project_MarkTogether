using System;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            // Validate
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Vui lòng nhập đầy đủ thông tin!";
                lblError.Visible = true;
                return;
            }

            try
            {
                btnLogin.Enabled = false;
                btnLogin.Text = "Đang đăng nhập...";
                lblError.Visible = false;

                // Kết nối server (nếu chưa)
                SocketClient.Instance.Connect("localhost", 5000);

                // Gửi yêu cầu đăng nhập
                Payload_AUTH_RESPONSE result = SocketClient.Instance.Login(username, password);

                if (result.Success)
                {
                    // Thành công → mở MainForm
                    MessageBox.Show($"Chào mừng {result.Username}!",
                        "Đăng nhập thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    this.Hide();
                    var mainForm = new TypeRenderForm();
                    mainForm.Text = $"MarkTogether - {result.Username}";
                    mainForm.FormClosed += (s, args) =>
                    {
                        SocketClient.Instance.Disconnect();
                        this.Close();
                    };
                    mainForm.Show();
                }
                else
                {
                    // Thất bại → hiện lỗi
                    lblError.Text = result.Message;
                    lblError.Visible = true;
                    SocketClient.Instance.Disconnect();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Không thể kết nối đến server!";
                lblError.Visible = true;
                MessageBox.Show($"Chi tiết lỗi: {ex.Message}",
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Đăng nhập";
            }
        }

        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Mở form đăng ký
            var registerForm = new RegisterForm();
            registerForm.ShowDialog(this);
        }

        // Cho phép nhấn Enter để đăng nhập
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
