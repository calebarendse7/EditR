using System.Collections;
using EditR.Models.RBList;

namespace EditR.Models;

/// <summary>
///     Represents collection of characters.
/// </summary>
/// <param name="x">The x boundary of the text bank.</param>
/// <param name="y">The y boundary of the text bank.</param>
public class TextBank((float Start, float End) x, (float Start, float End, float THeight) y, float scrollOffset)
    : IEnumerable<StyledChar>
{
    private readonly RbList<StyledChar> _charList = [];
    private readonly Dictionary<int, RowInfo> _fontsByRow = [];
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
                if (!_fontsByRow.TryGetValue(++cRow, out var rowInfo))
                {
                    Console.WriteLine($"Row height not found {cRow}");
                    break;
                }

                rStart += rowInfo.Height;
                if (rStart > rEnd)
                {
                    pNum++;
                    var pStart = y.THeight * pNum;
                    rEnd += pStart;
                    rStart = _pStart + pStart + rowInfo.Height;
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
        TextUtil.UpdateFrom(_charList, _fontsByRow, x, charPosition);
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void Remove(int charIndex)
    {
        var toRemove = _charList[charIndex];
        _charList.RemoveAt(charIndex);
        TextUtil.ReduceQuantity(_fontsByRow, toRemove.RowNum, toRemove.PtSize);
        if (_charList.Count > 0) TextUtil.UpdateFrom(_charList, _fontsByRow, x, charIndex - 1);
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