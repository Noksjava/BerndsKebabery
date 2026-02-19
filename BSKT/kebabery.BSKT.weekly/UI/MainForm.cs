using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using OverlayMaker.Models;
using OverlayMaker.Services;
using SkiaSharp;

namespace OverlayMaker.UI
{
    public partial class MainForm : Form
    {
        private const string DefaultAlvaraUrl = "https://web1-api.alvara.xyz/bts/?tab=all&page=1&limit=10&sortBy=7DaysPriceChange&sortOrder=-1";

        private readonly JsonFileService _json = new();
        private readonly Json1FileService _json1 = new();
        private readonly OverlayRenderer _renderer = new();
        private readonly PromptService _prompt = new();
        private readonly WebView2FetchService _webFetch = new();
        private readonly OverlayJsonBuilder _builder = new();
        private readonly IconCacheService _iconCache = new();

        private OverlayPayload? _payload;
        private string _apiUrl = DefaultAlvaraUrl;
        private bool _saveFetchedAsJson1 = true;

        public MainForm()
        {
            InitializeComponent();
            SetFormIcon();
            FormClosed += (_, __) => _webFetch.Dispose();
            SetStatus("Ready. Tip: put PPSupplySans-Regular font into ./fonts for consistent rendering.");
        }

        private void btnErase_Click(object? sender, EventArgs e)
        {
            ClearWorkspace();
        }

        private async void btnFetchAlvara_Click(object? sender, EventArgs e)
        {
            var requestUrl = _apiUrl.Trim();
            if (string.IsNullOrWhiteSpace(requestUrl))
            {
                SetStatus("API URL cannot be empty.");
                return;
            }

            ToggleFetchUi(false);
            try
            {
                SetStatus("Fetching via Alvara API...");
                var fetch = await _webFetch.FetchTextAsync(requestUrl);
                AppendStatus($"Raw jsResult: {fetch.JsResult}");
                AppendStatus($"Body/source: {fetch.Body}");
                WriteApiDebugSnapshots(fetch.Body);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "webview_jsResult.txt"), fetch.JsResult);

                if (!string.IsNullOrEmpty(fetch.Decoded))
                {
                    AppendStatus($"Decoded jsResult: {fetch.Decoded}");
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "webview_decoded.txt"), fetch.Decoded);
                }
                else
                {
                    AppendStatus("Decoded jsResult: <deserialize failed>");
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "webview_decoded.txt"), string.Empty);
                }

                if (fetch.Status != 200)
                {
                    var preview = fetch.Body.Length > 300 ? fetch.Body.Substring(0, 300) + "..." : fetch.Body;
                    AppendStatus($"Fetch failed with status {fetch.Status}: {preview}");
                    return;
                }

                using var doc = JsonDocument.Parse(fetch.Body);
                var build = _builder.BuildFromApi(doc);

                var imagePaths = await DownloadTopRowIconsAsync(build.Payload.Rows);
                OverlayJsonBuilder.Validate(build.Payload);
                var logSummary = string.Join(" | ", build.Logs.Take(3));

                if (_saveFetchedAsJson1)
                {
                    using var sfd = new SaveFileDialog();
                    sfd.Filter = "JSON1 file (*.json1)|*.json1";
                    sfd.Title = "Save fetched .json1";
                    sfd.FileName = "alvara-top5.json1";
                    sfd.InitialDirectory = AppContext.BaseDirectory;

                    var targetPath = sfd.ShowDialog() == DialogResult.OK
                        ? sfd.FileName
                        : Path.Combine(AppContext.BaseDirectory, sfd.FileName);

                    _json1.CompilePayload(build.Payload, imagePaths, targetPath);

                    _payload = _json1.Load(targetPath);
                    lblFile.Text = targetPath;
                    RenderPreview();
                    SetStatus($"Fetched via Alvara API and saved JSON1: {targetPath}. {logSummary}");
                    return;
                }

                _payload = build.Payload;
                lblFile.Text = "Fetched from API (not saved as .json1)";
                RenderPreview();
                SetStatus($"Fetched via Alvara API. JSON1 save skipped. {logSummary}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Fetch via Alvara API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus($"Fetch failed: {ex.Message}");
            }
            finally
            {
                ToggleFetchUi(true);
            }
        }

        private void btnLoadJson_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Overlay files (*.json;*.json1)|*.json;*.json1|JSON files (*.json)|*.json|JSON1 files (*.json1)|*.json1|All files (*.*)|*.*";
            ofd.Title = "Select overlay file";

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                _payload = Path.GetExtension(ofd.FileName).Equals(".json1", StringComparison.OrdinalIgnoreCase)
                    ? _json1.Load(ofd.FileName)
                    : _json.Load(ofd.FileName);

                lblFile.Text = ofd.FileName;

                RenderPreview();
                SetStatus("Overlay file loaded and preview rendered.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private void btnCompileJson1_Click(object? sender, EventArgs e)
        {
            try
            {
                using var ofdJson = new OpenFileDialog();
                ofdJson.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                ofdJson.Title = "Select source JSON";
                if (ofdJson.ShowDialog() != DialogResult.OK) return;

                using var ofdImages = new OpenFileDialog();
                ofdImages.Filter = "Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All files (*.*)|*.*";
                ofdImages.Title = "Select 5 images (will map #1..#5 by alphabetical filename)";
                ofdImages.Multiselect = true;
                if (ofdImages.ShowDialog() != DialogResult.OK) return;

                var picked = ofdImages.FileNames
                    .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (picked.Length != 5)
                {
                    SetStatus("Select exactly 5 pictures.");
                    return;
                }

                using var sfd = new SaveFileDialog();
                sfd.Filter = "JSON1 file (*.json1)|*.json1";
                sfd.Title = "Save compiled .json1";
                sfd.FileName = Path.GetFileNameWithoutExtension(ofdJson.FileName) + ".json1";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                _json1.Compile(ofdJson.FileName, picked, sfd.FileName);
                SetStatus($"Compiled .json1: {sfd.FileName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private void btnSavePng_Click(object? sender, EventArgs e)
        {
            if (_payload == null)
            {
                SetStatus("Load JSON or JSON1 first.");
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "PNG Image (*.png)|*.png";
            sfd.Title = "Save overlay PNG";
            sfd.FileName = "overlay.png";

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var bytes = _renderer.RenderToPngBytes(_payload);
                File.WriteAllBytes(sfd.FileName, bytes);
                SetStatus($"Saved: {sfd.FileName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private void btnCopyPng_Click(object? sender, EventArgs e)
        {
            if (_payload == null)
            {
                SetStatus("Load JSON or JSON1 first.");
                return;
            }

            try
            {
                var bytes = _renderer.RenderToPngBytes(_payload);
                ClipboardService.CopyPngImage(bytes);
                SetStatus("PNG copied to clipboard.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private void btnCopyPrompt_Click(object? sender, EventArgs e)
        {
            var promptText = _prompt.BuildPrompt();
            ClipboardService.CopyText(promptText);
            SetStatus("AI prompt copied to clipboard.");
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            using var settings = new SettingsForm(_apiUrl, _saveFetchedAsJson1);
            if (settings.ShowDialog(this) != DialogResult.OK)
                return;

            _apiUrl = settings.ApiUrl;
            _saveFetchedAsJson1 = settings.SaveFetchedAsJson1;
            SetStatus("Settings updated.");
        }

        private void RenderPreview()
        {
            if (_payload == null) return;

            using var bmp = _renderer.RenderToBitmap(_payload);
            picPreview.Image?.Dispose();
            picPreview.Image = ToGdiBitmap(bmp);
        }

        private void ClearWorkspace()
        {
            _payload = null;
            lblFile.Text = "No JSON loaded.";
            picPreview.Image?.Dispose();
            picPreview.Image = null;
            var cleanedDirs = _json1.CleanupExtractedFiles();
            SetStatus($"Erased current workspace. Cleaned {cleanedDirs} extracted json1 folder(s).");
        }

        private void WriteApiDebugSnapshots(string body)
        {
            var lastApiPath = Path.Combine(AppContext.BaseDirectory, "last_api_body.json");
            File.WriteAllText(lastApiPath, body);

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (!TryGetFirstDoc(doc.RootElement, out var firstDoc))
                {
                    AppendStatus("docs[0] not found at $.data.docs[0].");
                    return;
                }

                var firstDocPath = Path.Combine(AppContext.BaseDirectory, "first_doc.json");
                var pretty = JsonSerializer.Serialize(firstDoc, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(firstDocPath, pretty);

                var topLevelKeys = firstDoc.EnumerateObject().Select(p => p.Name).ToArray();
                AppendStatus($"docs[0] top-level keys: {string.Join(", ", topLevelKeys)}");

                foreach (var nestedKey in new[] { "token", "project", "bskt", "metadata", "profile" })
                {
                    if (!firstDoc.TryGetProperty(nestedKey, out var nested) || nested.ValueKind != JsonValueKind.Object)
                        continue;

                    var nestedKeys = nested.EnumerateObject().Select(p => p.Name).ToArray();
                    AppendStatus($"docs[0].{nestedKey} keys: {string.Join(", ", nestedKeys)}");
                }
            }
            catch (Exception ex)
            {
                AppendStatus($"Could not parse API body for docs[0] snapshot: {ex.Message}");
            }
        }

        private static bool TryGetFirstDoc(JsonElement root, out JsonElement firstDoc)
        {
            firstDoc = default;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                return false;
            if (!data.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array)
                return false;

            using var e = docs.EnumerateArray();
            if (!e.MoveNext()) return false;
            firstDoc = e.Current;
            return firstDoc.ValueKind == JsonValueKind.Object;
        }

        private Task<string[]> DownloadTopRowIconsAsync(OverlayRow[] rows)
        {
            var workDir = Path.Combine(AppContext.BaseDirectory, "work", "alvara-icons", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDir);

            var output = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var row = rows[i];
                var icon = row.Icon?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(icon))
                    continue;

                try
                {
                    var sourcePath = icon;
                    if (Uri.TryCreate(icon, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                        sourcePath = _iconCache.GetIconPath(icon);

                    if (!File.Exists(sourcePath))
                        continue;

                    var ext = Path.GetExtension(sourcePath);
                    if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".png";
                    var path = Path.Combine(workDir, $"{i + 1:00}_{SanitizeFileName(row.Name)}{ext}");
                    File.Copy(sourcePath, path, overwrite: true);
                    output.Add(path);
                }
                catch
                {
                    // icon is optional
                }
            }

            return Task.FromResult(output.ToArray());
        }

        private static string SanitizeFileName(string? name)
        {
            var val = string.IsNullOrWhiteSpace(name) ? "basket" : name;
            foreach (var c in Path.GetInvalidFileNameChars()) val = val.Replace(c, '_');
            return val.Trim();
        }

        private void ToggleFetchUi(bool enabled)
        {
            btnFetchAlvara.Enabled = enabled;
            btnSettings.Enabled = enabled;
        }

        private void SetFormIcon()
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "bskt_ico1.ico");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(Application.StartupPath, "bskt_ico1.ico");
            }

            if (File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }
        }

        private static Bitmap ToGdiBitmap(SKBitmap skBitmap)
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());
            return new Bitmap(ms);
        }

        private void SetStatus(string msg)
        {
            txtStatus.Text = msg;
        }

        private void AppendStatus(string msg)
        {
            txtStatus.Text = string.IsNullOrWhiteSpace(txtStatus.Text)
                ? msg
                : txtStatus.Text + Environment.NewLine + msg;
        }
    }
}
