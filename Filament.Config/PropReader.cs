namespace Filament.Config;

using System.Collections.Generic;
using Filament;

/// <summary>
/// Typed, never-throwing accessor over an entity's normalized property bag.
/// Numeric getters bridge TOML's long/double; vector/color getters read numeric
/// arrays into plain tuples (the Godot factory layer wraps them in engine types).
/// </summary>
public sealed class PropReader
{
    private readonly IReadOnlyDictionary<string, object?> _props;

    public PropReader(IReadOnlyDictionary<string, object?> props) => _props = props;

    public bool Has(string key) => _props.ContainsKey(key);

    public string? GetString(string key, string? fallback = null)
        => _props.TryGetValue(key, out var v) && v is string s ? s : fallback;

    public bool GetBool(string key, bool fallback = false)
        => _props.TryGetValue(key, out var v) && v is bool b ? b : fallback;

    public long GetLong(string key, long fallback = 0)
        => _props.TryGetValue(key, out var v) && v is long l ? l : fallback;

    public int GetInt(string key, int fallback = 0) => (int)GetLong(key, fallback);

    public double GetDouble(string key, double fallback = 0)
        => _props.TryGetValue(key, out var v) ? ToDouble(v, fallback) : fallback;

    public float GetFloat(string key, float fallback = 0) => (float)GetDouble(key, fallback);

    /// <summary>Read a <c>[x, y, z]</c> numeric array; missing components use 0.</summary>
    public (float X, float Y, float Z) GetVec3(string key, (float, float, float) fallback = default)
    {
        if (_props.TryGetValue(key, out var v) && v is IReadOnlyList<object?> l && l.Count >= 3)
            return (ToFloat(l[0]), ToFloat(l[1]), ToFloat(l[2]));
        return fallback;
    }

    /// <summary>Read a <c>[r, g, b]</c> or <c>[r, g, b, a]</c> array; alpha defaults to 1.</summary>
    public (float R, float G, float B, float A) GetColor(string key, (float, float, float, float) fallback = default)
    {
        if (_props.TryGetValue(key, out var v) && v is IReadOnlyList<object?> l && l.Count >= 3)
            return (ToFloat(l[0]), ToFloat(l[1]), ToFloat(l[2]), l.Count >= 4 ? ToFloat(l[3]) : 1f);
        return fallback;
    }

    public IReadOnlyDictionary<string, object?>? GetTable(string key)
        => _props.TryGetValue(key, out var v) && v is IReadOnlyDictionary<string, object?> d ? d : null;

    public IReadOnlyList<object?>? GetArray(string key)
        => _props.TryGetValue(key, out var v) && v is IReadOnlyList<object?> l ? l : null;

    /// <summary>When the value is a <c>"lua:&lt;script&gt;"</c> behavior ref, bind its logical path.</summary>
    public bool TryGetLuaRef(string key, out string scriptPath)
        => LuaRef.TryParse(GetString(key), out scriptPath);

    private static double ToDouble(object? v, double fallback) => v switch
    {
        long l => l,
        double d => d,
        _ => fallback,
    };

    private static float ToFloat(object? v) => v switch
    {
        long l => l,
        double d => (float)d,
        _ => 0f,
    };
}
