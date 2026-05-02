using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class ShareManagementForm : Form
    {
        private readonly string _docId;
        private readonly string _docTitle;
        private bool _isInitialLoading = true;

        public ShareManagementForm(string docId, string docTitle)
        {
            InitializeComponent();
            _docId = docId;
            _docTitle = docTitle;
            this.Text = $"Quản lý chia sẻ - {_docTitle}";

            cmbNewPerm.SelectedIndex = 0; // Default to Viewer
            LoadShareData();
        }

        private async void LoadShareData()
        {
            try
            {
                _isInitialLoading = true;
                var info = await System.Threading.Tasks.Task.Run(() => SocketClient.Instance.GetShareInfo(_docId));

                lblShareCode.Text = info.shareCode ?? "N/A";
                chkPublic.Checked = info.isPublic;
                
                if (info.publicPermission?.ToLower() == "editor")
                    rdoPublicEditor.Checked = true;
                else
                    rdoPublicViewer.Checked = true;

                UpdatePublicControlsVisibility();

                dgvCollabs.Rows.Clear();
                if (info.collaborators != null)
                {
                    foreach (var c in info.collaborators)
                    {
                        int rowIndex = dgvCollabs.Rows.Add(c.username, c.email, c.permission, "Xóa quyền");
                        dgvCollabs.Rows[rowIndex].Tag = c.userId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải thông tin chia sẻ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isInitialLoading = false;
            }
        }

        private void UpdatePublicControlsVisibility()
        {
            bool isPublic = chkPublic.Checked;
            lblPublicPerm.Enabled = isPublic;
            rdoPublicViewer.Enabled = isPublic;
            rdoPublicEditor.Enabled = isPublic;
        }

        private void chkPublic_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePublicControlsVisibility();
        }

        private void btnCopyCode_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lblShareCode.Text))
            {
                Clipboard.SetText(lblShareCode.Text);
                MessageBox.Show("Đã copy mã chia sẻ vào Clipboard!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnSavePublic_Click(object sender, EventArgs e)
        {
            try
            {
                bool isPublic = chkPublic.Checked;
                string perm = rdoPublicEditor.Checked ? "editor" : "viewer";

                await System.Threading.Tasks.Task.Run(() => SocketClient.Instance.SetPublic(_docId, isPublic, perm));
                MessageBox.Show("Đã cập nhật cài đặt công khai thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnShare_Click(object sender, EventArgs e)
        {
            string username = txtNewUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Vui lòng nhập Username.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string permission = cmbNewPerm.SelectedItem.ToString().ToLower();

            try
            {
                await System.Threading.Tasks.Task.Run(() => SocketClient.Instance.ShareDocument(_docId, username, permission));
                MessageBox.Show($"Đã chia sẻ cho {username} thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNewUsername.Clear();
                LoadShareData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chia sẻ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void dgvCollabs_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isInitialLoading || e.RowIndex < 0 || e.ColumnIndex != 2) return;

            int targetUserId = (int)dgvCollabs.Rows[e.RowIndex].Tag;
            string newPerm = dgvCollabs.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

            try
            {
                await System.Threading.Tasks.Task.Run(() => SocketClient.Instance.UpdatePermission(_docId, targetUserId, newPerm));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật quyền: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadShareData(); // Revert on error
            }
        }

        private async void dgvCollabs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 3) return;

            string targetUsername = dgvCollabs.Rows[e.RowIndex].Cells[0].Value.ToString();
            int targetUserId = (int)dgvCollabs.Rows[e.RowIndex].Tag;

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa quyền truy cập của {targetUsername}?", 
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    await System.Threading.Tasks.Task.Run(() => SocketClient.Instance.RevokeAccess(_docId, targetUserId));
                    LoadShareData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi thu hồi quyền: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
