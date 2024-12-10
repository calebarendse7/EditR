using SkiaSharp;

namespace editor.Models;

public record StyledCharacter(char Value, float FontSize, float FontWidth, (float X, int LineNum, int PNum) Position, SKColor Color);