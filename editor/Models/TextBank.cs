using System.Collections;
using SkiaSharp;

namespace editor.Models;

/// <summary>
///     Represents collection of characters.
/// </summary>
/// <param name="x">The x boundary of the text bank.</param>
/// <param name="y">The y boundary of the text bank.</param>
public class TextBank((float Start, float End) x, (float Start, float End, float THeight) y)
    : IEnumerable<StyledCharacter>
{
    private readonly IList<StyledCharacter> _charList = [];

    private readonly Dictionary<int, SortedDictionary<float, CharMetric>>
        _lMetrics = [];

    private readonly Dictionary<int, int> _pageLines = [];
    private (int PNum, int LNum) _bankPos = (1, 1);
    private float _column = x.Start;
    private float _lStartOffset;
    public int CharCount => _charList.Count;
    public int PageCount => _bankPos.PNum;

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<StyledCharacter> GetEnumerator()
    {
        return _charList.GetEnumerator();
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
    public void AddCharacter(char value, float width, float height, float size, float padding, SKColor color)
    {
        if (_column + width > x.End || width == 0)
        {
            _column = x.Start;
            _bankPos.LNum++;
            if (LineStart(_bankPos.LNum - 1) + height > _lStartOffset + y.THeight * (_bankPos.PNum - 1) + y.End)
            {
                _bankPos.PNum++;
                _pageLines.Add(_bankPos.PNum, _bankPos.LNum);
            }
        }

        var key = _bankPos.LNum;
        var charMetric = new CharMetric(height, padding);
        if (!_lMetrics.TryGetValue(key, out var info))
        {
            _lMetrics.Add(key, new SortedDictionary<float, CharMetric>(
                Comparer<float>.Create((a, b) => b.CompareTo(a))) { { size, charMetric } });
        }
        else
        {
            if (!info.TryGetValue(size, out var metric))
                info.Add(size, charMetric);
            else
                metric.Quantity++;
        }

        _charList.Add(new StyledCharacter(value, (width, height, size), (_column, _bankPos.PNum, _bankPos.LNum),
            color));
        _column += width;
    }

    /// <summary>
    ///     Calculates the start of the specified line in the text bank.
    /// </summary>
    /// <param name="lineNum">An int corresponding the line to find the start of.</param>
    /// <returns>A float representing the line start of lineNum.</returns>
    public float LineStart(int lineNum)
    {
        var result = _lStartOffset + y.Start;
        var page = 1;
        for (var i = 1; i <= lineNum; i++)
        {
            var (charSize, (height, _)) = _lMetrics[i].First();
            if (i > 1)
            {
                var (size, charMetric) = _lMetrics[i - 1].First();
                if (size > charSize) height += charMetric.Padding;
            }

            if (_pageLines.TryGetValue(page + 1, out var pageLine))
                if (i == pageLine)
                {
                    result = _lStartOffset + y.Start + y.THeight * page;
                    // Add way to remove padding
                    page++;
                }

            result += height * 1.15f;
        }

        return result;
    }

    /// <summary>
    ///     Changes the text bank offset.
    /// </summary>
    /// <param name="offset">A float representing the text bank offset.</param>
    public void SetOffset(float offset)
    {
        _lStartOffset = offset;
    }

    /// <summary>
    ///     Removes the last added character from the text bank.
    /// </summary>
    public void RemoveCharacter()
    {
        var removedChar = _charList[^1];
        _charList.RemoveAt(_charList.Count - 1);
        var lineInfo = _lMetrics[removedChar.Position.LNum];
        var charInfo = lineInfo[removedChar.Font.Size];
        if (charInfo.Quantity == 1)
            lineInfo.Remove(removedChar.Font.Size);
        //if (lineInfo.Count != 0) _fontSize = lineInfo.First().Key;
        else
            charInfo.Quantity--;
        if (_charList.Count >= 1)
        {
            var prev = _charList[^1];
            if (prev.Position.PNum < _bankPos.PNum) _bankPos.PNum--;
            if (prev.Position.LNum < _bankPos.LNum) _lMetrics.Remove(_bankPos.LNum);
            _bankPos.LNum = prev.Position.LNum;
            _column = prev.Position.X + prev.Font.Width;
        }
        else
        {
            _lMetrics.Remove(_bankPos.LNum);
            _bankPos = (1, 1);
            _column = x.Start;
        }
    }
}