namespace editor.Models;

/// <summary>
///     Represents the metrics of a character.
/// </summary>
/// <param name="lineHeight">Represents the font height of the character.</param>
/// <param name="padding">Represents the padding of the character.</param>
public class CharMetric(float lineHeight, float padding)
{
    /// <summary>
    ///     Represents the line height of the character.
    /// </summary>
    public float LineHeight { get; } = lineHeight;

    /// <summary>
    ///     Represents the line padding of the character.
    /// </summary>
    public float Padding { get; } = padding;

    /// <summary>
    ///     Represents the quantity of the character.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     Deconstructs a CharMetric.
    /// </summary>
    /// <param name="height">The height of the char.</param>
    /// <param name="padding">The height of the char.</param>
    /// <param name="quantity">The height of the char.</param>
    public void Deconstruct(out float height, out float padding, out int quantity)
    {
        height = LineHeight;
        padding = Padding;
        quantity = Quantity;
    }

    public override string ToString()
    {
        return $"(LineHeight: {LineHeight}, Padding: {Padding}, Quantity: {Quantity})";
    }
}