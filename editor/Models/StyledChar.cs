using SkiaSharp;

namespace editor.Models;

public class StyledChar(char value, float width, float height, float padding, float size, int ptSize, SKColor color)
{
    public char Value { get; } = value;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float Padding { get; } = padding;
    public float Size { get; } = size;
    public SKColor Color { get; } = color;

    public int PtSize { get; } = ptSize;
    public float Column { get; set; }
    public int RowNum { get; set; } = -1;
    public float Row { get; set; }


    public void Deconstruct(out char value, out float width, out float height, out float padding, out float size,
        out SKColor color)
    {
        value = Value;
        width = Width;
        height = Height;
        padding = Padding;
        size = Size;
        color = Color;
    }

    public override string ToString()
    {
        var p = Value == '\n' ? "\\n" : Value.ToString();
        return $"Value: {p}, Column: {Column}, Row: {RowNum}, Position {Row}";
    }
}