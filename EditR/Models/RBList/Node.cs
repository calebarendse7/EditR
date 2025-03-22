namespace EditR.Models.RBList;

public sealed record Node<T>(Colors Color, Child Left, T Data, Child Right)
    : Child
{
    public int SubtreeCount { get; } = (Left, Right) switch
    {
        (Node<T> l, Leaf<T>) => 1 + l.SubtreeCount,
        (Leaf<T>, Node<T> r) => 1 + r.SubtreeCount,
        (Node<T> l, Node<T> r) => 1 + l.SubtreeCount + r.SubtreeCount,
        _ => 1
    };
}