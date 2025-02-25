using SkiaSharp;

namespace EditR.Models;

public class Document((float Width, float Height) canvas)
{
    private const float TabWidth = 0.5f * 96;
    private readonly float _cHeight = canvas.Height;
    private readonly float _cCenter = canvas.Width / 2;
    private readonly TextBank _textBank = [];
    private readonly Dictionary<string, SKColor> _colors = [];
    private float _center;
    private float _pTotalHeight;
    private float _drawStartY;
    private float _cBottom;
    private int _cursorPos;
    private SKTypeface _typeface = SKTypeface.Default;

    /// <summary>
    ///     Updates the page 
    /// </summary>
    /// <param name="info">A DocumentInfo representing </param>
    public void UpdateSettings(DocumentInfo info)
    {
        _center = _cCenter - info.Width / 2;
        _cBottom = _cHeight - info.Gap;
        _pTotalHeight = info.Height + info.Gap;
        _drawStartY = info.Gap;
        _textBank.UpdateBoundaries(_center + info.LeftMargin, _center + info.Width - info.RightMargin, info.TopMargin,
            info.Height - info.BottomMargin,
            _pTotalHeight, _drawStartY);
    }

    /// <summary>
    ///     Draws the pages of the document.
    /// </summary>
    /// <param name="canvas">The canvas where the pages are drawn.</param>
    /// <param name="width">The width of each page.</param>
    /// <param name="height">The height of each page.</param>
    public void DrawPages(SKCanvas canvas, float width, float height)
    {
        canvas.Clear(SKColors.White);
        using SKPaint pagePaint = new();
        pagePaint.Style = SKPaintStyle.Fill;
        pagePaint.Color = SKColors.White;
        pagePaint.IsAntialias = true;

        using SKPaint pageOutlinePaint = new();
        pageOutlinePaint.Style = SKPaintStyle.Stroke;
        pageOutlinePaint.Color = SKColors.LightGray;
        pageOutlinePaint.IsAntialias = true;
        pageOutlinePaint.StrokeWidth = 1;

        for (var i = 0; i < _textBank.PageNum + 1; i++)
        {
            var y = _drawStartY + i * _pTotalHeight;
            canvas.DrawRect(_center, y, width, height, pagePaint);
            canvas.DrawRect(_center, y, width, height, pageOutlinePaint);
        }
    }

    /// <summary>
    ///     Draws the cursor.
    /// </summary>
    /// <param name="canvas">The canvas where the cursor is drawn.</param>
    /// <param name="topMargin">A float representing the top margin of each page.</param>
    /// <param name="leftMargin">A float representing the left margin of each page.</param>
    /// <param name="pxSize">A float representing the pixel font size.</param>
    /// <param name="lineSpace">A float representing the line space for each line.</param>
    public void DrawCursor(SKCanvas canvas, float topMargin, float leftMargin, float pxSize, float lineSpace)
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
                x = _center + leftMargin;
                if (_cursorPos < _textBank.TextCount)
                {
                    var next = _textBank[_cursorPos];
                    y = next.Row;
                    size = next.Size;
                }
                else
                {
                    y += TextUtil.LineHeight(new SKFont(_typeface, pxSize).Metrics) * lineSpace;
                }
            }

            cursorPaint.Color = char.IsWhiteSpace(c.Value) ? SKColors.Black : TextUtil.FindColor(_colors, c.Color);
        }
        else
        {
            x = _center + leftMargin;
            y = _drawStartY + topMargin +
                TextUtil.LineHeight(new SKFont(_typeface, pxSize).Metrics) * lineSpace;
        }

        canvas.DrawLine(x, y - size, x, y, cursorPaint);
    }

    /// <summary>
    ///     Scrolls the pages of the document.
    /// </summary>
    /// <param name="deltaY">The amount to scroll.</param>
    /// <param name="gap">A float representing the gap between each page.</param>
    public void Scroll(float deltaY, float gap)
    {
        _drawStartY -= deltaY;

        /* Set limits on scroll */
        if (_drawStartY > gap)
            _drawStartY = gap;
        else if (_drawStartY + (_textBank.PageNum + 1) * _pTotalHeight < _cBottom)
            _drawStartY = _cBottom - (_textBank.PageNum + 1) * _pTotalHeight;
        _textBank.SetOffset(_drawStartY);
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
        var color = "#00000000";
        if (character == '\t')
        {
            width = TabWidth;
        }
        else if (character != '\n')
        {
            width = textPaint.MeasureText(char.ToString(character));
            color = rgbColor;
        }

        _textBank.Add(new StyledChar
        {
            Value = character, Width = width, Height = TextUtil.LineHeight(textPaint.Metrics),
            Padding = textPaint.Metrics.Descent + textPaint.Metrics.Leading, Size = pxSize, PtSize = ptSize,
            Color = color
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
            paint.Color = TextUtil.FindColor(_colors, character.Color);
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