using SkiaSharp;

namespace editor.Models;

public static class TextUtil
{
    public static float LineHeight(SKFontMetrics metrics)
    {
        return -metrics.Ascent + metrics.Descent + metrics.Leading;
    }
}