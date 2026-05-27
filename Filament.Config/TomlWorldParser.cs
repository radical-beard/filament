namespace Filament.Config;

using System;
using System.Collections.Generic;
using System.Linq;
using Filament;
using Tomlyn;
using Tomlyn.Model;

/// <summary>
/// Parses a <c>*.world.toml</c> into a <see cref="WorldConfig"/>. Tomlyn model
/// types are normalized into plain CLR nesting (Dictionary / List / scalars) so
/// the rest of the framework never depends on Tomlyn. Parse and validation
/// failures come back as <see cref="ConfigError"/> values — no throws.
/// </summary>
public static class TomlWorldParser
{
    public static Result<WorldConfig, ConfigError> Parse(string source, string path = "<toml>")
    {
        Tomlyn.Syntax.DocumentSyntax doc;
        try
        {
            doc = Toml.Parse(source, path);
        }
        catch (Exception ex)
        {
            return Result.Err(new ConfigError($"toml parse threw: {ex.Message}"));
        }

        if (doc.HasErrors)
        {
            var msg = string.Join("; ", doc.Diagnostics.Select(d => d.Message));
            return Result.Err(new ConfigError($"toml parse error: {msg}"));
        }

        TomlTable root;
        try
        {
            root = doc.ToModel();
        }
        catch (Exception ex)
        {
            return Result.Err(new ConfigError($"toml model error: {ex.Message}"));
        }

        if (!root.TryGetValue("schema_version", out var sv) || sv is not long schemaVersion)
            return Result.Err(new ConfigError("missing or non-integer 'schema_version'"));

        var description = root.TryGetValue("description", out var d) && d is string ds ? ds : null;

        var entities = new Dictionary<string, EntityConfig>();
        if (root.TryGetValue("entities", out var entObj) && entObj is TomlTable entTable)
        {
            foreach (var kv in entTable)
            {
                var id = kv.Key;
                if (kv.Value is not TomlTable et)
                    return Result.Err(new ConfigError("entity is not a table", id));
                if (!et.TryGetValue("type", out var tObj) || tObj is not string type || type.Length == 0)
                    return Result.Err(new ConfigError("entity is missing a 'type'", id));

                var parent = et.TryGetValue("parent", out var p) && p is string ps ? ps : null;

                var props = new Dictionary<string, object?>();
                foreach (var pk in et)
                {
                    if (pk.Key is "type" or "parent") continue;
                    props[pk.Key] = Normalize(pk.Value);
                }
                entities[id] = new EntityConfig(id, type, parent, props);
            }
        }

        return Result.Ok(new WorldConfig((int)schemaVersion, description, entities));
    }

    private static object? Normalize(object? v) => v switch
    {
        TomlTable t => NormalizeTable(t),
        TomlTableArray ta => ta.Select(x => (object?)NormalizeTable(x)).ToList(),
        TomlArray a => a.Select(Normalize).ToList(),
        _ => v,
    };

    private static Dictionary<string, object?> NormalizeTable(TomlTable t)
    {
        var d = new Dictionary<string, object?>();
        foreach (var kv in t) d[kv.Key] = Normalize(kv.Value);
        return d;
    }
}
