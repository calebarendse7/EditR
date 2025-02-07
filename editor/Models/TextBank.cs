using System.Collections;
using Editor.Models.RBList;
using LanguageExt.Pretty;
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
    private readonly Dictionary<int, float> _heightByRow = [];
    private readonly Dictionary<int, SortedDictionary<float, CharMetric>> _lMetrics = [];
    private float _rowEnd = y.End + scrollOffset;
    private float _rowStart = y.Start + scrollOffset;
    public int TextCount => _charList.Count;
    public int PageNum { get; private set; }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<StyledChar> GetEnumerator()
    {
        var currRow = 0;
        var rStart = _rowStart;
        var rEnd = _rowEnd;
        var pNum = 0;
        foreach (var item in _charList)
        {
            if (Math.Abs(item.Column - x.Start) < 1)
            {
                try
                {
                    var rowHeight = _heightByRow[currRow];
                    rStart += rowHeight;
                    currRow++;

                    if (rStart > rEnd)
                    {
                        pNum++;
                        var start = y.THeight * pNum;
                        rEnd += start;
                        rStart = _rowStart + start + rowHeight;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(string.Join(" ", _heightByRow));
                }
            }

            item.Row = rStart;
            yield return item;
        }

        PageNum = pNum;
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
    /// <param name="charPosition">A int representing the character position.</param>
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
        RecalculatePositions(charPosition);
    }

    private void RecalculatePositions(int startPos)
    {
        float column;
        var rowNumber = 0;
        if (startPos == 0)
        {
            column = x.Start;
        }
        else
        {
            var r = _charList[startPos - 1];
            column = r.Column + r.Width;
            rowNumber = r.RowNum;
        }

        for (var i = startPos; i < _charList.Count; i++)
        {
            var c = _charList[i];
            if (column + c.Width > x.End)
            {
                column = x.Start;
                rowNumber++;
            }

            // If the character was on a different row
            if (c.RowNum != rowNumber && _lMetrics.TryGetValue(c.RowNum, out var val))
                if (val.TryGetValue(c.Size, out var m))
                {
                    if (m.Quantity > 1)
                        val[c.Size].DecQuantity();
                    else
                        val.Remove(c.Size);
                }

            if (_lMetrics.TryGetValue(rowNumber, out var value))
            {
                if (!value.TryAdd(c.Size, new CharMetric(c.Height, c.Padding))) value[c.Size].IncQuantity();
                
            }
            else
            {
                _lMetrics.Add(rowNumber,
                    new SortedDictionary<float, CharMetric>(Comparer<float>.Create((a, b) => b.CompareTo(a)))
                        { { c.Size, new CharMetric(c.Height, c.Padding) } });
            }

            c.Column = column;
            c.RowNum = rowNumber;
            UpdateRowStart(rowNumber);
            column += c.Width;
            
            if (c.Width == 0)
            {
                Console.WriteLine("Enter");
            }
        }
    }

    /// <summary>
    ///     Updates the starting position of the given row number.
    /// </summary>
    /// <param name="rowNum">An int representing the row to update.</param>
    private void UpdateRowStart(int rowNum)
    {
        var (size, (height, _, _)) = _lMetrics[rowNum].First();
        if (_lMetrics.TryGetValue(rowNum - 1, out var value))
        {
            var (prevSize, (_, padding, _)) = value.First();
            if (prevSize > size) height += padding;
        }

        _heightByRow[rowNum] = height * 1.15f;
    }

    /// <summary>
    ///     Changes the text bank offset.
    /// </summary>
    /// <param name="offset">A float representing the text bank offset.</param>
    public void SetOffset(float offset)
    {
        _rowStart = y.Start + offset;
        _rowEnd = y.End + offset;
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void RemoveCharacter(int charPosition)
    {
        _charList.RemoveAt(charPosition);
        RecalculatePositions(charPosition);
    }
}