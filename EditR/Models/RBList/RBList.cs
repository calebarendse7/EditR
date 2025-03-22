using System.Collections;
using LanguageExt;

namespace EditR.Models.RBList;

public class RbList<T> : IEnumerable<T>
{
    private Child _root = Leaf<T>.Empty;
    public int Count => _root is Node<T> n ? n.SubtreeCount : 0;

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return RawGet(_root, i);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Option<T> TryGetValue(int i)
    {
        if (Count == 0 || i >= Count) return Option<T>.None;

        return Get(_root, i);
    }

    public void Insert(int index, T key)
    {
        _root = InsertAux(_root, index, key).Map(Blacken).FromResult();
    }

    public void RemoveAt(int index)
    {
        _root = DeleteAux(_root, index).FromResult();
    }

    private static Result<Child> Balance(Child node)
    {
        return node switch
        {
            Node<T>(Colors.Black, Node<T>(Colors.Red, Node<T>(Colors.Red, var a, var x, var b), var y, var c), var z,
                var d) _ => Result<Child>.ToDo(new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, Node<T>(Colors.Red, var a, var x, Node<T>(Colors.Red, var b, var y, var c)), var z,
                var d) => Result<Child>.ToDo(new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, var a, var x,
                Node<T>(Colors.Red, Node<T>(Colors.Red, var b, var y, var c), var z, var d)) => Result<Child>.ToDo(
                    new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y, new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, var a, var x,
                Node<T>(Colors.Red, var b, var y, Node<T>(Colors.Red, var c, var z, var d))) => Result<Child>.ToDo(
                    new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y, new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, var a, var x, var b) => Result<Child>.Done(new Node<T>(Colors.Black, a, x, b)),
            Node<T>(Colors.Red, var a, var x, var b) => Result<Child>.ToDo(new Node<T>(Colors.Red, a, x, b)),
            _ => throw new Exception("Error occured while balancing")
        };
    }

    private static Result<Child> DeleteAux(Child child, int index)
    {
        return child switch
        {
            Leaf<T> l => Result<Child>.Done(l),
            Node<T>(_, var l, _, _) n when index == ChildCount(l) => DeleteCurrent(n),
            Node<T>(var k, var l, var x, var r) when index < ChildCount(l) => DeleteAux(l, index)
                .Map(rst => new Node<T>(k, rst, x, r)).Bind(EqLeft),
            Node<T>(var k, var l, var x, var r) => DeleteAux(r, index - ChildCount(l) - 1)
                .Map(rst => new Node<T>(k, l, x, rst)).Bind(EqRight),
            _ => throw new Exception("DeleteAux error")
        };
    }

    private static Result<Child> DeleteCurrent(Child child)
    {
        return child switch
        {
            Node<T>(Colors.Red, var a, _, Leaf<T>) => Result<Child>.Done(a),
            Node<T>(Colors.Black, var a, _, Leaf<T>) => BlackenDelete(a),
            Node<T>(var k, var a, _, var b) => DeleteMin(b)
                .Map((node, min) => node.Map(n => new Node<T>(k, a, min, n))).Bind(EqRight),
            _ => throw new Exception("DeleteCurrent error")
        };
    }

    private static (Result<Child> node, T min) DeleteMin(Child child)
    {
        return child switch
        {
            Node<T>(Colors.Red, Leaf<T>, var y, var b) => (Result<Child>.Done(b), y),
            Node<T>(Colors.Black, Leaf<T>, var y, var b) => (BlackenDelete(b), y),
            Node<T>(var k, var a, var y, var b) => DeleteMin(a)
                .Map((node, min) => (node.Map(n => new Node<T>(k, n, y, b)).Bind(EqLeft), min)),
            _ => throw new Exception("DeleteMin error")
        };
    }

    private static Result<Child> BalanceDelete(Child node)
    {
        return node switch
        {
            Node<T>(var k, Node<T>(Colors.Red, Node<T>(Colors.Red, var a, var x, var b), var y, var c), var z, var d) _
                => Result<Child>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(var k, Node<T>(Colors.Red, var a, var x, Node<T>(Colors.Red, var b, var y, var c)), var z, var d) =>
                Result<Child>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(var k, var a, var x, Node<T>(Colors.Red, Node<T>(Colors.Red, var b, var y, var c), var z, var d)) =>
                Result<Child>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(var k, var a, var x, Node<T>(Colors.Red, var b, var y, Node<T>(Colors.Red, var c, var z, var d))) =>
                Result<Child>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            _ => BlackenDelete(node)
        };
    }

    private static Result<Child> BlackenDelete(Child node)
    {
        return node switch
        {
            Node<T>(Colors.Red, var l, var x, var r) => Result<Child>.Done(new Node<T>(Colors.Black, l, x, r)),
            _ => Result<Child>.ToDo(node)
        };
    }

    private static Result<Child> EqLeft(Child node)
    {
        return node switch
        {
            Node<T>(var k, var l, var x, Node<T>(Colors.Black, var c, var y, var d)) => BalanceDelete(new Node<T>(k, l,
                x, new Node<T>(Colors.Red, c, y, d))),
            Node<T>(_, var a, var y, Node<T>(Colors.Red, var c, var z, var d)) => EqLeft(new Node<T>(Colors.Red, a,
                y, c)).Map(n => new Node<T>(Colors.Black, n, z, d)),
            _ => throw new Exception("EQLeft error")
        };
    }

    private static Result<Child> EqRight(Child node)
    {
        return node switch
        {
            Node<T>(var k, Node<T>(Colors.Black, var a, var x, var b), var y, var c) => BalanceDelete(new Node<T>(k,
                new Node<T>(Colors.Red, a, x, b), y, c)),
            Node<T>(_, Node<T>(Colors.Red, var a, var x, var b), var y, var c) => EqRight(new Node<T>(Colors.Red, b,
                y, c)).Map(rst => new Node<T>(Colors.Black, a, x, rst)),
            _ => throw new Exception("EQRight error")
        };
    }

    private static Result<Child> InsertAux(Child child, int index, T key)
    {
        return child switch
        {
            Leaf<T> => Result<Child>.ToDo(new Node<T>(Colors.Red, Leaf<T>.Empty, key, Leaf<T>.Empty)),
            Node<T>(var k, var l, var d, var r) when index <= ChildCount(l) => InsertAux(l, index, key)
                .Map(lt => new Node<T>(k, lt, d, r)).Bind(Balance),
            Node<T>(var k, var l, var d, var r) => InsertAux(r, index - ChildCount(l) - 1, key)
                .Map(rt => new Node<T>(k, l, d, rt)).Bind(Balance),
            _ => throw new Exception("Error occured while inserting")
        };
    }

    private static int ChildCount(Child child)
    {
        return child switch
        {
            Node<T> n => n.SubtreeCount,
            _ => 0
        };
    }

    private static Child Blacken(Child child)
    {
        return child switch
        {
            Node<T> n => n with { Color = Colors.Black },
            _ => child
        };
    }

    private static Option<T> Get(Child child, int index)
    {
        return child switch
        {
            Node<T>(_, var l, var d, _) when index == ChildCount(l) => d,
            Node<T>(_, var l, _, _) when index < ChildCount(l) => Get(l, index),
            Node<T>(_, var l, _, var r) => Get(r, index - ChildCount(l) - 1),
            _ => Option<T>.None
        };
    }

    private static T RawGet(Child child, int index)
    {
        return child switch
        {
            Node<T>(_, var l, var d, _) when index == ChildCount(l) => d,
            Node<T>(_, var l, _, _) when index < ChildCount(l) => RawGet(l, index),
            Node<T>(_, var l, _, var r) => RawGet(r, index - ChildCount(l) - 1),
            _ => throw new IndexOutOfRangeException()
        };
    }
}