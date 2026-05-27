namespace Filament.Config;

/// <summary>A TOML parse or validation failure, carried as a value.</summary>
public readonly record struct ConfigError(string Message, string? EntityId = null)
{
    public override string ToString()
        => EntityId is null ? Message : $"{Message} [entity: {EntityId}]";
}
