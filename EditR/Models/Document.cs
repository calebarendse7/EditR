using System.Collections.Immutable;
using SkiaSharp;

namespace EditR.Models;

public class Document((float Width, float Height) canvas)
{
    private const float TabWidth = 0.5f * 96;
    private readonly float _cCenter = canvas.Width / 2;
    private readonly float _cHeight = canvas.Height;
    private readonly Dictionary<string, SKColor> _colors = [];
    private readonly TextBank _textBank = new();
    private float _cBottom;
    private float _center;
    private float _cursorSize;

    private float _cursorX;
    private float _cursorY;
    private float _drawStartY;
    private int _endSelect;
    private int _insertPos;
    private bool _isSelected;
    private float _pTotalHeight;
    private int _startSelect;

    private bool _cursorUpdate = true;
    private bool _isClicked;
    private bool _isEndOfLine;
    private float _lastClickedX;
    private float _lastClickedY;

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
    public void DrawCursor(SKCanvas canvas)
    {
        if (_isSelected) return;
        using var cursorPaint = new SKPaint();
        cursorPaint.IsAntialias = true;
        cursorPaint.Color = SKColors.Black;
        cursorPaint.StrokeWidth = 1.5f;


        if (_isClicked)
        {
            (_insertPos, _isEndOfLine) = _textBank.FindNearestChar((_lastClickedX, _lastClickedY));
            _cursorUpdate = true;
            _isClicked = false;
        }

        if (_cursorUpdate)
        {
            var i = Math.Min(_insertPos, _textBank.Count - 1);
            _textBank[i].Match(val =>
            {
                var rowInfo = _textBank.GetRowInfo(val.RowNum);
                _cursorX = _isEndOfLine ? val.Column + val.Width : val.Column;
                _cursorY = rowInfo.RowOffset;
                _cursorSize = val.Size;
            }, () => Console.Error.WriteLine($"Document:DrawCursor: Could not find {i}"));
            _cursorUpdate = false;
        }

        canvas.DrawLine(_cursorX, _cursorY - _cursorSize, _cursorX, _cursorY, cursorPaint);
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
        else if (!char.IsControl(character))
        {
            width = textPaint.MeasureText(char.ToString(character));
            color = pageInfo.Color;
        }

        _textBank.Add(new StyledChar
        {
            Value = character,
            FontName = pageInfo.SelectedFont.Name,
            Width = width,
            Height = TextUtil.LineHeight(textPaint.Metrics),
            Padding = textPaint.Metrics.Descent + textPaint.Metrics.Leading,
            Size = pageInfo.PixelSize,
            PtSize = pageInfo.PointSize,
            Color = color
        }, _insertPos++);
        _cursorUpdate = true;
    }

    /// <summary>
    ///     Deletes a character from the document.
    /// </summary>
    public void DeleteChar()
    {
        if (_textBank.Count == 1 || _insertPos - 1 < 0) return;
        if (_isSelected)
        {
            _insertPos = _textBank.RemoveSelection((_startSelect, _endSelect));
            _isSelected = false;
        }
        else
        {
            _textBank.RemoveSingle(--_insertPos);
        }

        _cursorUpdate = true;
    }

    /// <summary>
    ///     Draws the document's characters to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas where the characters are drawn. </param>
    /// <param name="fontName">A Font representing the selected font. </param>
    /// <param name="fonts">A Dictionary representing the family name of the font. </param>
    public void DrawCharacters(SKCanvas canvas, Font fontName, ImmutableDictionary<Font, SKTypeface> fonts)
    {
        var textFont = new SKFont();
        var paint = new SKPaint();
        paint.IsAntialias = true;
        textFont.Typeface = fonts[fontName];

        _textBank.Each((character, info, i) =>
        {
            textFont.Size = character.Size;
            if (_isSelected && i >= _startSelect && i <= _endSelect)
            {
                paint.Color = SKColor.Parse("#BAD3FD");
                canvas.DrawRect(character.Column, info.RowOffset, character.Width, -character.Height, paint);
                //character.FontName = currFontIndex;
            }

            if (character.FontName != fontName)
                textFont.Typeface = fonts.GetValueOrDefault(fontName = character.FontName, SKTypeface.Default);

            paint.Color = TextUtil.FindColor(_colors, character.Color);
            canvas.DrawText(character.Value.ToString(), character.Column, info.RowOffset, textFont, paint);
            if (i != _textBank.Count - 1) return;
            textFont.Dispose();
            paint.Dispose();
        });
    }

    /// <summary>
    ///     Decrements the position of the document cursor.
    /// </summary>
    public void PanLeft()
    {
        if (_insertPos <= 0) return;
        _insertPos--;
        _cursorUpdate = true;
    }

    /// <summary>
    ///     Increments the position of the document cursor.
    /// </summary>
    public void PanRight()
    {
        if (_insertPos >= _textBank.Count) return;
        _insertPos++;
        _cursorUpdate = true;
    }

    /// <summary>
    ///     Moves the cursor position to the nearest character to a position.
    /// </summary>
    /// <param name="pos">A Tuple representing the coordinates of the position.</param>
    public void MoveCursor((float, float) pos)
    {
        if (_textBank.IsEmpty()) return;
        (_lastClickedX, _lastClickedY) = pos;
        _isClicked = true;
    }

    /// <summary>
    ///     Selects characters within a range.
    /// </summary>
    /// <param name="start">A Tuple representing the coordinates of the start of the range.</param>
    /// <param name="end">A Tuple representing the coordinates of the end of the range.</param>
    public void Select((float X, float Y) start, (float X, float Y) end)
    {
        if (_textBank.IsEmpty()) return;
        (_startSelect, _endSelect) = _textBank.FindRange(start, end);
        _isSelected = true;
    }

    /// <summary>
    ///     Unselects current selection of characters.
    /// </summary>
    public void Unselect()
    {
        _isSelected = false;
    }
}
