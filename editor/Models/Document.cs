using System.Security.Cryptography.X509Certificates;
using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace editor.Models;

public class Document
{
    private readonly float _bMargin;
    private readonly SKCanvasView _canvasView;
    private readonly float _cBottom;
    private readonly float _cCenter;
    private float _fontSize;
    private float _largestFontSize;
    private readonly float _lineSpace;
    private readonly float _pEnd;
    private readonly float _pGap;
    private readonly float _pHeight;
    private readonly float _pOffset;
    private readonly float _pWidth;
    private readonly float _tMargin;
    private readonly float _curOffsetX;
    private float _textWidth;
    private float _curOffsetY;

    private readonly float _curOriginX;
    private float _curOriginY;
    private float _drawStart;

    private int _pages;

    private readonly int _pIndex;
    private float _pStart;
    private readonly List<(char character, float size)> _textList;

    /// <summary>
    ///     Creates a document with the specified dimensions.
    /// </summary>
    /// <param name="canvasView">A reference to the canvas</param>
    /// <param name="cHeight">The height of the canvas. </param>
    /// <param name="pWidth">The width of each page. </param>
    /// <param name="pHeight">The height of each page. </param>
    /// <param name="pGap">The space between each page. </param>
    /// <param name="lMargin">The left margin of each page. </param>
    /// <param name="rMargin">The right margin of each page.</param>
    /// <param name="tMargin">The top margin of each page.</param>
    /// <param name="bMargin">The bottom margin of each page.</param>
    /// <param name="fontSize">The size for page text and the cursor.</param>
    public Document(SKCanvasView canvasView, float cHeight, float pWidth, float pHeight, float pGap, float lMargin,
        float rMargin, float tMargin, float bMargin, float fontSize)
    {
        _drawStart = 50;
        _lineSpace = 1.15f;
        _pStart = _drawStart + _pIndex * _pGap;

        _cCenter = cHeight - pWidth / 2;
        _cBottom = cHeight - tMargin;
        _canvasView = canvasView;

        _pGap = pHeight + pGap;

        _pages = 1;
        _pIndex = 0;
        _pOffset = 50;

        _pWidth = pWidth;
        _pHeight = pHeight;
        _bMargin = bMargin;

        _tMargin = tMargin;

        _curOriginX = _cCenter + lMargin;
        _curOriginY = _pStart + tMargin;
        _curOffsetX = 0;
        _curOffsetY = 0;

        _pEnd = _cCenter + pWidth - rMargin;
        _fontSize = fontSize;
        _largestFontSize = 0f;
        _textList = [];
        _textWidth = 0;
    }

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

        for (var i = 0; i < _pages; i++)
        {
            var y = _drawStart + i * _pGap;
            canvas.DrawRect(_cCenter, y, _pWidth, _pHeight, pagePaint);
            canvas.DrawRect(_cCenter, y, _pWidth, _pHeight, pageOutlinePaint);
        }
    }

    public void DrawCursor(SKCanvas canvas)
    {
        _curOriginY = _pStart + _tMargin;

        using var cursorPaint = new SKPaint();
        cursorPaint.Style = SKPaintStyle.Fill;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1;

        var x = _curOriginX + _curOffsetX;
        canvas.DrawLine(x, _curOriginY, x, _curOriginY + _fontSize, cursorPaint);
    }

    public void Scroll(float deltaY)
    {
        _drawStart -= deltaY;

        /* Set limits on scroll */
        if (_drawStart + (_pages - 1) * (_pHeight + _pOffset) + _pHeight < _cBottom)
            _drawStart = _cBottom - (_pHeight * _pages + _pOffset * (_pages - 1));
        else if (_drawStart > _tMargin) _drawStart = _tMargin;

        _pStart = _drawStart + _pIndex * _pGap;

        _canvasView.Invalidate();
    }

    public void AddPage()
    {
        _pages++;
        _canvasView.Invalidate();
    }

    public void AddChar(char text)
    {
        _textList.Add((text, _fontSize));
        if(_fontSize > _largestFontSize) _largestFontSize = _fontSize;
        _canvasView.Invalidate();
    }

    public void DeleteText()
    {
        if (_textList.Count <= 0) return;
        _textList.RemoveAt(_textList.Count - 1);
        _canvasView.Invalidate();
    }
    public void DrawText(SKCanvas canvas)
    {
        _curOriginY = _pStart + _tMargin;

        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.Black;
        textPaint.IsAntialias = true;
        textPaint.StrokeWidth = 11;
        textPaint.Typeface = SKTypeface.FromFamilyName("Ariel");

        if (_textList.Count <= 0) return;

        (int x, int y) lineOffset = (0, 0);
        var yOffset = _largestFontSize * _lineSpace;

        var offset = 0f;
        foreach (var text in _textList)
        {
            textPaint.TextSize = text.size;

            var xPos = _curOriginX + offset;
            var yPos = _curOriginY + yOffset * lineOffset.y;

            if (xPos > _pEnd)
            {
                lineOffset = (1, lineOffset.y + 1);
                xPos = _curOriginX;
                offset = 0;
                yPos += yOffset;
            }
            
            offset += textPaint.MeasureText(char.ToString(text.character));
            canvas.DrawText(char.ToString(text.character), xPos, yPos, textPaint);
        }
        
    }

    public int PageCount()
    {
        return _pages;
    }
    
    public float FontSize()
    {
        return _fontSize;
    }

    public void ChangeFontSize(float fontSize)
    {
        _fontSize = fontSize * (96f / 72f);
    }
    
}