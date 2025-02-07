namespace Editor.Models.RBList;

public class Result<T> where T : class
{
    private readonly bool _isDone;
    private readonly T _value;

    private Result(T value, bool done)
    {
        _value = value;
        _isDone = done;
    }

    public static Result<T> Done(T obj)
    {
        return new Result<T>(obj, true);
    }

    public static Result<T> ToDo(T obj)
    {
        return new Result<T>(obj, false);
    }

    public Result<T> Bind(Func<T, Result<T>> bind)
    {
        return _isDone switch
        {
            true => Done(_value),
            false => bind(_value)
        };
    }

    public Result<T> Map(Func<T, T> map)
    {
        return _isDone switch
        {
            true => Done(map(_value)),
            false => ToDo(map(_value))
        };
    }

    public T FromResult()
    {
        return _value;
    }
}