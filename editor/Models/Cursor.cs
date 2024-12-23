namespace editor.Models;

public class Cursor((float X, float Y) origin, float lineEnd)
{
    /// <summary>
    ///     Gets the current cursor page number;
    /// </summary>
    /// <returns>An int representing the cursor page number.</returns>
    public int PageNumber { get; private set; } = 1;

    /// <summary>
    ///     Gets the cursor X position.
    /// </summary>
    /// <returns>A float representing the cursor X position.</returns>
    public float Position { get; private set; } = origin.X;

    /// <summary>
    ///     Gets the cursor line number.
    /// </summary>
    /// <returns>An int representing the cursor line number.</returns>
    public int LineNumber { get; private set; } = 1;

    /// <summary>
    ///     Moves the cursor.
    /// </summary>
    /// <param name="pos">The position to move the cursor to.</param>
    /// <param name="width">The font width.</param>
    public void Move((float X, int lineNum, int PNum) pos, float width)
    {
        Position = pos.X + width;
        LineNumber = pos.lineNum;
        PageNumber = pos.PNum;
    }

    /// <summary>
    ///     Checks if the position given is within the cursor boundaries.
    /// </summary>
    /// <param name="pos">The position to validate.</param>
    /// <param name="linePosition">The current line y value.</param>
    /// <param name="pageEnd">The end position of the last page.</param>
    /// <returns>A Tuple representing a valid cursor position.</returns>
    public (float, int, int) ValidatePosition((float Width, float LineHeight) pos, float linePosition, float pageEnd)
    {
        (float X, int LineNum, int PNum) valid = (Position + pos.Width, LineNumber, PageNumber);

        if (valid.X > lineEnd || pos.Width == 0)
        {
            valid.X = origin.X;
            valid.LineNum++;

            if (!(origin.Y + linePosition + pos.LineHeight > pageEnd)) return valid;
            valid.PNum++;
            valid.LineNum++;
        }
        else
        {
            valid.X -= pos.Width;
        }

        return valid;
    }

    /// <summary>
    ///     Moves the cursor to the origin.
    /// </summary>
    public void MoveOrigin()
    {
        Position = origin.X;
        LineNumber = 1;
    }
}