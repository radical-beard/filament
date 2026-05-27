namespace Filament;

using System;

/// <summary>
/// Marks a record/struct as marshalled across the C#↔Lua boundary. The source
/// generator emits bidirectional <c>ToLua</c>/<c>FromLua</c> converters that
/// produce plain Lua tables with snake_case keys.
///
/// The type must be <c>partial</c> and constructible by the generator (a record
/// or a type with a single accessible positional/primary constructor).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ScriptableAttribute : Attribute
{
    public ScriptableAttribute(string? luaTypeName = null) => LuaTypeName = luaTypeName;

    /// <summary>Optional override for the type's logical name (diagnostics / future
    /// registries). Defaults to the C# type name.</summary>
    public string? LuaTypeName { get; }
}

/// <summary>
/// Overrides the Lua table key for a member, or excludes it from marshalling.
/// By default the key is the member name converted to snake_case.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ScriptMemberAttribute : Attribute
{
    public ScriptMemberAttribute(string? key = null) => Key = key;

    /// <summary>Explicit Lua key (verbatim, no snake_case conversion).</summary>
    public string? Key { get; }

    /// <summary>When true, the member is skipped by both ToLua and FromLua.</summary>
    public bool Ignore { get; set; }
}
