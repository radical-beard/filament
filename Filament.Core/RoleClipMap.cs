namespace Filament;

using System.Collections.Generic;

/// <summary>
/// Maps a semantic animation <em>role</em> (e.g. "idle", "dodge", "hitstun") to a
/// concrete clip name. This is the data half of the <c>play_role</c> primitive:
/// state machines emit roles, this resolves the clip, and the host plays it as a
/// purely cosmetic layer — so a missing/renamed clip is a cosmetic gap (a
/// <c>None</c>), never a soft-lock. Keys are matched verbatim (author them
/// snake_case to match Lua/TOML conventions).
/// </summary>
public sealed class RoleClipMap
{
    private readonly Dictionary<string, string> _map;

    public RoleClipMap(IReadOnlyDictionary<string, string> roleToClip)
        => _map = new Dictionary<string, string>(roleToClip);

    public static RoleClipMap Empty { get; } = new(new Dictionary<string, string>());

    /// <summary>The clip bound to a role, or <c>None</c> — never null, never throws.</summary>
    public Option<string> Resolve(string role)
        => _map.TryGetValue(role, out var clip) ? Option.Some(clip) : Option.None;

    public bool Has(string role) => _map.ContainsKey(role);

    public int Count => _map.Count;
}
