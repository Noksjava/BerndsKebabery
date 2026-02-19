namespace OverlayMaker.Models
{
    public sealed class OverlayPayload
    {
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 650;
        public OverlayRow[] Rows { get; set; } = [];
        public OverlayStyle? Style { get; set; } = null;
    }

    public sealed class OverlayRow
    {
        public int? Rank { get; set; }
        public string Name { get; set; } = "";
        public string Ticker { get; set; } = "";
        public double Change24h { get; set; }
        public double Change7d { get; set; }
        public string Icon { get; set; } = ""; // local path (relative or absolute)
        public string? Color24h { get; set; }  // optional override hex
        public string? Color7d { get; set; }   // optional override hex
    }

    public sealed class OverlayStyle
    {
        public string Color_Pos { get; set; } = "#16B89A";
        public string Color_Neg { get; set; } = "#EB5757";
        public string Color_Text { get; set; } = "#2A2233";
        public string Color_Label { get; set; } = "#9B95A6";
        public string Pill_Fill { get; set; } = "#E84FA8";
    }
}
