using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using Markdig;
using System.Linq;
using System.Drawing; // [ADDED]

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
        private string _lastMarkdownText = string.Empty;
        private PendingOpType? _pendingOpType;
        private int _pendingOpStartPos;
        private string _pendingOpText = string.Empty;
        private bool _trackRealtimeOps;
        private int _clientRevision;
        private readonly string _permission; // [ADDED]

        // [OT] Modified to 400ms debounce as requested
        private const int FlushIntervalMs = 400;

        private enum PendingOpType { Insert, Delete }

        private sealed class TextDelta { public int Position { get; set; } public string DeletedText { get; set; } public string InsertedText { get; set; } }

        private const string PreviewHtmlTemplate = "<!DOCTYPE html><html><head><meta charset='utf-8'/><meta name='viewport' content='width=device-width, initial-scale=1'/><link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github.min.css'/><script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/highlight.min.js'></script><style>html,body{background:#ffffff !important;color:#1f2328 !important;}html{color-scheme:light;}body{font-family:Segoe UI,Arial,sans-serif;padding:16px;line-height:1.6;margin:0;}h1,h2,h3{margin-top:1.2em;}p,li,td,th,blockquote{color:#1f2328;}pre{background:#f6f8fa !important;color:#24292f !important;padding:10px;border-radius:6px;overflow:auto;border:1px solid #d0d7de;}code{font-family:Consolas,monospace;background:#f6f8fa;color:#cf222e;padding:2px 4px;border-radius:4px;}pre code{background:transparent;color:inherit;padding:0;}blockquote{border-left:4px solid #ddd;margin:0;padding:0 12px;color:#666;}table{border-collapse:collapse;}th,td{border:1px solid #ddd;padding:6px 10px;background:#fff;}.hljs{background:#f6f8fa !important;color:#24292f !important;}</style><script>window.updatePreviewFromBase64 = function (base64) { var body = document.body; var html = ''; if (base64) { try { html = decodeURIComponent(escape(window.atob(base64))); } catch(e) { html = ''; } } body.innerHTML = html; if (window.hljs) { body.querySelectorAll('pre code').forEach(function(block){ hljs.highlightElement(block); }); } };</script></head><body></body></html>";

        public TypeRenderForm() : this(null, null, null, "owner", 0) { }

        public TypeRenderForm(string docId, string title, string initialContent, string permission, int revision) // [MODIFIED]
        {
            InitializeComponent();
            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            _renderDebounceTimer = new Timer { Interval = 280 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _opFlushTimer = new Timer { Interval = FlushIntervalMs }; // [OT]
            _opFlushTimer.Tick += OpFlushTimer_Tick;
            webPreview.TabStop = false;
            Shown += TypeRenderForm_Shown;
            _docId = docId; _docTitle = title; _initialContent = initialContent;
            _permission = (permission ?? "viewer").ToLower(); // [ADDED]
            _clientRevision = revision; // [FIX] Initialize from server state

            ApplyInitialDocumentState();

            _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
            _trackRealtimeOps = !string.IsNullOrWhiteSpace(_docId) && _permission != "viewer"; // [MODIFIED]

            // [ADDED] Handle Viewer permission
            if (_permission == "viewer")
            {
                txtRawMarkdown.ReadOnly = true;
                txtRawMarkdown.BackColor = Color.FromArgb(245, 245, 245);
                
                var lblViewer = new Label
                {
                    Text = "Bạn chỉ có quyền xem tài liệu này.",
                    Dock = DockStyle.Top,
                    BackColor = Color.LightYellow,
                    ForeColor = Color.DarkOrange,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 30
                };
                // Adding to form moves splitMain down, which is expected for viewer mode.
                // For non-viewer users, this block is skipped, fixing Bug 1.
                this.Controls.Add(lblViewer);
                lblViewer.BringToFront();
            }

            if (!string.IsNullOrEmpty(_docId))
            {
                System.IO.File.AppendAllText("client_debug.log", $"[{DateTime.Now:HH:mm:ss.fff}] [TypeRender] Subscribed to BroadcastReceived for docId={_docId}\n");
                SocketClient.Instance.BroadcastReceived += OnBroadcastReceived;
                
                // [FIX] Subscribe to revision ACKs to stay in sync with server authoritative state
                SocketClient.Instance.RevisionAckReceived += OnRevisionAck;
            }

            RenderMarkdown();
        }

        // [FIX] Sync to server's authoritative revision
        private void OnRevisionAck(int serverRevision)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnRevisionAck(serverRevision)));
                return;
            }
            if (serverRevision > _clientRevision)
                _clientRevision = serverRevision;
        }

        private void OnBroadcastReceived(MarkTogether.Shared.Payload_OP_BROADCAST broadcast)
        {
            System.IO.File.AppendAllText("client_debug.log", $"[{DateTime.Now:HH:mm:ss.fff}] [TypeRender] OnBroadcastReceived docId={broadcast?.docID} _docId={_docId}\n");
            if (broadcast == null || broadcast.docID != _docId)
                return;
            
            if (this.InvokeRequired) 
            { 
                this.BeginInvoke(new Action(() => OnBroadcastReceived(broadcast))); 
                return; 
            }

            try
            {
                // [FIX] Flush pending local ops first to avoid cursor/text conflicts
                FlushPendingEditOperation();

                txtRawMarkdown.TextChanged -= txtRawMarkdown_TextChanged;
                int savedCursor = txtRawMarkdown.SelectionStart;
                string currentText = txtRawMarkdown.Text ?? string.Empty;
                var ops = broadcast.ops ?? new List<MarkTogether.Shared.EditOpItem>();
                foreach (var op in ops)
                {
                    if (broadcast.opType == "insert") { if (op.pos >= 0 && op.pos <= currentText.Length) { currentText = currentText.Insert(op.pos, op.text); if (op.pos <= savedCursor) savedCursor += op.text.Length; } }
                    else if (broadcast.opType == "delete") { if (op.pos >= 0 && op.pos < currentText.Length) { int len = Math.Min(op.text.Length, currentText.Length - op.pos); currentText = currentText.Remove(op.pos, len); if (op.pos < savedCursor) savedCursor -= Math.Min(len, savedCursor - op.pos); } }
                }
                txtRawMarkdown.Text = currentText;
                txtRawMarkdown.SelectionStart = Math.Max(0, Math.Min(savedCursor, currentText.Length));
                _lastMarkdownText = currentText;
                
                // [FIX] Always take the higher revision to stay in sync with server baseline
                _clientRevision = Math.Max(_clientRevision, broadcast.clientResivion);

                _renderRequestVersion++; _renderDebounceTimer.Stop(); _renderDebounceTimer.Start();
            }
            finally { txtRawMarkdown.TextChanged += txtRawMarkdown_TextChanged; }
        }

        private void ApplyInitialDocumentState()
        {
            if (!string.IsNullOrWhiteSpace(_docId)) { Text = string.IsNullOrWhiteSpace(_docTitle) ? "MarkTogether - Tài liệu" : $"MarkTogether - {_docTitle}"; txtRawMarkdown.Text = _initialContent ?? string.Empty; return; }
            txtRawMarkdown.Text = "# MarkTogether\r\n\r\nChào mừng bạn đến với **Type Render**.\r\n\r\n- Gõ markdown ở khung bên trái\r\n- Khung bên phải sẽ render tương ứng\r\n\r\n```csharp\r\nConsole.WriteLine(\"Hello Markdown!\");\r\n```";
        }

        private async void TypeRenderForm_Shown(object sender, EventArgs e) { await InitializeWebViewAsync(); QueuePreviewUpdate(); }

        private async Task InitializeWebViewAsync()
        {
            if (_webViewReady) return;
            await webPreview.EnsureCoreWebView2Async();
            webPreview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false; webPreview.CoreWebView2.Settings.AreDevToolsEnabled = false; webPreview.CoreWebView2.Settings.IsStatusBarEnabled = false; webPreview.CoreWebView2.Settings.IsZoomControlEnabled = true; webPreview.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webPreview.NavigateToString(PreviewHtmlTemplate);
        }

        private void CoreWebView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e) { if (!e.IsSuccess) return; _webViewReady = true; QueuePreviewUpdate(); }

        private void txtRawMarkdown_TextChanged(object sender, EventArgs e) { TrackRealtimeEditOps(txtRawMarkdown.Text ?? string.Empty); _renderRequestVersion++; _renderDebounceTimer.Stop(); _renderDebounceTimer.Start(); }

        private void TrackRealtimeEditOps(string newText)
        {
            string oldText = _lastMarkdownText ?? string.Empty;
            if (newText == oldText) return;
            if (!_trackRealtimeOps || string.IsNullOrWhiteSpace(_docId) || !SocketClient.Instance.IsLoggedIn) { _lastMarkdownText = newText; return; }
            var delta = ComputeTextDelta(oldText, newText);
            if (delta == null) { FlushPendingEditOperation(); _lastMarkdownText = newText; return; }
            if (!string.IsNullOrEmpty(delta.DeletedText) && !string.IsNullOrEmpty(delta.InsertedText)) { FlushPendingEditOperation(); QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText); FlushPendingEditOperation(); QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText); FlushPendingEditOperation(); }
            else if (!string.IsNullOrEmpty(delta.InsertedText)) { QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText); }
            else if (!string.IsNullOrEmpty(delta.DeletedText)) { QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText); }
            _lastMarkdownText = newText;
        }

        private TextDelta ComputeTextDelta(string oldText, string newText)
        {
            int prefix = 0; int minLen = Math.Min(oldText.Length, newText.Length);
            while (prefix < minLen && oldText[prefix] == newText[prefix]) prefix++;
            int oldSuffix = oldText.Length - 1; int newSuffix = newText.Length - 1;
            while (oldSuffix >= prefix && newSuffix >= prefix && oldText[oldSuffix] == newText[newSuffix]) { oldSuffix--; newSuffix--; }
            string deleted = oldSuffix >= prefix ? oldText.Substring(prefix, oldSuffix - prefix + 1) : string.Empty;
            string inserted = newSuffix >= prefix ? newText.Substring(prefix, newSuffix - prefix + 1) : string.Empty;
            return new TextDelta { Position = prefix, DeletedText = deleted, InsertedText = inserted };
        }

        private void QueueOperation(PendingOpType opType, int position, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_pendingOpType.HasValue && _pendingOpType.Value != opType) FlushPendingEditOperation();
            if (!_pendingOpType.HasValue) { _pendingOpType = opType; _pendingOpStartPos = position; _pendingOpText = string.Empty; }
            if (!CanMergePending(opType, position, text)) { FlushPendingEditOperation(); _pendingOpType = opType; _pendingOpStartPos = position; _pendingOpText = string.Empty; }
            MergeIntoPending(opType, position, text); RestartOpFlushTimer();
        }

        private bool CanMergePending(PendingOpType opType, int position, string text)
        {
            if (!_pendingOpType.HasValue || string.IsNullOrEmpty(_pendingOpText)) return true;
            if (_pendingOpType.Value != opType) return false;
            if (opType == PendingOpType.Insert) return position == _pendingOpStartPos + _pendingOpText.Length;
            return position == _pendingOpStartPos || position + text.Length == _pendingOpStartPos;
        }

        private void MergeIntoPending(PendingOpType opType, int position, string text)
        {
            if (opType == PendingOpType.Insert) { _pendingOpText += text; }
            else { if (position + text.Length == _pendingOpStartPos) { _pendingOpText = text + _pendingOpText; _pendingOpStartPos = position; } else { _pendingOpText += text; } }
        }

        private void OpFlushTimer_Tick(object sender, EventArgs e) { _opFlushTimer.Stop(); FlushPendingEditOperation(); }

        private void RestartOpFlushTimer() { _opFlushTimer.Stop(); _opFlushTimer.Start(); }

        private void FlushPendingEditOperation()
        {
            _opFlushTimer.Stop();
            if (_pendingOpType.HasValue && !string.IsNullOrEmpty(_pendingOpText)) { SendCurrentPendingChunk(_pendingOpText); }
            _pendingOpType = null; _pendingOpText = string.Empty;
        }

        private void SendCurrentPendingChunk(string chunk)
        {
            if (!_pendingOpType.HasValue || string.IsNullOrEmpty(chunk) || string.IsNullOrWhiteSpace(_docId)) return;
            var ops = new List<MarkTogether.Shared.EditOpItem> { new MarkTogether.Shared.EditOpItem { pos = _pendingOpStartPos, text = chunk, timestamp = DateTime.UtcNow } };
            try 
            { 
                if (_pendingOpType.Value == PendingOpType.Insert) 
                { 
                    SocketClient.Instance.SendInsertOps(_docId, _clientRevision, ops); 
                } 
                else 
                { 
                    SocketClient.Instance.SendDeleteOps(_docId, _clientRevision, ops); 
                } 
                // [FIX] REMOVED _clientRevision++ here.
                // We now wait for the server's authoritative ACK via OnRevisionAck.
            }
            catch { }
        }

        private void RenderDebounceTimer_Tick(object sender, EventArgs e) { _renderDebounceTimer.Stop(); RenderMarkdown(); }

        private async void RenderMarkdown()
        {
            int requestVersion = _renderRequestVersion; string markdown = txtRawMarkdown.Text ?? string.Empty;
            string htmlBody; try { htmlBody = await Task.Run(() => Markdown.ToHtml(markdown, _pipeline)); } catch { return; }
            if (requestVersion != _renderRequestVersion || IsDisposed) return;
            _pendingHtmlBody = htmlBody; QueuePreviewUpdate();
        }

        private void QueuePreviewUpdate() { _hasPendingPreviewUpdate = true; if (_isPreviewUpdating || !_webViewReady || webPreview.CoreWebView2 == null) return; _ = ProcessPreviewQueueAsync(); }

        private async Task ProcessPreviewQueueAsync() { _isPreviewUpdating = true; try { while (_hasPendingPreviewUpdate && _webViewReady && webPreview.CoreWebView2 != null && !IsDisposed) { _hasPendingPreviewUpdate = false; await UpdatePreviewBodyAsync(_pendingHtmlBody); } } finally { _isPreviewUpdating = false; } }

        private async Task UpdatePreviewBodyAsync(string htmlBody) { if (!_webViewReady || webPreview.CoreWebView2 == null) return; string base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlBody ?? string.Empty)); try { await webPreview.CoreWebView2.ExecuteScriptAsync($"window.updatePreviewFromBase64('{base64Html}');"); } catch { return; } }

        protected override void OnFormClosed(FormClosedEventArgs e) 
        { 
            if (!string.IsNullOrEmpty(_docId)) 
            {
                SocketClient.Instance.BroadcastReceived -= OnBroadcastReceived; 
                SocketClient.Instance.RevisionAckReceived -= OnRevisionAck; // [FIX]
            }
            _opFlushTimer.Tick -= OpFlushTimer_Tick; 
            _opFlushTimer.Dispose(); 
            _renderDebounceTimer.Tick -= RenderDebounceTimer_Tick; 
            _renderDebounceTimer.Dispose(); 
            if (webPreview.CoreWebView2 != null) webPreview.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted; 
            Shown -= TypeRenderForm_Shown; 
            base.OnFormClosed(e); 
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { FlushPendingEditOperation(); if (!string.IsNullOrWhiteSpace(_docId) && SocketClient.Instance.IsLoggedIn) { try { if (_permission != "viewer") SocketClient.Instance.SaveDocument(_docId, txtRawMarkdown.Text ?? string.Empty); SocketClient.Instance.LeaveDocument(_docId); } catch (Exception ex) { MessageBox.Show($"Không thể tự động lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); } } base.OnFormClosing(e); }
    }
}
