using System.Collections;
using System.Text;
using EditR.Models.RBList;
using LanguageExt;
using Microsoft.VisualBasic;
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
    public int Count => _charList.Count;

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
        for (var i = 0; i < _charList.Count; i++)
        {
            var item = _charList[i];
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
        var minDist = float.MaxValue;
        var start = 0;
        for (var i = 0; i < _fontsByRow.Count; i++)
        {
            var info = _fontsByRow[i];
            var r = TextUtil.CalcDist(pos.Y, info.RowStart, minDist).Case;
            if (r is not float f) break;
            minDist = f;
            start = info.Index;
        }

        return TextUtil.FindFromStart(start, pos.X, _charList);
    }

    /// <summary>
    ///     Finds and selects the character within a range.
    /// </summary>
    /// <param name="start">A Tuple representing the coordinates of the start of the range.</param>
    /// <param name="end">A Tuple representing the coordinates of the end of the range.</param>
    /// <returns>A Tuple with 2 integers representing the character indices nearest to start and end of the range.</returns>
    public (int, int) FindRange((float X, float Y) start, (float X, float Y) end)
    {
        var (startMin, endMin) = (float.MaxValue, float.MaxValue);
        var (foundStart, foundEnd) = (false, false);
        var (startI, endI) = (0, 0);
        
        for (var i = 0; i < _fontsByRow.Count && !(foundStart && foundEnd); i++)
        {
            var info = _fontsByRow[i];
            if (!foundStart)
            {
                (foundStart, startMin, startI) = TextUtil.CalcDist(start.Y, info.RowStart, startMin)
                    .Match(f => (false, f, info.Index), (true, startMin, startI));
            }
            
            if (!foundEnd)
            {
                (foundEnd, endMin, endI) = TextUtil.CalcDist(end.Y, info.RowStart, endMin)
                    .Match(f => (false, f, info.Index), (true, endMin, endI));
            }
        }

        var result = (TextUtil.FindFromStart(startI, start.X, _charList),
            TextUtil.FindFromStart(endI, end.X, _charList));
       
        return startI == endI && end.X < start.X ? result.Map((a, b) => (b, a)) : result;
        
    }
}