namespace EditR.Models;

/// <summary>
///     Represents the metrics of a character.
/// </summary>
/// <param name="LineHeight">Represents the font height of the character.</param>
/// <param name="Padding">Represents the padding of the character.</param>
/// <param name="Quantity">Represents </param>
public class CharMetric
{
    public required float LineHeight { get; init; }
    public required float Padding { get; init; }
    public required int Quantity { get; set; }

    public void Deconstruct(out float lineHeight, out float padding, out int quantity)
    {
        lineHeight = LineHeight;
        padding = Padding;
        quantity = Quantity;
    }
}