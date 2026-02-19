using System.Text.Json;
using OverlayMaker.Models;

namespace OverlayMaker.Services
{
    public sealed class JsonFileService
    {
        private static readonly JsonSerializerOptions Opt = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public OverlayPayload Load(string path)
        {
            var json = File.ReadAllText(path);
            var payload = JsonSerializer.Deserialize<OverlayPayload>(json, Opt);
            if (payload == null) throw new Exception("Failed to parse JSON.");
            payload.Rows ??= [];
            return payload;
        }
    }
}
