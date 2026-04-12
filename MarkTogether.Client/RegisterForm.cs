using System;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            // Validate
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Username và Password không được để trống!";
                lblError.Visible = true;
                return;
            }

            if (password.Length < 6)
            {
                lblError.Text = "Mật khẩu phải có ít nhất 6 ký tự!";
                lblError.Visible = true;
                return;
            }

            if (password != confirmPassword)
            {
                lblError.Text = "Mật khẩu xác nhận không khớp!";
                lblError.Visible = true;
                return;
            }

            try
            {
                btnRegister.Enabled = false;
                btnRegister.Text = "Đang đăng ký...";
                lblError.Visible = false;

                // Kết nối nếu chưa kết nối
                SocketClient.Instance.Connect("localhost", 5000);

                // Gửi yêu cầu đăng ký
                AuthResponse result = SocketClient.Instance.Register(username, email, password);

                if (result.Success)
                {
                    MessageBox.Show("Đăng ký thành công!\nBạn có thể đăng nhập ngay.",
                        "Thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    SocketClient.Instance.Disconnect(); // Disconnect để LoginForm kết nối lại
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
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
                btnRegister.Enabled = true;
                btnRegister.Text = "Đăng ký";
            }
        }

        private void lnkBackToLogin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
        }

        // Cho phép nhấn Enter để submit
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
