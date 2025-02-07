namespace editor.Models;

/// <summary>
///     Represents the metrics of a character.
/// </summary>
/// <param name="lineHeight">Represents the font height of the character.</param>
/// <param name="padding">Represents the padding of the character.</param>
public class CharMetric(float lineHeight, float padding)
{
    public float LineHeight { get; } = lineHeight;
    public float Padding { get; } = padding;

    public void Deconstruct(out float height, out float padding, out int quantity)
    {
        height = LineHeight;
        padding = Padding;
        quantity = Quantity;
    }

    public void IncQuantity()
    {
        Quantity++;
    }
    public void DecQuantity()
    {
        Quantity--;
    }

    /// <summary>
    ///     Represents the quantity of the character.
    /// </summary>
    public int Quantity { get; private set; } = 1;
}