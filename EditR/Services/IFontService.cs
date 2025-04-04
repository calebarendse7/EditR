using System.Collections.Immutable;
using EditR.Models;
using SkiaSharp;

namespace EditR.Services;

public interface IFontService
{
    Task LoadFont(Font name);
    ImmutableDictionary<Font, SKTypeface> GetAllFonts();
    SKTypeface GetFont(Font fontIndex);
}