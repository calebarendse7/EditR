using System.Collections.Immutable;
using SkiaSharp;

namespace EditR.Services;

public interface IFontService
{
    Task LoadFont(int fontIndex);
    ImmutableDictionary<int, SKTypeface> GetAllFonts();
    SKTypeface GetFont(int fontIndex);
}