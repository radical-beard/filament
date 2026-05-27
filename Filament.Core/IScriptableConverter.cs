namespace Filament;

using MoonSharp.Interpreter;

/// <summary>
/// Bidirectional marshaller between a C# <c>[Scriptable]</c> type and a plain
/// Lua table. Implementations are emitted by the source generator and
/// self-registered into <see cref="ScriptableMarshal"/> at assembly load.
/// </summary>
public interface IScriptableConverter<T>
{
    /// <summary>Build a fresh Lua table (snake_case keys) from a C# value.</summary>
    Table ToLua(T value, Script script);

    /// <summary>Read a C# value from a Lua value, or an <c>Err</c> on any mismatch.</summary>
    Result<T, LuaError> FromLua(DynValue value);
}
