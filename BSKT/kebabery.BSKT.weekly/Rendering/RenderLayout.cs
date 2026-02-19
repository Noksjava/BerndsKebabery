namespace OverlayMaker.Rendering
{
    /// <summary>
    /// Layout coordinates for the overlay. Tweak these once to match your Figma template.
    /// </summary>
    public sealed class RenderLayout
    {
        public int StartY { get; set; } = 40;
        public int RowH { get; set; } = 120;

        public int XRank { get; set; } = 40;
        public int XIcon { get; set; } = 110;
        public int IconSize { get; set; } = 68;

        public int XName { get; set; } = 220;

        public int X24 { get; set; } = 700;
        public int X7d { get; set; } = 960;

        public int NameMaxChars { get; set; } = 16;
    }
}
