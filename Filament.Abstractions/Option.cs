namespace Filament;

using System;

/// <summary>
/// Rust-style option: <c>Some(value)</c> or <c>None</c>. This is how the
/// scripting boundary represents legitimate absence (e.g. an optional Lua
/// table key) — never <c>null</c>.
///
/// Construct with <c>Option.Some(x)</c> or the bare <c>Option.None</c>, which
/// target-converts to any <c>Option&lt;T&gt;</c>.
/// </summary>
public readonly struct Option<T>
{
    private readonly bool _isSome;
    private readonly T _value;

    private Option(T value)
    {
        _isSome = true;
        _value = value;
    }

    public static Option<T> Some(T value) => new(value);
    public static Option<T> None => default;

    public bool IsSome => _isSome;
    public bool IsNone => !_isSome;

    public bool TryGet(out T value)
    {
        value = _value;
        return _isSome;
    }

    public T UnwrapOr(T fallback) => _isSome ? _value : fallback;

    public Option<U> Map<U>(Func<T, U> map)
        => _isSome ? Option<U>.Some(map(_value)) : Option<U>.None;

    public static implicit operator Option<T>(NoneSentinel _) => default;
}

/// <summary>Type-inferred <c>None</c> sentinel; converts to any <c>Option&lt;T&gt;</c>.</summary>
public readonly struct NoneSentinel { }

/// <summary>Factory helpers so call sites read <c>Option.Some(x)</c> / <c>Option.None</c>.</summary>
public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);
    public static NoneSentinel None => default;
}
