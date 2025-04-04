using System.Collections.Immutable;
using EditR.Models;
using SkiaSharp;

namespace EditR.Services;

public class FontService(HttpClient client) : IFontService
{
    private ImmutableDictionary<Font, SKTypeface> _fonts = ImmutableDictionary<Font, SKTypeface>.Empty;

    public async Task LoadFont(Font name)
    {
        if (_fonts.ContainsKey(name)) return;
        await using var stream = await client.GetStreamAsync($"fonts/{name}.ttf");
        var t = SKTypeface.FromStream(stream);
        _fonts = _fonts.Add(name, t);
    }

    public ImmutableDictionary<Font, SKTypeface> GetAllFonts()
    {
        return _fonts;
    }

    public SKTypeface GetFont(Font name)
    {
        return _fonts[name];
    }
}