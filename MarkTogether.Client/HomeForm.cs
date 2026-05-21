using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.UI;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class HomeForm : Form
    {
        private bool _isLoadingDocuments;
        private bool _isOpeningDocument;
        private List<DocInfo> _allDocuments = new List<DocInfo>();

        public HomeForm()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyDown += HomeForm_KeyDown;
            Load += HomeForm_Load;
            Shown += HomeForm_Shown;
        }

        private void HomeForm_Load(object sender, EventArgs e)
        {
            // ─── Apply theme/style ───
            UiFactory.StylePrimaryButton(btnNew);
            UiFactory.StyleSecondaryButton(btnImport);
            UiFactory.StylePrimaryButton(btnJoinCode);
            UiFactory.StyleDangerButton(btnLogout);

            // Header divider line
            pnlHeader.Paint += (s, ev) =>
            {
                using (var pen = new System.Drawing.Pen(AppTheme.Divider))
                    ev.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            // pnlJoin: rounded region + focus-aware border (single Paint handler, không dùng StyleAsCard để tránh double-paint)
            pnlJoin.BackColor = AppTheme.Surface;
            UiFactory.ApplyRoundedRegion(pnlJoin, AppTheme.CornerRadius);
            pnlJoin.Resize += (s, ev) => UiFactory.ApplyRoundedRegion(pnlJoin, AppTheme.CornerRadius);
            txtJoinCode.GotFocus += (s, ev) => { pnlJoin.Tag = "focus"; pnlJoin.Invalidate(); };
            txtJoinCode.LostFocus += (s, ev) => { pnlJoin.Tag = null; pnlJoin.Invalidate(); };
            pnlJoin.Paint += (s, ev) =>
            {
                bool focused = pnlJoin.Tag as string == "focus";
                UiFactory.DrawBorder(ev.Graphics, pnlJoin.ClientRectangle,
                    focused ? AppTheme.BorderFocus : AppTheme.Border,
                    AppTheme.CornerRadius);
            };

            // pnlListContainer as card
            UiFactory.StyleAsCard(pnlListContainer, AppTheme.CornerRadiusLg);

            // ListView styled
            UiFactory.StyleListView(listDocuments);

            cmbSortMode.SelectedIndex = 0;

            // Căn phải động — tránh Anchor-bug khi panel chưa được siz đúng lúc InitializeComponent
            pnlHeader.Resize += (s, _) => LayoutHeaderButtons();
            pnlActionBar.Resize += (s, _) => LayoutActionBarButtons();
            pnlFilter.Resize += (s, _) => LayoutFilterBar();
            LayoutHeaderButtons();
            LayoutActionBarButtons();
            LayoutFilterBar();
        }

        private async void HomeForm_Shown(object sender, EventArgs e)
        {
            string username = SocketClient.Instance.Username ?? "user";
            lblWelcome.Text = $"Xin chào, {username}";
            LayoutHeaderButtons();
            await LoadDocumentsAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  Load + bind
        // ═══════════════════════════════════════════════════════════
        private async Task LoadDocumentsAsync()
        {
            if (_isLoadingDocuments) return;
            try
            {
                _isLoadingDocuments = true;
                ToggleLoadingState(true);
                var response = await Task.Run(() => SocketClient.Instance.GetDocuments());
                _allDocuments = response?.documents ?? new List<DocInfo>();
                ApplySortAndBind();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách tài liệu.\n\nChi tiết: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isLoadingDocuments = false;
                ToggleLoadingState(false);
            }
        }

        private void ApplySortAndBind()
        {
            IEnumerable<DocInfo> docs = _allDocuments ?? new List<DocInfo>();
            string sort = cmbSortMode.SelectedItem?.ToString() ?? "Mới nhất";
            switch (sort)
            {
                case "Cũ nhất": docs = docs.OrderBy(d => d.updateAt); break;
                case "A → Z": docs = docs.OrderBy(d => d.title ?? "", StringComparer.CurrentCultureIgnoreCase); break;
                case "Z → A": docs = docs.OrderByDescending(d => d.title ?? "", StringComparer.CurrentCultureIgnoreCase); break;
                default: docs = docs.OrderByDescending(d => d.updateAt); break;
            }
            BindDocuments(docs.ToList());
        }

        private void BindDocuments(List<DocInfo> docs)
        {
            listDocuments.BeginUpdate();
            listDocuments.Items.Clear();
            foreach (var doc in docs)
            {
                var item = new ListViewItem(doc.title ?? "(Không tiêu đề)");
                item.SubItems.Add(doc.updateAt == default(DateTime)
                    ? "—"
                    : doc.updateAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                item.SubItems.Add(FormatPermission(doc.permission));
                item.Tag = doc;
                listDocuments.Items.Add(item);
            }
            listDocuments.EndUpdate();
            lblCount.Text = $"{listDocuments.Items.Count} tài liệu";
            LayoutFilterBar();
        }

        private static string FormatPermission(string p)
        {
            switch ((p ?? "").ToLowerInvariant())
            {
                case "owner": return "Chủ sở hữu";
                case "editor": return "Chỉnh sửa";
                case "viewer": return "Xem";
                default: return p ?? "—";
            }
        }

        private async void HomeForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoadDocumentsAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Actions
        // ═══════════════════════════════════════════════════════════
        private async void btnCreateDocument_Click(object sender, EventArgs e)
        {
            using (var dlg = new CreateDocumentForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    ToggleLoadingState(true);
                    var created = await Task.Run(() => SocketClient.Instance.CreateDocument(dlg.DocumentTitle));
                    await LoadDocumentsAsync();
                    OpenDocumentEditor(created.docID, created.title, created.content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể tạo tài liệu mới.\n\nChi tiết: {ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    ToggleLoadingState(false);
                }
            }
        }

        private async void btnImportMd_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Title = "Chọn file Markdown để import";
                openDialog.Filter = "Markdown (*.md)|*.md|Tất cả (*.*)|*.*";
                openDialog.CheckFileExists = true;
                if (openDialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    ToggleLoadingState(true);
                    string filePath = openDialog.FileName;
                    string content = await Task.Run(() => File.ReadAllText(filePath));
                    string title = Path.GetFileNameWithoutExtension(filePath);
                    var created = await Task.Run(() => SocketClient.Instance.CreateDocument(title, content));
                    await LoadDocumentsAsync();
                    OpenDocumentEditor(created.docID, created.title, created.content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể import file.\n\nChi tiết: {ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    ToggleLoadingState(false);
                }
            }
        }

        private void cmbSortMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySortAndBind();
        }

        private async void listDocuments_ItemActivate(object sender, EventArgs e)
        {
            await OpenSelectedDocumentAsync();
        }

        private async Task OpenSelectedDocumentAsync()
        {
            if (_isOpeningDocument) return;
            if (listDocuments.SelectedItems.Count == 0) return;

            var selectedDoc = listDocuments.SelectedItems[0].Tag as DocInfo;
            if (selectedDoc == null || string.IsNullOrWhiteSpace(selectedDoc.docID))
            {
                MessageBox.Show("Không xác định được tài liệu được chọn.", "Lỗi");
                return;
            }

            try
            {
                _isOpeningDocument = true;
                ToggleLoadingState(true);
                var opened = await Task.Run(() => SocketClient.Instance.OpenDocument(selectedDoc.docID));
                OpenDocumentEditor(opened.docID, opened.title, opened.content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở tài liệu.\n\nChi tiết: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isOpeningDocument = false;
                ToggleLoadingState(false);
            }
        }

        private void OpenDocumentEditor(string docId, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(docId))
            {
                MessageBox.Show("Không thể mở tài liệu (thiếu mã ID).", "Lỗi");
                return;
            }
            var editor = new TypeRenderForm(docId, title, content);
            editor.Show(this);
        }

        private void ToggleLoadingState(bool isLoading)
        {
            btnNew.Enabled = !isLoading;
            btnImport.Enabled = !isLoading;
            cmbSortMode.Enabled = !isLoading;
            listDocuments.Enabled = !isLoading;
            btnLogout.Enabled = !isLoading;
            btnJoinCode.Enabled = !isLoading;
            txtJoinCode.Enabled = !isLoading;
        }

        // ═══════════════════════════════════════════════════════════
        //  LOGOUT
        // ═══════════════════════════════════════════════════════════
        private async void btnLogout_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Đăng xuất",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                ToggleLoadingState(true);
                await Task.Run(() => SocketClient.Instance.Logout());
            }
            catch { }
            finally { ToggleLoadingState(false); }

            var login = new LoginForm();
            login.Show();
            this.Close();
        }

        // ═══════════════════════════════════════════════════════════
        //  JOIN BY CODE
        // ═══════════════════════════════════════════════════════════
        private async void btnJoinCode_Click(object sender, EventArgs e)
        {
            string code = (txtJoinCode.Text ?? "").Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Vui lòng nhập mã chia sẻ.", "Tham gia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ToggleLoadingState(true);
                var resp = await Task.Run(() => SocketClient.Instance.JoinByShareCode(code));
                if (!resp.success)
                {
                    MessageBox.Show(resp.message ?? "Không thể tham gia tài liệu.", "Tham gia",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                txtJoinCode.Clear();
                await LoadDocumentsAsync();
                OpenDocumentEditor(resp.docID, resp.title, resp.content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Tham gia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToggleLoadingState(false);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LAYOUT — căn phải động (tránh WinForms Anchor-bug với nested panel)
        // ═══════════════════════════════════════════════════════════
        private void LayoutHeaderButtons()
        {
            if (pnlHeader.ClientSize.Width <= 0) return;
            int right = pnlHeader.ClientSize.Width - AppTheme.SpaceXl;
            int btnY = (pnlHeader.Height - AppTheme.ButtonHeight) / 2;
            int gap = AppTheme.SpaceSm;

            btnLogout.Location = new System.Drawing.Point(right - btnLogout.Width, btnY);
            right -= btnLogout.Width + gap * 2;
            lblWelcome.Location = new System.Drawing.Point(
                Math.Max(lblBrand.Right + AppTheme.SpaceLg, right - lblWelcome.PreferredWidth),
                (pnlHeader.Height - lblWelcome.Height) / 2);
        }

        private void LayoutActionBarButtons()
        {
            if (pnlActionBar.ClientSize.Width <= 0) return;
            int right = pnlActionBar.ClientSize.Width;
            int btnY = 16;
            int gap = AppTheme.SpaceSm;

            btnNew.Location = new System.Drawing.Point(right - btnNew.Width, btnY);
            right -= btnNew.Width + gap;
            btnImport.Location = new System.Drawing.Point(right - btnImport.Width, btnY);
        }

        private void LayoutFilterBar()
        {
            if (pnlFilter.ClientSize.Width <= 0) return;
            int right = pnlFilter.ClientSize.Width - AppTheme.SpaceMd;
            lblCount.Location = new System.Drawing.Point(
                right - lblCount.PreferredWidth,
                (pnlFilter.Height - lblCount.Height) / 2);
        }
    }
}