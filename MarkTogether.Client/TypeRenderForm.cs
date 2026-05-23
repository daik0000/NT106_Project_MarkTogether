using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Client.Services;
using MarkTogether.Client.UI;
using MarkTogether.Shared;
using Markdig;
using Newtonsoft.Json;

namespace MarkTogether.Client
{
    public partial class TypeRenderForm : Form
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly Timer _renderDebounceTimer;
        private bool _webViewReady;
        private string _pendingHtmlBody = string.Empty;
        private bool _isPreviewUpdating;
        private bool _hasPendingPreviewUpdate;
        private int _renderRequestVersion;
        private readonly string _docId;
        private readonly string _docTitle;
        private readonly string _initialContent;
        private readonly Timer _opFlushTimer;
        private readonly Timer _autosaveTimer;
        private readonly Timer _draftTimer;
        private string _lastMarkdownText = string.Empty;
        private PendingOpType? _pendingOpType;
        private int _pendingOpStartPos;
        private string _pendingOpText = string.Empty;
        private bool _trackRealtimeOps;
        private int _clientRevision;
        private string _permission = "viewer";
        private string _shareCode;
        private bool _suppressOpTracking;
        private bool _hasUnsavedChanges;
        private bool _isAutosaving;
        private bool _periodicEnabled;
        private int _periodicIntervalMs;
        private string _lastSavedContent = string.Empty;
        private readonly List<CommentDto> _comments = new List<CommentDto>();
        private readonly Stack<string> _aiUndoStack = new Stack<string>();
        private const int AiUndoMaxDepth = 10;

        // ─── Paste handling ────────────────────────────────────────
        private volatile bool _isPasting;
        private readonly object _pasteLock = new object();
        private readonly Queue<string> _pasteChunkQueue = new Queue<string>();
        private Task _pasteSendTask;

        private const int MaxCharsPerPacket = 5;
        private const int DefaultPeriodicAutosaveIntervalMs = 60000;
        private const int DraftSaveDebounceMs = 2000;
        private const int PasteThresholdChars = 20;

        private enum PendingOpType { Insert, Delete }

        private sealed class TextDelta
        {
            public int Position { get; set; }
            public string DeletedText { get; set; }
            public string InsertedText { get; set; }
        }

        private const string PreviewHtmlTemplate = "<!DOCTYPE html>" +
            "<html><head><meta charset='utf-8'/>" +
            "<meta name='viewport' content='width=device-width,initial-scale=1'/>" +
            "<link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github-dark.min.css'/>" +
            "<script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/highlight.min.js'></script>" +
            "<style>" +
            "*{box-sizing:border-box}" +
            "html,body{background:#fff !important;color:#1a202c !important}" +
            "html{color-scheme:light}" +
            "body{font-family:-apple-system,'Segoe UI Variable Text','Segoe UI',system-ui,Arial,sans-serif;" +
            "font-size:15px;line-height:1.75;padding:32px 40px;margin:0 auto;max-width:820px}" +
            "h1{font-size:2em;font-weight:700;border-bottom:2px solid #e2e8f0;padding-bottom:.4em;" +
            "margin:1.5em 0 .75em;color:#0f172a;letter-spacing:-.02em}" +
            "h2{font-size:1.5em;font-weight:700;border-bottom:1px solid #e2e8f0;padding-bottom:.3em;" +
            "margin:1.5em 0 .75em;color:#1e293b}" +
            "h3{font-size:1.25em;font-weight:600;margin:1.25em 0 .5em;color:#1e293b}" +
            "h4,h5,h6{font-weight:600;margin:1em 0 .5em;color:#475569}" +
            "p{margin:0 0 1em}" +
            "a{color:#2563eb;text-decoration:none}" +
            "a:hover{text-decoration:underline}" +
            "strong{font-weight:700;color:#0f172a}" +
            "em{font-style:italic;color:#334155}" +
            "code{font-family:'Cascadia Code','Fira Code',Consolas,'Courier New',monospace;" +
            "font-size:.875em;background:#f1f5f9;color:#be123c;" +
            "padding:2px 6px;border-radius:5px;border:1px solid #e2e8f0}" +
            "pre{background:#0f172a !important;padding:18px 22px;border-radius:12px;overflow:auto;" +
            "margin:1.25em 0;font-size:.875em;line-height:1.65;" +
            "box-shadow:0 4px 16px rgba(0,0,0,.25);border:1px solid #1e293b}" +
            "pre code{background:transparent !important;color:inherit;padding:0;border:none;border-radius:0;font-size:1em}" +
            "blockquote{border-left:4px solid #2563eb;background:#eff6ff;" +
            "margin:1.25em 0;padding:14px 20px;color:#1e40af;border-radius:0 10px 10px 0}" +
            "blockquote p{margin:0}" +
            "ul,ol{padding-left:1.75em;margin:.5em 0 1em}" +
            "li{margin:.3em 0}" +
            "li>ul,li>ol{margin:.25em 0}" +
            "table{border-collapse:collapse;width:100%;margin:1.25em 0;" +
            "border-radius:10px;overflow:hidden;border:1px solid #e2e8f0}" +
            "thead{background:#f8fafc}" +
            "th{padding:10px 16px;text-align:left;font-weight:600;color:#475569;" +
            "font-size:.875em;text-transform:uppercase;letter-spacing:.04em;border-bottom:2px solid #e2e8f0}" +
            "td{padding:10px 16px;border-bottom:1px solid #f1f5f9;color:#374151}" +
            "tr:last-child td{border-bottom:none}" +
            "tbody tr:hover td{background:#f8fafc}" +
            "hr{border:none;border-top:1px solid #e2e8f0;margin:2em 0}" +
            "img{max-width:100%;border-radius:10px;box-shadow:0 2px 12px rgba(0,0,0,.1);margin:.5em 0}" +
            "::-webkit-scrollbar{width:6px;height:6px}" +
            "::-webkit-scrollbar-track{background:transparent}" +
            "::-webkit-scrollbar-thumb{background:#cbd5e1;border-radius:3px}" +
            "::-webkit-scrollbar-thumb:hover{background:#94a3b8}" +
            "@media print{body{padding:0;max-width:none}" +
            "pre,blockquote,table{page-break-inside:avoid}" +
            "h1,h2,h3{page-break-after:avoid}}" +
            "</style>" +
            "<script>" +
            "window.updatePreviewFromBase64=function(base64){" +
            "var body=document.body;var html='';" +
            "if(base64){try{html=decodeURIComponent(escape(window.atob(base64)));}catch(e){html='';}}" +
            "body.innerHTML=html;" +
            "if(window.hljs){body.querySelectorAll('pre code').forEach(function(b){hljs.highlightElement(b);});}" +
            "};" +
            "</script>" +
            "</head><body></body></html>";

        public TypeRenderForm() : this(null, null, null) { }

        public TypeRenderForm(string docId, string title, string initialContent)
        {
            InitializeComponent();

            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            _renderDebounceTimer = new Timer { Interval = 280 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            _opFlushTimer = new Timer { Interval = 250 };
            _opFlushTimer.Tick += OpFlushTimer_Tick;

            _autosaveTimer = new Timer { Interval = DefaultPeriodicAutosaveIntervalMs };
            _autosaveTimer.Tick += AutosaveTimer_Tick;

            _draftTimer = new Timer { Interval = DraftSaveDebounceMs };
            _draftTimer.Tick += DraftTimer_Tick;
            _periodicIntervalMs = DefaultPeriodicAutosaveIntervalMs;

            webPreview.TabStop = false;
            txtRawMarkdown.ContextMenu = new ContextMenu();

            Shown += TypeRenderForm_Shown;

            _docId = docId;
            _docTitle = title;
            _initialContent = initialContent;

            cmbAiMode.SelectedIndex = 0;

            ApplyInitialDocumentState();
            ApplyTheme();

            _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
            _lastSavedContent = _lastMarkdownText;
            _trackRealtimeOps = !string.IsNullOrWhiteSpace(_docId);
            ApplyPeriodicTimer();

            // Subscribe push events
            if (!string.IsNullOrWhiteSpace(_docId))
            {
                SocketClient.Instance.OnOpBroadcast += HandleOpBroadcast;
                SocketClient.Instance.OnChatBroadcast += HandleChatBroadcast;
                SocketClient.Instance.OnCommentBroadcast += HandleCommentBroadcast;
                SocketClient.Instance.OnDocReloadBroadcast += HandleDocReloadBroadcast;
            }

            RenderMarkdown();
        }

        private void ApplyInitialDocumentState()
        {
            if (!string.IsNullOrWhiteSpace(_docId))
            {
                Text = string.IsNullOrWhiteSpace(_docTitle)
                    ? "MarkTogether - Tài liệu"
                    : $"MarkTogether - {_docTitle}";
                txtRawMarkdown.Text = _initialContent ?? string.Empty;
                return;
            }

            txtRawMarkdown.Text =
                "# MarkTogether\r\n\r\n" +
                "Chào mừng bạn đến với **Type Render**.\r\n\r\n" +
                "- Gõ markdown ở khung bên trái\r\n" +
                "- Khung bên phải sẽ render tương ứng\r\n";
        }

        private async void TypeRenderForm_Shown(object sender, EventArgs e)
        {
            await InitializeWebViewAsync();
            QueuePreviewUpdate();

            // Đồng bộ permission + share code từ DOC_OPEN response (nếu được mở từ HomeForm
            // thì hiện đã có content. Gọi lại OpenDocument để chắc chắn join room + lấy permission.)
            if (!string.IsNullOrWhiteSpace(_docId))
            {
                try
                {
                    var info = await Task.Run(() => SocketClient.Instance.OpenDocument(_docId));
                    _permission = info?.permission ?? "viewer";
                    _shareCode = info?.shareCode;
                    UpdatePermissionUi();
                    await LoadChatHistoryAsync();
                    await LoadCommentsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không lấy được thông tin tài liệu: {ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void UpdatePermissionUi()
        {
            string label;
            System.Drawing.Color fg, bg;
            switch ((_permission ?? "viewer").ToLowerInvariant())
            {
                case "owner":
                    label = "Chủ sở hữu";
                    fg = AppTheme.Success;
                    bg = System.Drawing.Color.FromArgb(0xDC, 0xFC, 0xE7);
                    break;
                case "editor":
                    label = "Chỉnh sửa";
                    fg = AppTheme.Primary;
                    bg = System.Drawing.Color.FromArgb(0xDB, 0xEA, 0xFE);
                    break;
                default:
                    label = "Chỉ xem";
                    fg = AppTheme.TextSecondary;
                    bg = System.Drawing.Color.FromArgb(0xF1, 0xF5, 0xF9);
                    break;
            }
            lblPermissionBadge.Text = label;
            lblPermissionBadge.ForeColor = fg;
            lblPermissionBadge.BackColor = bg;
            UiFactory.ApplyRoundedRegion(lblPermissionBadge, AppTheme.CornerRadius);

            bool canEdit = _permission == "owner" || _permission == "editor";
            txtRawMarkdown.ReadOnly = !canEdit;
            btnSave.Enabled = canEdit;
            btnInsertImage.Enabled = canEdit;
            btnVersions.Enabled = canEdit;
            btnShare.Enabled = _permission == "owner";

            // Doc title
            if (!string.IsNullOrWhiteSpace(_docTitle))
                lblDocTitle.Text = _docTitle;
        }

        // ═══════════════════════════════════════════════════════════
        //  Theme & layout
        // ═══════════════════════════════════════════════════════════
        private void ApplyTheme()
        {
            BackColor = AppTheme.Background;

            UiFactory.StyleSecondaryButton(btnBack);
            UiFactory.StylePrimaryButton(btnSave);
            UiFactory.StyleSuccessButton(btnShare);
            UiFactory.StyleSecondaryButton(btnVersions);
            UiFactory.StyleSecondaryButton(btnExportPdf);
            UiFactory.StyleSecondaryButton(btnAddComment);
            UiFactory.StyleSecondaryButton(btnInsertImage);
            UiFactory.StyleGhostButton(btnToggleSide);

            UiFactory.StyleAsCard(pnlEditor, AppTheme.CornerRadiusLg);
            UiFactory.StyleAsCard(pnlPreview, AppTheme.CornerRadiusLg);

            // Header divider
            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(AppTheme.Divider))
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            // Style chat / comments / AI buttons
            UiFactory.StylePrimaryButton(btnChatSend);
            UiFactory.StyleSecondaryButton(btnCommentResolve);
            UiFactory.StyleDangerButton(btnCommentDelete);
            UiFactory.StyleGhostButton(btnCommentRefresh);
            UiFactory.StylePrimaryButton(btnAiSend);

            UiFactory.ApplyRoundedRegion(lblPermissionBadge, AppTheme.CornerRadius);
            lblPermissionBadge.Resize += (s, ev) =>
                UiFactory.ApplyRoundedRegion(lblPermissionBadge, AppTheme.CornerRadius);

            // Bottom divider for section headers (editor & preview labels)
            lblRaw.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(AppTheme.Border))
                    e.Graphics.DrawLine(pen, 0, lblRaw.Height - 1, lblRaw.Width, lblRaw.Height - 1);
            };
            lblPreview.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(AppTheme.Border))
                    e.Graphics.DrawLine(pen, 0, lblPreview.Height - 1, lblPreview.Width, lblPreview.Height - 1);
            };

            // Layout toolbar buttons (right-aligned chain)
            pnlHeader.Resize += (s, e) => LayoutToolbarButtons();
            LayoutToolbarButtons();

            // Style chat input border
            txtChatInput.Resize += (s, e) => UiFactory.ApplyRoundedRegion(txtChatInput, AppTheme.CornerRadius);
            UiFactory.ApplyRoundedRegion(txtChatInput, AppTheme.CornerRadius);

            // Modern flat tab headers with primary underline for selected tab
            tabSide.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tabSide.DrawItem += (s, e) => DrawSideTabItem(e);
            tabSide.Padding = new System.Drawing.Point(16, 6);

            // Two-line chat item layout
            lstChat.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            lstChat.ItemHeight = 50;
            lstChat.DrawItem += (s, e) => DrawChatListItem(e);

            // Comment item with resolved indicator
            lstComments.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            lstComments.ItemHeight = 48;
            lstComments.DrawItem += (s, e) => DrawCommentListItem(e);
        }

        private void LayoutToolbarButtons()
        {
            int padding = AppTheme.SpaceLg;
            int top = 12;
            int gap = AppTheme.SpaceSm;
            int x = pnlHeader.ClientSize.Width - padding;

            // Right to left
            x -= btnToggleSide.Width;
            btnToggleSide.Location = new System.Drawing.Point(x, top);
            x -= gap + btnSave.Width;
            btnSave.Location = new System.Drawing.Point(x, top);
            x -= gap + btnShare.Width;
            btnShare.Location = new System.Drawing.Point(x, top);
            x -= gap + btnVersions.Width;
            btnVersions.Location = new System.Drawing.Point(x, top);
            x -= gap + btnExportPdf.Width;
            btnExportPdf.Location = new System.Drawing.Point(x, top);
            x -= gap + btnAddComment.Width;
            btnAddComment.Location = new System.Drawing.Point(x, top);
            x -= gap + btnInsertImage.Width;
            btnInsertImage.Location = new System.Drawing.Point(x, top);
            x -= gap + cmbAutosaveInterval.Width;
            cmbAutosaveInterval.Location = new System.Drawing.Point(x, top);
            x -= gap + chkPeriodicAutosave.Width;
            chkPeriodicAutosave.Location = new System.Drawing.Point(x, top + 8);

            // Permission badge gần title
            lblPermissionBadge.Location = new System.Drawing.Point(
                lblDocTitle.Right + AppTheme.SpaceMd,
                lblDocTitle.Top + 4);
        }

        private void DrawSideTabItem(DrawItemEventArgs e)
        {
            bool selected = tabSide.SelectedIndex == e.Index;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var br = new System.Drawing.SolidBrush(selected ? AppTheme.Surface : AppTheme.Background))
                g.FillRectangle(br, e.Bounds);
            if (selected)
            {
                using (var br = new System.Drawing.SolidBrush(AppTheme.Primary))
                    g.FillRectangle(br, e.Bounds.X + 2, e.Bounds.Bottom - 3, e.Bounds.Width - 4, 3);
            }
            string text = tabSide.TabPages[e.Index].Text;
            var textColor = selected ? AppTheme.Primary : AppTheme.TextSecondary;
            var font = selected ? AppTheme.BodyBold : AppTheme.Body;
            TextRenderer.DrawText(g, text, font, e.Bounds, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DrawChatListItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstChat.Items.Count) return;
            var g = e.Graphics;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            var bgColor = selected ? AppTheme.SelectedFill
                : (e.Index % 2 == 0 ? AppTheme.Surface : AppTheme.Background);
            using (var br = new System.Drawing.SolidBrush(bgColor)) g.FillRectangle(br, e.Bounds);

            string raw = lstChat.Items[e.Index]?.ToString() ?? "";
            string time = "", user = "", msg = raw;
            if (raw.StartsWith("[") && raw.Contains("]"))
            {
                int ci = raw.IndexOf(']');
                time = raw.Substring(1, ci - 1);
                string rest = raw.Substring(ci + 2).Trim();
                int sep = rest.IndexOf(':');
                if (sep > 0) { user = rest.Substring(0, sep); msg = rest.Substring(sep + 1).Trim(); }
                else msg = rest;
            }

            int lp = AppTheme.SpaceLg, y1 = e.Bounds.Y + 6, y2 = e.Bounds.Y + 28;
            TextRenderer.DrawText(g, user, AppTheme.BodyBold,
                new System.Drawing.Rectangle(e.Bounds.X + lp, y1, 160, 18),
                AppTheme.Primary, TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, time, AppTheme.Caption,
                new System.Drawing.Rectangle(e.Bounds.X + lp + 168, y1 + 2, 110, 14),
                AppTheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, msg, AppTheme.Body,
                new System.Drawing.Rectangle(e.Bounds.X + lp, y2, e.Bounds.Width - lp * 2, 18),
                AppTheme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            using (var pen = new System.Drawing.Pen(AppTheme.Divider))
                g.DrawLine(pen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void DrawCommentListItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstComments.Items.Count) return;
            var g = e.Graphics;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            var bgColor = selected ? AppTheme.SelectedFill : AppTheme.Surface;
            using (var br = new System.Drawing.SolidBrush(bgColor)) g.FillRectangle(br, e.Bounds);

            string raw = lstComments.Items[e.Index]?.ToString() ?? "";
            bool resolved = raw.StartsWith("✓");
            var accentColor = resolved ? AppTheme.Success : AppTheme.Primary;
            using (var br = new System.Drawing.SolidBrush(accentColor))
                g.FillRectangle(br, e.Bounds.X, e.Bounds.Y + 6, 3, e.Bounds.Height - 12);

            int lp = AppTheme.SpaceLg + 4;
            var textColor = resolved ? AppTheme.TextSecondary : AppTheme.TextPrimary;
            TextRenderer.DrawText(g, raw, AppTheme.Body,
                new System.Drawing.Rectangle(e.Bounds.X + lp, e.Bounds.Y + 4, e.Bounds.Width - lp - AppTheme.SpaceSm, e.Bounds.Height - 8),
                textColor, TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using (var pen = new System.Drawing.Pen(AppTheme.Divider))
                g.DrawLine(pen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async Task InitializeWebViewAsync()
        {
            if (_webViewReady) return;
            await webPreview.EnsureCoreWebView2Async();
            webPreview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webPreview.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webPreview.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webPreview.CoreWebView2.Settings.IsZoomControlEnabled = true;
            webPreview.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webPreview.NavigateToString(PreviewHtmlTemplate);
        }

        private void CoreWebView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;
            _webViewReady = true;
            QueuePreviewUpdate();
        }

        private void txtRawMarkdown_TextChanged(object sender, EventArgs e)
        {
            // Bọc toàn bộ handler để bug bất kỳ trong delta/queue không làm crash WinForms message pump.
            try
            {
                if (!_suppressOpTracking)
                {
                    TrackRealtimeEditOps(txtRawMarkdown.Text ?? string.Empty);
                }
                else
                {
                    _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
                }

                if (!_suppressOpTracking && !string.IsNullOrWhiteSpace(_docId))
                {
                    _hasUnsavedChanges = (txtRawMarkdown.Text ?? string.Empty) != _lastSavedContent;
                }

                if (!_suppressOpTracking && _hasUnsavedChanges && !string.IsNullOrWhiteSpace(_docId)
                    && (_permission == "owner" || _permission == "editor"))
                {
                    _draftTimer.Stop();
                    _draftTimer.Start();
                }

                _renderRequestVersion++;
                _renderDebounceTimer.Stop();
                _renderDebounceTimer.Start();
            }
            catch (Exception ex)
            {
                // KHÔNG log nội dung văn bản — chỉ log độ dài + exception để chẩn đoán
                Logger.Log($"[Editor] TextChanged exception (textLen={(txtRawMarkdown.Text ?? string.Empty).Length}): {ex.GetType().Name}: {ex.Message}");
            }
        }

        private async void AutosaveTimer_Tick(object sender, EventArgs e)
        {
            if (_isAutosaving || !_periodicEnabled || !_hasUnsavedChanges || string.IsNullOrWhiteSpace(_docId))
                return;

            bool canEdit = _permission == "owner" || _permission == "editor";
            if (!canEdit || !SocketClient.Instance.IsLoggedIn)
                return;

            string content = txtRawMarkdown.Text ?? string.Empty;
            if (content == _lastSavedContent)
            {
                _hasUnsavedChanges = false;
                return;
            }

            _isAutosaving = true;
            try
            {
                await Task.Run(() => SocketClient.Instance.SaveDocument(
                    _docId, content, "periodic", _periodicIntervalMs / 60000));
                _lastSavedContent = content;
                _hasUnsavedChanges = false;
                Text = $"MarkTogether - {_docTitle} (tự lưu định kỳ lúc {DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                Text = $"MarkTogether - {_docTitle} (tự lưu lỗi: {ex.Message})";
            }
            finally
            {
                _isAutosaving = false;
            }
        }

        private async void DraftTimer_Tick(object sender, EventArgs e)
        {
            _draftTimer.Stop();

            if (!_hasUnsavedChanges || string.IsNullOrWhiteSpace(_docId))
                return;

            bool canEdit = _permission == "owner" || _permission == "editor";
            if (!canEdit || !SocketClient.Instance.IsLoggedIn)
                return;

            try
            {
                string content = txtRawMarkdown.Text ?? string.Empty;
                await Task.Run(() => SocketClient.Instance.SaveDocument(_docId, content, "draft"));
                // Draft không phải save chính thức: không reset _hasUnsavedChanges / _lastSavedContent.
            }
            catch
            {
                // Draft autosave chạy nền, không hiển thị lỗi để tránh làm gián đoạn người dùng.
            }
        }

        private void TrackRealtimeEditOps(string newText)
        {
            string oldText = _lastMarkdownText ?? string.Empty;
            if (newText == oldText) return;

            if (!_trackRealtimeOps || string.IsNullOrWhiteSpace(_docId) || !SocketClient.Instance.IsLoggedIn)
            {
                _lastMarkdownText = newText;
                return;
            }

            var delta = ComputeTextDelta(oldText, newText);
            if (delta == null)
            {
                FlushPendingEditOperation();
                _lastMarkdownText = newText;
                return;
            }

            if (!string.IsNullOrEmpty(delta.DeletedText) && !string.IsNullOrEmpty(delta.InsertedText))
            {
                FlushPendingEditOperation();
                QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText);
                FlushPendingEditOperation();
                QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText);
                FlushPendingEditOperation();
            }
            else if (!string.IsNullOrEmpty(delta.InsertedText))
            {
                QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText);
            }
            else if (!string.IsNullOrEmpty(delta.DeletedText))
            {
                QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText);
            }

            _lastMarkdownText = newText;
        }

        private TextDelta ComputeTextDelta(string oldText, string newText)
        {
            int prefix = 0;
            int minLen = Math.Min(oldText.Length, newText.Length);
            while (prefix < minLen && oldText[prefix] == newText[prefix]) prefix++;

            int oldSuffix = oldText.Length - 1;
            int newSuffix = newText.Length - 1;
            while (oldSuffix >= prefix && newSuffix >= prefix && oldText[oldSuffix] == newText[newSuffix])
            {
                oldSuffix--;
                newSuffix--;
            }

            string deleted = oldSuffix >= prefix ? oldText.Substring(prefix, oldSuffix - prefix + 1) : string.Empty;
            string inserted = newSuffix >= prefix ? newText.Substring(prefix, newSuffix - prefix + 1) : string.Empty;

            return new TextDelta { Position = prefix, DeletedText = deleted, InsertedText = inserted };
        }

        private void QueueOperation(PendingOpType opType, int position, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_pendingOpType.HasValue && _pendingOpType.Value != opType)
                FlushPendingEditOperation();

            if (!_pendingOpType.HasValue)
            {
                _pendingOpType = opType;
                _pendingOpStartPos = position;
                _pendingOpText = string.Empty;
            }

            if (!CanMergePending(opType, position, text))
            {
                FlushPendingEditOperation();
                _pendingOpType = opType;
                _pendingOpStartPos = position;
                _pendingOpText = string.Empty;
            }

            MergeIntoPending(opType, position, text);
            SendPendingChunksIfNeeded();
            RestartOpFlushTimer();
        }

        private bool CanMergePending(PendingOpType opType, int position, string text)
        {
            if (!_pendingOpType.HasValue || string.IsNullOrEmpty(_pendingOpText)) return true;
            if (_pendingOpType.Value != opType) return false;

            if (opType == PendingOpType.Insert)
                return position == _pendingOpStartPos + _pendingOpText.Length;

            if (position == _pendingOpStartPos) return true;
            return position + text.Length == _pendingOpStartPos;
        }

        private void MergeIntoPending(PendingOpType opType, int position, string text)
        {
            if (opType == PendingOpType.Insert) { _pendingOpText += text; return; }

            if (position + text.Length == _pendingOpStartPos)
            {
                _pendingOpText = text + _pendingOpText;
                _pendingOpStartPos = position;
            }
            else
            {
                _pendingOpText += text;
            }
        }

        private void SendPendingChunksIfNeeded()
        {
            while (_pendingOpType.HasValue && _pendingOpText.Length >= MaxCharsPerPacket)
            {
                string chunk = _pendingOpText.Substring(0, MaxCharsPerPacket);
                SendCurrentPendingChunk(chunk);
                _pendingOpText = _pendingOpText.Substring(MaxCharsPerPacket);
                // Chỉ tăng position cho insert; delete luôn xóa tại cùng vị trí
                if (_pendingOpType.Value == PendingOpType.Insert)
                    _pendingOpStartPos += MaxCharsPerPacket;
            }
        }

        private void OpFlushTimer_Tick(object sender, EventArgs e)
        {
            _opFlushTimer.Stop();
            FlushPendingEditOperation();
        }

        private void RestartOpFlushTimer()
        {
            _opFlushTimer.Stop();
            _opFlushTimer.Start();
        }

        private void FlushPendingEditOperation()
        {
            _opFlushTimer.Stop();
            while (_pendingOpType.HasValue && !string.IsNullOrEmpty(_pendingOpText))
            {
                int size = Math.Min(MaxCharsPerPacket, _pendingOpText.Length);
                string chunk = _pendingOpText.Substring(0, size);
                SendCurrentPendingChunk(chunk);
                _pendingOpText = _pendingOpText.Substring(size);
                // Chỉ tăng position cho insert; delete luôn xóa tại cùng vị trí
                if (_pendingOpType.Value == PendingOpType.Insert)
                    _pendingOpStartPos += size;
            }
            _pendingOpType = null;
            _pendingOpText = string.Empty;
        }

        private void SendCurrentPendingChunk(string chunk)
        {
            if (!_pendingOpType.HasValue || string.IsNullOrEmpty(chunk) || string.IsNullOrWhiteSpace(_docId)) return;
            if (!_trackRealtimeOps || !SocketClient.Instance.IsLoggedIn) return;
            if (_permission != "owner" && _permission != "editor") return;

            lock (_pasteLock)
            {
                char op = _pendingOpType.Value == PendingOpType.Insert ? 'I' : 'D';
                _pasteChunkQueue.Enqueue($"{op}|{_pendingOpStartPos}|{chunk}");
            }
            EnsurePasteSenderRunning();
        }

        // ═══════════════════════════════════════════════════════════
        //  Push handlers (chạy từ thread khác → dùng BeginInvoke)
        // ═══════════════════════════════════════════════════════════
        private void HandleOpBroadcast(Payload_OP_BROADCAST p)
        {
            if (p == null || p.docID != _docId || p.userID == SocketClient.Instance.UserId) return;
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke((Action)(() =>
            {
                try
                {
                    _suppressOpTracking = true;
                    int caret = txtRawMarkdown.SelectionStart;
                    string current = txtRawMarkdown.Text ?? string.Empty;

                    foreach (var op in p.ops ?? Enumerable.Empty<EditOpItem>())
                    {
                        if (op == null) continue;
                        int pos = Math.Max(0, Math.Min(op.pos, current.Length));
                        string text = op.text ?? "";
                        if (p.opType == "insert")
                        {
                            current = current.Insert(pos, text);
                            if (caret >= pos) caret += text.Length;
                        }
                        else // delete
                        {
                            int len = Math.Min(text.Length, current.Length - pos);
                            if (len > 0)
                            {
                                current = current.Remove(pos, len);
                                if (caret > pos) caret -= Math.Min(len, caret - pos);
                            }
                        }
                    }

                    txtRawMarkdown.Text = current;
                    txtRawMarkdown.SelectionStart = Math.Max(0, Math.Min(caret, current.Length));
                    _lastMarkdownText = current;
                }
                catch { }
                finally
                {
                    _suppressOpTracking = false;
                }
            }));
        }

        private void HandleChatBroadcast(Payload_CHAT_BROADCAST msg)
        {
            if (msg == null || msg.docID != _docId) return;
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke((Action)(() =>
            {
                AppendChatMessage(msg);
            }));
        }

        private void HandleCommentBroadcast(Payload_COMMENT_BROADCAST b)
        {
            if (b == null || b.comment == null || b.comment.docID != _docId) return;
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke((Action)(async () =>
            {
                await LoadCommentsAsync();
            }));
        }

        private void HandleDocReloadBroadcast(Payload_DOC_RELOAD_BROADCAST b)
        {
            if (b == null || b.docID != _docId) return;
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke((Action)(async () =>
            {
                try
                {
                    var info = await Task.Run(() => SocketClient.Instance.OpenDocument(_docId));
                    _suppressOpTracking = true;
                    txtRawMarkdown.Text = info.content ?? string.Empty;
                    _lastMarkdownText = txtRawMarkdown.Text;
                    _suppressOpTracking = false;

                    MessageBox.Show("Tài liệu vừa được khôi phục. Nội dung đã được tải lại.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            }));
        }

        // ═══════════════════════════════════════════════════════════
        //  Render preview
        // ═══════════════════════════════════════════════════════════
        private void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            RenderMarkdown();
        }

        private async void RenderMarkdown()
        {
            int requestVersion = _renderRequestVersion;
            string markdown = txtRawMarkdown.Text ?? string.Empty;
            int markdownLen = markdown.Length;
            string htmlBody;

            // Dùng Stopwatch (background-safe) để đo render. KHÔNG log nội dung văn bản
            // để tránh lộ dữ liệu — chỉ log độ dài + thời gian + exception nếu có.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                string renderMarkdown = await Task.Run(() => PrepareMarkdownImagesForPreview(markdown));
                htmlBody = await Task.Run(() => Markdown.ToHtml(renderMarkdown, _pipeline));
                sw.Stop();

                // Chỉ log khi render chậm để tránh nhiễu file log
                if (sw.ElapsedMilliseconds > 500)
                {
                    Logger.Log($"[Render] slow markdown render: {sw.ElapsedMilliseconds}ms textLen={markdownLen}");
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.Log($"[Render] markdown render FAILED after {sw.ElapsedMilliseconds}ms textLen={markdownLen}: {ex.GetType().Name}: {ex.Message}");
                return;
            }

            if (requestVersion != _renderRequestVersion || IsDisposed) return;

            _pendingHtmlBody = htmlBody;
            QueuePreviewUpdate();
        }

        private void QueuePreviewUpdate()
        {
            _hasPendingPreviewUpdate = true;
            if (_isPreviewUpdating || !_webViewReady || webPreview.CoreWebView2 == null) return;
            _ = ProcessPreviewQueueAsync();
        }

        private async Task ProcessPreviewQueueAsync()
        {
            _isPreviewUpdating = true;
            try
            {
                while (_hasPendingPreviewUpdate && _webViewReady && webPreview.CoreWebView2 != null && !IsDisposed)
                {
                    _hasPendingPreviewUpdate = false;
                    await UpdatePreviewBodyAsync(_pendingHtmlBody);
                }
            }
            finally
            {
                _isPreviewUpdating = false;
            }
        }

        private async Task UpdatePreviewBodyAsync(string htmlBody)
        {
            if (!_webViewReady || webPreview.CoreWebView2 == null) return;
            string base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlBody ?? string.Empty));
            try
            {
                await webPreview.CoreWebView2.ExecuteScriptAsync($"window.updatePreviewFromBase64('{base64Html}');");
            }
            catch (Exception ex)
            {
                // WebView2 có thể đang busy / mất context. Không crash, chỉ log để chẩn đoán
                // các trường hợp freeze trên máy người dùng.
                Logger.Log($"[Preview] ExecuteScriptAsync failed (htmlLen={(htmlBody ?? string.Empty).Length}): {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Toolbar handlers
        // ═══════════════════════════════════════════════════════════
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_docId)) return;
            try
            {
                FlushPendingEditOperation();
                string content = txtRawMarkdown.Text ?? string.Empty;
                await Task.Run(() => SocketClient.Instance.SaveDocument(_docId, content, "manual"));
                _lastSavedContent = content;
                _hasUnsavedChanges = false;
                Text = $"MarkTogether - {_docTitle} (đã lưu lúc {DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu thất bại: {ex.Message}", "Lỗi");
            }
        }

        private void chkPeriodicAutosave_CheckedChanged(object sender, EventArgs e)
        {
            _periodicEnabled = chkPeriodicAutosave.Checked;
            cmbAutosaveInterval.Enabled = _periodicEnabled;
            ApplyPeriodicTimer();
        }

        private void cmbAutosaveInterval_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbAutosaveInterval.SelectedIndex)
            {
                case 0:
                    _periodicIntervalMs = 60000;
                    break;
                case 1:
                    _periodicIntervalMs = 300000;
                    break;
                case 2:
                    _periodicIntervalMs = 1800000;
                    break;
                default:
                    _periodicIntervalMs = DefaultPeriodicAutosaveIntervalMs;
                    break;
            }

            ApplyPeriodicTimer();
        }

        private void ApplyPeriodicTimer()
        {
            _autosaveTimer.Stop();

            if (_periodicEnabled && _periodicIntervalMs > 0 && _trackRealtimeOps)
            {
                _autosaveTimer.Interval = _periodicIntervalMs;
                _autosaveTimer.Start();
            }
        }

        private void btnShare_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_docId) || _permission != "owner") return;
            using (var dlg = new ShareDocumentForm(_docId, _shareCode))
            {
                dlg.ShowDialog(this);
            }
        }

        private void btnVersions_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_docId)) return;
            using (var dlg = new VersionHistoryForm(_docId))
            {
                dlg.ShowDialog(this);
                if (dlg.DocumentWasRestored && dlg.RestoredContent != null)
                {
                    _suppressOpTracking = true;
                    txtRawMarkdown.Text = dlg.RestoredContent;
                    _lastMarkdownText = txtRawMarkdown.Text;
                    _suppressOpTracking = false;
                }
            }
        }

        private async void btnExportPdf_Click(object sender, EventArgs e)
        {
            if (!_webViewReady || webPreview.CoreWebView2 == null)
            {
                MessageBox.Show("Preview chưa sẵn sàng. Vui lòng chờ rồi thử lại.", "Export PDF");
                return;
            }

            // Đảm bảo đã render xong
            await Task.Delay(300);

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PDF (*.pdf)|*.pdf";
                dlg.FileName = (string.IsNullOrWhiteSpace(_docTitle) ? "document" : _docTitle) + ".pdf";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    btnExportPdf.Enabled = false;
                    bool ok = await webPreview.CoreWebView2.PrintToPdfAsync(dlg.FileName);
                    if (ok)
                    {
                        MessageBox.Show($"Đã xuất PDF:\n{dlg.FileName}", "Export PDF",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Xuất PDF thất bại.", "Export PDF",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Export PDF");
                }
                finally
                {
                    btnExportPdf.Enabled = true;
                }
            }
        }

        private async void btnInsertImage_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_docId)) return;
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Image (*.png;*.jpg;*.jpeg;*.gif;*.webp)|*.png;*.jpg;*.jpeg;*.gif;*.webp";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    byte[] bytes = await Task.Run(() => File.ReadAllBytes(dlg.FileName));
                    if (bytes.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Ảnh không được vượt quá 5 MB.", "Insert Image");
                        return;
                    }
                    string mime = GuessMime(dlg.FileName);
                    string fileName = Path.GetFileName(dlg.FileName);
                    var resp = await Task.Run(() =>
                        SocketClient.Instance.UploadImage(fileName, mime, bytes, false, _docId));
                    if (!resp.success)
                    {
                        MessageBox.Show(resp.message ?? "Upload thất bại.", "Insert Image");
                        return;
                    }

                    // Lưu markdown bằng imageId server, không lưu đường dẫn file:/// local.
                    // Khi preview, PrepareMarkdownImagesForPreview sẽ tải/cache ảnh rồi đổi tạm sang file:///.
                    await CacheImageLocallyAsync(resp.imageId, mime, bytes);
                    string snippet = $"![{Path.GetFileNameWithoutExtension(fileName)}](marktogether-image://{resp.imageId})";

                    int caret = txtRawMarkdown.SelectionStart;
                    string text = txtRawMarkdown.Text ?? "";
                    text = text.Insert(caret, snippet);
                    txtRawMarkdown.Text = text;
                    txtRawMarkdown.SelectionStart = caret + snippet.Length;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Insert Image");
                }
            }
        }

        private static string GuessMime(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".bmp": return "image/bmp";
                default: return "application/octet-stream";
            }
        }

        private string PrepareMarkdownImagesForPreview(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return markdown ?? string.Empty;

            return Regex.Replace(
                markdown,
                @"!\[([^\]]*)\]\(marktogether-image://([^)]+)\)",
                match =>
                {
                    string alt = match.Groups[1].Value;
                    string imageId = match.Groups[2].Value.Trim();

                    try
                    {
                        string localPath = EnsureImageCached(imageId);
                        if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
                            return match.Value;

                        // WebView2 NavigateToString/ExecuteScript đôi khi không load ổn định file:/// local.
                        // Dùng data URI để ảnh render độc lập với quyền truy cập file local.
                        string mime = GuessMime(localPath);
                        string base64 = Convert.ToBase64String(File.ReadAllBytes(localPath));
                        string dataUri = $"data:{mime};base64,{base64}";
                        return $"![{alt}]({dataUri})";
                    }
                    catch
                    {
                        return match.Value;
                    }
                });
        }

        private string EnsureImageCached(string imageId)
        {
            if (string.IsNullOrWhiteSpace(imageId)) return null;

            string dir = Path.Combine(Path.GetTempPath(), "MarkTogether", _docId ?? "any");
            Directory.CreateDirectory(dir);

            foreach (var path in Directory.GetFiles(dir, imageId + ".*"))
            {
                if (File.Exists(path)) return path;
            }

            var resp = SocketClient.Instance.GetImage(imageId);
            if (resp == null || resp.data == null || resp.data.Length == 0)
                return null;

            string ext = MimeToExt(resp.mimeType);
            string localPath = Path.Combine(dir, imageId + ext);
            File.WriteAllBytes(localPath, resp.data);
            return localPath;
        }

        private async Task<string> CacheImageLocallyAsync(string imageId, string mime, byte[] data)
        {
            string dir = Path.Combine(Path.GetTempPath(), "MarkTogether", _docId ?? "any");
            Directory.CreateDirectory(dir);
            string ext = MimeToExt(mime);
            string path = Path.Combine(dir, imageId + ext);
            await Task.Run(() => File.WriteAllBytes(path, data));
            return path;
        }

        private static string MimeToExt(string mime)
        {
            switch ((mime ?? "").ToLowerInvariant())
            {
                case "image/png": return ".png";
                case "image/jpeg":
                case "image/jpg": return ".jpg";
                case "image/gif": return ".gif";
                case "image/webp": return ".webp";
                case "image/bmp": return ".bmp";
                default: return ".bin";
            }
        }

        private void btnToggleSide_Click(object sender, EventArgs e)
        {
            splitOuter.Panel2Collapsed = !splitOuter.Panel2Collapsed;
            btnToggleSide.Text = splitOuter.Panel2Collapsed ? "≡" : "✕";
        }

        // ═══════════════════════════════════════════════════════════
        //  CHAT
        // ═══════════════════════════════════════════════════════════
        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.GetChatHistory(_docId, 50));
                lstChat.Items.Clear();
                foreach (var m in resp?.messages ?? new List<Payload_CHAT_BROADCAST>())
                {
                    AppendChatMessage(m);
                }
            }
            catch (Exception ex)
            {
                lstChat.Items.Add($"(Không tải được lịch sử: {ex.Message})");
            }
        }

        private void AppendChatMessage(Payload_CHAT_BROADCAST m)
        {
            if (m == null) return;
            string time = m.sentAt.ToLocalTime().ToString("HH:mm:ss");
            string line = $"[{time}] {m.username}: {m.content}";
            lstChat.Items.Add(line);
            lstChat.TopIndex = lstChat.Items.Count - 1;
        }

        private async void btnChatSend_Click(object sender, EventArgs e)
        {
            await SendChatAsync();
        }

        private async void txtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await SendChatAsync();
            }
        }

        private async Task SendChatAsync()
        {
            string content = (txtChatInput.Text ?? "").Trim();
            if (string.IsNullOrEmpty(content) || string.IsNullOrWhiteSpace(_docId)) return;

            try
            {
                btnChatSend.Enabled = false;
                txtChatInput.Clear();
                await Task.Run(() => SocketClient.Instance.SendChat(_docId, content));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gửi tin nhắn thất bại: {ex.Message}", "Chat");
            }
            finally
            {
                btnChatSend.Enabled = true;
                txtChatInput.Focus();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  COMMENT
        // ═══════════════════════════════════════════════════════════
        private async void btnAddComment_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_docId)) return;

            int start = txtRawMarkdown.SelectionStart;
            int len = txtRawMarkdown.SelectionLength;
            string anchor = len > 0 ? txtRawMarkdown.SelectedText : "";

            using (var dlg = new NewCommentForm(anchor))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var req = new Payload_COMMENT_CREATE_Request
                    {
                        docID = _docId,
                        anchorStart = start,
                        anchorEnd = start + len,
                        anchorText = anchor.Length > 200 ? anchor.Substring(0, 200) : anchor,
                        content = dlg.CommentText
                    };
                    await Task.Run(() => SocketClient.Instance.CreateComment(req));
                    await LoadCommentsAsync();
                    tabSide.SelectedTab = tabComments;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tạo comment thất bại: {ex.Message}", "Comment");
                }
            }
        }

        private async Task LoadCommentsAsync()
        {
            try
            {
                var resp = await Task.Run(() => SocketClient.Instance.ListComments(_docId));
                _comments.Clear();
                if (resp?.comments != null) _comments.AddRange(resp.comments);

                lstComments.Items.Clear();
                foreach (var c in _comments)
                {
                    string status = c.resolved ? "✓ " : "";
                    string snippet = (c.anchorText ?? "").Replace("\r", " ").Replace("\n", " ");
                    if (snippet.Length > 40) snippet = snippet.Substring(0, 40) + "...";
                    string content = (c.content ?? "").Replace("\r", " ").Replace("\n", " ");
                    if (content.Length > 60) content = content.Substring(0, 60) + "...";
                    string line = $"{status}[{c.username}] {c.createdAt.ToLocalTime():HH:mm} | \"{snippet}\" → {content}";
                    lstComments.Items.Add(line);
                }
            }
            catch (Exception ex)
            {
                lstComments.Items.Add($"(Lỗi: {ex.Message})");
            }
        }

        private void lstComments_DoubleClick(object sender, EventArgs e)
        {
            int idx = lstComments.SelectedIndex;
            if (idx < 0 || idx >= _comments.Count) return;
            var c = _comments[idx];
            try
            {
                int s = Math.Max(0, Math.Min(c.anchorStart, txtRawMarkdown.TextLength));
                int len = Math.Max(0, Math.Min(c.anchorEnd - c.anchorStart, txtRawMarkdown.TextLength - s));
                txtRawMarkdown.SelectionStart = s;
                txtRawMarkdown.SelectionLength = len;
                txtRawMarkdown.ScrollToCaret();
                txtRawMarkdown.Focus();
            }
            catch { }
        }

        private async void btnCommentResolve_Click(object sender, EventArgs e)
        {
            int idx = lstComments.SelectedIndex;
            if (idx < 0 || idx >= _comments.Count) return;
            var c = _comments[idx];
            try
            {
                await Task.Run(() => SocketClient.Instance.ResolveComment(c.id, !c.resolved));
                await LoadCommentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Comment");
            }
        }

        private async void btnCommentDelete_Click(object sender, EventArgs e)
        {
            int idx = lstComments.SelectedIndex;
            if (idx < 0 || idx >= _comments.Count) return;
            var c = _comments[idx];
            if (MessageBox.Show("Xoá comment này?", "Xoá",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                await Task.Run(() => SocketClient.Instance.DeleteComment(c.id));
                await LoadCommentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Comment");
            }
        }

        private async void btnCommentRefresh_Click(object sender, EventArgs e)
        {
            await LoadCommentsAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  AI
        // ═══════════════════════════════════════════════════════════
        private void btnAiSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new AISettingsForm())
            {
                dlg.ShowDialog(this);
            }
        }

        private void chkAiEditMode_CheckedChanged(object sender, EventArgs e)
        {
            bool on = chkAiEditMode.Checked;
            cmbAiMode.Enabled = !on;

            if (on && txtRawMarkdown.ReadOnly)
            {
                MessageBox.Show(
                    "Bạn chỉ có quyền xem tài liệu này. Action mode sẽ bị từ chối khi gửi.",
                    "Trợ lý AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnAiUndo_Click(object sender, EventArgs e)
        {
            if (_aiUndoStack.Count == 0) return;
            if (txtRawMarkdown.ReadOnly)
            {
                MessageBox.Show("Tài liệu đang ở chế độ chỉ xem.", "Trợ lý AI");
                return;
            }

            string snapshot = _aiUndoStack.Pop();
            txtRawMarkdown.Text = snapshot;
            txtAiHistory.AppendText($"AI ↶ Hoàn tác.{Environment.NewLine}{Environment.NewLine}");
            btnAiUndo.Enabled = _aiUndoStack.Count > 0;
        }

        private async void btnAiSend_Click(object sender, EventArgs e)
        {
            string prompt = (txtAiPrompt.Text ?? "").Trim();
            string modeLabel = cmbAiMode.SelectedItem?.ToString() ?? "Hỏi đáp";
            string mode = MapAiModeLabelToKey(modeLabel);
            string ctx = txtRawMarkdown.SelectedText ?? "";
            bool editMode = chkAiEditMode.Checked;

            if (editMode && txtRawMarkdown.ReadOnly)
            {
                MessageBox.Show("Bạn chỉ có quyền xem, không thể dùng AI để sửa văn bản.",
                    "Trợ lý AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(prompt) && (!editMode && string.IsNullOrEmpty(ctx)))
            {
                MessageBox.Show("Hãy nhập câu hỏi hoặc chọn đoạn văn bản để làm ngữ cảnh.", "AI");
                return;
            }

            if (editMode && string.IsNullOrEmpty(prompt))
            {
                MessageBox.Show("Hãy mô tả thao tác AI cần thực hiện.", "AI");
                return;
            }

            var aiCfg = AISettingsStore.Load();
            if (string.IsNullOrWhiteSpace(aiCfg.ApiKey))
            {
                var result = MessageBox.Show(
                    "Bạn chưa cấu hình API key cho AI. Mở 'Cài đặt AI' bây giờ?",
                    "Trợ lý AI",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                    btnAiSettings_Click(sender, e);
                return;
            }

            txtAiHistory.AppendText($"[{(editMode ? "Action mode" : modeLabel)}] Bạn: {prompt}{Environment.NewLine}");
            if (!editMode && !string.IsNullOrEmpty(ctx))
                txtAiHistory.AppendText($"  (ngữ cảnh: {Truncate(ctx, 80)}){Environment.NewLine}");
            txtAiPrompt.Clear();

            try
            {
                btnAiSend.Enabled = false;
                var resp = await Task.Run(() =>
                {
                    if (editMode)
                    {
                        return SocketClient.Instance.AskAI(
                            "chat", prompt, "", _docId,
                            aiCfg.ApiKey, aiCfg.Model, aiCfg.Provider,
                            actionMode: "edit",
                            documentText: txtRawMarkdown.Text ?? "",
                            selectionStart: txtRawMarkdown.SelectionStart,
                            selectionEnd: txtRawMarkdown.SelectionStart + txtRawMarkdown.SelectionLength,
                            cursorPosition: txtRawMarkdown.SelectionStart);
                    }

                    return SocketClient.Instance.AskAI(
                        mode, prompt, ctx, _docId,
                        aiCfg.ApiKey, aiCfg.Model, aiCfg.Provider);
                });

                if (!resp.success)
                {
                    txtAiHistory.AppendText($"AI lỗi: {resp.message}{Environment.NewLine}{Environment.NewLine}");
                }
                else if (resp.kind == "edit_plan" && !string.IsNullOrEmpty(resp.editPlanJson))
                {
                    HandleEditPlanResponse(resp.editPlanJson);
                }
                else
                {
                    txtAiHistory.AppendText($"AI: {resp.text}{Environment.NewLine}{Environment.NewLine}");
                }

                txtAiHistory.SelectionStart = txtAiHistory.TextLength;
                txtAiHistory.ScrollToCaret();
            }
            catch (Exception ex)
            {
                txtAiHistory.AppendText($"AI lỗi: {ex.Message}{Environment.NewLine}{Environment.NewLine}");
            }
            finally
            {
                btnAiSend.Enabled = true;
            }
        }

        private void HandleEditPlanResponse(string planJson)
        {
            AiEditPlan plan;
            try
            {
                plan = JsonConvert.DeserializeObject<AiEditPlan>(planJson);
            }
            catch (Exception ex)
            {
                txtAiHistory.AppendText($"AI lỗi parse plan: {ex.Message}{Environment.NewLine}");
                return;
            }

            if (plan == null) return;

            string oldText = txtRawMarkdown.Text ?? "";
            var applyResult = AIEditPlanApplier.Apply(oldText, plan);
            if (!applyResult.Ok)
            {
                txtAiHistory.AppendText($"AI lỗi áp dụng plan: {applyResult.Error}{Environment.NewLine}");
                return;
            }

            using (var dlg = new AIEditPreviewForm(oldText, plan, applyResult.NewText))
            {
                var dr = dlg.ShowDialog(this);
                if (dr == DialogResult.OK)
                {
                    if (txtRawMarkdown.ReadOnly)
                    {
                        MessageBox.Show("Tài liệu chuyển sang chế độ chỉ xem.", "Trợ lý AI");
                        return;
                    }

                    if ((txtRawMarkdown.Text ?? "") != oldText)
                    {
                        var confirm = MessageBox.Show(
                            "Tài liệu đã thay đổi từ lúc AI phân tích. Vẫn áp dụng?",
                            "Trợ lý AI", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (confirm != DialogResult.Yes) return;

                        applyResult = AIEditPlanApplier.Apply(txtRawMarkdown.Text ?? "", plan);
                        if (!applyResult.Ok)
                        {
                            txtAiHistory.AppendText($"AI lỗi áp dụng plan (re-apply): {applyResult.Error}{Environment.NewLine}");
                            return;
                        }
                    }

                    TrimAiUndoStackIfNeeded();
                    _aiUndoStack.Push(txtRawMarkdown.Text ?? "");

                    txtRawMarkdown.Text = applyResult.NewText;
                    btnAiUndo.Enabled = true;

                    txtAiHistory.AppendText($"AI ✓ Đã áp dụng: {plan.Summary}{Environment.NewLine}{Environment.NewLine}");
                }
                else if (dr == DialogResult.Retry)
                {
                    txtAiHistory.AppendText($"AI (gợi ý không áp dụng): {plan.Summary}{Environment.NewLine}");
                    if (!string.IsNullOrEmpty(plan.Notes))
                        txtAiHistory.AppendText($"  Ghi chú: {plan.Notes}{Environment.NewLine}");
                    txtAiHistory.AppendText(Environment.NewLine);
                }
                else
                {
                    txtAiHistory.AppendText($"AI ✗ Đã từ chối thay đổi.{Environment.NewLine}{Environment.NewLine}");
                }
            }
        }

        private void TrimAiUndoStackIfNeeded()
        {
            if (_aiUndoStack.Count < AiUndoMaxDepth) return;

            var arr = _aiUndoStack.ToArray();
            _aiUndoStack.Clear();
            for (int i = arr.Length - 2; i >= 0; i--)
                _aiUndoStack.Push(arr[i]);
        }

        private static string Truncate(string s, int len)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= len ? s : s.Substring(0, len) + "...";
        }

        private static string MapAiModeLabelToKey(string label)
        {
            switch (label)
            {
                case "Tóm tắt": return "summarize";
                case "Viết tiếp": return "continue";
                case "Dịch": return "translate";
                default: return "chat";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════════════
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _opFlushTimer.Tick -= OpFlushTimer_Tick;
            _opFlushTimer.Dispose();
            _renderDebounceTimer.Tick -= RenderDebounceTimer_Tick;
            _renderDebounceTimer.Dispose();
            _autosaveTimer.Tick -= AutosaveTimer_Tick;
            _autosaveTimer.Dispose();
            _draftTimer.Tick -= DraftTimer_Tick;
            _draftTimer.Dispose();

            if (webPreview.CoreWebView2 != null)
                webPreview.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;

            // Unsubscribe events
            if (!string.IsNullOrWhiteSpace(_docId))
            {
                SocketClient.Instance.OnOpBroadcast -= HandleOpBroadcast;
                SocketClient.Instance.OnChatBroadcast -= HandleChatBroadcast;
                SocketClient.Instance.OnCommentBroadcast -= HandleCommentBroadcast;
                SocketClient.Instance.OnDocReloadBroadcast -= HandleDocReloadBroadcast;

                try { SocketClient.Instance.LeaveDocument(_docId); } catch { }
            }

            Shown -= TypeRenderForm_Shown;
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Flush trước để đẩy chunk typing cuối vào queue, sau đó mới wait
            FlushPendingEditOperation();

            try
            {
                var task = _pasteSendTask;
                if (task != null && !task.IsCompleted)
                    task.Wait(TimeSpan.FromSeconds(3));
            }
            catch { /* ignore — vẫn tiếp tục đóng form */ }

            if (!string.IsNullOrWhiteSpace(_docId)
                && SocketClient.Instance.IsLoggedIn
                && (_permission == "owner" || _permission == "editor"))
            {
                try
                {
                    SocketClient.Instance.SaveDocument(_docId, txtRawMarkdown.Text ?? string.Empty, "manual");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể tự động lưu khi đóng.\n{ex.Message}",
                        "Lưu tài liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (txtRawMarkdown != null
                && txtRawMarkdown.Focused
                && !txtRawMarkdown.ReadOnly
                && IsPasteKeyCombo(keyData))
            {
                try
                {
                    if (TryHandlePaste())
                        return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Paste] Intercept failed: {ex.Message}");
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static bool IsPasteKeyCombo(Keys keyData)
        {
            return keyData == (Keys.Control | Keys.V)
                || keyData == (Keys.Shift | Keys.Insert);
        }

        private bool TryHandlePaste()
        {
            string clipboardText = null;
            try
            {
                if (Clipboard.ContainsText())
                    clipboardText = Clipboard.GetText(TextDataFormat.UnicodeText);
            }
            catch
            {
                clipboardText = null;
            }

            if (string.IsNullOrEmpty(clipboardText))
                return false;

            clipboardText = NormalizePastedText(clipboardText);

            if (clipboardText.Length < PasteThresholdChars)
                return false;

            PerformFastPaste(clipboardText);
            return true;
        }

        private static string NormalizePastedText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Regex.Replace(text, @"\r\n|\r|\n", "\r\n");
        }

        private void PerformFastPaste(string pasted)
        {
            if (string.IsNullOrEmpty(pasted)) return;

            FlushPendingEditOperation();

            int selStart = txtRawMarkdown.SelectionStart;
            int selLength = txtRawMarkdown.SelectionLength;
            string currentText = txtRawMarkdown.Text ?? string.Empty;
            string selectedText = selLength > 0
                ? currentText.Substring(selStart, selLength)
                : string.Empty;

            _isPasting = true;
            _suppressOpTracking = true;
            try
            {
                txtRawMarkdown.SelectedText = pasted;

                int newCaret = selStart + pasted.Length;
                txtRawMarkdown.SelectionStart = newCaret;
                txtRawMarkdown.SelectionLength = 0;

                _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
                _hasUnsavedChanges = _lastMarkdownText != _lastSavedContent;
            }
            finally
            {
                _suppressOpTracking = false;
            }

            _renderRequestVersion++;
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();

            bool needSendOps = _trackRealtimeOps
                && !string.IsNullOrWhiteSpace(_docId)
                && SocketClient.Instance.IsLoggedIn
                && (_permission == "owner" || _permission == "editor");

            if (!needSendOps)
            {
                _isPasting = false;
                return;
            }

            EnqueuePasteOperations(selStart, selectedText, pasted);
            EnsurePasteSenderRunning();
        }

        private void EnqueuePasteOperations(int basePos, string replacedSelection, string pasted)
        {
            lock (_pasteLock)
            {
                if (!string.IsNullOrEmpty(replacedSelection))
                {
                    for (int i = 0; i < replacedSelection.Length;)
                    {
                        int size = SafeChunkLength(replacedSelection, i, MaxCharsPerPacket);
                        string chunk = replacedSelection.Substring(i, size);
                        _pasteChunkQueue.Enqueue($"D|{basePos}|{chunk}");
                        i += size;
                    }
                }

                int insertPos = basePos;
                for (int i = 0; i < pasted.Length;)
                {
                    int size = SafeChunkLength(pasted, i, MaxCharsPerPacket);
                    string chunk = pasted.Substring(i, size);
                    _pasteChunkQueue.Enqueue($"I|{insertPos}|{chunk}");
                    insertPos += size;
                    i += size;
                }
            }
        }

        private static int SafeChunkLength(string text, int start, int maxLen)
        {
            int len = Math.Min(maxLen, text.Length - start);
            if (len <= 0) return 0;

            if (len < text.Length - start && char.IsHighSurrogate(text[start + len - 1]))
                len--;

            return Math.Max(1, len);
        }

        private void EnsurePasteSenderRunning()
        {
            lock (_pasteLock)
            {
                if (_pasteSendTask != null && !_pasteSendTask.IsCompleted)
                    return;

                _pasteSendTask = Task.Run((Action)PasteSenderLoop);
            }
        }

        private void PasteSenderLoop()
        {
            try
            {
                while (true)
                {
                    string entry;
                    lock (_pasteLock)
                    {
                        if (_pasteChunkQueue.Count == 0)
                        {
                            _isPasting = false;
                            return;
                        }

                        entry = _pasteChunkQueue.Dequeue();
                    }

                    char opChar = entry[0];
                    int firstBar = 1;
                    int secondBar = entry.IndexOf('|', firstBar + 1);
                    if (secondBar < 0) continue;

                    int pos = int.Parse(entry.Substring(firstBar + 1, secondBar - firstBar - 1));
                    string text = entry.Substring(secondBar + 1);

                    var ops = new List<EditOpItem>
                    {
                        new EditOpItem
                        {
                            pos = pos,
                            text = text,
                            timestamp = DateTime.UtcNow
                        }
                    };

                    try
                    {
                        int rev = _clientRevision;
                        if (opChar == 'I')
                            SocketClient.Instance.SendInsertOps(_docId, rev, ops);
                        else
                            SocketClient.Instance.SendDeleteOps(_docId, rev, ops);

                        System.Threading.Interlocked.Increment(ref _clientRevision);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Paste] Send chunk failed: {ex.Message}");
                        if (!SocketClient.Instance.IsLoggedIn)
                        {
                            lock (_pasteLock)
                            {
                                _pasteChunkQueue.Clear();
                            }
                            return;
                        }
                    }
                }
            }
            finally
            {
                lock (_pasteLock)
                {
                    _isPasting = false;
                }
            }
        }
    }
}
