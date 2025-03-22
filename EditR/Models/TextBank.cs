using System.Collections;
using EditR.Models.RBList;
using LanguageExt;

namespace EditR.Models;

/// <summary>
///     Represents collection of characters.
/// </summary>
public class TextBank : IEnumerable<StyledChar>
{
    private readonly RbList<StyledChar> _charList = [];
    private readonly Dictionary<int, RowInfo> _fontsByRow = [];
    private float _bottomMargin;

    private float _offsetHeight;
    private float _rStart;
    private (float Start, float End) _x;
    private (float Start, float Height, float THeight) _y;
    public int Count => _charList.Count;

    public int PageNum { get; private set; }

    public Option<StyledChar> this[int i] => _charList.TryGetValue(i);

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
        for (var i = 0; i < _charList.Count; i++)
        {
            var value = _charList.TryGetValue(i);
            if (value.Case is not StyledChar item)
            {
                Console.Error.WriteLine($"TextBank:GetEnumerator: Could find {i}");
                break;
            }

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

                rowInfo.RowStart = rStart;
                rowInfo.Index = i;
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
    public void RemoveSingle(int charIndex)
    {
        var value = _charList.TryGetValue(charIndex);
        if (value.Case is not StyledChar toRemove)
        {
            Console.Error.WriteLine($"TextBank:RemoveSingle: Could not remove single {charIndex}");
            return;
        }

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

    /// <summary>
    ///     Finds character nearest to a point in the TextBank.
    /// </summary>
    /// <param name="pos">A Tuple representing the origin point.</param>
    /// <returns>An int representing the index of the nearest character to the origin point.</returns>
    public int FindNearestChar((float X, float Y) pos)
    {
        return TextUtil.NearestCharIndex(pos, _fontsByRow, _charList, true);
    }

    /// <summary>
    ///     Finds and selects the character within a range.
    /// </summary>
    /// <param name="pointOne">A Tuple representing a point. </param>
    /// <param name="pointTwo">A Tuple representing a point. </param>
    /// <returns>A Tuple with 2 integers representing the character indices nearest each of the points.</returns>
    public (int, int) FindRange((float X, float Y) pointOne, (float X, float Y) pointTwo)
    {
        var (a, b) = (pointOne.Y <= pointTwo.Y) switch
        {
            true => (TextUtil.NearestCharIndex(pointOne, _fontsByRow, _charList),
                TextUtil.NearestCharIndex(pointTwo, _fontsByRow, _charList)),

            false => (TextUtil.NearestCharIndex(pointTwo, _fontsByRow, _charList),
                TextUtil.NearestCharIndex(pointOne, _fontsByRow, _charList))
        };
        return a < b ? (a, b) : (b, a);
    }

    /// <summary>
    ///     Removes characters within a given range.
    /// </summary>
    /// <param name="selection">A tuple of two integers representing the range. </param>
    /// <returns>An int representing the start of the range. </returns>
    public int RemoveSelection((int Start, int End) selection)
    {
        for (var i = Math.Min(selection.End, _charList.Count - 1); i >= selection.Start; i--)
        {
            var value = _charList.TryGetValue(i);
            if (value.Case is not StyledChar toRemove)
            {
                Console.Error.WriteLine($"TextBank:RemoveSelection: Could not remove selection {i}");
                break;
            }

            _charList.RemoveAt(i);
            TextUtil.ReduceQuantity(_fontsByRow, toRemove.RowNum, toRemove.PtSize);
        }

        if (_charList.Count > 0) TextUtil.UpdateFrom(_charList, _fontsByRow, _x, selection.Start);
        return selection.Start;
    }

    /// <summary>
    ///     Check if the TextBank is empty.
    /// </summary>
    /// <returns>A bool representing if the TextBank is empty. </returns>
    public bool IsEmpty()
    {
        return _charList.Count == 0 && _fontsByRow.Count == 0;
    }
}