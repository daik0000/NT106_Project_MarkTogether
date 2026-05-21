using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    public partial class VersionHistoryForm : Form
    {
        private readonly string _docId;

        /// <summary>Set true nếu user đã restore — để form cha biết và reload doc.</summary>
        public bool DocumentWasRestored { get; private set; }

        public string RestoredContent { get; private set; }

        public VersionHistoryForm(string docId)
        {
            InitializeComponent();
            _docId = docId;

            BackColor = AppTheme.Background;
            UiFactory.StyleListView(listVersions);
            UiFactory.StyleSecondaryButton(btnRefresh);
            UiFactory.StyleSuccessButton(btnRestore);
            UiFactory.StyleDangerButton(btnDelete);
            UiFactory.StyleGhostButton(btnClose);
            UiFactory.StyleFooterPanel(pnlButtons);
            UiFactory.StyleSplitContainer(split);

            Shown += async (s, e) => await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.GetVersions(_docId, page: 1, limit: 100));
                listVersions.Items.Clear();
                if (resp?.versions == null) return;

                foreach (var v in resp.versions)
                {
                    var item = new ListViewItem(v.savedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"));
                    item.SubItems.Add(v.savedByUsername ?? "-");
                    item.SubItems.Add(v.label ?? "");
                    item.Tag = v.id;
                    listVersions.Items.Add(item);
                }

                lblCount.Text = $"Tổng: {resp.total} version (hiển thị {resp.versions.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được lịch sử: {ex.Message}", "Version");
            }
        }

        private async void listVersions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listVersions.SelectedItems.Count == 0)
            {
                txtPreview.Text = "";
                return;
            }

            string id = listVersions.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(id)) return;

            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.GetVersionDetail(id));
                if (!resp.success)
                {
                    txtPreview.Text = $"(Lỗi: {resp.message})";
                    return;
                }
                txtPreview.Text = resp.contentSnapshot ?? "";
            }
            catch (Exception ex)
            {
                txtPreview.Text = $"(Lỗi: {ex.Message})";
            }
        }

        private async void btnRestore_Click(object sender, EventArgs e)
        {
            if (listVersions.SelectedItems.Count == 0)
            {
                MessageBox.Show("Hãy chọn version cần khôi phục.", "Khôi phục");
                return;
            }
            string id = listVersions.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(id)) return;

            if (MessageBox.Show("Khôi phục version này sẽ ghi đè nội dung hiện tại. Tiếp tục?",
                "Khôi phục", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.RestoreVersion(_docId, id));
                if (!resp.success)
                {
                    MessageBox.Show(resp.message ?? "Khôi phục thất bại.", "Khôi phục");
                    return;
                }

                DocumentWasRestored = true;
                RestoredContent = resp.newContent;
                MessageBox.Show("Đã khôi phục thành công.", "Khôi phục");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Khôi phục");
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (listVersions.SelectedItems.Count == 0) return;
            string id = listVersions.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(id)) return;

            if (MessageBox.Show("Xoá version này?", "Xoá",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.DeleteVersion(id));
                MessageBox.Show(resp.message ?? "OK", "Xoá");
                if (resp.success) await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Xoá");
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await ReloadAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}