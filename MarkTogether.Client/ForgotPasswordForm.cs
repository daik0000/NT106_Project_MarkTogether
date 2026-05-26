using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    public partial class ForgotPasswordForm : Form
    {
        private string _email;

        public ForgotPasswordForm()
        {
            InitializeComponent();
            Load += ForgotPasswordForm_Load;
        }

        private void ForgotPasswordForm_Load(object sender, EventArgs e)
        {
            BackColor = AppTheme.Background;
            UiFactory.StyleAsCard(pnlCard);
            UiFactory.StylePrimaryButton(btnSendOtp);
            UiFactory.StyleSuccessButton(btnResetPassword);
            UiFactory.StyleGhostButton(btnCancel);
            UiFactory.StyleInputPanel(pnlEmail);
            UiFactory.StyleInputPanel(pnlOtp);
            UiFactory.StyleInputPanel(pnlNewPassword);
            UiFactory.StyleInputPanel(pnlConfirmPassword);
            ShowStep1();
        }

        private void ShowStep1()
        {
            pnlStepEmail.Visible = true;
            pnlStepReset.Visible = false;
            lblMessage.Visible = false;
            AcceptButton = btnSendOtp;
            txtEmail.Focus();
        }

        private void ShowStep2()
        {
            pnlStepEmail.Visible = false;
            pnlStepReset.Visible = true;
            lblMaskedEmail.Text = $"Mã OTP đã được gửi tới {MaskEmail(_email)} nếu email tồn tại.";
            AcceptButton = btnResetPassword;
            txtOtp.Focus();
        }

        private async void btnSendOtp_Click(object sender, EventArgs e)
        {
            string email = (txtEmail.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                ShowMessage("Email không hợp lệ.", false);
                return;
            }

            try
            {
                btnSendOtp.Enabled = false;
                btnSendOtp.Text = "Đang gửi...";
                EnsureConnected();

                var resp = await Task.Run(() => SocketClient.Instance.RequestPasswordReset(email));
                if (!resp.Success)
                {
                    ShowMessage(resp.Message ?? "Không gửi được mã OTP.", false);
                    return;
                }

                _email = email;
                ShowMessage(resp.Message ?? "Nếu email tồn tại, mã OTP đã được gửi.", true);
                ShowStep2();
            }
            catch (Exception ex)
            {
                ShowMessage("Không thể gửi yêu cầu: " + ex.Message, false);
            }
            finally
            {
                btnSendOtp.Enabled = true;
                btnSendOtp.Text = "Gửi mã";
            }
        }

        private async void btnResetPassword_Click(object sender, EventArgs e)
        {
            string otp = (txtOtp.Text ?? string.Empty).Trim();
            string newPassword = txtNewPassword.Text ?? string.Empty;
            string confirm = txtConfirmPassword.Text ?? string.Empty;

            if (!Regex.IsMatch(otp, @"^\d{6}$"))
            {
                ShowMessage("OTP phải gồm đúng 6 chữ số.", false);
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowMessage("Mật khẩu phải có ít nhất 6 ký tự.", false);
                return;
            }

            if (newPassword != confirm)
            {
                ShowMessage("Mật khẩu xác nhận không khớp.", false);
                return;
            }

            try
            {
                btnResetPassword.Enabled = false;
                btnResetPassword.Text = "Đang đặt lại...";
                EnsureConnected();

                var resp = await Task.Run(() => SocketClient.Instance.ResetPassword(_email, otp, newPassword));
                if (!resp.Success)
                {
                    ShowMessage(resp.Message ?? "Đặt lại mật khẩu thất bại.", false);
                    return;
                }

                MessageBox.Show(resp.Message ?? "Đặt lại mật khẩu thành công.",
                    "Quên mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowMessage("Không thể đặt lại mật khẩu: " + ex.Message, false);
            }
            finally
            {
                btnResetPassword.Enabled = true;
                btnResetPassword.Text = "Đặt lại mật khẩu";
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void lnkBack_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowStep1();
        }

        private void txtOtp_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void EnsureConnected()
        {
            if (!SocketClient.Instance.IsLoggedIn)
            {
                try
                {
                    SocketClient.Instance.ConnectFromConfig();
                }
                catch
                {
                    // Connect() tự return nếu đã connected; lỗi thật sẽ được request báo lại.
                    throw;
                }
            }
        }

        private void ShowMessage(string message, bool success)
        {
            lblMessage.Text = "  " + message;
            lblMessage.ForeColor = success ? AppTheme.Success : System.Drawing.Color.FromArgb(0xEF, 0x44, 0x44);
            lblMessage.BackColor = success
                ? System.Drawing.Color.FromArgb(0xDC, 0xFC, 0xE7)
                : System.Drawing.Color.FromArgb(0xFE, 0xF2, 0xF2);
            lblMessage.Visible = true;
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return "***";
            string[] parts = email.Split('@');
            string name = parts[0];
            return name.Substring(0, Math.Min(2, name.Length)) + "***@" + parts[1];
        }
    }
}
