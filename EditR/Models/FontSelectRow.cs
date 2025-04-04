namespace EditR.Models;

public readonly struct FontSelectRow
{
    public required string Value { get; init; }
    public required Font Name { get; init; }

    public override string ToString()
    {
        return Value;
    }
}