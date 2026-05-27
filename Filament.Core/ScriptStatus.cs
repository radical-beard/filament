namespace Filament;

public enum ScriptStatusKind
{
    /// <summary>Loaded for the first time.</summary>
    Loaded,
    /// <summary>Reloaded after an edit; the live chunk was swapped.</summary>
    Reloaded,
    /// <summary>An edit failed to parse/run; the previous chunk stays live.</summary>
    Failed,
    /// <summary>The script file was deleted; its module was dropped.</summary>
    Removed,
}

/// <summary>A registry status event for one script path (replaces engine logging).</summary>
public readonly record struct ScriptStatus(ScriptStatusKind Kind, string Path, string? Message = null);
