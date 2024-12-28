namespace editor.Models;

/// <summary>
///     Represents the metrics of a character.
/// </summary>
/// <param name="LineHeight">Represents the font height of the character.</param>
/// <param name="Padding">Represents the padding of the character.</param>
public record CharMetric(float LineHeight, float Padding)
{
    /// <summary>
    ///     Represents the quantity of the character.
    /// </summary>
    public int Quantity { get; set; } = 1;
}