using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OverlayMaker.Services
{
    public sealed class WebView2FetchService : IDisposable
    {
        private const string BootstrapUrl = "https://bskt.alvara.xyz/";

        private Form? _host;
        private WebView2? _webView;
        private bool _initialized;

        public async Task<WebView2FetchResult> FetchTextAsync(string requestUrl, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var webView = _webView!.CoreWebView2;
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
            {
                try
                {
                    var message = e.TryGetWebMessageAsString();
                    if (string.IsNullOrWhiteSpace(message)) return;

                    using var envelopeDoc = JsonDocument.Parse(message);
                    if (!envelopeDoc.RootElement.TryGetProperty("type", out var typeEl)) return;
                    if (!string.Equals(typeEl.GetString(), "alvaraFetch", StringComparison.Ordinal)) return;

                    tcs.TrySetResult(message);
                }
                catch
                {
                    // ignore malformed messages
                }
            }

            webView.WebMessageReceived += Handler;
            try
            {
                var encodedUrl = JsonSerializer.Serialize(requestUrl);
                var script = @"(() => {
  const API_URL = __API_URL__;
  fetch(API_URL, { method:'GET', mode:'cors', credentials:'include', cache:'no-store',
    headers:{'Accept':'application/json, text/plain, */*'} })
    .then(r => r.text().then(body => ({status:r.status, body})))
    .then(({status, body}) => chrome.webview.postMessage(JSON.stringify({type:'alvaraFetch', ok:true, status, body})))
    .catch(e => chrome.webview.postMessage(JSON.stringify({type:'alvaraFetch', ok:false, error:String(e)})));
})();".Replace("__API_URL__", encodedUrl);

                _ = await webView.ExecuteScriptAsync(script);

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), ct);
                var completed = await Task.WhenAny(tcs.Task, timeoutTask);
                if (completed != tcs.Task)
                    throw new TimeoutException("Timed out waiting for WebView2 fetch result.");

                var payloadJson = await tcs.Task;
                return ParseFetchPayload(payloadJson);
            }
            finally
            {
                webView.WebMessageReceived -= Handler;
            }
        }

        private static WebView2FetchResult ParseFetchPayload(string payloadJson)
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            var ok = root.TryGetProperty("ok", out var okEl) && okEl.ValueKind is JsonValueKind.True;
            if (!ok)
            {
                var error = root.TryGetProperty("error", out var errEl) ? errEl.GetString() ?? "Unknown fetch error." : "Unknown fetch error.";
                return new WebView2FetchResult(0, error, payloadJson, string.Empty);
            }

            var status = root.TryGetProperty("status", out var statusEl) && statusEl.TryGetInt32(out var parsedStatus)
                ? parsedStatus
                : 0;
            var body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? string.Empty : string.Empty;
            return new WebView2FetchResult(status, body, payloadJson, string.Empty);
        }

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_initialized) return;

            _host = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(1, 1),
                Location = new Point(-32000, -32000),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Opacity = 0
            };

            _webView = new WebView2 { Dock = DockStyle.Fill };
            _host.Controls.Add(_webView);
            _host.Show();

            try
            {
                await _webView.EnsureCoreWebView2Async();
            }
            catch (WebView2RuntimeNotFoundException ex)
            {
                throw new Exception(
                    "Microsoft Edge WebView2 Runtime is not installed. Please install it from https://developer.microsoft.com/microsoft-edge/webview2/ and restart the app.",
                    ex);
            }

            await NavigateAndWaitAsync(_webView, BootstrapUrl, ct);
            await WaitForDomContentLoadedAsync(_webView, ct);
            _initialized = true;
        }

        private static Task NavigateAndWaitAsync(WebView2 webView, string url, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                webView.NavigationCompleted -= Handler;
                if (e.IsSuccess) tcs.TrySetResult(true);
                else tcs.TrySetException(new Exception($"WebView2 navigation failed: {e.WebErrorStatus}"));
            }

            webView.NavigationCompleted += Handler;
            webView.CoreWebView2.Navigate(url);

            if (ct.CanBeCanceled)
                ct.Register(() => tcs.TrySetCanceled(ct));

            return tcs.Task;
        }

        private static async Task WaitForDomContentLoadedAsync(WebView2 webView, CancellationToken ct)
        {
            var readyStateResult = await webView.CoreWebView2.ExecuteScriptAsync("document.readyState");
            var readyState = JsonSerializer.Deserialize<string>(readyStateResult) ?? string.Empty;
            if (string.Equals(readyState, "interactive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(readyState, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
            {
                webView.CoreWebView2.DOMContentLoaded -= Handler;
                tcs.TrySetResult(true);
            }

            webView.CoreWebView2.DOMContentLoaded += Handler;

            if (ct.CanBeCanceled)
                ct.Register(() => tcs.TrySetCanceled(ct));

            await tcs.Task;
        }

        public void Dispose()
        {
            _webView?.Dispose();
            _host?.Close();
            _host?.Dispose();
        }
    }

    public sealed record WebView2FetchResult(int Status, string Body, string JsResult, string Decoded);
}
