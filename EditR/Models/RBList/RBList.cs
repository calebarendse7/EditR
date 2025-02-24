using System.Collections;

namespace EditR.Models.RBList;

public class RbList<T> : IEnumerable<T>
{
    private Child<T> _root = Leaf<T>.Empty;
    public int Count => _root is Node<T> n ? n.SubtreeCount : 0;

    public T this[int i] => Get(_root, i);

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Insert(int index, T key)
    {
        _root = InsertAux(_root, index, key).Map(Blacken).FromResult();
    }

    public void RemoveAt(int index)
    {
        _root = DeleteAux(_root, index).FromResult();
    }

    private static Result<Child<T>> Balance(Child<T> node)
    {
        return node switch
        {
            Node<T>(Colors.Black, Node<T>(Colors.Red, Node<T>(Colors.Red, var a, var x, var b), var y, var c), var z,
                var d) n => Result<Child<T>>.ToDo(new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, Node<T>(Colors.Red, var a, var x, Node<T>(Colors.Red, var b, var y, var c)), var z,
                var d) => Result<Child<T>>.ToDo(new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, var a, var x,
                Node<T>(Colors.Red, Node<T>(Colors.Red, var b, var y, var c), var z, var d)) => Result<Child<T>>.ToDo(
                    new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y, new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, var a, var x,
                Node<T>(Colors.Red, var b, var y, Node<T>(Colors.Red, var c, var z, var d))) => Result<Child<T>>.ToDo(
                    new Node<T>(Colors.Red, new Node<T>(Colors.Black, a, x, b), y, new Node<T>(Colors.Black, c, z, d))),
            Node<T>(Colors.Black, var a, var x, var b) => Result<Child<T>>.Done(new Node<T>(Colors.Black, a, x, b)),
            Node<T>(Colors.Red, var a, var x, var b) => Result<Child<T>>.ToDo(new Node<T>(Colors.Red, a, x, b)),
            _ => throw new Exception("Error occured while balancing")
        };
    }

    private static Result<Child<T>> DeleteAux(Child<T> child, int index)
    {
        return child switch
        {
            Leaf<T> l => Result<Child<T>>.Done(l),
            Node<T>(_, var l, _, _) n when index == ChildCount(l) => DeleteCurrent(n),
            Node<T>(var k, var l, var x, var r) when index < ChildCount(l) => DeleteAux(l, index)
                .Map(rst => new Node<T>(k, rst, x, r)).Bind(EqLeft),
            Node<T>(var k, var l, var x, var r) => DeleteAux(r, index - ChildCount(l) - 1)
                .Map(rst => new Node<T>(k, l, x, rst)).Bind(EqRight),
            _ => throw new Exception("DeleteAux error")
        };
    }

    private static Result<Child<T>> DeleteCurrent(Child<T> child)
    {
        return child switch
        {
            Node<T>(Colors.Red, var a, _, Leaf<T>) => Result<Child<T>>.Done(a),
            Node<T>(Colors.Black, var a, _, Leaf<T>) => BlackenDelete(a),
            Node<T>(var k, var a, var y, var b) => DeleteMin(b)
                .Map((node, min) => node.Map(n => new Node<T>(k, a, min, n))).Bind(EqRight),
            _ => throw new Exception("DeleteCurrent error")
        };
    }

    private static (Result<Child<T>> node, T min) DeleteMin(Child<T> child)
    {
        return child switch
        {
            Node<T>(Colors.Red, Leaf<T>, var y, var b) => (Result<Child<T>>.Done(b), y),
            Node<T>(Colors.Black, Leaf<T>, var y, var b) => (BlackenDelete(b), y),
            Node<T>(var k, var a, var y, var b) => DeleteMin(a)
                .Map((node, min) => (node.Map(n => new Node<T>(k, n, y, b)).Bind(EqLeft), min)),
            _ => throw new Exception("DeleteMin error")
        };
    }

    private static Result<Child<T>> BalanceDelete(Child<T> node)
    {
        return node switch
        {
            Node<T>(var k, Node<T>(Colors.Red, Node<T>(Colors.Red, var a, var x, var b), var y, var c), var z, var d) n
                => Result<Child<T>>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(var k, Node<T>(Colors.Red, var a, var x, Node<T>(Colors.Red, var b, var y, var c)), var z, var d) =>
                Result<Child<T>>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(var k, var a, var x, Node<T>(Colors.Red, Node<T>(Colors.Red, var b, var y, var c), var z, var d)) =>
                Result<Child<T>>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            Node<T>(var k, var a, var x, Node<T>(Colors.Red, var b, var y, Node<T>(Colors.Red, var c, var z, var d))) =>
                Result<Child<T>>.Done(new Node<T>(k, new Node<T>(Colors.Black, a, x, b), y,
                    new Node<T>(Colors.Black, c, z, d))),
            _ => BlackenDelete(node)
        };
    }

    private static Result<Child<T>> BlackenDelete(Child<T> node)
    {
        return node switch
        {
            Node<T>(Colors.Red, var l, var x, var r) => Result<Child<T>>.Done(new Node<T>(Colors.Black, l, x, r)),
            _ => Result<Child<T>>.ToDo(node)
        };
    }

    private static Result<Child<T>> EqLeft(Child<T> node)
    {
        return node switch
        {
            Node<T>(var k, var l, var x, Node<T>(Colors.Black, var c, var y, var d)) => BalanceDelete(new Node<T>(k, l,
                x, new Node<T>(Colors.Red, c, y, d))),
            Node<T>(var k, var a, var y, Node<T>(Colors.Red, var c, var z, var d)) => EqLeft(new Node<T>(Colors.Red, a,
                y, c)).Map(n => new Node<T>(Colors.Black, n, z, d)),
            _ => throw new Exception("EQLeft error")
        };
    }

    private static Result<Child<T>> EqRight(Child<T> node)
    {
        return node switch
        {
            Node<T>(var k, Node<T>(Colors.Black, var a, var x, var b), var y, var c) => BalanceDelete(new Node<T>(k,
                new Node<T>(Colors.Red, a, x, b), y, c)),
            Node<T>(var k, Node<T>(Colors.Red, var a, var x, var b), var y, var c) => EqRight(new Node<T>(Colors.Red, b,
                y, c)).Map(rst => new Node<T>(Colors.Black, a, x, rst)),
            _ => throw new Exception("EQRight error")
        };
    }

    private static Result<Child<T>> InsertAux(Child<T> child, int index, T key)
    {
        return child switch
        {
            Leaf<T> => Result<Child<T>>.ToDo(new Node<T>(Colors.Red, Leaf<T>.Empty, key, Leaf<T>.Empty)),
            Node<T>(var k, var l, var d, var r) when index <= ChildCount(l) => InsertAux(l, index, key)
                .Map(lt => new Node<T>(k, lt, d, r)).Bind(Balance),
            Node<T>(var k, var l, var d, var r) => InsertAux(r, index - ChildCount(l) - 1, key)
                .Map(rt => new Node<T>(k, l, d, rt)).Bind(Balance),
            _ => throw new Exception("Error occured while inserting")
        };
    }

    private static int ChildCount(Child<T> child)
    {
        return child switch
        {
            Node<T> n => n.SubtreeCount,
            _ => 0
        };
    }

    private static Child<T> Blacken(Child<T> child)
    {
        return child switch
        {
            Node<T> n => n with { Color = Colors.Black },
            _ => child
        };
    }

    private static T Get(Child<T> child, int index)
    {
        return child switch
        {
            Node<T>(_, var l, var d, _) when index == ChildCount(l) => d,
            Node<T>(_, var l, _, _) when index < ChildCount(l) => Get(l, index),
            Node<T>(_, var l, _, var r) => Get(r, index - ChildCount(l) - 1),
            _ => throw new IndexOutOfRangeException($"Index {index} out of range of RBList")
        };
    }
}