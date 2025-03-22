namespace EditR.Models;

public class StyledChar
{
    public required char Value { get; init; }
    public required int FontIndex { get; set; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public required float Padding { get; init; }
    public required float Size { get; init; }
    public required string Color { get; init; }
    public required int PtSize { get; init; }
    public float Column { get; set; }
    public int RowNum { get; set; } = -1;
    public float Row { get; set; }

    public void Deconstruct(out char value, out float width, out float height, out float padding, out float size,
        out string color)
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
        return $"(Value: {p}, Column: {Column}, Row: {Row}, RowNumber: {RowNum})";
    }
}