namespace editor.Models;

public class Cursor((float X, float Y) origin, (float X, float Y) boundary)
{
    private float _positionX = origin.X;
    private float _positionY = origin.Y;
    private float _offset;
    
    /// <summary>
    /// Moves the cursor.
    /// </summary>
    /// <param name="pos">The position to move the cursor to.</param>
    /// <param name="width">The font width.</param>
    public void MoveCursor((float X, float Y) pos, float width)
    {
        _positionX = pos.X + width;
        _positionY = pos.Y;
    }
    /// <summary>
    /// Checks if the position given is within the cursor boundaries.
    /// </summary>
    /// <param name="pos">The position to validate.</param>
    /// <returns>A Tuple representing a valid cursor position</returns>
    public (float, float) ValidatePosition((float X, float Y) pos)
    {
        (float X, float Y) valid = (_positionX + pos.X, _positionY);

        if (valid.X > boundary.X)
        {
            Console.WriteLine("Cursor past the end");
            valid.X = origin.X;
            valid.Y += pos.Y;

            // For now this will not work for multiple pages.
            if (valid.Y > boundary.Y) valid.Y -= pos.Y;
        }
        else
        {
            valid.X -= pos.X;
        }

        return valid;
    }
    /// <summary>
    /// Sets the cursor offset.
    /// </summary>
    /// <param name="offset">The offset amount</param>
    public void SetCursorOffset(float offset)
    {
        _offset = -50 + offset;
    }

    public float Offset()
    {
        return _offset;
    }
    /// <summary>
    /// Moves the cursor to the origin.
    /// </summary>
    public void MoveCursorOrigin()
    {
        _positionX = origin.X;
        _positionY = origin.Y;
    }
    /// <summary>
    /// Gets the cursor position
    /// </summary>
    /// <returns>A Tuple representing the current cursor position.</returns>
    public (float X, float Y) position => (_positionX, _positionY);
}