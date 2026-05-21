using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.UI;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class ShareDocumentForm : Form
    {
        private readonly string _docId;
        private string _shareCode;

        public ShareDocumentForm(string docId, string initialShareCode)
        {
            InitializeComponent();
            _docId = docId;
            _shareCode = initialShareCode;

            txtShareCode.Text = _shareCode ?? "(chưa có)";
            cmbPermission.SelectedIndex = 0;

            // Apply theme
            UiFactory.StylePrimaryButton(btnInvite);
            UiFactory.StyleSecondaryButton(btnCopy);
            UiFactory.StyleSecondaryButton(btnRegen);
            UiFactory.StyleSecondaryButton(btnUpdatePerm);
            UiFactory.StyleDangerButton(btnRevoke);
            UiFactory.StyleGhostButton(btnClose);
            UiFactory.StyleListView(listCollab);
            UiFactory.StyleAsCard(pnlShareCode, AppTheme.CornerRadius);

            Shown += async (s, e) => await ReloadCollaboratorsAsync();
        }

        private async void btnRegen_Click(object sender, EventArgs e)
        {
            try
            {
                btnRegen.Enabled = false;
                var resp = await Task.Run(() => SocketClient.Instance.RegenerateShareCode(_docId));
                if (!resp.success)
                {
                    MessageBox.Show(resp.message ?? "Không tạo được mã.", "Mã chia sẻ");
                    return;
                }
                _shareCode = resp.shareCode;
                txtShareCode.Text = _shareCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Mã chia sẻ");
            }
            finally
            {
                btnRegen.Enabled = true;
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_shareCode)) return;
            try { Clipboard.SetText(_shareCode); }
            catch { }
            MessageBox.Show("Đã copy mã chia sẻ.", "Mã chia sẻ");
        }

        private async void btnInvite_Click(object sender, EventArgs e)
        {
            string username = (txtUsername.Text ?? "").Trim();
            string permission = (cmbPermission.SelectedItem?.ToString() ?? "viewer").ToLowerInvariant();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Vui lòng nhập username.", "Chia sẻ");
                return;
            }

            try
            {
                btnInvite.Enabled = false;
                var resp = await Task.Run(() =>
                    SocketClient.Instance.ShareDocument(_docId, username, permission));
                MessageBox.Show(resp.message ?? (resp.success ? "Thành công" : "Thất bại"), "Chia sẻ");
                if (resp.success)
                {
                    txtUsername.Clear();
                    await ReloadCollaboratorsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Chia sẻ");
            }
            finally
            {
                btnInvite.Enabled = true;
            }
        }

        private async Task ReloadCollaboratorsAsync()
        {
            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.ListCollaborators(_docId));
                listCollab.Items.Clear();
                if (resp == null || !resp.success || resp.collaborators == null) return;

                if (!string.IsNullOrEmpty(resp.shareCode))
                {
                    _shareCode = resp.shareCode;
                    txtShareCode.Text = _shareCode;
                }

                foreach (var c in resp.collaborators)
                {
                    var item = new ListViewItem(c.username ?? "");
                    item.SubItems.Add(c.email ?? "");
                    item.SubItems.Add(c.permission ?? "");
                    item.SubItems.Add(c.invitedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                    item.Tag = c;
                    listCollab.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được danh sách: {ex.Message}", "Chia sẻ");
            }
        }

        private async void btnUpdatePerm_Click(object sender, EventArgs e)
        {
            if (listCollab.SelectedItems.Count == 0) return;
            var c = listCollab.SelectedItems[0].Tag as CollaboratorDto;
            if (c == null) return;

            using (var dlg = new Form { Width = 320, Height = 160, Text = "Đổi quyền", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, BackColor = AppTheme.Surface, Font = AppTheme.Body })
            {
                var lbl = new Label { Text = $"Quyền mới cho {c.username}:", Left = 12, Top = 15, AutoSize = true, Font = AppTheme.BodyBold, ForeColor = AppTheme.TextPrimary };
                var cmb = new ComboBox { Left = 12, Top = 40, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.Body };
                cmb.Items.AddRange(new object[] { "viewer", "editor" });
                cmb.SelectedItem = c.permission == "editor" ? "editor" : "viewer";
                var btnOk = new Button { Text = "OK", Left = 130, Top = 80, Width = 75, Height = AppTheme.ButtonHeightSmall, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Hủy", Left = 215, Top = 80, Width = 75, Height = AppTheme.ButtonHeightSmall, DialogResult = DialogResult.Cancel };
                UiFactory.StylePrimaryButton(btnOk);
                UiFactory.StyleSecondaryButton(btnCancel);
                dlg.Controls.AddRange(new Control[] { lbl, cmb, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string newPerm = cmb.SelectedItem.ToString();
                try
                {
                    var resp = await Task.Run(() =>
                        SocketClient.Instance.UpdatePermission(_docId, c.userId, newPerm));
                    MessageBox.Show(resp.message ?? "OK", "Đổi quyền");
                    if (resp.success) await ReloadCollaboratorsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Đổi quyền");
                }
            }
        }

        private async void btnRevoke_Click(object sender, EventArgs e)
        {
            if (listCollab.SelectedItems.Count == 0) return;
            var c = listCollab.SelectedItems[0].Tag as CollaboratorDto;
            if (c == null) return;

            if (MessageBox.Show($"Thu hồi quyền của {c.username}?", "Thu hồi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.RevokeAccess(_docId, c.userId));
                MessageBox.Show(resp.message ?? "OK", "Thu hồi");
                if (resp.success) await ReloadCollaboratorsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Thu hồi");
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}