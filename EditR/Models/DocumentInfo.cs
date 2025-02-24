namespace EditR.Models;

public class DocumentInfo
{
    public readonly string[] Fonts = ["Arial", "Times New Roman"];
    public int FontSize { get; set; } = 11;
    public float PixelSize { get; set; } = 11 * TextUtil.PixelPointRatio;
    public string FontName { get; set; } = "Arial";
    public string Color { get; set; } = "rgb(0, 0, 0)";
}