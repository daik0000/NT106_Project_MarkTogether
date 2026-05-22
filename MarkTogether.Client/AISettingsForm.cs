using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.Services;
using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    public partial class AISettingsForm : Form
    {
        public AISettingsForm()
        {
            InitializeComponent();

            BackColor = AppTheme.Surface;
            Font = AppTheme.Body;
            ForeColor = AppTheme.TextPrimary;

            UiFactory.StylePrimaryButton(btnSave);
            UiFactory.StyleSecondaryButton(btnTest);
            UiFactory.StyleSecondaryButton(btnToggleShow);
            UiFactory.StyleGhostButton(btnCancel);

            lblHint.ForeColor = AppTheme.TextSecondary;
            lblStatus.ForeColor = AppTheme.TextSecondary;
        }

        private void AISettingsForm_Load(object sender, EventArgs e)
        {
            cmbProvider.SelectedIndex = 0;

            cmbModel.Items.Clear();
            foreach (string model in AISettingsStore.SupportedModels)
                cmbModel.Items.Add(model);

            var settings = AISettingsStore.Load();
            int modelIndex = cmbModel.Items.IndexOf(settings.Model);
            cmbModel.SelectedIndex = modelIndex >= 0 ? modelIndex : 0;
            txtApiKey.Text = settings.ApiKey ?? "";
        }

        private void btnToggleShow_Click(object sender, EventArgs e)
        {
            txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
            btnToggleShow.Text = txtApiKey.UseSystemPasswordChar ? "Hiện" : "Ẩn";
        }

        private async void btnTest_Click(object sender, EventArgs e)
        {
            string apiKey = (txtApiKey.Text ?? "").Trim();
            string model = cmbModel.SelectedItem?.ToString() ?? "gemini-1.5-flash-latest";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                SetStatus("Vui lòng nhập API key trước khi test.", AppTheme.Error);
                return;
            }

            try
            {
                btnTest.Enabled = false;
                SetStatus("Đang test...", AppTheme.TextSecondary);

                var resp = await Task.Run(() =>
                    SocketClient.Instance.AskAI("chat", "ping", "", null, apiKey, model, "gemini"));

                if (resp != null && resp.success)
                    SetStatus("✓ OK", AppTheme.Success);
                else
                    SetStatus("✗ " + (resp?.message ?? "Không có phản hồi."), AppTheme.Error);
            }
            catch (Exception ex)
            {
                SetStatus("✗ " + ex.Message, AppTheme.Error);
            }
            finally
            {
                btnTest.Enabled = true;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            AISettingsStore.Save(new AISettings
            {
                Provider = "gemini",
                Model = cmbModel.SelectedItem?.ToString() ?? "gemini-1.5-flash-latest",
                ApiKey = (txtApiKey.Text ?? "").Trim()
            });

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = "Trạng thái: " + text;
            lblStatus.ForeColor = color;
        }
    }
}