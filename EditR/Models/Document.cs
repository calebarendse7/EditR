using System.Collections.Immutable;
using SkiaSharp;

namespace EditR.Models;

public class Document((float Width, float Height) canvas)
{
    private const float TabWidth = 0.5f * 96;
    private readonly float _cCenter = canvas.Width / 2;
    private readonly float _cHeight = canvas.Height;
    private readonly Dictionary<string, SKColor> _colors = [];
    private readonly TextBank _textBank = [];
    private float _cBottom;
    private float _center;
    private float _cursorSize;

    private float _cursorX;
    private float _cursorY;
    private float _drawStartY;
    private int _endSelect;
    private int _insertPos;

    private bool _isPositionChanged;
    private bool _isSelected;
    private float _pTotalHeight;
    private int _startSelect;

    /// <summary>
    ///     Updates the page
    /// </summary>
    /// <param name="info">A DocumentInfo representing the dimensions of the page.</param>
    public void UpdateSettings(PageInfo info)
    {
        _center = _cCenter - info.Width / 2;
        _cBottom = _cHeight - info.Gap;
        _pTotalHeight = info.Height + info.Gap;
        _drawStartY = info.Gap;
        _textBank.UpdateBoundaries(_center + info.LeftMargin, _center + info.Width - info.RightMargin, info.TopMargin,
            info.Height,
            _pTotalHeight, info.BottomMargin, _drawStartY);
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
    /// <param name="typeface">An SKTypeface representing the selected typeface.</param>
    public void DrawCursor(SKCanvas canvas, float topMargin, float leftMargin, float pxSize, float lineSpace,
        SKTypeface typeface)
    {
        if (_isSelected) return;
        using var cursorPaint = new SKPaint();
        cursorPaint.IsAntialias = true;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1.5f;

        if (_textBank.IsEmpty())
        {
            _cursorX = _center + leftMargin;
            _cursorY = _drawStartY + topMargin +
                       TextUtil.LineHeight(new SKFont(typeface, pxSize).Metrics) * lineSpace;
            _cursorSize = pxSize;
        }
        else if (_isPositionChanged)
        {
            var i = Math.Min(_insertPos, _textBank.Count - 1);
            var current = _textBank[i];
            if (current.Case is StyledChar c)
            {
                (_cursorX, _cursorY) = (_insertPos == _textBank.Count, c.Value == '\n') switch
                {
                    (true, false) => (c.Column + c.Width, c.Row),
                    (false, true) => (c.Column, c.Row),
                    (true, true) => (_center + leftMargin, c.Row + c.Height * lineSpace),
                    (false, false) => (c.Column, c.Row)
                };
                _cursorSize = c.Size;
            }
            else
            {
                Console.Error.WriteLine($"Document:DrawCursor: Could not find {i}");
            }
        }

        canvas.DrawLine(_cursorX, _cursorY - _cursorSize, _cursorX, _cursorY, cursorPaint);
        _isPositionChanged = false;
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
        _isPositionChanged = true;
    }

    /// <summary>
    ///     Adds a character to the document.
    /// </summary>
    /// <param name="character">A char representing the character to add.</param>
    /// <param name="pageInfo">A PageInfo representing the parameters of each page.</param>
    /// <param name="typeface">An SKTypeface representing the selected typeface.</param>
    public void AddChar(char character, PageInfo pageInfo, SKTypeface typeface)
    {
        if (_isSelected)
        {
            _insertPos = _textBank.RemoveSelection((_startSelect, _endSelect));
            _isSelected = false;
        }

        using var textPaint = new SKFont();
        textPaint.Size = pageInfo.PixelSize;
        textPaint.Typeface = typeface;

        float width = 0;
        var color = "#00000000";

        if (character == '\t')
        {
            width = TabWidth;
        }
        else if (character != '\n')
        {
            width = textPaint.MeasureText(char.ToString(character));
            color = pageInfo.Color;
        }

        _textBank.Add(new StyledChar
        {
            Value = character,
            FontIndex = pageInfo.SelectedFont.Index,
            Width = width,
            Height = TextUtil.LineHeight(textPaint.Metrics),
            Padding = textPaint.Metrics.Descent + textPaint.Metrics.Leading,
            Size = pageInfo.PixelSize,
            PtSize = pageInfo.PointSize,
            Color = color
        }, _insertPos++);
        _isPositionChanged = true;
    }

    /// <summary>
    ///     Deletes a character from the document.
    /// </summary>
    public void DeleteChar()
    {
        if (_textBank.Count == 0 || _insertPos - 1 < 0) return;
        if (_isSelected)
        {
            _insertPos = _textBank.RemoveSelection((_startSelect, _endSelect));
            _isSelected = false;
        }
        else
        {
            _textBank.RemoveSingle(--_insertPos);
        }

        _isPositionChanged = true;
    }

    /// <summary>
    ///     Draws the document's characters to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas where the characters are drawn. </param>
    /// <param name="currFontIndex">An int representing the index of the selected font. </param>
    /// <param name="fonts">A Dictionary representing the family name of the font. </param>
    public void DrawCharacters(SKCanvas canvas, int currFontIndex, ImmutableDictionary<int, SKTypeface> fonts)
    {
        using var textFont = new SKFont();
        using var paint = new SKPaint();
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;
        var fIndex = -1;
        var i = 0;
        foreach (var character in _textBank)
        {
            textFont.Size = character.Size;
            if (_isSelected && i >= _startSelect && i <= _endSelect)
            {
                paint.Color = SKColor.Parse("#BAD3FD");
                canvas.DrawRect(character.Column, character.Row, character.Width, -character.Height, paint);
                character.FontIndex = currFontIndex;
            }

            if (fIndex != character.FontIndex)
                textFont.Typeface = fonts.GetValueOrDefault(fIndex = character.FontIndex, SKTypeface.Default);

            paint.Color = TextUtil.FindColor(_colors, character.Color);
            canvas.DrawText(character.Value.ToString(), character.Column, character.Row, textFont, paint);
            i++;
        }
    }

    /// <summary>
    ///     Decrements the position of the document cursor.
    /// </summary>
    public void PanLeft()
    {
        if (_insertPos <= 0) return;
        _insertPos--;
        _isPositionChanged = true;
    }

    /// <summary>
    ///     Increments the position of the document cursor.
    /// </summary>
    public void PanRight()
    {
        if (_insertPos >= _textBank.Count) return;
        _insertPos++;
        _isPositionChanged = true;
    }

    /// <summary>
    ///     Moves the cursor position to the nearest character to a position.
    /// </summary>
    /// <param name="pos">A Tuple representing the coordinates of the position.</param>
    public void MoveCursor((float, float) pos)
    {
        if (_textBank.IsEmpty()) return;
        _insertPos = _textBank.FindNearestChar(pos);
        _isPositionChanged = true;
    }

    /// <summary>
    ///     Selects characters within a range.
    /// </summary>
    /// <param name="start">A Tuple representing the coordinates of the start of the range.</param>
    /// <param name="end">A Tuple representing the coordinates of the end of the range.</param>
    public void Select((float X, float Y) start, (float X, float Y) end)
    {
        if (_textBank.IsEmpty()) return;
        _isSelected = true;
        (_startSelect, _endSelect) = _textBank.FindRange(start, end);
    }

    /// <summary>
    ///     Unselects current selection of characters.
    /// </summary>
    public void Unselect()
    {
        _isSelected = false;
    }
}