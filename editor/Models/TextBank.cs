using System.Collections;
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
    private readonly Dictionary<int, SortedDictionary<int, CharMetric>> _fontsByRow = [];
    private readonly Dictionary<int, float> _heightByRow = [];
    private float _pEnd = y.End + scrollOffset;

    private float _pStart = y.Start + scrollOffset;
    public int TextCount => _charList.Count;
    public int PageNum { get; private set; }
    public StyledChar this[int i] => _charList[i];

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<StyledChar> GetEnumerator()
    {
        var cRow = -1;
        var pNum = 0;
        var rStart = _pStart;
        var rEnd = _pEnd;
        foreach (var item in _charList)
        {
            if (cRow != item.RowNum)
            {
                if (!_heightByRow.TryGetValue(++cRow, out var rowHeight))
                    Console.WriteLine($"Row height not found {cRow}");
                rStart += rowHeight;
                if (rStart > rEnd)
                {
                    pNum++;
                    var pStart = y.THeight * pNum;
                    rEnd += pStart;
                    rStart = _pStart + pStart + rowHeight;
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
    /// <param name="charPosition">An int representing the character position.</param>
    /// <param name="width">A float representing the font width of the character.</param>
    /// <param name="height">A float representing the font height of the character.</param>
    /// <param name="size">A float representing the font size of the character.</param>
    /// <param name="padding">A float representing the font padding of the character.</param>
    /// <param name="color">A float representing the font color of the character.</param>
    public void Add(char value, int charPosition, float width, float height, float size, float padding, int ptSize,
        SKColor color)
    {
        _charList.Insert(charPosition, new StyledChar(value, width, height, padding, size, ptSize, color));
        TextUtil.UpdateFrom(_charList, _fontsByRow, _heightByRow, x, charPosition);
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void Remove(int charIndex)
    {
        var toRemove = _charList[charIndex];
        _charList.RemoveAt(charIndex);
        TextUtil.ReduceQuantity(_fontsByRow, toRemove.RowNum, toRemove.PtSize);
        if (_charList.Count > 0) TextUtil.UpdateFrom(_charList, _fontsByRow, _heightByRow, x, charIndex - 1);
    }

    /// <summary>
    ///     Changes the text bank offset.
    /// </summary>
    /// <param name="offset">A float representing the text bank offset.</param>
    public void SetOffset(float offset)
    {
        _pStart = y.Start + offset;
        _pEnd = y.End + offset;
    }
}