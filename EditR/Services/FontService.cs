using System.Collections.Immutable;
using SkiaSharp;

namespace EditR.Services;

public class FontService(HttpClient client) : IFontService
{
    private ImmutableDictionary<int, SKTypeface> _fonts = ImmutableDictionary<int, SKTypeface>.Empty;

    public async Task LoadFont(int fontIndex)
    {
        if (_fonts.ContainsKey(fontIndex)) return;
        await using var stream = await client.GetStreamAsync($"fonts/{fontIndex}.ttf");
        var t = SKTypeface.FromStream(stream);
        _fonts = _fonts.Add(fontIndex, t);
    }

    public ImmutableDictionary<int, SKTypeface> GetAllFonts()
    {
        return _fonts;
    }

    public SKTypeface GetFont(int fontIndex)
    {
        return _fonts[fontIndex];
    }
}