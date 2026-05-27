namespace Filament;

using System;

/// <summary>
/// Rust-style result: either an <c>Ok</c> value of <typeparamref name="T"/> or
/// an <c>Err</c> value of <typeparamref name="E"/>. The whole point of the
/// scripting boundary is that failures travel as values, not exceptions — so
/// callers must handle both arms.
///
/// Construct with the <see cref="Result"/> helpers: <c>return Result.Ok(x);</c>
/// or <c>return Result.Err(e);</c> — both target-convert to the right
/// <c>Result&lt;T,E&gt;</c>.
/// </summary>
public readonly struct Result<T, E>
{
    private readonly bool _isOk;
    private readonly T _value;
    private readonly E _error;

    internal Result(bool isOk, T value, E error)
    {
        _isOk = isOk;
        _value = value;
        _error = error;
    }

    public bool IsOk => _isOk;
    public bool IsErr => !_isOk;

    /// <summary>True + binds the value when this is Ok.</summary>
    public bool TryGet(out T value)
    {
        value = _value;
        return _isOk;
    }

    /// <summary>True + binds the error when this is Err.</summary>
    public bool TryGetError(out E error)
    {
        error = _error;
        return !_isOk;
    }

    /// <summary>Programmer-error guard only — never call on the boundary path.</summary>
    public T Unwrap() => _isOk
        ? _value
        : throw new InvalidOperationException($"Unwrap() on an Err result: {_error}");

    public E UnwrapErr() => !_isOk
        ? _error
        : throw new InvalidOperationException("UnwrapErr() on an Ok result");

    public Result<U, E> Map<U>(Func<T, U> map)
        => _isOk ? new Result<U, E>(true, map(_value), default!) : new Result<U, E>(false, default!, _error);

    public Result<T, F> MapErr<F>(Func<E, F> map)
        => _isOk ? new Result<T, F>(true, _value, default!) : new Result<T, F>(false, default!, map(_error));

    public Result<U, E> AndThen<U>(Func<T, Result<U, E>> bind)
        => _isOk ? bind(_value) : new Result<U, E>(false, default!, _error);

    public R Match<R>(Func<T, R> ok, Func<E, R> err)
        => _isOk ? ok(_value) : err(_error);

    public void Match(Action<T> ok, Action<E> err)
    {
        if (_isOk) ok(_value); else err(_error);
    }

    public static implicit operator Result<T, E>(ResultOk<T> ok) => new(true, ok.Value, default!);
    public static implicit operator Result<T, E>(ResultErr<E> err) => new(false, default!, err.Error);
}

/// <summary>Intermediate from <see cref="Result.Ok{T}"/>; converts to any <c>Result&lt;T,E&gt;</c>.</summary>
public readonly struct ResultOk<T>
{
    public readonly T Value;
    public ResultOk(T value) => Value = value;
}

/// <summary>Intermediate from <see cref="Result.Err{E}"/>; converts to any <c>Result&lt;T,E&gt;</c>.</summary>
public readonly struct ResultErr<E>
{
    public readonly E Error;
    public ResultErr(E error) => Error = error;
}

/// <summary>Factory helpers so call sites read <c>Result.Ok(x)</c> / <c>Result.Err(e)</c>.</summary>
public static class Result
{
    public static ResultOk<T> Ok<T>(T value) => new(value);
    public static ResultErr<E> Err<E>(E error) => new(error);
}
