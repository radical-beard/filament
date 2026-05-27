namespace Filament;

using global::Godot;
using MoonSharp.Interpreter;
using Script = MoonSharp.Interpreter.Script; // disambiguate from Godot.Script

/// <summary>
/// Marshalling for Godot value types (<see cref="Vector2"/>/<see cref="Vector3"/>/
/// <see cref="Color"/>) ↔ Lua tables. Lives in Filament.Godot (not Core, which is
/// engine-agnostic); the <c>[Scriptable]</c> generator emits calls here when a
/// member is one of these types. Lua sees plain tables: <c>{x=,y=,z=}</c> / <c>{r=,g=,b=,a=}</c>.
/// </summary>
public static class GodotMarshal
{
    public static Table ToLua(Vector2 v, Script s)
    {
        var t = new Table(s);
        t.Set("x", DynValue.NewNumber(v.X));
        t.Set("y", DynValue.NewNumber(v.Y));
        return t;
    }

    public static Table ToLua(Vector3 v, Script s)
    {
        var t = new Table(s);
        t.Set("x", DynValue.NewNumber(v.X));
        t.Set("y", DynValue.NewNumber(v.Y));
        t.Set("z", DynValue.NewNumber(v.Z));
        return t;
    }

    public static Table ToLua(Color c, Script s)
    {
        var t = new Table(s);
        t.Set("r", DynValue.NewNumber(c.R));
        t.Set("g", DynValue.NewNumber(c.G));
        t.Set("b", DynValue.NewNumber(c.B));
        t.Set("a", DynValue.NewNumber(c.A));
        return t;
    }

    public static Result<Vector2, LuaError> AsVector2(DynValue v, string type, string field)
    {
        if (v.Type != DataType.Table) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        var t = v.Table;
        if (!Comp(t, "x", out var x) || !Comp(t, "y", out var y))
            return Result.Err(LuaError.FieldCoercion(type, field, "expected { x, y } numbers"));
        return Result.Ok(new Vector2(x, y));
    }

    public static Result<Vector3, LuaError> AsVector3(DynValue v, string type, string field)
    {
        if (v.Type != DataType.Table) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        var t = v.Table;
        if (!Comp(t, "x", out var x) || !Comp(t, "y", out var y) || !Comp(t, "z", out var z))
            return Result.Err(LuaError.FieldCoercion(type, field, "expected { x, y, z } numbers"));
        return Result.Ok(new Vector3(x, y, z));
    }

    public static Result<Color, LuaError> AsColor(DynValue v, string type, string field)
    {
        if (v.Type != DataType.Table) return Result.Err(LuaError.FieldCoercion(type, field, v.Type.ToString()));
        var t = v.Table;
        if (!Comp(t, "r", out var r) || !Comp(t, "g", out var g) || !Comp(t, "b", out var b))
            return Result.Err(LuaError.FieldCoercion(type, field, "expected { r, g, b[, a] } numbers"));
        var aDv = t.Get("a");
        var a = aDv.Type == DataType.Number ? (float)aDv.Number : 1f; // alpha defaults to opaque
        return Result.Ok(new Color(r, g, b, a));
    }

    private static bool Comp(Table t, string key, out float value)
    {
        var dv = t.Get(key);
        if (dv.Type == DataType.Number) { value = (float)dv.Number; return true; }
        value = 0f;
        return false;
    }
}
