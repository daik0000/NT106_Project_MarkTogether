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

            pnlButtons.Resize += (s, e) => LayoutFooterButtons();
            listVersions.Resize += (s, e) => LayoutVersionColumns();
            split.Resize += (s, e) => LayoutVersionColumns();

            LayoutFooterButtons();
            LayoutVersionColumns();

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
                    string rawLabel = v.label ?? "";
                    string badge = "📝 Thủ công";
                    if (rawLabel.StartsWith("periodic:", StringComparison.OrdinalIgnoreCase))
                        badge = "⏱ Định kỳ";
                    else if (rawLabel.StartsWith("draft:", StringComparison.OrdinalIgnoreCase))
                        badge = "💾 Nháp";

                    var item = new ListViewItem(v.savedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"));
                    item.SubItems.Add(v.savedByUsername ?? "-");
                    item.SubItems.Add($"{badge} — {rawLabel}");
                    item.Tag = v.id;
                    listVersions.Items.Add(item);
                }

                lblCount.Text = $"Tổng: {resp.total} version (hiển thị {resp.versions.Count})";
                LayoutVersionColumns();
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

        private void LayoutFooterButtons()
        {
            if (pnlButtons.ClientSize.Width <= 0) return;

            int gap = AppTheme.SpaceMd;
            int y = (pnlButtons.ClientSize.Height - AppTheme.ButtonHeightSmall) / 2;
            int right = pnlButtons.ClientSize.Width;

            btnClose.Location = new System.Drawing.Point(right - btnClose.Width, y);
            right = btnClose.Left - gap;
            btnDelete.Location = new System.Drawing.Point(right - btnDelete.Width, y);
            right = btnDelete.Left - gap;
            btnRestore.Location = new System.Drawing.Point(right - btnRestore.Width, y);
            right = btnRestore.Left - gap;
            btnRefresh.Location = new System.Drawing.Point(right - btnRefresh.Width, y);

            lblCount.Location = new System.Drawing.Point(
                0,
                (pnlButtons.ClientSize.Height - lblCount.Height) / 2);
            lblCount.MaximumSize = new System.Drawing.Size(
                Math.Max(120, btnRefresh.Left - gap),
                0);
        }

        private void LayoutVersionColumns()
        {
            if (listVersions.ClientSize.Width <= 0 || listVersions.Columns.Count < 3) return;

            int width = Math.Max(320, listVersions.ClientSize.Width - 8);
            int timeWidth = 170;
            int userWidth = 130;
            int labelWidth = Math.Max(120, width - timeWidth - userWidth);

            colTime.Width = timeWidth;
            colUser.Width = userWidth;
            colLabel.Width = labelWidth;
        }
    }
}