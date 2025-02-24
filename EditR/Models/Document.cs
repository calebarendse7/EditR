using SkiaSharp;

namespace EditR.Models;

public class Document
{
    private const float TabWidth = 0.5f * 96;
    private readonly float _cBottom;
    private readonly float _cCenter;
    private readonly float _lineSpace;
    private readonly (float Width, float Height, float LMargin, float RMargin, float TMargin, float BMargin) _page;
    private readonly float _pGap;
    private readonly float _pTotalHeight;
    private readonly TextBank _textBank;
    private int _cursorPos;
    private float _drawStart;
    private SKTypeface _typeface;
    private readonly Dictionary<string, SKColor> _colors;

    /// <summary>
    ///     Creates a document with the specified parameters.
    /// </summary>
    /// <param name="canvas">A tuple representing the width and height of the canvas.</param>
    /// <param name="pWidth">The width of each page.</param>
    /// <param name="pHeight">The height of each page.</param>
    /// <param name="pGap">The space between each page.</param>
    /// <param name="lMargin">The left margin of each page.</param>
    /// <param name="rMargin">The right margin of each page.</param>
    /// <param name="tMargin">The top margin of each page.</param>
    /// <param name="bMargin">The bottom margin of each page.</param>
    /// <param name="lineSpace">The line space for lines on each page.</param>
    public Document((float Width, float Height) canvas, float pWidth, float pHeight, float pGap, float lMargin,
        float rMargin, float tMargin, float bMargin, float lineSpace)
    {
        _cCenter = canvas.Width / 2 - pWidth / 2;
        _cBottom = canvas.Height - pGap;

        _page = (pWidth, pHeight, lMargin, rMargin, tMargin, bMargin);
        _drawStart = pGap;
        _pGap = pGap;
        _lineSpace = lineSpace;

        _pTotalHeight = pHeight + pGap;


        _typeface = SKTypeface.Default;
        _textBank = new TextBank((_cCenter + lMargin, _cCenter + pWidth - rMargin),
            (_page.TMargin, pHeight - bMargin, _pTotalHeight), _drawStart);
        _colors = [];
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
            var y = _drawStart + i * _pTotalHeight;
            canvas.DrawRect(_cCenter, y, _page.Width, _page.Height, pagePaint);
            canvas.DrawRect(_cCenter, y, _page.Width, _page.Height, pageOutlinePaint);
        }
    }

    /// <summary>
    ///     Draws the cursor.
    /// </summary>
    /// <param name="canvas">The canvas where the cursor is drawn.</param>
    public void DrawCursor(SKCanvas canvas, float pxSize)
    {
        using var cursorPaint = new SKPaint();
        cursorPaint.IsAntialias = true;
        cursorPaint.Style = SKPaintStyle.Fill;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1.5f;
    
        float x;
        float y;
        var size = pxSize;
        var pos = _cursorPos - 1;
        if (_textBank.TextCount > 0 && pos >= 0)
        {
            var c = _textBank[pos];
            x = c.Column + c.Width;
            y = c.Row;
            size = c.Size;
            if (c.Value == '\n')
            {
                x = _cCenter + _page.LMargin;
                if (_cursorPos < _textBank.TextCount)
                {
                    var next = _textBank[_cursorPos];
                    y = next.Row;
                    size = next.Size;
                }
                else
                {
                    y += TextUtil.LineHeight(new SKFont(_typeface, pxSize).Metrics) * _lineSpace;
                }
            }

            //cursorPaint.Color = c.Width == 0 ? SKColors.Black : TextUtil.FindColor(_colors, c.Color);
            cursorPaint.Color = SKColors.Black;
        }
        else
        {
            x = _cCenter + _page.LMargin;
            y = _drawStart + _page.TMargin +
                TextUtil.LineHeight(new SKFont(_typeface, pxSize).Metrics) * _lineSpace;
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
        if (_drawStart > _pGap)
            _drawStart = _pGap;
        else if (_drawStart + (_textBank.PageNum + 1) * _pTotalHeight < _cBottom)
            _drawStart = _cBottom - (_textBank.PageNum + 1) * _pTotalHeight;
        _textBank.SetOffset(_drawStart);
    }

    /// <summary>
    ///     Adds a character to the document.
    /// </summary>
    /// <param name="character">A char representing the character to add.</param>
    /// <param name="rgbColor">A string representing the font color.</param>
    /// <param name="ptSize">An int representing the point font size.</param>
    /// <param name="pxSize">A float representing the pixel font size.</param>
    public void AddChar(char character, string rgbColor, int ptSize, float pxSize)
    {
        using var textPaint = new SKFont();
        textPaint.Size = pxSize;
        textPaint.Typeface = _typeface;

        float width = 0;
        if (character == '\t')
        {
            width = TabWidth;
        }
        else if (character != '\n')
        {
            width = textPaint.MeasureText(char.ToString(character));
        }

        _textBank.Add(new StyledChar
        {
            Value = character, Width = width, Height = TextUtil.LineHeight(textPaint.Metrics),
            Padding = textPaint.Metrics.Descent + textPaint.Metrics.Leading, Size = pxSize, PtSize = ptSize,
            Color = rgbColor
        }, _cursorPos++);
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
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;

        foreach (var character in _textBank)
        {
            textFont.Size = character.Size;
            //paint.Color = TextUtil.FindColor(_colors, character.Color);
            canvas.DrawText(character.Value.ToString(), character.Column, character.Row, textFont, paint);
        }
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