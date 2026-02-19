using System.Globalization;
using System.Text.Json;
using OverlayMaker.Models;

namespace OverlayMaker.Services
{
    public sealed class OverlayJsonBuilder
    {
        public OverlayBuildResult BuildFromApi(JsonDocument doc, int width = 1200, int height = 650)
        {
            var logs = new List<string>();
            var rowsArray = FindRowsArray(doc.RootElement, logs);
            if (rowsArray.ValueKind != JsonValueKind.Array)
                throw new Exception("No suitable basket rows array found in API response.");

            var rows = new List<OverlayRow>();
            int rank = 1;
            foreach (var item in rowsArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var name = PickString(item, ["name"], out var nameKey) ?? "";
                var symbol = PickNestedString(item, ["metadata", "symbol"], out var tickerKey) ?? "";
                var ticker = string.IsNullOrWhiteSpace(symbol) ? "" : "$" + symbol.ToUpperInvariant();

                var ch24 = PickNumber(item, ["24hourPriceChange"], out var ch24Key);
                var ch7 = PickNumber(item, ["7DaysPriceChange"], out var ch7Key);
                var icon = PickNestedString(item, ["metadata", "image"], out var iconKey) ?? "";

                logs.Add($"row#{rank}: name={nameKey ?? "<none>"}, ticker={tickerKey ?? "<none>"}, 24h={ch24Key ?? "<none>"}, 7d={ch7Key ?? "<none>"}, icon={iconKey ?? "<none>"}");

                rows.Add(new OverlayRow
                {
                    Rank = rank,
                    Name = name,
                    Ticker = ticker,
                    Change24h = ch24,
                    Change7d = ch7,
                    Icon = icon
                });

                rank++;
                if (rows.Count == 5) break;
            }

            if (rows.Count < 5)
                throw new Exception($"API conversion returned only {rows.Count} rows; need 5.");

            var payload = new OverlayPayload
            {
                Width = width,
                Height = height,
                Rows = rows.ToArray()
            };

            Validate(payload);
            return new OverlayBuildResult(payload, logs);
        }

        public static void Validate(OverlayPayload payload)
        {
            if (payload.Rows.Length != 5) throw new Exception("Validation failed: expected exactly 5 rows.");
            foreach (var row in payload.Rows)
            {
                _ = row.Change24h + row.Change7d;
            }
        }

        private static JsonElement FindRowsArray(JsonElement root, List<string> logs)
        {
            foreach (var probe in new[] { "data", "items", "results", "rows" })
            {
                if (TryGetProperty(root, probe, out var value))
                {
                    if (value.ValueKind == JsonValueKind.Array && LooksLikeRowsArray(value))
                    {
                        logs.Add($"array source: root.{probe}");
                        return value;
                    }

                    if (value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var nested in new[] { "docs", "items", "results", "rows", "data" })
                        {
                            if (TryGetProperty(value, nested, out var nestedVal) && nestedVal.ValueKind == JsonValueKind.Array && LooksLikeRowsArray(nestedVal))
                            {
                                logs.Add($"array source: root.{probe}.{nested}");
                                return nestedVal;
                            }
                        }
                    }
                }
            }

            if (TryFindPlausibleArray(root, "$", out var anyPath, out var anyArray))
            {
                logs.Add($"array source: dynamic search at {anyPath}");
                return anyArray;
            }

            return default;
        }

        private static bool TryFindPlausibleArray(JsonElement node, string path, out string foundPath, out JsonElement found)
        {
            if (node.ValueKind == JsonValueKind.Array && LooksLikeRowsArray(node))
            {
                foundPath = path;
                found = node;
                return true;
            }

            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in node.EnumerateObject())
                {
                    if (TryFindPlausibleArray(prop.Value, $"{path}.{prop.Name}", out foundPath, out found))
                        return true;
                }
            }

            foundPath = "";
            found = default;
            return false;
        }

        private static bool LooksLikeRowsArray(JsonElement arr)
        {
            int checkedCount = 0;
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                checkedCount++;
                bool hasNameLike = HasAnyProperty(item, ["name", "title"]);
                bool hasTickerLike = HasAnyProperty(item, ["symbol", "ticker"]) || (TryGetProperty(item, "metadata", out var m) && HasAnyProperty(m, ["symbol"]));
                bool hasChangeLike = HasAnyProperty(item, ["7DaysPriceChange", "24hourPriceChange", "priceChange7d", "change7d", "weeklyPriceChange", "priceChange24h", "change24h", "dailyPriceChange", "24hPriceChange"]);
                if (hasNameLike && (hasTickerLike || hasChangeLike)) return true;
                if (checkedCount >= 5) break;
            }

            return false;
        }

        private static string? PickString(JsonElement obj, IEnumerable<string> keys, out string? used)
        {
            foreach (var key in keys)
            {
                if (!TryGetProperty(obj, key, out var val)) continue;
                if (val.ValueKind == JsonValueKind.String)
                {
                    used = key;
                    return val.GetString();
                }

                if (val.ValueKind == JsonValueKind.Number)
                {
                    used = key;
                    return val.ToString();
                }
            }

            used = null;
            return null;
        }

        private static string? PickNestedString(JsonElement obj, IReadOnlyList<string> path, out string? used)
        {
            if (TryGetNestedProperty(obj, path, out var val))
            {
                if (val.ValueKind == JsonValueKind.String)
                {
                    used = string.Join('.', path);
                    return val.GetString();
                }

                if (val.ValueKind == JsonValueKind.Number)
                {
                    used = string.Join('.', path);
                    return val.ToString();
                }
            }

            used = null;
            return null;
        }

        private static double PickNumber(JsonElement obj, IEnumerable<string> keys, out string? used)
        {
            foreach (var key in keys)
            {
                if (!TryGetProperty(obj, key, out var val)) continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d))
                {
                    used = key;
                    return d;
                }

                if (val.ValueKind == JsonValueKind.String)
                {
                    var parsed = ParseNumericString(val.GetString());
                    if (parsed.HasValue)
                    {
                        used = key;
                        return parsed.Value;
                    }
                }
            }

            used = null;
            return 0;
        }

        private static double? ParseNumericString(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var clean = s.Trim().Replace("%", "");
            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            return null;
        }

        private static bool HasAnyProperty(JsonElement obj, IEnumerable<string> keys)
            => keys.Any(k => TryGetProperty(obj, k, out _));

        private static bool TryGetNestedProperty(JsonElement obj, IReadOnlyList<string> path, out JsonElement value)
        {
            value = obj;
            foreach (var segment in path)
            {
                if (!TryGetProperty(value, segment, out var next))
                {
                    value = default;
                    return false;
                }

                value = next;
            }

            return true;
        }

        private static bool TryGetProperty(JsonElement obj, string propertyName, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in obj.EnumerateObject())
                {
                    if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = p.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }

    public sealed record OverlayBuildResult(OverlayPayload Payload, IReadOnlyList<string> Logs);
}
