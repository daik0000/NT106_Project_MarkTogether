using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using Markdig;
using System.Linq; // [ADDED]

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

        private const int MaxCharsPerPacket = 5;

        private enum PendingOpType
        {
            Insert,
            Delete
        }

        private sealed class TextDelta
        {
            public int Position { get; set; }
            public string DeletedText { get; set; }
            public string InsertedText { get; set; }
        }

        private const string PreviewHtmlTemplate = "<!DOCTYPE html>" +
                                                   "<html><head><meta charset='utf-8'/>" +
                                                   "<meta name='viewport' content='width=device-width, initial-scale=1'/>" +
                                                   "<link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github.min.css'/>" +
                                                   "<script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/highlight.min.js'></script>" +
                                                   "<style>" +
                                                   "html,body{background:#ffffff !important;color:#1f2328 !important;}" +
                                                   "html{color-scheme:light;}" +
                                                   "body{font-family:Segoe UI,Arial,sans-serif;padding:16px;line-height:1.6;margin:0;}" +
                                                   "h1,h2,h3{margin-top:1.2em;}" +
                                                   "p,li,td,th,blockquote{color:#1f2328;}" +
                                                   "pre{background:#f6f8fa !important;color:#24292f !important;padding:10px;border-radius:6px;overflow:auto;border:1px solid #d0d7de;}" +
                                                   "code{font-family:Consolas,monospace;background:#f6f8fa;color:#cf222e;padding:2px 4px;border-radius:4px;}" +
                                                   "pre code{background:transparent;color:inherit;padding:0;}" +
                                                   "blockquote{border-left:4px solid #ddd;margin:0;padding:0 12px;color:#666;}" +
                                                   "table{border-collapse:collapse;}" +
                                                   "th,td{border:1px solid #ddd;padding:6px 10px;background:#fff;}" +
                                                   ".hljs{background:#f6f8fa !important;color:#24292f !important;}" +
                                                   "</style>" +
                                                   "<script>" +
                                                   "window.updatePreviewFromBase64 = function (base64) {" +
                                                   "  var body = document.body;" +
                                                   "  var html = '';" +
                                                   "  if (base64) {" +
                                                   "    try { html = decodeURIComponent(escape(window.atob(base64))); } catch(e) { html = ''; }" +
                                                   "  }" +
                                                   "  body.innerHTML = html;" +
                                                   "  if (window.hljs) {" +
                                                   "    body.querySelectorAll('pre code').forEach(function(block){ hljs.highlightElement(block); });" +
                                                   "  }" +
                                                   "};" +
                                                   "</script>" +
                                                   "</head><body></body></html>";

        public TypeRenderForm()
            : this(null, null, null)
        {
        }

        public TypeRenderForm(string docId, string title, string initialContent)
        {
            InitializeComponent();

            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            _renderDebounceTimer = new Timer
            {
                Interval = 280
            };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            _opFlushTimer = new Timer
            {
                Interval = 250
            };
            _opFlushTimer.Tick += OpFlushTimer_Tick;

            webPreview.TabStop = false;

            Shown += TypeRenderForm_Shown;

            _docId = docId;
            _docTitle = title;
            _initialContent = initialContent;

            ApplyInitialDocumentState();

            _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
            _trackRealtimeOps = !string.IsNullOrWhiteSpace(_docId);

            // [ADDED] Register broadcast event
            if (!string.IsNullOrEmpty(_docId))
            {
                SocketClient.Instance.BroadcastReceived += OnBroadcastReceived;
            }

            RenderMarkdown();
        }

        // [ADDED] Handle broadcast from other users
        private void OnBroadcastReceived(MarkTogether.Shared.Payload_OP_BROADCAST broadcast)
        {
            if (broadcast == null || broadcast.docID != _docId)
                return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnBroadcastReceived(broadcast)));
                return;
            }

            try
            {
                // Disable event to prevent infinite loops
                txtRawMarkdown.TextChanged -= txtRawMarkdown_TextChanged;

                int savedCursor = txtRawMarkdown.SelectionStart;
                string currentText = txtRawMarkdown.Text ?? string.Empty;

                // Sort ops by position descending if deleting to avoid shifting issues? 
                // Or just apply sequentially as instructed.
                var ops = broadcast.ops ?? new List<MarkTogether.Shared.EditOpItem>();

                foreach (var op in ops)
                {
                    if (broadcast.opType == "insert")
                    {
                        if (op.pos >= 0 && op.pos <= currentText.Length)
                        {
                            currentText = currentText.Insert(op.pos, op.text);
                            if (op.pos <= savedCursor)
                                savedCursor += op.text.Length;
                        }
                    }
                    else if (broadcast.opType == "delete")
                    {
                        if (op.pos >= 0 && op.pos < currentText.Length)
                        {
                            int lenToRemove = Math.Min(op.text.Length, currentText.Length - op.pos);
                            currentText = currentText.Remove(op.pos, lenToRemove);
                            if (op.pos < savedCursor)
                                savedCursor -= Math.Min(lenToRemove, savedCursor - op.pos);
                        }
                    }
                }

                txtRawMarkdown.Text = currentText;
                txtRawMarkdown.SelectionStart = Math.Max(0, Math.Min(savedCursor, currentText.Length));
                _lastMarkdownText = currentText;
                _clientRevision++; // As requested

                // Trigger render
                _renderRequestVersion++;
                _renderDebounceTimer.Stop();
                _renderDebounceTimer.Start();
            }
            finally
            {
                txtRawMarkdown.TextChanged += txtRawMarkdown_TextChanged;
            }
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
                "- Khung bên phải sẽ render tương ứng\r\n\r\n" +
                "```csharp\r\n" +
                "Console.WriteLine(\"Hello Markdown!\");\r\n" +
                "```";
        }

        private async void TypeRenderForm_Shown(object sender, EventArgs e)
        {
            await InitializeWebViewAsync();
            QueuePreviewUpdate();
        }

        private async Task InitializeWebViewAsync()
        {
            if (_webViewReady)
                return;

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
            if (!e.IsSuccess)
                return;

            _webViewReady = true;
            QueuePreviewUpdate();
        }

        private void txtRawMarkdown_TextChanged(object sender, EventArgs e)
        {
            TrackRealtimeEditOps(txtRawMarkdown.Text ?? string.Empty);

            _renderRequestVersion++;
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        private void TrackRealtimeEditOps(string newText)
        {
            string oldText = _lastMarkdownText ?? string.Empty;

            if (newText == oldText)
                return;

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
            while (prefix < minLen && oldText[prefix] == newText[prefix])
            {
                prefix++;
            }

            int oldSuffix = oldText.Length - 1;
            int newSuffix = newText.Length - 1;
            while (oldSuffix >= prefix && newSuffix >= prefix && oldText[oldSuffix] == newText[newSuffix])
            {
                oldSuffix--;
                newSuffix--;
            }

            string deleted = oldSuffix >= prefix
                ? oldText.Substring(prefix, oldSuffix - prefix + 1)
                : string.Empty;

            string inserted = newSuffix >= prefix
                ? newText.Substring(prefix, newSuffix - prefix + 1)
                : string.Empty;

            return new TextDelta
            {
                Position = prefix,
                DeletedText = deleted,
                InsertedText = inserted
            };
        }

        private void QueueOperation(PendingOpType opType, int position, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (_pendingOpType.HasValue && _pendingOpType.Value != opType)
            {
                FlushPendingEditOperation();
            }

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
            if (!_pendingOpType.HasValue || string.IsNullOrEmpty(_pendingOpText))
                return true;

            if (_pendingOpType.Value != opType)
                return false;

            if (opType == PendingOpType.Insert)
            {
                return position == _pendingOpStartPos + _pendingOpText.Length;
            }

            // Delete
            if (position == _pendingOpStartPos)
            {
                // Forward delete keeps same start position
                return true;
            }

            // Backspace-style delete: new deletion is immediately on the left
            return position + text.Length == _pendingOpStartPos;
        }

        private void MergeIntoPending(PendingOpType opType, int position, string text)
        {
            if (opType == PendingOpType.Insert)
            {
                _pendingOpText += text;
                return;
            }

            // Delete merge
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
                _pendingOpStartPos += size;
            }

            _pendingOpType = null;
            _pendingOpText = string.Empty;
        }

        private void SendCurrentPendingChunk(string chunk)
        {
            if (!_pendingOpType.HasValue || string.IsNullOrEmpty(chunk) || string.IsNullOrWhiteSpace(_docId))
                return;

            var ops = new List<MarkTogether.Shared.EditOpItem>
            {
                new MarkTogether.Shared.EditOpItem
                {
                    pos = _pendingOpStartPos,
                    text = chunk,
                    timestamp = DateTime.UtcNow
                }
            };

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

                _clientRevision++;
            }
            catch
            {
                // Ignore transport errors here to avoid breaking typing flow.
            }
        }

        private void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            RenderMarkdown();
        }

        private async void RenderMarkdown()
        {
            int requestVersion = _renderRequestVersion;
            string markdown = txtRawMarkdown.Text ?? string.Empty;

            string htmlBody;
            try
            {
                htmlBody = await Task.Run(() => Markdown.ToHtml(markdown, _pipeline));
            }
            catch
            {
                return;
            }

            if (requestVersion != _renderRequestVersion || IsDisposed)
                return;

            _pendingHtmlBody = htmlBody;
            QueuePreviewUpdate();
        }

        private void QueuePreviewUpdate()
        {
            _hasPendingPreviewUpdate = true;

            if (_isPreviewUpdating || !_webViewReady || webPreview.CoreWebView2 == null)
                return;

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
            if (!_webViewReady || webPreview.CoreWebView2 == null)
                return;

            string base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlBody ?? string.Empty));
            try
            {
                await webPreview.CoreWebView2.ExecuteScriptAsync($"window.updatePreviewFromBase64('{base64Html}');");
            }
            catch
            {
                // Ignore transient script execution errors while WebView2 is still stabilizing.
                return;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // [ADDED] Unregister broadcast event
            if (!string.IsNullOrEmpty(_docId))
            {
                SocketClient.Instance.BroadcastReceived -= OnBroadcastReceived;
            }

            _opFlushTimer.Tick -= OpFlushTimer_Tick;
            _opFlushTimer.Dispose();

            _renderDebounceTimer.Tick -= RenderDebounceTimer_Tick;
            _renderDebounceTimer.Dispose();
            if (webPreview.CoreWebView2 != null)
            {
                webPreview.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            }
            Shown -= TypeRenderForm_Shown;
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            FlushPendingEditOperation();

            if (!string.IsNullOrWhiteSpace(_docId) && SocketClient.Instance.IsLoggedIn)
            {
                try
                {
                    string latestContent = txtRawMarkdown.Text ?? string.Empty;
                    SocketClient.Instance.SaveDocument(_docId, latestContent);

                    // [ADDED] Send DOC_LEAVE
                    SocketClient.Instance.LeaveDocument(_docId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể tự động lưu tài liệu khi đóng.\n\nChi tiết: {ex.Message}",
                        "Lỗi lưu tài liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            base.OnFormClosing(e);
        }
    }
}
