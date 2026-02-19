using SkiaSharp;
using OverlayMaker.Models;

namespace OverlayMaker.Rendering
{
    public sealed class RenderStyle
    {
        public SKColor Pos { get; }
        public SKColor Neg { get; }
        public SKColor Text { get; }
        public SKColor Label { get; }
        public SKColor PillFill { get; }

        public RenderStyle(OverlayStyle? s)
        {
            s ??= new OverlayStyle();
            Pos = Hex(s.Color_Pos);
            Neg = Hex(s.Color_Neg);
            Text = Hex(s.Color_Text);
            Label = Hex(s.Color_Label);
            PillFill = Hex(s.Pill_Fill);
        }

        public static SKColor Hex(string h)
        {
            h = (h ?? "").Trim().TrimStart('#');
            if (h.Length == 3) h = string.Concat(h[0], h[0], h[1], h[1], h[2], h[2]);
            if (h.Length != 6) return new SKColor(0, 0, 0, 255);

            byte r = Convert.ToByte(h.Substring(0, 2), 16);
            byte g = Convert.ToByte(h.Substring(2, 2), 16);
            byte b = Convert.ToByte(h.Substring(4, 2), 16);
            return new SKColor(r, g, b, 255);
        }
    }
}
