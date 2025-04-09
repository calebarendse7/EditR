using EditR.Models.RBList;
using LanguageExt;
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
    public static void UpdateFont(Dictionary<int, RowInfo> fontsByRow, StyledChar c)
    {
        if (!fontsByRow.TryGetValue(c.RowNum, out var value))
        {
            value = new RowInfo();
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
    private static bool Update(Dictionary<int, RowInfo> fontsByRow, RowInfo current, int rowNum, float lineSpace)
    {
        if (current.SizeByMetric.Count == 0) return false;
        var (size, (height, _, _)) = current.SizeByMetric.Last();
        if (fontsByRow.TryGetValue(rowNum - 1, out var lastVal) && lastVal.SizeByMetric.Count > 0)
        {
            var (prevSize, (_, padding, _)) = lastVal.SizeByMetric.Last();
            if (size == current.LargestSize && prevSize == lastVal.LargestSize) return false;
            if (prevSize > size) height += padding;
        }
        else if (size == current.LargestSize)
        {
            return false;
        }

        current.Height = height * lineSpace;
        current.LargestSize = size;
        return true;
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
    /// <returns>An int representing the index of the nearest char.</returns>
    public static int NearestCharIndex((float X, float Y) point, Dictionary<int, RowInfo> infoByRow,
        RbList<StyledChar> charList)
    {
        var minDist = float.MaxValue;
        var row = -1;
        var result = -1;
        int k;
        for (k = 0; k < infoByRow.Count; k++)
        {
            var info = infoByRow[k];
            var r = ManDist(point.Y, info.RowOffset);
            if (r > minDist) break;
            minDist = r;
            row = info.Start;
        }

        minDist = float.MaxValue;
        for (var i = row; i < charList.Count; i++)
        {
            if (charList.TryGetValue(i).Case is not StyledChar c)
            {
                Console.Error.WriteLine($"TextUtil:NearestCharIndex: Could not find char from row index {i}.");
                break;
            }

            var dist = ManDist(point.X, c.Column);

            if (dist > minDist) break;

            result = i;
            minDist = dist;
        }

        return result;
    }

    public static Fin<Unit> RemoveChar(RbList<StyledChar> charList, Dictionary<int, RowInfo> infoByRow, int index)
    {
        if (charList.TryGetValue(index).Case is not StyledChar toRemove)
            return Fin<Unit>.Fail($"TextUtil:RemoveChar: Could not find char to remove {index}");
        charList.RemoveAt(index);
        ReduceQuantity(infoByRow, toRemove.RowNum, toRemove.PtSize);
        return Fin<Unit>.Succ(default);
    }

    public static (int, float, float) NearestPosition((float X, float Y) point, Dictionary<int, RowInfo> infoByRow,
        RbList<StyledChar> charList)
    {
        var minDist = float.MaxValue;
        var row = -1;
        var indexResult = -1;
        var rowResult = 0f;
        var columnResult = 0f;

        int k;
        for (k = 0; k < infoByRow.Count; k++)
        {
            var info = infoByRow[k];
            var r = ManDist(point.Y, info.RowOffset);
            if (r > minDist) break;
            minDist = r;
            row = info.Start;
            rowResult = info.Row;
        }

        minDist = float.MaxValue;
        var nextRow = infoByRow.TryGetValue(k, out var next) ? next.Start : charList.Count;
        for (var i = row; i < charList.Count; i++)
        {
            if (charList.TryGetValue(i).Case is not StyledChar c)
            {
                Console.Error.WriteLine($"TextUtil:NearestCharIndex: Could not find char from row index {i}.");
                break;
            }

            var dist = ManDist(point.X, c.Column);
            var result = c.Column;
            if (i + 1 == nextRow)
            {
                var end = c.Column + c.Width;
                var distEnd = ManDist(point.X, end);
                if (dist > distEnd)
                {
                    result = end;
                    dist = distEnd;
                }
            }

            if (dist > minDist) break;
            columnResult = result;
            indexResult = i;
            minDist = dist;
        }

        return (indexResult, columnResult, rowResult);
    }

    public static (int, bool) Nearest((float X, float Y) point, Dictionary<int, RowInfo> infoByRow,
        RbList<StyledChar> charList)
    {
        var minDist = float.MaxValue;
        var row = -1;
        var indexResult = -1;
        var rowResult = 0f;
        var eol = false;

        int k;
        for (k = 0; k < infoByRow.Count; k++)
        {
            var info = infoByRow[k];
            var r = ManDist(point.Y, info.RowOffset);
            if (r > minDist) break;
            minDist = r;
            row = info.Start;
            rowResult = info.Row;
        }

        minDist = float.MaxValue;
        var nextRow = infoByRow.TryGetValue(k, out var next) ? next.Start : charList.Count;
        for (var i = row; i < charList.Count; i++)
        {
            if (charList.TryGetValue(i).Case is not StyledChar c)
            {
                Console.Error.WriteLine($"TextUtil:NearestCharIndex: Could not find char from row index {i}.");
                break;
            }

            var dist = ManDist(point.X, c.Column);
            if (i + 1 == nextRow)
            {
                var end = c.Column + c.Width;
                var distEnd = ManDist(point.X, end);
                if (dist > distEnd)
                {
                    eol = true;
                    dist = distEnd;
                }
            }

            if (dist > minDist) break;
            indexResult = i;
            minDist = dist;
        }

        return (indexResult, eol);
    }
}