namespace Filament.Config;

using System.Collections.Generic;

/// <summary>
/// A parsed <c>*.world.toml</c>: schema version, optional description, and the
/// entities keyed by id. Engine-free — the Godot factory layer interprets these.
/// </summary>
public sealed class WorldConfig
{
    public int SchemaVersion { get; }
    public string? Description { get; }
    public IReadOnlyDictionary<string, EntityConfig> Entities { get; }

    public WorldConfig(int schemaVersion, string? description, IReadOnlyDictionary<string, EntityConfig> entities)
    {
        SchemaVersion = schemaVersion;
        Description = description;
        Entities = entities;
    }
}

/// <summary>
/// One entity: its id, factory <see cref="Type"/>, optional <see cref="Parent"/>,
/// and a normalized property bag (nested tables → dictionaries, arrays → lists,
/// scalars as bool/long/double/string). Transform keys (position/rotation/...) and
/// any <c>"lua:"</c> behavior refs live in <see cref="Props"/>.
/// </summary>
public sealed class EntityConfig
{
    public string Id { get; }
    public string Type { get; }
    public string? Parent { get; }
    public IReadOnlyDictionary<string, object?> Props { get; }

    public EntityConfig(string id, string type, string? parent, IReadOnlyDictionary<string, object?> props)
    {
        Id = id;
        Type = type;
        Parent = parent;
        Props = props;
    }
}
