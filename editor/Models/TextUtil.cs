using Editor.Models.RBList;
using SkiaSharp;

namespace editor.Models;

public static class TextUtil
{
    /// <summary>
    ///     Calculates line height given font metrics.
    /// </summary>
    /// <param name="metrics">A SKFontMetrics representing the information of the font.</param>
    /// <returns>A float representing the calculated line height.</returns>
    public static float LineHeight(SKFontMetrics metrics)
    {
        return -metrics.Ascent + metrics.Descent + metrics.Leading;
    }

    /// <summary>
    ///     Reduces the quantity of a font size from a row.
    /// </summary>
    /// <param name="fontsByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="rowNum">An int representing the row to remove from.</param>
    /// <param name="size">A float representing the font size to remove.</param>
    public static void ReduceQuantity(Dictionary<int, SortedDictionary<int, CharMetric>> fontsByRow, int rowNum,
        int size)
    {
        if (!fontsByRow.TryGetValue(rowNum, out var value)) return;
        if (!value.TryGetValue(size, out var metric)) return;
        metric.Quantity--;
        if (metric.Quantity != 0) return;
        value.Remove(size);
    }

    /// <summary>
    ///     Updates the font information for the row of a character.
    /// </summary>
    /// <param name="fontsByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="c">A StyledChar representing the character added to the TextBank.</param>
    private static void UpdateFont(Dictionary<int, SortedDictionary<int, CharMetric>> fontsByRow, StyledChar c)
    {
        if (!fontsByRow.TryGetValue(c.RowNum, out var value))
        {
            value = new SortedDictionary<int, CharMetric>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            fontsByRow.Add(c.RowNum, value);
        }

        if (!value.TryGetValue(c.PtSize, out var metric))
        {
            metric = new CharMetric(c.Height, c.Padding);
            value.Add(c.PtSize, metric);
        }

        metric.Quantity++;
    }

    /// <summary>
    ///     Updates the line height for a specified row.
    /// </summary>
    /// <param name="fontsByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="heightByRow">A dictionary representing the heights for each row.</param>
    /// <param name="rowNum">An int representing the row number.</param>
    /// <param name="lineSpace">A float representing the line spacing for the document.</param>
    private static void UpdateHeight(Dictionary<int, SortedDictionary<int, CharMetric>> fontsByRow,
        Dictionary<int, float> heightByRow, int rowNum, float lineSpace)
    {
        if (!fontsByRow.TryGetValue(rowNum, out var value)) return;
        if (value.Count == 0) return;
        var (size, (height, _, _)) = value.First();

        if (fontsByRow.TryGetValue(rowNum - 1, out var lastVal) && lastVal.Count > 0)
        {
            var (prevSize, (_, padding, _)) = value.First();
            if (prevSize > size) height += padding;
        }

        heightByRow[rowNum] = height * lineSpace;
    }

    /// <summary>
    ///     Updates the column and row positions of the characters in the TextBank starting at a given index.
    /// </summary>
    /// <param name="charList">A list representing the characters in the TextBank.</param>
    /// <param name="fontsByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="heightByRow">A dictionary representing the heights for each row.</param>
    /// <param name="x">A tuple of floats representing the start and end positions of the document in the x direction.</param>
    /// <param name="index">An int representing the index of the first character to recalculate from.</param>
    public static void UpdateFrom(RbList<StyledChar> charList,
        Dictionary<int, SortedDictionary<int, CharMetric>> fontsByRow, Dictionary<int, float> heightByRow,
        (float Start, float End) x, int index)
    {
        var column = x.Start;
        var rowNumber = 0;
        var isNextLine = false;
        if (index > 0)
        {
            var r = charList[index - 1];
            rowNumber = r.RowNum;
            column = r.Column + r.Width;
            isNextLine = r.Value == '\n';
        }

        for (var i = index; i < charList.Count; i++)
        {
            var c = charList[i];
            if (column + c.Width > x.End || isNextLine)
            {
                column = x.Start;
                rowNumber++;
            }
            var storedRow = c.RowNum;
            c.Column = column;
            c.RowNum = rowNumber;

            // If the character was on a different row
            if (storedRow == -1)
            {
                UpdateFont(fontsByRow, c);
            }
            else if (storedRow != rowNumber)
            {
                ReduceQuantity(fontsByRow, storedRow, c.PtSize);
                UpdateFont(fontsByRow, c);
            }

            UpdateHeight(fontsByRow, heightByRow, c.RowNum, 1.15f);
            column += c.Width;
            isNextLine = c.Value == '\n';
        }
    }
}