using SkiaSharp;

namespace editor.Models;

/// <summary>
///     Represents a character.
/// </summary>
/// <param name="Value">Represents the value of the character.</param>
/// <param name="Font">Represents the font details of the character.</param>
/// <param name="Position">Represents the visual position of the character.</param>
/// <param name="Color">Represents the color of the character.</param>
public record StyledCharacter(
    char Value,
    (float Width, float Height, float Size) Font,
    (float X, int PNum, int LNum) Position,
    SKColor Color);