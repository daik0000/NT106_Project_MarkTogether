using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Markdig;

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

            webPreview.TabStop = false;

            Shown += TypeRenderForm_Shown;

            txtRawMarkdown.Text =
                "# MarkTogether\r\n\r\n" +
                "Chào mừng bạn đến với **Type Render**.\r\n\r\n" +
                "- Gõ markdown ở khung bên trái\r\n" +
                "- Khung bên phải sẽ render tương ứng\r\n\r\n" +
                "```csharp\r\n" +
                "Console.WriteLine(\"Hello Markdown!\");\r\n" +
                "```";

            RenderMarkdown();
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
            _renderRequestVersion++;
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
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
            _renderDebounceTimer.Tick -= RenderDebounceTimer_Tick;
            _renderDebounceTimer.Dispose();
            if (webPreview.CoreWebView2 != null)
            {
                webPreview.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            }
            Shown -= TypeRenderForm_Shown;
            base.OnFormClosed(e);
        }
    }
}
