namespace EditR.Models;

public class DocumentInfo
{
    public readonly string[] Fonts = ["Arial", "Times New Roman"];
    public int PointSize { get; set; } = 11;
    public float PixelSize { get; set; } = 11 * TextUtil.PixelPointRatio;
    public string FontName { get; set; } = "Arial";
    public string Color { get; set; } = "#000000";
    public float Width { get; set; } = 816;
    public float Height { get; set; } = 1056;
    public float Gap { get; set; } = 50;
    public float LeftMargin { get; set; } = 96;
    public float RightMargin { get; set; } = 96;
    public float TopMargin { get; set; } = 96;
    public float BottomMargin { get; set; } = 96;
    public float LineSpacing { get; set; } = 1.15f;
    public bool DocumentChanged { get; set; } = true;
}