namespace Editor.Models.RBList;

public sealed record Leaf<T> : Child<T>
{
    private static readonly Leaf<T> Instance = new();

    private Leaf()
    {
    }

    public static Child<T> Empty => Instance;
}