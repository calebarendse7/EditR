namespace EditR.Models;

public readonly struct FontSelectRow
{
    public required string Value { get; init; }
    public required int Index { get; init; }
    public override string ToString() => Value;
}