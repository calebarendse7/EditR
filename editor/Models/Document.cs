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

    private readonly float _lineSpace;

    private readonly (float Width, float Height, float LMargin, float RMargin, float TMargin, float BMargin) _page;
    private readonly float _pBottomEnd;
    private readonly float _pGap;
    private readonly int _pIndex;
    private readonly List<StyledCharacter> _textList;

    private float _drawStart;
    private float _fontSize;
    private float _lineHeight;

    private int _pageCount;
    private float _pStart;

    /// <summary>
    /// Creates a document with the specified parameters.
    /// </summary>
    /// <param name="cView">A reference to the canvas</param>
    /// <param name="cHeight">The height of the canvas</param>
    /// <param name="pWidth">The width of each page.</param>
    /// <param name="pHeight">The height of each page.</param>
    /// <param name="pGap">The space between each page</param>
    /// <param name="lMargin">The left margin of each page. </param>
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

        _pStart = _drawStart + _pIndex * _pBottomEnd;
        _pageCount = 1;
        _pIndex = 0;

        var pEnd = _cCenter + pWidth - rMargin;

        _fontSize = fontSize * PixelPointRatio;
        _lineHeight = _fontSize * _lineSpace;

        _textList = [];

        _cursor = new Cursor((_cCenter + lMargin, _pStart + tMargin),
            (pEnd, _drawStart + pHeight - bMargin));
    }

    /// <summary>
    /// Draws the pages of the document.
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
    /// Draws the cursor.
    /// </summary>
    /// <param name="canvas">The canvas where the cursor is drawn.</param>
    public void DrawCursor(SKCanvas canvas)
    {
        using var cursorPaint = new SKPaint();
        cursorPaint.Style = SKPaintStyle.Fill;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1;

        canvas.DrawLine(_cursor.position.X, _cursor.position.Y + _cursor.Offset() - _fontSize, _cursor.position.X,
            _cursor.position.Y + _lineHeight - _fontSize + _cursor.Offset(), cursorPaint);
    }
    /// <summary>
    /// Scrolls the pages of the document. 
    /// </summary>
    /// <param name="deltaY">The amount to scroll.</param>
    public void Scroll(float deltaY)
    {
        _drawStart -= deltaY;

        /* Set limits on scroll */
        if (_drawStart + (_pageCount - 1) * _pBottomEnd + _page.Height < _cBottom)
            _drawStart = _cBottom - (_page.Height * _pageCount + _pGap * (_pageCount - 1));
        else if (_drawStart > _page.TMargin) _drawStart = _page.TMargin;

        _pStart = _drawStart + _pIndex * _pBottomEnd;
        _cursor.SetCursorOffset(_pStart);
        _cView.Invalidate();
    }
    /// <summary>
    /// Adds a page to the document.
    /// </summary>
    public void AddPage()
    {
        _pageCount++;
        _cView.Invalidate();
    }
    /// <summary>
    /// Adds a character to the document.
    /// </summary>
    /// <param name="character">The new character.</param>
    public void AddChar(char character)
    {
        using var textPaint = new SKPaint();
        textPaint.TextSize = _fontSize;
        textPaint.Typeface = SKTypeface.FromFamilyName("Ariel");

        var width = textPaint.MeasureText(char.ToString(character));

        var c =
            new StyledCharacter(character, _fontSize, width, _cursor.ValidatePosition((width, _lineHeight)));
        _cursor.MoveCursor(c.Position, width);
        _textList.Add(c);
        _cView.Invalidate();
    }
    /// <summary>
    /// Deletes a character from the document.
    /// </summary>
    public void DeleteChar()
    {
        if (_textList.Count <= 0) return;
        _textList.RemoveAt(_textList.Count - 1);
        if (_textList.Count >= 1)
        {
            var prev = _textList[^1];
            _cursor.MoveCursor(prev.Position, prev.FontWidth);
        }
        else
        {
            _cursor.MoveCursorOrigin();
        }

        _cView.Invalidate();
    }
    /// <summary>
    /// Draws the document's characters to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas where the characters are drawn.</param>
    public void DrawCharacters(SKCanvas canvas)
    {
        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.Black;
        textPaint.IsAntialias = true;
        textPaint.Typeface = SKTypeface.FromFamilyName("Ariel");

        if (_textList.Count < 1) return;

        foreach (var (value, fontSize, width, position) in _textList)
        {
            textPaint.TextSize = fontSize;
            canvas.DrawText(char.ToString(value), position.X, position.Y + _cursor.Offset(), textPaint);
        }
    }
    /// <summary>
    /// Changes the font size of the document.
    /// </summary>
    /// <param name="fontSize">The new font size.</param>
    public void ChangeFontSize(float fontSize)
    {
        var fontSizePixels = fontSize * PixelPointRatio;
        if (fontSizePixels > _fontSize) _lineHeight = fontSizePixels * _lineSpace;
        _fontSize = fontSizePixels;
    }
}