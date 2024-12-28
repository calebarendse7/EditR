namespace editor.Models;

public static class ListExtension
{
    private static int _i = 1;

    public static T PeekEnd<T>(this List<T> list)
    {
        if (_i > list.Count) throw new InvalidOperationException();
        return list[^_i++];
    }

    public static T Peek<T>(this List<T> list)
    {
        if (_i < 2) throw new InvalidOperationException();
        return list[^--_i];
    }
}