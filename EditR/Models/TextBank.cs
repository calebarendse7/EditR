using EditR.Models.RBList;
using LanguageExt;
using LanguageExt.Common;

namespace EditR.Models;

/// <summary>
///     Represents collection of characters.
/// </summary>
public class TextBank
{
    private readonly RbList<StyledChar> _charList = [];
    private readonly Dictionary<int, RowInfo> _fontsByRow = [];
    private float _bottomMargin;
    private bool _isChanged = true;

    private int _lastChangedIndex;

    private float _offsetHeight;
    private float _rStart;
    private (float Start, float End) _x;
    private (float Start, float Height, float THeight) _y;

    public int Count => _charList.Count;

    public int PageNum { get; private set; }

    public Option<StyledChar> this[int i] => _charList.TryGetValue(i);

    public void Each(Action<StyledChar, RowInfo, int> action)
    {
        if (_charList.Count == 0) return;
        var cRow = -1;
        var pNum = 0;
        var rStartOffset = _rStart;
        var rEnd = _offsetHeight - _bottomMargin;
        RowInfo? rowInfo = null;


        var isUpdateStarted = false;
        var column = _x.Start;
        var row = 0;
        var isNewLine = false;

        if (_isChanged)
        {
            isUpdateStarted = true;
            _charList.TryGetValue(_lastChangedIndex - 1).IfSome(val =>
            {
                column = val.Column + val.Width;
                row = val.RowNum;
                isNewLine = val.Value == '\n';
            });
            for (var i = _lastChangedIndex; i < _charList.Count; i++)
            {
                var value = _charList.TryGetValue(i);
                if (value.Case is not StyledChar item)
                {
                    Console.Error.WriteLine($"TextBank:Each: Could find {i}");
                    break;
                }
                
                if (column + item.Width > _x.End || isNewLine)
                {
                    column = _x.Start;
                    row++;
                }

                isNewLine = item.Value == '\n';

                var storedRow = item.RowNum;
                item.Column = column;
                item.RowNum = row;

                if (storedRow == -1)
                {
                    // The largest font may change here. So previous lines may have to be moved. 
                    TextUtil.UpdateFont(_fontsByRow, item);
                }
                else if (storedRow != row)
                {
                    // The largest font may change here. So previous lines may have to be moved. 
                    TextUtil.ReduceQuantity(_fontsByRow, storedRow, item.PtSize);
                    TextUtil.UpdateFont(_fontsByRow, item);
                }

                column += item.Width;
            }
        }

        for (var i = 0; i < _charList.Count; i++)
        {
            var value = _charList.TryGetValue(i);
            if (value.Case is not StyledChar item)
            {
                Console.Error.WriteLine($"TextBank:Each: Could find {i}");
                break;
            }

            if (cRow == item.RowNum && rowInfo is not null)
            {
                // Call draw function
                action(item, rowInfo, i);
                continue;
            }

            // Fetch next row information and move row value forward appropriately.
            if (!_fontsByRow.TryGetValue(++cRow, out rowInfo))
            {
                Console.Error.WriteLine(
                    $"TextBank:Each: Row info not found for {cRow}. ({string.Join(" ", _fontsByRow)})");
                break;
            }

            rStartOffset += rowInfo.Height;
            if (rStartOffset > rEnd)
            {
                pNum++;
                var pStart = _y.THeight * pNum;
                rEnd = _offsetHeight + pStart - _bottomMargin;
                rStartOffset = _rStart + pStart + rowInfo.Height;
            }

            rowInfo.RowOffset = rStartOffset;
            rowInfo.Start = i;
            // Call draw function
            action(item, rowInfo, i);
        }

        if (isUpdateStarted) _isChanged = false;
        PageNum = pNum;
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
        if (!_isChanged) _lastChangedIndex = charPosition;
        _charList.Insert(charPosition, item);
        _isChanged = true;
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void RemoveSingle(int charIndex)
    {
        TextUtil.RemoveChar(_charList, _fontsByRow, charIndex).Match(_ =>
        {
            _lastChangedIndex = Math.Min(charIndex, _lastChangedIndex);
            _isChanged = true;
        }, err => { Console.Error.WriteLine($"RemoveSingle:{err}"); });
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
    public (int, bool) FindNearestChar((float X, float Y) pos)
    {
        return TextUtil.Nearest(pos, _fontsByRow, _charList);
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
            if (TextUtil.RemoveChar(_charList, _fontsByRow, i).Case is not Error err) continue;
            Console.Error.WriteLine($"RemoveSelection:{err}");
            break;
        }

        //if (_charList.Count > 0) TextUtil.UpdateFrom(_charList, _fontsByRow, _x, selection.Start);
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

    public RowInfo GetRowInfo(int rowNum)
    {
        return _fontsByRow[rowNum];
    }
}