using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace editor.Models;

public class Document
{
    private const float PixelPointRatio = 96f / 72f;
    private readonly float _cBottom;
    private readonly float _cCenter;
    private readonly Cursor _cursor;
    private readonly SKCanvasView _cView;
    private readonly Dictionary<int, (float size, float lineHeight, float padding)> _lineMetrics;

    private readonly float _lineSpace;

    private readonly (float Width, float Height, float LMargin, float RMargin, float TMargin, float BMargin) _page;
    private readonly float _pageEnd;
    private readonly float _pBottomEnd;
    private readonly float _pGap;
    private readonly List<StyledCharacter> _textList;

    private float _drawStart;
    private float _fontSize;
    private float _lastDrawY;
    private int _pageCount;
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
    public Document(SKCanvasView cView, float cHeight, float pWidth, float pHeight, float pGap, float lMargin,
        float rMargin, float tMargin, float bMargin, float fontSize)
    {
        _cView = cView;
        _cCenter = cHeight - pWidth / 2;
        _cBottom = cHeight - tMargin;

        _page = (pWidth, pHeight, lMargin, rMargin, tMargin, bMargin);
        _drawStart = pGap;
        _pGap = pGap;
        _lineSpace = 1.15f;

        _pBottomEnd = pHeight + pGap;

        _pageCount = 1;

        _fontSize = fontSize * PixelPointRatio;

        _textList = [];
        _cursor = new Cursor((_cCenter + lMargin, _drawStart + tMargin),
            _cCenter + pWidth - rMargin);
        _pageEnd = pHeight - bMargin;
        _lineMetrics = new Dictionary<int, (float size, float height, float padding)>();
        _typeface = SKTypeface.Default;
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

        for (var i = 0; i < _pageCount; i++)
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

        if (_textList.Count == 0)
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
        if (_drawStart + (_pageCount - 1) * _pBottomEnd + _page.Height < _cBottom)
            _drawStart = _cBottom - (_page.Height * _pageCount + _pGap * (_pageCount - 1));
        else if (_drawStart > _page.TMargin) _drawStart = _page.TMargin;

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

        if (character != '\n')
        {
            color = SKColors.Black;
            width = textPaint.MeasureText(char.ToString(character));
        }

        var padding = textPaint.FontMetrics.Descent + textPaint.FontMetrics.Leading;
        var height = TextUtil.LineHeight(textPaint.FontMetrics);

        var c = new StyledCharacter
        (character, _fontSize, width,
            _cursor.ValidatePosition((width, height), _lastDrawY,
                _drawStart + _pBottomEnd * (_pageCount - 1) + _pageEnd),
            color);

        var key = c.Position.LineNum;
        if (_lineMetrics.TryGetValue(key, out var font))
        {
            if (c.FontSize > font.size)
                _lineMetrics[key] = (c.FontSize, height, padding);
        }
        else
        {
            _lineMetrics.Add(key, (c.FontSize, height, padding));
        }

        _cursor.Move(c.Position, c.FontWidth);
        _textList.Add(c);

        if (_cursor.PageNumber > _pageCount) _pageCount++;

        _cView.Invalidate();
    }

    /// <summary>
    ///     Deletes a character from the document.
    /// </summary>
    public void DeleteChar()
    {
        if (_textList.Count <= 0) return;
        _textList.RemoveAt(_textList.Count - 1);
        if (_textList.Count >= 1)
        {
            var prev = _textList[^1];
            if (prev.Position.PNum < _pageCount) _pageCount--;
            if (prev.Position.LineNum < _cursor.LineNumber) _lineMetrics.Remove(_cursor.LineNumber);

            _cursor.Move(prev.Position, prev.FontWidth);
        }
        else
        {
            _lineMetrics.Remove(_cursor.LineNumber);
            _cursor.MoveOrigin();
        }

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
        if (_textList.Count < 1) return;

        var lineStart = _drawStart + _page.TMargin;
        var page = 1;
        var drawPosition = 0;
        var lastSize = 0f;
        var currentY = lineStart;

        foreach (var (value, fontSize, _, (x, lineNum, pNum), color) in _textList)
        {
            textPaint.TextSize = fontSize;
            textPaint.Color = color;

            if (pNum > page)
            {
                currentY = lineStart + _pBottomEnd * page;
                page++;
            }

            if (lineNum != drawPosition)
            {
                var (size, tLineHeight, _) = _lineMetrics[lineNum];

                if (lastSize > size) tLineHeight += _lineMetrics[drawPosition].padding;

                currentY += tLineHeight * _lineSpace;
                lastSize = size;
                drawPosition = lineNum;
            }

            canvas.DrawText(char.ToString(value), x, currentY, textPaint);
        }

        _lastDrawY = currentY;
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