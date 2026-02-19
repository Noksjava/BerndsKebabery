using SkiaSharp;
using OverlayMaker.Models;
using OverlayMaker.Rendering;

namespace OverlayMaker.Services
{
    public sealed class OverlayRenderer
    {
        private readonly RenderLayout _layout = new();
        private readonly string _fontPath = Path.Combine(AppContext.BaseDirectory, "fonts", "PPSupplySans-Regular.ttf");
        private readonly IconCacheService _iconCache = new();

        public RenderLayout Layout => _layout;
        public bool DebugLayoutOnce { get; set; }

        public SKBitmap RenderToBitmap(OverlayPayload payload)
        {
            var style = new RenderStyle(payload.Style);

            var info = new SKImageInfo(payload.Width, payload.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bmp = new SKBitmap(info);
            using var canvas = new SKCanvas(bmp);
            canvas.Clear(SKColors.Transparent);

            var tf = File.Exists(_fontPath) ? SKTypeface.FromFile(_fontPath) : SKTypeface.Default;

            const float topPad = 18f;
            const float bottomPad = 18f;
            float rowH = (payload.Height - topPad - bottomPad) / 5f;
            float iconSize = Clamp(rowH * 0.78f, 82f, 110f);
            const float iconX = 115f;
            const float rankX = 40f;
            float nameX = iconX + iconSize + 26f;
            float change24X = payload.Width * 0.62f;
            float change7dX = payload.Width * 0.80f;

            using var paintName = new SKPaint { Typeface = tf, TextSize = Clamp(rowH * 0.34f, 36f, 58f), IsAntialias = true, Color = style.Text };
            using var paintTicker = new SKPaint { Typeface = tf, TextSize = Clamp(rowH * 0.23f, 24f, 38f), IsAntialias = true, Color = SKColors.White };
            using var paintPct = new SKPaint { Typeface = tf, TextSize = Clamp(rowH * 0.37f, 40f, 66f), IsAntialias = true, IsStroke = false };
            using var paintLabel = new SKPaint { Typeface = tf, TextSize = Clamp(rowH * 0.19f, 22f, 34f), IsAntialias = true, Color = style.Label };
            using var paintRank = new SKPaint { Typeface = tf, TextSize = Clamp(rowH * 0.30f, 28f, 48f), IsAntialias = true, Color = style.Label };

            var drawDebug = DebugLayoutOnce;
            DebugLayoutOnce = false;

            for (int i = 0; i < payload.Rows.Length; i++)
            {
                var row = payload.Rows[i];
                var rank = row.Rank ?? (i + 1);

                float rowTop = topPad + i * rowH;
                float rowCenterY = topPad + (i + 0.5f) * rowH;
                float iconY = rowCenterY - iconSize / 2f;
                float rankY = rowCenterY + paintRank.TextSize * 0.35f;
                float nameBaselineY = rowCenterY - 8f;
                float pillY = nameBaselineY + 14f;
                float percentBaselineY = rowCenterY - 2f;
                float labelY = percentBaselineY + paintLabel.TextSize + 8f;

                if (drawDebug)
                    DrawRowDebugGuides(canvas, payload.Width, rowTop, rowH, rowCenterY, nameBaselineY, percentBaselineY, labelY);

                canvas.DrawText($"#{rank}", rankX, rankY, paintRank);

                var drewIcon = false;
                if (!string.IsNullOrWhiteSpace(row.Icon))
                {
                    try
                    {
                        var iconPath = ResolveLocalIconPath(row.Icon);
                        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                        {
                            using var iconSrc = SKBitmap.Decode(iconPath);
                            if (iconSrc != null)
                            {
                                using var circ = CircleCrop(iconSrc, (int)iconSize);
                                canvas.DrawBitmap(circ, iconX, iconY);
                                drewIcon = true;
                            }
                        }
                    }
                    catch
                    {
                        // fallback to identicon
                    }
                }

                if (!drewIcon)
                    DrawIdenticon(canvas, row, iconX, iconY, iconSize, tf);

                var name = row.Name ?? "";
                canvas.DrawText(name, nameX, nameBaselineY, paintName);

                if (!string.IsNullOrWhiteSpace(row.Ticker))
                {
                    var pillHeight = Clamp(rowH * 0.28f, 30f, 46f);
                    var pillPadX = Clamp(rowH * 0.12f, 14f, 20f);
                    var tw = paintTicker.MeasureText(row.Ticker);
                    var pillW = Math.Max(84f, tw + pillPadX * 2f);
                    var pillRect = new SKRect(nameX, pillY, nameX + pillW, pillY + pillHeight);
                    DrawRoundRect(canvas, pillRect, 12, style.PillFill);

                    var fm = paintTicker.FontMetrics;
                    var textBaseline = pillRect.MidY - (fm.Ascent + fm.Descent) / 2f;
                    canvas.DrawText(row.Ticker, nameX + pillPadX, textBaseline, paintTicker);
                }

                var c24 = row.Change24h;
                var c7 = row.Change7d;

                var c24Col = !string.IsNullOrWhiteSpace(row.Color24h) ? RenderStyle.Hex(row.Color24h!) : (c24 < 0 ? style.Neg : style.Pos);
                var c7Col = !string.IsNullOrWhiteSpace(row.Color7d) ? RenderStyle.Hex(row.Color7d!) : (c7 < 0 ? style.Neg : style.Pos);

                paintPct.Color = c24Col;
                canvas.DrawText(FmtPct(c24), change24X, percentBaselineY, paintPct);
                canvas.DrawText("24H Change", change24X, labelY, paintLabel);

                paintPct.Color = c7Col;
                canvas.DrawText(FmtPct(c7), change7dX, percentBaselineY, paintPct);
                canvas.DrawText("7D Change", change7dX, labelY, paintLabel);
            }

            return bmp;
        }

        public byte[] RenderToPngBytes(OverlayPayload payload)
        {
            using var bmp = RenderToBitmap(payload);
            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private string ResolveLocalIconPath(string icon)
        {
            var v = icon.Trim();
            if (Uri.TryCreate(v, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                return _iconCache.GetIconPath(v);

            return v;
        }

        private static void DrawRowDebugGuides(SKCanvas canvas, int width, float rowTop, float rowH, float rowCenterY, float nameBaselineY, float percentBaselineY, float labelY)
        {
            using var boxPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, Color = new SKColor(40, 170, 255, 140), StrokeWidth = 1 };
            using var centerPaint = new SKPaint { IsAntialias = true, Color = new SKColor(255, 99, 132, 180), StrokeWidth = 1 };
            using var baselinePaint = new SKPaint { IsAntialias = true, Color = new SKColor(140, 240, 140, 170), StrokeWidth = 1 };

            canvas.DrawRect(new SKRect(8, rowTop, width - 8, rowTop + rowH), boxPaint);
            canvas.DrawLine(8, rowCenterY, width - 8, rowCenterY, centerPaint);
            canvas.DrawLine(8, nameBaselineY, width - 8, nameBaselineY, baselinePaint);
            canvas.DrawLine(8, percentBaselineY, width - 8, percentBaselineY, baselinePaint);
            canvas.DrawLine(8, labelY, width - 8, labelY, baselinePaint);
        }

        private static float Clamp(float v, float min, float max) => Math.Min(max, Math.Max(min, v));

        private static void DrawIdenticon(SKCanvas canvas, OverlayRow row, float iconX, float iconY, float iconSize, SKTypeface tf)
        {
            var seed = (row.Name ?? row.Ticker ?? "?").Trim();
            if (string.IsNullOrWhiteSpace(seed)) seed = "?";

            var hue = Math.Abs(seed.GetHashCode()) % 360;
            var bg = SKColor.FromHsv(hue, 55, 78);
            var textColor = SKColors.White;
            var cx = iconX + (iconSize / 2f);
            var cy = iconY + (iconSize / 2f);
            var r = iconSize / 2f;

            using (var fill = new SKPaint { IsAntialias = true, Color = bg, Style = SKPaintStyle.Fill })
                canvas.DrawCircle(cx, cy, r, fill);

            var initials = GetInitials(row);
            using var tp = new SKPaint
            {
                Typeface = tf,
                TextSize = iconSize * 0.36f,
                IsAntialias = true,
                Color = textColor,
                TextAlign = SKTextAlign.Center
            };
            var fm = tp.FontMetrics;
            var baseline = cy - (fm.Ascent + fm.Descent) / 2f;
            canvas.DrawText(initials, cx, baseline, tp);
        }

        private static string GetInitials(OverlayRow row)
        {
            var source = !string.IsNullOrWhiteSpace(row.Name) ? row.Name! : row.Ticker ?? "?";
            var parts = source.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpperInvariant();
        }

        private static string FmtPct(double x) => (x < 0) ? $"-{Math.Abs(x):0.00}%" : $"{x:0.00}%";

        private static void DrawRoundRect(SKCanvas canvas, SKRect rect, float radius, SKColor fill)
        {
            using var paint = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(rect, radius, radius, paint);
        }

        private static SKBitmap CircleCrop(SKBitmap src, int size)
        {
            var dst = new SKBitmap(size, size, true);
            using var canvas = new SKCanvas(dst);
            canvas.Clear(SKColors.Transparent);

            float cx = size / 2f;
            float cy = size / 2f;
            float r = size / 2f;

            using var paint = new SKPaint { IsAntialias = true };
            using var path = new SKPath();
            path.AddCircle(cx, cy, r);
            canvas.ClipPath(path, SKClipOperation.Intersect, true);

            float scale = Math.Max((float)size / src.Width, (float)size / src.Height);
            float w = src.Width * scale;
            float h = src.Height * scale;
            float x = (size - w) / 2f;
            float y = (size - h) / 2f;

            canvas.DrawBitmap(src, new SKRect(x, y, x + w, y + h), paint);
            return dst;
        }
    }
}
