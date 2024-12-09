namespace editor.Models;

public class Cursor((float X, float Y) origin, (float X, float Y) boundary)
{
    private int _lineNumber = 1;
    private int _pageNumber;

    /// <summary>
    ///     Gets the cursor X position
    /// </summary>
    /// <returns>A Float representing the current cursor X position.</returns>
    public float Position { get; private set; } = origin.X;

    /// <summary>
    ///     Moves the cursor.
    /// </summary>
    /// <param name="pos">The position to move the cursor to.</param>
    /// <param name="width">The font width.</param>
    public void MoveCursor((float X, int Y, int PNum) pos, float width)
    {
        Position = pos.X + width;
        _lineNumber = pos.Y;
        _pageNumber = pos.PNum;
    }

    /// <summary>
    ///     Checks if the position given is within the cursor boundaries.
    /// </summary>
    /// <param name="pos">The position to validate.</param>
    /// <returns>A Tuple representing a valid cursor position</returns>
    public (float, int, int) ValidatePosition((float X, float LineNum) pos)
    {
        (float X, int LineNum, int PNum) valid = (Position + pos.X, _lineNumber, _pageNumber);

        if (valid.X > boundary.X)
        {
            valid.X = origin.X;
            valid.LineNum++;
            if (!(origin.Y + valid.LineNum * pos.LineNum > boundary.Y)) return valid;
            valid.PNum++;
            valid.LineNum = 1;
        }
        else
        {
            valid.X -= pos.X;
        }

        return valid;
    }

    /// <summary>
    ///     Moves the cursor to the origin.
    /// </summary>
    public void MoveCursorOrigin()
    {
        Position = origin.X;
        _lineNumber = 1;
    }
}