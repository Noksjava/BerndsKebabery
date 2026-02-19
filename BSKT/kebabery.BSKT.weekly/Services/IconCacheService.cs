using System.Security.Cryptography;
using System.Text;

namespace OverlayMaker.Services
{
    public sealed class IconCacheService
    {
        private static readonly HttpClient Http = new();
        private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "cache", "icons");

        public string GetIconPath(string iconUrl)
        {
            if (string.IsNullOrWhiteSpace(iconUrl))
                throw new ArgumentException("Icon URL cannot be empty.", nameof(iconUrl));

            if (!Uri.TryCreate(iconUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                throw new ArgumentException("Invalid icon URL.", nameof(iconUrl));

            Directory.CreateDirectory(_cacheDir);

            var ext = Path.GetExtension(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
                ext = ".png";

            var hash = Sha1(iconUrl.Trim());
            var outPath = Path.Combine(_cacheDir, hash + ext.ToLowerInvariant());
            if (File.Exists(outPath))
                return outPath;

            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            req.Headers.Referrer = new Uri("https://bskt.alvara.xyz/");
            req.Headers.TryAddWithoutValidation("Accept", "image/*");

            using var resp = Http.Send(req);
            resp.EnsureSuccessStatusCode();
            var bytes = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            File.WriteAllBytes(outPath, bytes);
            return outPath;
        }

        private static string Sha1(string text)
        {
            var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(text));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
