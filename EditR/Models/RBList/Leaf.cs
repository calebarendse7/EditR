namespace EditR.Models.RBList;

public sealed record Leaf<T> : Child
{
    private static readonly Leaf<T> Instance = new();

    private Leaf()
    {
    }

    public static Child Empty => Instance;
}