using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private TypeRenderForm _activeEditor;
        private string _activeEditorDocId;

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
            pnlJoin.Resize += (s, _) => LayoutJoinPanel();
            pnlListContainer.Resize += (s, _) => LayoutDocumentColumns();
            listDocuments.Resize += (s, _) => LayoutDocumentColumns();
            LayoutHeaderButtons();
            LayoutActionBarButtons();
            LayoutFilterBar();
            LayoutDocumentColumns();

            BuildDocumentContextMenu();
        }

        private void BuildDocumentContextMenu()
        {
            var menu = new ContextMenuStrip();
            var miDelete = new ToolStripMenuItem("Xóa tài liệu");
            miDelete.ShortcutKeyDisplayString = "Del";
            miDelete.Click += async (s, _) => await DeleteSelectedDocumentAsync();
            menu.Items.Add(miDelete);

            // Chỉ enable menu xóa khi item được chọn là owner — tránh user thao tác rồi bị server reject
            menu.Opening += (s, ev) =>
            {
                var doc = GetSelectedDocument();
                miDelete.Enabled = doc != null
                    && string.Equals(doc.permission, "owner", StringComparison.OrdinalIgnoreCase);
                if (doc == null) ev.Cancel = true;
            };

            listDocuments.ContextMenuStrip = menu;
        }

        private DocInfo GetSelectedDocument()
        {
            if (listDocuments.SelectedItems.Count == 0) return null;
            return listDocuments.SelectedItems[0].Tag as DocInfo;
        }

        private bool CanOpenAnotherDocument(string requestedDocId = null)
        {
            if (_activeEditor == null || _activeEditor.IsDisposed)
            {
                _activeEditor = null;
                _activeEditorDocId = null;
                return true;
            }

            if (_activeEditor.WindowState == FormWindowState.Minimized)
                _activeEditor.WindowState = FormWindowState.Normal;
            _activeEditor.Activate();
            _activeEditor.BringToFront();

            string message = string.Equals(_activeEditorDocId, requestedDocId, StringComparison.OrdinalIgnoreCase)
                ? "Tài liệu này đang được mở trong cửa sổ soạn thảo."
                : "Mỗi kết nối chỉ mở được một tài liệu. Hãy đóng cửa sổ soạn thảo đang mở trước khi mở tài liệu khác.";

            MessageBox.Show(message, "Tài liệu đang mở",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private async Task DeleteSelectedDocumentAsync()
        {
            var doc = GetSelectedDocument();
            if (doc == null || string.IsNullOrWhiteSpace(doc.docID)) return;

            if (!string.Equals(doc.permission, "owner", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Chỉ chủ sở hữu mới có thể xóa tài liệu này.",
                    "Xóa tài liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string title = string.IsNullOrWhiteSpace(doc.title) ? "(Không tiêu đề)" : doc.title;
            var confirm = MessageBox.Show(
                $"Xóa tài liệu \"{title}\"?\n\nThao tác này sẽ chuyển tài liệu vào trạng thái đã xóa và biến mất khỏi danh sách của bạn.",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            try
            {
                ToggleLoadingState(true);
                var resp = await Task.Run(() => SocketClient.Instance.DeleteDocument(doc.docID));
                if (resp == null || !resp.success)
                {
                    MessageBox.Show(resp?.message ?? "Xóa tài liệu thất bại.",
                        "Xóa tài liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                await LoadDocumentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa tài liệu.\n\nChi tiết: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToggleLoadingState(false);
            }
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
            LayoutDocumentColumns();
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
                return;
            }

            // Phím Delete: chỉ kích hoạt khi đang focus danh sách + có item được chọn
            // → tránh việc người dùng đang gõ trong ô join code vô tình xóa file.
            if (e.KeyCode == Keys.Delete && listDocuments.Focused && listDocuments.SelectedItems.Count > 0)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await DeleteSelectedDocumentAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Actions
        // ═══════════════════════════════════════════════════════════
        private async void btnCreateDocument_Click(object sender, EventArgs e)
        {
            if (!CanOpenAnotherDocument()) return;

            using (var dlg = new CreateDocumentForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    ToggleLoadingState(true);
                    var created = await Task.Run(() => SocketClient.Instance.CreateDocument(dlg.DocumentTitle));
                    await LoadDocumentsAsync();
                    OpenDocumentEditor(created.docID, created.title, created.content, "owner", created.revision);
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
            if (!CanOpenAnotherDocument()) return;

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
                    // Đọc UTF-8 và normalize line endings → \r\n. Lý do: txtRawMarkdown
                    // là System.Windows.Forms.TextBox multiline; nó KHÔNG hiển thị xuống dòng
                    // với '\n' đơn lẻ. File .md viết trên Linux/macOS (LF) sẽ bị dính liền
                    // nếu không normalize.
                    string content = await Task.Run(() =>
                    {
                        string raw = File.ReadAllText(filePath, Encoding.UTF8);
                        return raw.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                    });
                    string title = Path.GetFileNameWithoutExtension(filePath);
                    var created = await Task.Run(() => SocketClient.Instance.CreateDocument(title, content));
                    await LoadDocumentsAsync();
                    OpenDocumentEditor(created.docID, created.title, created.content, "owner", created.revision);
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

            if (!CanOpenAnotherDocument(selectedDoc.docID)) return;

            try
            {
                _isOpeningDocument = true;
                ToggleLoadingState(true);
                var opened = await Task.Run(() => SocketClient.Instance.OpenDocument(selectedDoc.docID));
                OpenDocumentEditor(opened.docID, opened.title, opened.content, opened.permission, opened.revision);
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

        private void OpenDocumentEditor(string docId, string title, string content, string permission = null, int revision = 0)
        {
            if (string.IsNullOrWhiteSpace(docId))
            {
                MessageBox.Show("Không thể mở tài liệu (thiếu mã ID).", "Lỗi");
                return;
            }
            if (!CanOpenAnotherDocument(docId)) return;

            var editor = new TypeRenderForm(docId, title, content, permission, revision);
            _activeEditor = editor;
            _activeEditorDocId = docId;
            editor.FormClosed += (s, e) =>
            {
                if (ReferenceEquals(_activeEditor, editor))
                {
                    _activeEditor = null;
                    _activeEditorDocId = null;
                }
            };
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
            if (!CanOpenAnotherDocument()) return;

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
                OpenDocumentEditor(resp.docID, resp.title, resp.content, resp.permission, resp.revision);
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

            int gap = AppTheme.SpaceMd;
            int right = pnlActionBar.ClientSize.Width;
            int btnY = 16;

            btnNew.Location = new System.Drawing.Point(right - btnNew.Width, btnY);
            btnImport.Location = new System.Drawing.Point(btnNew.Left - gap - btnImport.Width, btnY);

            int reservedForActions = Math.Max(0, pnlActionBar.ClientSize.Width - btnImport.Left + gap);
            int textMaxWidth = Math.Max(320, pnlActionBar.ClientSize.Width - reservedForActions - gap);
            lblPageTitle.MaximumSize = new System.Drawing.Size(textMaxWidth, 0);
            lblPageSubtitle.MaximumSize = new System.Drawing.Size(textMaxWidth, 0);

            int joinTop = 88;
            int joinMaxWidth = btnImport.Left - gap;
            int joinWidth = joinMaxWidth >= 520 ? joinMaxWidth : pnlActionBar.ClientSize.Width;

            pnlJoin.Location = new System.Drawing.Point(0, joinTop);
            pnlJoin.Size = new System.Drawing.Size(Math.Max(360, joinWidth), AppTheme.InputHeight + 24);
            LayoutJoinPanel();
        }

        private void LayoutJoinPanel()
        {
            if (pnlJoin.ClientSize.Width <= 0) return;

            int paddingX = AppTheme.SpaceLg;
            int paddingY = AppTheme.SpaceMd;
            int gap = AppTheme.SpaceMd;

            btnJoinCode.Location = new System.Drawing.Point(
                pnlJoin.ClientSize.Width - paddingX - btnJoinCode.Width,
                paddingY);

            int inputRight = btnJoinCode.Left - gap;
            int inputWidth = Math.Max(80, inputRight - paddingX);
            txtJoinCode.Location = new System.Drawing.Point(paddingX, paddingY);
            txtJoinCode.Size = new System.Drawing.Size(inputWidth, AppTheme.InputHeight);
        }

        private void LayoutFilterBar()
        {
            if (pnlFilter.ClientSize.Width <= 0) return;
            int right = pnlFilter.ClientSize.Width - AppTheme.SpaceMd;
            lblCount.Location = new System.Drawing.Point(
                right - lblCount.PreferredWidth,
                (pnlFilter.Height - lblCount.Height) / 2);
        }

        private void LayoutDocumentColumns()
        {
            if (listDocuments.ClientSize.Width <= 0 || listDocuments.Columns.Count < 3) return;

            int width = Math.Max(640, listDocuments.ClientSize.Width);
            int permissionWidth = 180;
            int updatedWidth = 260;
            int titleWidth = Math.Max(260, width - updatedWidth - permissionWidth);

            colTitle.Width = titleWidth;
            colUpdatedAt.Width = updatedWidth;
            colPermission.Width = permissionWidth;
        }
    }
}
