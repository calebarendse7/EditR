using System.Collections;
using EditR.Models.RBList;

namespace EditR.Models;

/// <summary>
///     Represents collection of characters.
/// </summary>
public class TextBank : IEnumerable<StyledChar>
{
    private readonly RbList<StyledChar> _charList = [];
    private readonly Dictionary<int, RowInfo> _fontsByRow = [];
    public int TextCount => _charList.Count;
    public int PageNum { get; private set; }

    private float _offsetHeight;
    private float _rStart;
    private (float Start, float End) _x;
    private (float Start, float Height, float THeight) _y;
    private float _bottomMargin;

    public StyledChar this[int i] => _charList[i];

    /// <summary>
    ///     Updates the boundary of the TextBank.
    /// </summary>
    /// <param name="xStart">A float representing the x starting position.</param>
    /// <param name="xEnd">A float representing the x ending position.</param>
    /// <param name="yStart">A float representing the y starting position.</param>
    /// <param name="yHeight">A float representing the page height.</param>
    /// <param name="yTotalHeight">A float representing the total page height with the page gap.</param>
    /// <param name="bottomMargin">A float representing the bottom margin.</param>
    /// <param name="offset">A float representing the drawing offset.</param>
    public void UpdateBoundaries(float xStart, float xEnd, float yStart, float yHeight, float yTotalHeight,
        float bottomMargin, float offset)
    {
        _x = (xStart, xEnd);
        _y = (yStart, yHeight, yTotalHeight);
        _bottomMargin = bottomMargin;
        _rStart = yStart + offset;
        _offsetHeight = yHeight + offset;
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<StyledChar> GetEnumerator()
    {
        var cRow = -1;
        var pNum = 0;
        var rStart = _rStart;
        var rEnd = _offsetHeight - _bottomMargin;
        foreach (var item in _charList)
        {
            if (cRow != item.RowNum)
            {
                if (!_fontsByRow.TryGetValue(++cRow, out var rowInfo))
                {
                    Console.WriteLine($"Row height not found {cRow}");
                    break;
                }

                rStart += rowInfo.Height;
                if (rStart > rEnd)
                {
                    pNum++;
                    var pStart = _y.THeight * pNum;
                    rEnd = _offsetHeight + pStart - _bottomMargin;
                    rStart = _rStart + pStart + rowInfo.Height;
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
    public void Add(StyledChar item, int charPosition)
    {
        _charList.Insert(charPosition, item);
        TextUtil.UpdateFrom(_charList, _fontsByRow, _x, charPosition);
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void Remove(int charIndex)
    {
        var toRemove = _charList[charIndex];
        _charList.RemoveAt(charIndex);
        TextUtil.ReduceQuantity(_fontsByRow, toRemove.RowNum, toRemove.PtSize);
        if (_charList.Count > 0) TextUtil.UpdateFrom(_charList, _fontsByRow, _x, charIndex - 1);
    }

    /// <summary>
    ///     Changes the text bank offset.
    /// </summary>
    /// <param name="offset">A float representing the text bank offset.</param>
    public void SetOffset(float offset)
    {
        _rStart = _y.Start + offset;
        _offsetHeight = _y.Height + offset;
    }
}