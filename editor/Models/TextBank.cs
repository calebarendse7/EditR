using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Editor.Models.RBList;
using SkiaSharp;

namespace editor.Models;

/// <summary>
///     Represents collection of characters.
/// </summary>
/// <param name="x">The x boundary of the text bank.</param>
/// <param name="y">The y boundary of the text bank.</param>
public class TextBank((float Start, float End) x, (float Start, float End, float THeight) y, float scrollOffset)
    : IEnumerable<StyledChar>
{
    private readonly RbList<StyledChar> _charList = [];
    private readonly Dictionary<int, SortedDictionary<float, CharMetric>> _lMetrics = [];
    private bool _hasOffsetChanged;
    private float _scrollOffset = scrollOffset;
    public int TextCount => _charList.Count;
    public int PageNum { get; private set; }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<StyledChar> GetEnumerator()
    {
        var h = 0f;
        var currRow = 0;
        //var rowEnd = y.THeight * PageNum + y.End;
        var pEnd = y.THeight;
        var page = 0f;
        var pNum = 0;
        foreach (var item in _charList)
        {
            if (item.Column == x.Start)
            {
                // Means we are on a different line
                // New row currRow + max LH of next row + padding of previous if greater else move to next page
                currRow++;
                var (height, _, _) = _lMetrics[currRow].First().Value;
                h += height;
                if (h > pEnd)
                {
                    Console.WriteLine("On next page");
                    // pNum++;
                    // pEnd = y.THeight * pNum + y.End;
                    // page = y.Start + pNum * y.THeight;
                }
            }

            item.RowOffset = h;
            if (_hasOffsetChanged)
                item.ScrollOffset = item.Row + _scrollOffset;

            yield return item;
        }

        if (_hasOffsetChanged) _hasOffsetChanged = false;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Adds a character to the text bank.
    /// </summary>
    /// <param name="value">A char representing the character.</param>
    /// <param name="width">A float representing the font width of the character.</param>
    /// <param name="height">A float representing the font height of the character.</param>
    /// <param name="size">A float representing the font size of the character.</param>
    /// <param name="padding">A float representing the font padding of the character.</param>
    /// <param name="color">A float representing the font color of the character.</param>
    public void AddCharacter(char value, int charPosition, float width, float height, float size, float padding,
        SKColor color)
    {
        _charList.Insert(charPosition, new StyledChar
            { Value = value, Width = width, Height = height, Padding = padding, Size = size, Color = color });
        //Console.WriteLine(string.Join("\n", _charList));
        RecalculatePositions(charPosition);
    }

    private void RecalculatePositions(int startPos)
    {
        float column;
        float pageStart;
        var rowNumber = 0;
        if (startPos == 0)
        {
            column = x.Start;
            // Normally add height of first character font + _charList[0].Height
            pageStart = y.Start;
        }
        else
        {
            var r = _charList[startPos - 1];
            column = r.Column + r.Width;
            pageStart = r.Page;
            rowNumber = r.Row;
        }

        var rowEnd = y.THeight * PageNum + y.End;
        for (var i = startPos; i < _charList.Count; i++)
        {
            var c = _charList[i];
            if (column + c.Width > x.End || c.Width == 0)
            {
                column = x.Start;
                rowNumber++;
                // if (page > rowEnd)
                // {
                //     PageNum++;
                //     page = y.Start + PageNum * y.THeight;
                // }
            }

            // If the character was on a different row
            if (c.Row != 0 && c.Row != rowNumber && _lMetrics.TryGetValue(c.Row, out var val))
                if (val.TryGetValue(c.Size, out var m))
                {
                    if (m.Quantity > 1)
                        val[c.Size].DecQuantity();
                    else
                        val.Remove(c.Size);
                }

            if (!_lMetrics.TryGetValue(rowNumber, out var value))
            {
                _lMetrics.Add(rowNumber,
                    new SortedDictionary<float, CharMetric>(Comparer<float>.Create((a, b) => b.CompareTo(a)))
                        { { c.Size, new CharMetric(c.Height, c.Padding) } });
            }
            else
            {
                if (!value.TryAdd(c.Size, new CharMetric(c.Height, c.Padding))) value[c.Size].IncQuantity();
            }

            c.Column = column;
            c.Row = rowNumber;
            c.ScrollOffset = pageStart + _scrollOffset;
            column += c.Width;
        }
    }

    /// <summary>
    ///     Changes the text bank offset.
    /// </summary>
    /// <param name="offset">A float representing the text bank offset.</param>
    public void SetOffset(float offset)
    {
        _scrollOffset = offset;
        _hasOffsetChanged = true;
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void RemoveCharacter()
    {
        // var removedChar = _charList[^1];
        // _charList.RemoveAt(_charList.Count - 1);
        // var lineInfo = _lMetrics[removedChar.Position.LNum];
        // var charInfo = lineInfo[removedChar.Font.Size];
        // if (charInfo.Quantity == 1)
        //     lineInfo.Remove(removedChar.Font.Size);
        // //if (lineInfo.Count != 0) _fontSize = lineInfo.First().Key;
        // else
        //     charInfo.Quantity--;
        // if (_charList.Count >= 1)
        // {
        //     var prev = _charList[^1];
        //     if (prev.Position.PNum < _bankPos.PNum) _bankPos.PNum--;
        //     if (prev.Position.LNum < _bankPos.LNum) _lMetrics.Remove(_bankPos.LNum);
        //     _bankPos.LNum = prev.Position.LNum;
        //     _column = prev.Position.X + prev.Font.Width;
        // }
        // else
        // {
        //     _lMetrics.Remove(_bankPos.LNum);
        //     _bankPos = (1, 1);
        //     _column = x.Start;
        // }
    }
}