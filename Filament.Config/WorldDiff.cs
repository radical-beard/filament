namespace Filament.Config;

using System.Collections.Generic;

/// <summary>The delta between two <see cref="WorldConfig"/>s. Pure data — the Godot
/// layer decides patch-vs-respawn per changed entity using its field metadata.</summary>
public sealed class WorldDiff
{
    public IReadOnlyList<EntityConfig> Added { get; }
    public IReadOnlyList<string> Removed { get; }
    public IReadOnlyList<EntityChange> Changed { get; }

    public WorldDiff(IReadOnlyList<EntityConfig> added, IReadOnlyList<string> removed, IReadOnlyList<EntityChange> changed)
    {
        Added = added;
        Removed = removed;
        Changed = changed;
    }

    public bool IsEmpty => Added.Count == 0 && Removed.Count == 0 && Changed.Count == 0;
}

/// <summary>An entity present in both configs but differing. <see cref="TypeChanged"/>
/// (type or parent) forces a respawn; otherwise <see cref="ChangedKeys"/> drives a patch.</summary>
public sealed class EntityChange
{
    public string Id { get; }
    public bool TypeChanged { get; }
    public IReadOnlyList<string> ChangedKeys { get; }
    public EntityConfig Old { get; }
    public EntityConfig New { get; }

    public EntityChange(string id, bool typeChanged, IReadOnlyList<string> changedKeys, EntityConfig old, EntityConfig @new)
    {
        Id = id;
        TypeChanged = typeChanged;
        ChangedKeys = changedKeys;
        Old = old;
        New = @new;
    }
}

public static class WorldDiffComputer
{
    public static WorldDiff Compute(WorldConfig old, WorldConfig @new)
    {
        var added = new List<EntityConfig>();
        var removed = new List<string>();
        var changed = new List<EntityChange>();

        foreach (var kv in @new.Entities)
            if (!old.Entities.ContainsKey(kv.Key)) added.Add(kv.Value);

        foreach (var kv in old.Entities)
            if (!@new.Entities.ContainsKey(kv.Key)) removed.Add(kv.Key);

        foreach (var kv in @new.Entities)
        {
            if (!old.Entities.TryGetValue(kv.Key, out var o)) continue;
            var n = kv.Value;
            var typeChanged = o.Type != n.Type || o.Parent != n.Parent;
            var changedKeys = DiffKeys(o.Props, n.Props);
            if (typeChanged || changedKeys.Count > 0)
                changed.Add(new EntityChange(kv.Key, typeChanged, changedKeys, o, n));
        }

        return new WorldDiff(added, removed, changed);
    }

    private static List<string> DiffKeys(IReadOnlyDictionary<string, object?> a, IReadOnlyDictionary<string, object?> b)
    {
        var keys = new HashSet<string>(a.Keys);
        keys.UnionWith(b.Keys);
        var result = new List<string>();
        foreach (var k in keys)
        {
            var aHas = a.TryGetValue(k, out var av);
            var bHas = b.TryGetValue(k, out var bv);
            if (aHas != bHas || !ConfigValue.DeepEqual(av, bv)) result.Add(k);
        }
        return result;
    }
}

/// <summary>Recursive structural equality over normalized config values
/// (scalars, lists, tables). Bridges TOML's long/double so 3 == 3.0.</summary>
public static class ConfigValue
{
    public static bool DeepEqual(object? a, object? b)
    {
        if (a is null || b is null) return a is null && b is null;
        if (IsNumber(a) && IsNumber(b)) return ToDouble(a) == ToDouble(b);
        if (a is string sa && b is string sb) return sa == sb;
        if (a is bool ba && b is bool bb) return ba == bb;

        if (a is IReadOnlyList<object?> la && b is IReadOnlyList<object?> lb)
        {
            if (la.Count != lb.Count) return false;
            for (int i = 0; i < la.Count; i++)
                if (!DeepEqual(la[i], lb[i])) return false;
            return true;
        }

        if (a is IReadOnlyDictionary<string, object?> da && b is IReadOnlyDictionary<string, object?> db)
        {
            if (da.Count != db.Count) return false;
            foreach (var kv in da)
            {
                if (!db.TryGetValue(kv.Key, out var bv)) return false;
                if (!DeepEqual(kv.Value, bv)) return false;
            }
            return true;
        }

        return a.Equals(b);
    }

    private static bool IsNumber(object o) => o is long or double or int or float;
    private static double ToDouble(object o) => o switch
    {
        long l => l,
        int i => i,
        float f => f,
        double d => d,
        _ => double.NaN,
    };
}
