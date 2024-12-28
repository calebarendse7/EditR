using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace editor.Models;

public class Document
{
    private const float PixelPointRatio = 96f / 72;
    private const float TabWidth = 0.5f * 96;
    private readonly float _cBottom;
    private readonly float _cCenter;
    private readonly Cursor _cursor;
    private readonly SKCanvasView _cView;

    private readonly float _lineSpace;

    private readonly (float Width, float Height, float LMargin, float RMargin, float TMargin, float BMargin) _page;
    private readonly float _pBottomEnd;
    private readonly float _pGap;
    private readonly TextBank _textBank;

    private float _drawStart;
    private float _fontSize;
    private float _lastDrawY;
    private SKTypeface _typeface;

    /// <summary>
    ///     Creates a document with the specified parameters.
    /// </summary>
    /// <param name="cView">A reference to the canvas</param>
    /// <param name="cHeight">The height of the canvas</param>
    /// <param name="pWidth">The width of each page.</param>
    /// <param name="pHeight">The height of each page.</param>
    /// <param name="pGap">The space between each page.</param>
    /// <param name="lMargin">The left margin of each page.</param>
    /// <param name="rMargin">The right margin of each page.</param>
    /// <param name="tMargin">The top margin of each page.</param>
    /// <param name="bMargin">The bottom margin of each page.</param>
    /// <param name="fontSize">The size for page text and the cursor.</param>
    /// <param name="lineSpace">The line space for lines on each page.</param>
    public Document(SKCanvasView cView, float cHeight, float pWidth, float pHeight, float pGap, float lMargin,
        float rMargin, float tMargin, float bMargin, float fontSize, float lineSpace)
    {
        _cView = cView;
        _cCenter = cHeight - pWidth / 2;
        _cBottom = cHeight - tMargin;

        _page = (pWidth, pHeight, lMargin, rMargin, tMargin, bMargin);
        _drawStart = pGap;
        _pGap = pGap;
        _lineSpace = lineSpace;

        _pBottomEnd = pHeight + pGap;

        _fontSize = fontSize * PixelPointRatio;

        _cursor = new Cursor((_cCenter + lMargin, _drawStart + tMargin),
            _cCenter + pWidth - rMargin);
        _typeface = SKTypeface.Default;
        _textBank = new TextBank((_cCenter + lMargin, _cCenter + pWidth - rMargin),
            (_page.TMargin, pHeight - bMargin, _pBottomEnd));
        _textBank.SetOffset(_drawStart);
    }

    /// <summary>
    ///     Draws the pages of the document.
    /// </summary>
    /// <param name="canvas">The canvas where the pages are drawn.</param>
    public void DrawPages(SKCanvas canvas)
    {
        canvas.Clear(SKColors.White);
        using SKPaint pagePaint = new();
        pagePaint.Style = SKPaintStyle.Fill;
        pagePaint.Color = SKColors.White;
        pagePaint.IsAntialias = true;

        using SKPaint pageOutlinePaint = new();
        pageOutlinePaint.Style = SKPaintStyle.Stroke;
        pageOutlinePaint.Color = SKColors.LightGray;
        pageOutlinePaint.StrokeWidth = 1;

        for (var i = 0; i < _textBank.PageCount; i++)
        {
            var y = _drawStart + i * _pBottomEnd;
            canvas.DrawRect(_cCenter, y, _page.Width, _page.Height, pagePaint);
            canvas.DrawRect(_cCenter, y, _page.Width, _page.Height, pageOutlinePaint);
        }
    }

    /// <summary>
    ///     Draws the cursor.
    /// </summary>
    /// <param name="canvas">The canvas where the cursor is drawn.</param>
    public void DrawCursor(SKCanvas canvas)
    {
        using var cursorPaint = new SKPaint();
        cursorPaint.Style = SKPaintStyle.Fill;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1.5f;

        if (_textBank.CharCount == 0)
            _lastDrawY = _drawStart + _page.TMargin +
                         TextUtil.LineHeight(new SKFont(_typeface, _fontSize).Metrics) * _lineSpace;

        canvas.DrawLine(_cursor.Position, _lastDrawY - _fontSize, _cursor.Position, _lastDrawY, cursorPaint);
    }

    /// <summary>
    ///     Scrolls the pages of the document.
    /// </summary>
    /// <param name="deltaY">The amount to scroll.</param>
    public void Scroll(float deltaY)
    {
        _drawStart -= deltaY;

        /* Set limits on scroll */
        if (_drawStart + (_textBank.PageCount - 1) * _pBottomEnd + _page.Height < _cBottom)
            _drawStart = _cBottom - (_page.Height * _textBank.PageCount + _pGap * (_textBank.PageCount - 1));
        else if (_drawStart > _page.TMargin) _drawStart = _page.TMargin;

        _textBank.SetOffset(_drawStart);
        _cView.Invalidate();
    }

    /// <summary>
    ///     Adds a character to the document.
    /// </summary>
    /// <param name="character">The new character.</param>
    public void AddChar(char character)
    {
        using var textPaint = new SKPaint();
        textPaint.TextSize = _fontSize;
        textPaint.Typeface = _typeface;

        var color = SKColors.Empty;
        float width = 0;

        if (character == '\t')
        {
            width = TabWidth;
        }
        else if (character != '\n')
        {
            color = SKColors.Black;
            width = textPaint.MeasureText(char.ToString(character));
        }

        _textBank.AddCharacter(character, width, TextUtil.LineHeight(textPaint.FontMetrics), _fontSize,
            textPaint.FontMetrics.Descent + textPaint.FontMetrics.Leading, color);

        _cView.Invalidate();
    }

    /// <summary>
    ///     Deletes a character from the document.
    /// </summary>
    public void DeleteChar()
    {
        if (_textBank.CharCount == 0) return;
        _textBank.RemoveCharacter();

        _cView.Invalidate();
    }

    /// <summary>
    ///     Draws the document's characters to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas where the characters are drawn.</param>
    public void DrawCharacters(SKCanvas canvas)
    {
        using var textPaint = new SKPaint();
        textPaint.IsAntialias = true;
        textPaint.Typeface = _typeface;

        var line = 0;
        float y = 0;
        foreach (var (value, (_, _, size), (x, _, lNum), color) in _textBank)
        {
            textPaint.TextSize = size;
            textPaint.Color = color;
            if (lNum > line)
            {
                y = _textBank.LineStart(lNum);
                line++;
            }

            canvas.DrawText(char.ToString(value), x, y, textPaint);
        }
    }

    /// <summary>
    ///     Changes the font size of the document.
    /// </summary>
    /// <param name="fontSize">The new font size.</param>
    public void ChangeFontSize(float fontSize)
    {
        _fontSize = fontSize * PixelPointRatio;
        _cView.Invalidate();
    }

    /// <summary>
    ///     Changes the typeface of the document.
    /// </summary>
    /// <param name="typeface">The new typeface.</param>
    public void SetTypeface(SKTypeface typeface)
    {
        _typeface = typeface;
    }
}