using System.Collections;
using System.Text;
using EditR.Models.RBList;
using LanguageExt;
using MudBlazor;

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

    /// <summary>
    ///     Finds character nearest to a point in the TextBank.
    /// </summary>
    /// <param name="pos">A Tuple representing the origin point.</param>
    /// <returns>An int representing the index of the nearest character to the origin point.</returns>
    public int FindNearestChar((float X, float Y) pos)
    {
        var result = 0;
        var textCount = _charList.Count - 1;
        var rowDist = float.MaxValue;
        var columnDist = float.MaxValue;
        var rowNum = -1;
        var i = 0;
        for (i = 0; i < _charList.Count; i++)
        {
            var c = _charList[i];
            TextUtil.CheckDist((columnDist, rowDist), (rowNum, c.RowNum), pos, (c.Column, c.Row), (i, result), false).Match(
                value => (columnDist, rowDist, rowNum, result) = value, () => { i = _charList.Count; });
            if (result == textCount && Math.Abs(pos.X - (c.Column + c.Width)) < Math.Abs(pos.X - c.Column))
            {
                result++;
            }
        }
        return result;
    }
    /// <summary>
    ///     Finds and selects the character within a range.
    /// </summary>
    /// <param name="start">A Tuple representing the coordinates of the start of the range.</param>
    /// <param name="end">A Tuple representing the coordinates of the end of the range.</param>
    /// <returns>A Tuple with 2 integers representing the character indices nearest to start and end of the range.</returns>
    public (int, int) FindRange((float X, float Y) start, (float X, float Y) end)
    {
        var (startRowDist, startColDist) = (float.MaxValue, float.MaxValue);
        var (endRowDist, endColDist) = (float.MaxValue, float.MaxValue);

        var (startRow, endRow) = (-1, -1);
        var (startId, endId) = (0, 0);
        var i = 0;
        for (i = 0; i < _charList.Count; i++)
        {
            var c = _charList[i];
            var pt = (c.Column, c.Row);
            TextUtil.CheckDist((endColDist, endRowDist), (endRow, c.RowNum),
                    end, pt, (i, endId))
                .Match(
                    value =>
                        (endColDist, endRowDist, endRow, endId) = value,
                    () => { i = _charList.Count; });
            TextUtil.CheckDist((startColDist, startRowDist), (startRow, c.RowNum),
                    start, pt, (i, startId))
                .Match(value => { (startColDist, startRowDist, startRow, startId) = value; }, () =>
                {
                    if (i != _charList.Count) c.IsSelected = true;
                });
        }
        _charList[startId].IsSelected = true;
        return (startId, endId);
    }
}