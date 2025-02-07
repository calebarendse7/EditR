using SkiaSharp;

namespace editor.Models;

public class StyledChar
{
    public char Value { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float Padding { get; init; }
    public float Size { get; init; }
    public SKColor Color { get; init; }
    public float Column { get; set; }
    public int RowNum { get; set; }
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
        return $"Value: {Value}, Column: {Column}, Row: {RowNum}";
    }
}