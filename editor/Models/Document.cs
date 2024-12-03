using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace editor.Models;
public class Document
{
    private readonly SKCanvasView _canvasView;
    private readonly float _pGap;
    private float _drawStart;
    
    private float _curOriginX;
    private float _curOriginY;
    private float _curOffsetX;
    private float _curOffsetY;
    
    private int _pages;
    private float _pStart;
    private readonly float _pEnd;
    private readonly float _pWidth;
    private readonly float _pHeight;
    private readonly float _bMargin;
    private readonly float _tMargin;
    private readonly float _pOffset;
    private readonly float _fontSize;   
    private readonly float _cCenter;
    private readonly float _cBottom;

    private int _pIndex;
    private List<string> _textList;
    private readonly float _lineSpace;
    
    /// <summary>
    /// Creates a document with the specified dimensions. 
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
    public Document(SKCanvasView canvasView, float cHeight, float pWidth, float pHeight, float pGap, float lMargin, float rMargin, float tMargin, float bMargin, float fontSize)
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
        _textList = [];
    }

    public void DrawPages(SKCanvas canvas){
        canvas.Clear(SKColors.White);
        using SKPaint pagePaint = new();
        pagePaint.Style = SKPaintStyle.Fill;
        pagePaint.Color = SKColors.White;
        pagePaint.IsAntialias = true;
        
        using SKPaint pageOutlinePaint = new();
        pageOutlinePaint.Style = SKPaintStyle.Stroke;
        pageOutlinePaint.Color = SKColors.LightGray;
        pageOutlinePaint.StrokeWidth = 1;

        for(var i = 0; i < _pages; i++){
            var y = _drawStart + i * _pGap;
            canvas.DrawRect(_cCenter, y, _pWidth, _pHeight, pagePaint);
            canvas.DrawRect(_cCenter, y, _pWidth, _pHeight, pageOutlinePaint);
        }
    }
    public void DrawCursor(SKCanvas canvas){
        _curOriginY = _pStart + _tMargin;
        
        using var cursorPaint = new SKPaint();
        cursorPaint.Style = SKPaintStyle.Fill;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1;
        
        var x = _curOriginX + _curOffsetX;
        canvas.DrawLine(x, _curOriginY, x, _curOriginY + _fontSize, cursorPaint);
    }
    public void Scroll(float deltaY){
        _drawStart -= deltaY;
        
        /* Set limits on scroll */
        if(_drawStart + (_pages - 1) * (_pHeight + _pOffset) + _pHeight < _cBottom) {
            _drawStart = _cBottom - (_pHeight * _pages + _pOffset * (_pages - 1));
        }else if(_drawStart > _tMargin){
            _drawStart = _tMargin;
        }

        _pStart = _drawStart + _pIndex * _pGap;

        _canvasView.Invalidate();
    }
    public void AddPage(){
        _pages++;
        _canvasView.Invalidate();
    }

    public void AddText(string text)
    {
        _textList.Add(text);
        _canvasView.Invalidate();
    }
    public void DrawText(SKCanvas canvas)
    {
        _curOriginY = _pStart + _tMargin;
        
        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.Black;
        textPaint.IsAntialias = true;
        textPaint.TextSize = _fontSize;
        textPaint.StrokeWidth = 11;
        textPaint.Typeface = SKTypeface.FromFamilyName(familyName: "Ariel");

        if (_textList.Count <= 0) return;
        
        var textRect = new SKRect();
        textPaint.MeasureText(_textList[0], ref textRect);
       
        (int x, int y) lineOffset = (0, 0);
        var yOffset = _fontSize * _lineSpace;
        foreach (var text in _textList)
        {
            var xPos = _curOriginX + textRect.Width * lineOffset.x;
            var yPos = _curOriginY + yOffset * lineOffset.y;
            
            if (xPos > _pEnd)
            {
                lineOffset = (1, lineOffset.y + 1);
                xPos = _curOriginX;
                yPos += yOffset;
            }
            else
            {
                lineOffset.x++;
            }
            canvas.DrawText(text, xPos, yPos, textPaint);
        }

    }

    public int PageCount() => _pages;
    public float FontSize() => _fontSize;
}