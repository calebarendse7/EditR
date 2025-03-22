using EditR.Models.RBList;
using SkiaSharp;

namespace EditR.Models;

public static class TextUtil
{
    public const float PixelPointRatio = 96f / 72;

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
    public static void ReduceQuantity(Dictionary<int, RowInfo> fontsByRow, int rowNum,
        int size)
    {
        if (!fontsByRow.TryGetValue(rowNum, out var value)) return;
        if (!value.SizeByMetric.TryGetValue(size, out var metric)) return;
        metric.Quantity--;
        if (metric.Quantity != 0) return;
        value.SizeByMetric.Remove(size);
        if (value.SizeByMetric.Count == 0)
            fontsByRow.Remove(rowNum);
        else
            Update(fontsByRow, value, rowNum, 1.15f);
    }

    /// <summary>
    ///     Updates the font information for the row of a character.
    /// </summary>
    /// <param name="fontsByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="c">A StyledChar representing the character added to the TextBank.</param>
    private static void UpdateFont(Dictionary<int, RowInfo> fontsByRow, StyledChar c)
    {
        if (!fontsByRow.TryGetValue(c.RowNum, out var value))
        {
            value = new RowInfo { Height = 0 };
            fontsByRow.Add(c.RowNum, value);
        }

        if (!value.SizeByMetric.TryGetValue(c.PtSize, out var metric))
        {
            metric = new CharMetric { LineHeight = c.Height, Padding = c.Padding, Quantity = 0 };
            value.SizeByMetric.Add(c.PtSize, metric);
            Update(fontsByRow, value, c.RowNum, 1.15f);
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
    /// <param name="rowNum">An int representing the row number.</param>
    /// <param name="current">A RowInfo representing the current row.</param>
    /// <param name="lineSpace">A float representing the line spacing for the document.</param>
    private static void Update(Dictionary<int, RowInfo> fontsByRow, RowInfo current, int rowNum, float lineSpace)
    {
        if (current.SizeByMetric.Count == 0) return;
        var (size, (height, _, _)) = current.SizeByMetric.Last();
        if (fontsByRow.TryGetValue(rowNum - 1, out var lastVal) && lastVal.SizeByMetric.Count > 0)
        {
            var (prevSize, (_, padding, _)) = lastVal.SizeByMetric.Last();
            if (prevSize > size) height += padding;
        }

        current.Height = height * lineSpace;
    }

    /// <summary>
    ///     Updates the column and row positions of the characters in the TextBank starting at a given index.
    /// </summary>
    /// <param name="charList">A list representing the characters in the TextBank.</param>
    /// <param name="fontsByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="x">A tuple of floats representing the start and end positions of the document in the x direction.</param>
    /// <param name="index">An int representing the index of the first character to recalculate from.</param>
    public static void UpdateFrom(RbList<StyledChar> charList,
        Dictionary<int, RowInfo> fontsByRow,
        (float Start, float End) x, int index)
    {
        var column = x.Start;
        var rowNumber = 0;
        var isNextLine = false;
        if (index > 0)
        {
            var r = charList.TryGetValue(index - 1);
            if (r.Case is not StyledChar c)
            {
                Console.Error.WriteLine("TextUtil:UpdateFrom: Could not find previous character.");
                return;
            }

            rowNumber = c.RowNum;
            column = c.Column + c.Width;
            isNextLine = c.Value == '\n';
        }

        for (var i = index; i < charList.Count; i++)
        {
            var result = charList.TryGetValue(i);
            if (result.Case is not StyledChar c) break;
            if (column + c.Width > x.End || isNextLine)
            {
                column = x.Start;
                rowNumber++;
            }

            isNextLine = c.Value == '\n';

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

            column += c.Width;
        }
    }

    /// <summary>
    ///     Finds a color in a list of colors given a key.
    /// </summary>
    /// <param name="colors">A Dictionary of colors mapped to strings to search.</param>
    /// <param name="color">A string representing the key to search for.</param>
    /// <returns>An SKColor representing the color found.</returns>
    public static SKColor FindColor(Dictionary<string, SKColor> colors, string color)
    {
        if (colors.TryGetValue(color, out var c)) return c;
        var result = SKColor.Parse(color);
        colors[color] = result;
        return result;
    }

    /// <summary>
    ///     Calculates one component of Manhattan distance.
    /// </summary>
    /// <param name="pointOne">A float representing the first point's first component.</param>
    /// <param name="pointTwo">A float representing the second point's first component.</param>
    /// <returns>A float representing the manhattan distance. </returns>
    private static float ManDist(float pointOne, float pointTwo)
    {
        return Math.Abs(pointTwo - pointOne);
    }

    /// <summary>
    ///     Finds the nearest char to a point from position in a list.
    /// </summary>
    /// <param name="point">An int representing the starting position.</param>
    /// <param name="infoByRow">
    ///     A dictionary representing each font size by quantity, sorted in descending order, for each
    ///     row.
    /// </param>
    /// <param name="charList">An RbList of StyledChar representing the list to search.</param>
    /// <param name="nearStart">A bool indicating if the char should be found near the start X value.  </param>
    /// <returns>An int representing the index of the nearest char.</returns>
    public static int NearestCharIndex((float X, float Y) point, Dictionary<int, RowInfo> infoByRow,
        RbList<StyledChar> charList, bool nearStart = false)
    {
        var minDist = float.MaxValue;
        var row = -1;
        var result = -1;
        for (var i = 0; i < infoByRow.Count; i++)
        {
            var info = infoByRow[i];
            var r = ManDist(point.Y, info.RowStart);
            if (r > minDist) break;
            minDist = r;
            row = info.Index;
        }

        minDist = float.MaxValue;
        for (var i = row; i < charList.Count; i++)
        {
            var value = charList.TryGetValue(i);
            if (value.Case is not StyledChar c)
            {
                Console.Error.WriteLine($"TextUtil:NearestCharIndex: Could not find char from row index {i}.");
                break;
            }

            var total = c.Column + c.Width;
            var dist = ManDist(point.X, nearStart ? c.Column : total);
            if (dist > minDist) break;
            result = i;
            minDist = dist;
            if (nearStart && dist > ManDist(point.X, total)) result++;
        }

        return result;
    }
}