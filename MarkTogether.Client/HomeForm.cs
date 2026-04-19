using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class HomeForm : Form
    {
        private bool _isLoadingDocuments;
        private bool _isOpeningDocument;
        private readonly ImageList _rowHeightImageList;
        private List<DocInfo> _allDocuments = new List<DocInfo>();
        private readonly Font _headerFont;

        public HomeForm()
        {
            InitializeComponent();

            KeyPreview = true;
            KeyDown += HomeForm_KeyDown;

            _rowHeightImageList = new ImageList
            {
                ImageSize = new System.Drawing.Size(1, 24)
            };
            listDocuments.SmallImageList = _rowHeightImageList;

            _headerFont = new Font("Segoe UI", 10F, FontStyle.Bold);
            ApplyRoundedButtonStyle(btnCreateDocument, Color.DodgerBlue, Color.RoyalBlue, 12);
            ApplyRoundedButtonStyle(btnImportMd, Color.FromArgb(155, 89, 182), Color.FromArgb(142, 68, 173), 12);

            Resize += HomeForm_Resize;
            Shown += HomeForm_Shown;
        }

        private void HomeForm_Resize(object sender, EventArgs e)
        {
            ApplyButtonRoundedRegion(btnCreateDocument, 12);
            ApplyButtonRoundedRegion(btnImportMd, 12);
        }

        private async void HomeForm_Shown(object sender, EventArgs e)
        {
            lblWelcome.Text = $"Hello, {SocketClient.Instance.Username}";
            if (cmbSortMode.SelectedIndex < 0)
            {
                cmbSortMode.SelectedIndex = 0;
            }
            await LoadDocumentsAsync();
        }

        private async Task LoadDocumentsAsync()
        {
            if (_isLoadingDocuments)
                return;

            try
            {
                _isLoadingDocuments = true;
                ToggleLoadingState(true);

                var response = await System.Threading.Tasks.Task.Run(() => SocketClient.Instance.GetDocuments());
                _allDocuments = response?.documents ?? new List<DocInfo>();
                ApplySortAndBind();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load documents.\n\nDetails: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            string selectedSort = cmbSortMode.SelectedItem?.ToString() ?? "New to Old";

            switch (selectedSort)
            {
                case "Old to New":
                    docs = docs.OrderBy(d => d.updateAt);
                    break;
                case "A to Z":
                    docs = docs.OrderBy(d => d.title ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);
                    break;
                case "Z to A":
                    docs = docs.OrderByDescending(d => d.title ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);
                    break;
                default:
                    docs = docs.OrderByDescending(d => d.updateAt);
                    break;
            }

            BindDocuments(docs.ToList());
        }

        private void BindDocuments(List<DocInfo> docs)
        {
            listDocuments.BeginUpdate();
            listDocuments.Items.Clear();

            foreach (var doc in docs)
            {
                var item = new ListViewItem(doc.title ?? "(Untitled)");
                item.SubItems.Add(doc.updateAt == default(DateTime)
                    ? "-"
                    : doc.updateAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                item.SubItems.Add(string.IsNullOrWhiteSpace(doc.permission) ? "Owner" : doc.permission);
                item.Tag = doc;
                listDocuments.Items.Add(item);
            }

            listDocuments.EndUpdate();
            lblCount.Text = $"Total documents: {listDocuments.Items.Count}";
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

        private async void btnCreateDocument_Click(object sender, EventArgs e)
        {
            using (var dlg = new CreateDocumentForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ToggleLoadingState(true);
                    var created = await Task.Run(() => SocketClient.Instance.CreateDocument(dlg.DocumentTitle));
                    await LoadDocumentsAsync();

                    OpenDocumentEditor(created.docID, created.title, created.content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to create a new document.\n\nDetails: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
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
                openDialog.Title = "Choose a Markdown file to import";
                openDialog.Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*";
                openDialog.CheckFileExists = true;
                openDialog.Multiselect = false;

                if (openDialog.ShowDialog(this) != DialogResult.OK)
                    return;

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
                    MessageBox.Show($"Unable to import markdown file.\n\nDetails: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
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
            if (_isOpeningDocument)
                return;

            if (listDocuments.SelectedItems.Count == 0)
                return;

            var selectedDoc = listDocuments.SelectedItems[0].Tag as DocInfo;
            if (selectedDoc == null || string.IsNullOrWhiteSpace(selectedDoc.docID))
            {
                MessageBox.Show("Cannot identify the selected document.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show($"Unable to open document.\n\nDetails: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show("Invalid docID. Cannot open this document.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var editor = new TypeRenderForm(docId, title, content);
            editor.Show(this);
        }

        private void ToggleLoadingState(bool isLoading)
        {
            btnCreateDocument.Enabled = !isLoading;
            btnImportMd.Enabled = !isLoading;
            cmbSortMode.Enabled = !isLoading;
            listDocuments.Enabled = !isLoading;
        }

        private void listDocuments_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var gradientBrush = new LinearGradientBrush(
                e.Bounds,
                Color.FromArgb(198, 225, 255),
                Color.FromArgb(228, 241, 255),
                LinearGradientMode.Vertical))
            using (var borderPen = new Pen(Color.FromArgb(120, 166, 217)))
            using (var accentPen = new Pen(Color.FromArgb(70, 130, 180), 2f))
            {
                e.Graphics.FillRectangle(gradientBrush, e.Bounds);
                e.Graphics.DrawRectangle(borderPen, e.Bounds);
                e.Graphics.DrawLine(accentPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                _headerFont,
                e.Bounds,
                Color.FromArgb(20, 50, 90),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void ApplyRoundedButtonStyle(Button button, Color backColor, Color borderColor, int radius)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = borderColor;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;

            ApplyButtonRoundedRegion(button, radius);
            button.Resize += (s, e) => ApplyButtonRoundedRegion(button, radius);
        }

        private void ApplyButtonRoundedRegion(Button button, int radius)
        {
            if (button.Width <= 0 || button.Height <= 0)
                return;

            using (var path = CreateRoundedRectanglePath(button.ClientRectangle, radius))
            {
                button.Region = new Region(path);
            }
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, radius * 2);
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            var path = new GraphicsPath();

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void listDocuments_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listDocuments_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _headerFont?.Dispose();
            _rowHeightImageList?.Dispose();
            base.OnFormClosed(e);
        }

    }
}
