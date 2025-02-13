using SkiaSharp;

namespace editor.Models;

public class Document
{
    private const float PixelPointRatio = 96f / 72;
    private const float TabWidth = 0.5f * 96;
    private readonly float _cBottom;
    private readonly float _cCenter;
    private readonly float _lineSpace;
    private readonly (float Width, float Height, float LMargin, float RMargin, float TMargin, float BMargin) _page;
    private readonly float _pBottomEnd;
    private readonly float _pGap;
    private readonly TextBank _textBank;
    private int _cursorPos;
    private float _drawStart;
    private float _fontSize;
    private SKTypeface _typeface;

    /// <summary>
    ///     Creates a document with the specified parameters.
    /// </summary>
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
    public Document(float cHeight, float pWidth, float pHeight, float pGap, float lMargin,
        float rMargin, float tMargin, float bMargin, float fontSize, float lineSpace)
    {
        _cCenter = cHeight - pWidth / 2;
        _cBottom = cHeight - tMargin;

        _page = (pWidth, pHeight, lMargin, rMargin, tMargin, bMargin);
        _drawStart = pGap;
        _pGap = pGap;
        _lineSpace = lineSpace;

        _pBottomEnd = pHeight + pGap;

        _fontSize = fontSize * PixelPointRatio;

        _typeface = SKTypeface.Default;
        _textBank = new TextBank((_cCenter + lMargin, _cCenter + pWidth - rMargin),
            (_page.TMargin, pHeight - bMargin, _pBottomEnd), _drawStart);
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

        for (var i = 0; i < _textBank.PageNum + 1; i++)
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

        float x;
        float y;
        var size = _fontSize;
        if (_textBank.TextCount > 0 && _cursorPos - 1 >= 0)
        {
            var c = _textBank[_cursorPos - 1];
            Console.WriteLine($"Drawing cursor before {c.Value} on row {c.RowNum}({c.Row}) with {_textBank.TextCount}");
            x = c.Column + c.Width;
            y = c.Row;
            size = c.Size;
        }
        else
        {
            x = _cCenter + _page.LMargin;
            y = _drawStart + _page.TMargin +
                TextUtil.LineHeight(new SKFont(_typeface, _fontSize).Metrics) * _lineSpace;
        }

        canvas.DrawLine(x, y - size, x, y, cursorPaint);
    }

    /// <summary>
    ///     Scrolls the pages of the document.
    /// </summary>
    /// <param name="deltaY">The amount to scroll.</param>
    public void Scroll(float deltaY)
    {
        _drawStart -= deltaY;

        /* Set limits on scroll */
        if (_drawStart + _textBank.PageNum * _pBottomEnd + _page.Height < _cBottom)
            _drawStart = _cBottom - (_page.Height * (_textBank.PageNum + 1) + _pGap * _textBank.PageNum);
        else if (_drawStart > _page.TMargin) _drawStart = _page.TMargin;

        _textBank.SetOffset(_drawStart);
    }

    /// <summary>
    ///     Adds a character to the document.
    /// </summary>
    /// <param name="character">The new character.</param>
    public void AddChar(char character)
    {
        using var textPaint = new SKFont();
        textPaint.Size = _fontSize;
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

        _textBank.Add(character, _cursorPos++, width, TextUtil.LineHeight(textPaint.Metrics), _fontSize,
            textPaint.Metrics.Descent + textPaint.Metrics.Leading, color);
    }

    /// <summary>
    ///     Deletes a character from the document.
    /// </summary>
    public void DeleteChar()
    {
        if (_textBank.TextCount == 0 || _cursorPos - 1 < 0) return;
        _textBank.Remove(--_cursorPos);
    }

    /// <summary>
    ///     Draws the document's characters to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas where the characters are drawn.</param>
    public void DrawCharacters(SKCanvas canvas)
    {
        using var textFont = new SKFont();
        using var paint = new SKPaint();
        textFont.Typeface = _typeface;

        foreach (var character in _textBank)
        {
            textFont.Size = character.Size;
            paint.Color = character.Color;
            canvas.DrawText(character.Value.ToString(), character.Column, character.Row, textFont, paint);
        }
    }

    /// <summary>
    ///     Changes the font size of the document.
    /// </summary>
    /// <param name="fontSize">The new font size.</param>
    public void ChangeFontSize(float fontSize)
    {
        _fontSize = fontSize * PixelPointRatio;
    }

    /// <summary>
    ///     Changes the typeface of the document.
    /// </summary>
    /// <param name="typeface">The new typeface.</param>
    public void SetTypeface(SKTypeface typeface)
    {
        _typeface = typeface;
    }

    /// <summary>
    ///     Decrements the position of the document cursor.
    /// </summary>
    public void PanLeft()
    {
        if (_cursorPos > 0) _cursorPos--;
    }

    /// <summary>
    ///     Increments the position of the document cursor.
    /// </summary>
    public void PanRight()
    {
        if (_cursorPos < _textBank.TextCount) _cursorPos++;
    }
}