using System.Text.Json;
using OverlayMaker.Models;

namespace OverlayMaker.Services
{
    public sealed class Json1FileService
    {
        private static readonly JsonSerializerOptions Opt = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly List<string> _tempDirs = [];

        public void Compile(string jsonPath, string[] imagePaths, string outputPath)
        {
            var payload = new JsonFileService().Load(jsonPath);
            CompilePayload(payload, imagePaths, outputPath);
        }

        public void CompilePayload(OverlayPayload payload, string[] imagePaths, string outputPath)
        {
            var sorted = imagePaths
                .Where(File.Exists)
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();

            var rows = payload.Rows
                .OrderBy(r => r.Rank ?? int.MaxValue)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();

            if (rows.Length != 5)
                throw new Exception("Payload must include exactly 5 rows for JSON1 compilation.");

            var rankedImages = sorted
                .Select(path => new
                {
                    Path = path,
                    FileName = Path.GetFileName(path)
                })
                .Select(x =>
                {
                    var prefix = x.FileName.Split('_', 2)[0];
                    return int.TryParse(prefix, out var rank) && rank is >= 1 and <= 5
                        ? new { x.Path, x.FileName, Rank = rank }
                        : null;
                })
                .Where(x => x != null)
                .Select(x => x!)
                .GroupBy(x => x.Rank)
                .ToDictionary(g => g.Key, g => g.First(), EqualityComparer<int>.Default);

            for (int i = 0; i < 5; i++)
            {
                var rank = i + 1;
                rows[i].Rank = rank;
                rows[i].Icon = "";
                if (rankedImages.TryGetValue(rank, out var image))
                    rows[i].Icon = $"json1://{rank}/{image.FileName}";
            }

            payload.Rows = rows;

            var bundle = new Json1Bundle
            {
                Payload = payload,
                Images = rankedImages
                    .OrderBy(kv => kv.Key)
                    .Select(kv => new Json1Image
                    {
                        Rank = kv.Key,
                        FileName = kv.Value.FileName,
                        ContentBase64 = Convert.ToBase64String(File.ReadAllBytes(kv.Value.Path))
                    })
                    .ToArray()
            };

            var json1 = JsonSerializer.Serialize(bundle, Opt);
            File.WriteAllText(outputPath, json1);
        }

        public OverlayPayload Load(string path)
        {
            var raw = File.ReadAllText(path);
            var bundle = JsonSerializer.Deserialize<Json1Bundle>(raw, Opt);
            if (bundle?.Payload == null) throw new Exception("Invalid .json1 file.");

            bundle.Payload.Rows ??= [];

            var tempDir = Path.Combine(AppContext.BaseDirectory, "work", "json1", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            _tempDirs.Add(tempDir);

            foreach (var image in bundle.Images ?? [])
            {
                if (string.IsNullOrWhiteSpace(image.FileName) || string.IsNullOrWhiteSpace(image.ContentBase64)) continue;

                var bytes = Convert.FromBase64String(image.ContentBase64);
                var outPath = Path.Combine(tempDir, $"{image.Rank}_{image.FileName}");
                File.WriteAllBytes(outPath, bytes);

                var row = bundle.Payload.Rows.FirstOrDefault(r => (r.Rank ?? -1) == image.Rank);
                if (row != null)
                {
                    row.Icon = outPath;
                }
            }

            return bundle.Payload;
        }

        public int CleanupExtractedFiles()
        {
            int deleted = 0;
            foreach (var dir in _tempDirs.ToArray())
            {
                if (!Directory.Exists(dir))
                {
                    _tempDirs.Remove(dir);
                    continue;
                }

                try
                {
                    Directory.Delete(dir, recursive: true);
                    deleted++;
                    _tempDirs.Remove(dir);
                }
                catch
                {
                    // keep directory registered if cleanup fails
                }
            }

            return deleted;
        }

        private sealed class Json1Bundle
        {
            public OverlayPayload? Payload { get; set; }
            public Json1Image[]? Images { get; set; }
        }

        private sealed class Json1Image
        {
            public int Rank { get; set; }
            public string FileName { get; set; } = "";
            public string ContentBase64 { get; set; } = "";
        }
    }
}
