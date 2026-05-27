namespace Filament;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MoonSharp.Interpreter;

/// <summary>
/// Runtime support for generated <c>[Scriptable]</c> converters: a process-wide
/// registry (populated by each assembly's module initializer) plus the scalar
/// readers the generated <c>FromLua</c> code calls. Every reader returns a
/// <see cref="Result{T, LuaError}"/> — coercion failures are values, not throws.
/// </summary>
public static class ScriptableMarshal
{
    private static readonly ConcurrentDictionary<Type, object> _converters = new();

    /// <summary>The largest integer a double represents exactly (2^53).</summary>
    private const double SafeIntegerMagnitude = 9007199254740992d;

    public static void Register<T>(IScriptableConverter<T> converter) => _converters[typeof(T)] = converter;

    public static bool TryGet<T>(out IScriptableConverter<T> converter)
    {
        if (_converters.TryGetValue(typeof(T), out var o))
        {
            converter = (IScriptableConverter<T>)o;
            return true;
        }
        converter = null!;
        return false;
    }

    /// <summary>Programmer-error guard: a [Scriptable] type's converter is
    /// registered by its assembly's module initializer, so a miss means the
    /// type isn't [Scriptable] or its assembly never loaded.</summary>
    public static IScriptableConverter<T> Get<T>()
        => TryGet<T>(out var c)
            ? c
            : throw new InvalidOperationException(
                $"no [Scriptable] converter registered for '{typeof(T)}'; mark the type [Scriptable].");

    public static Table ToLua<T>(T value, Script script) => Get<T>().ToLua(value, script);

    public static Result<T, LuaError> FromLua<T>(DynValue value) => Get<T>().FromLua(value);

    public static bool IsAbsent(DynValue? v) => v is null || v.IsNil() || v.IsVoid();

    // ─── scalar readers (called by generated FromLua) ────────────────────────

    public static Result<bool, LuaError> AsBool(DynValue v, string type, string field)
        => v.Type == DataType.Boolean
            ? Result.Ok(v.Boolean)
            : Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));

    public static Result<int, LuaError> AsInt(DynValue v, string type, string field)
    {
        if (v.Type != DataType.Number) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        var d = v.Number;
        if (d < int.MinValue || d > int.MaxValue || Math.Floor(d) != d)
            return Result.Err(LuaError.FieldCoercion(type, field, $"number {d} is not a 32-bit integer"));
        return Result.Ok((int)d);
    }

    public static Result<long, LuaError> AsLong(DynValue v, string type, string field)
    {
        if (v.Type != DataType.Number) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        var d = v.Number;
        if (Math.Abs(d) > SafeIntegerMagnitude || Math.Floor(d) != d)
            return Result.Err(LuaError.FieldCoercion(type, field, $"number {d} is not an exact integer"));
        return Result.Ok((long)d);
    }

    public static Result<float, LuaError> AsFloat(DynValue v, string type, string field)
        => v.Type == DataType.Number
            ? Result.Ok((float)v.Number)
            : Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));

    public static Result<double, LuaError> AsDouble(DynValue v, string type, string field)
        => v.Type == DataType.Number
            ? Result.Ok(v.Number)
            : Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));

    public static Result<string, LuaError> AsString(DynValue v, string type, string field)
        => v.Type == DataType.String
            ? Result.Ok(v.String)
            : Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));

    public static Result<TEnum, LuaError> AsEnum<TEnum>(DynValue v, string type, string field)
        where TEnum : struct, Enum
    {
        if (v.Type != DataType.String) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        if (Enum.TryParse<TEnum>(v.String, ignoreCase: true, out var e)) return Result.Ok(e);
        return Result.Err(LuaError.FieldCoercion(type, field, $"'{v.String}' is not a {typeof(TEnum).Name}"));
    }

    public static Result<T, LuaError> AsScriptable<T>(DynValue v, string type, string field)
    {
        if (v.Type != DataType.Table) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        return FromLua<T>(v);
    }

    public static Result<List<T>, LuaError> AsList<T>(
        DynValue v, string type, string field, Func<DynValue, Result<T, LuaError>> readElement)
    {
        if (v.Type != DataType.Table) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        var tbl = v.Table;
        var n = tbl.Length; // Lua '#' — the 1-based array length.
        var list = new List<T>(n);
        for (int i = 1; i <= n; i++)
        {
            var ev = tbl.Get(i);
            if (IsAbsent(ev))
                return Result.Err(LuaError.FieldCoercion(type, field, $"nil hole at index {i}"));
            var r = readElement(ev);
            if (r.TryGetError(out var e)) return Result.Err(e);
            r.TryGet(out var item);
            list.Add(item);
        }
        return Result.Ok(list);
    }
}
