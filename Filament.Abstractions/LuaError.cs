namespace Filament;

/// <summary>The category of a scripting-boundary failure.</summary>
public enum LuaErrorKind
{
    /// <summary>No loaded script/module for the requested logical path.</summary>
    ScriptNotLoaded,
    /// <summary>The module exists but doesn't export the requested method.</summary>
    MethodMissing,
    /// <summary>The Lua chunk raised an error while running.</summary>
    RuntimeError,
    /// <summary>A returned primitive value couldn't coerce to the C# return type.</summary>
    ReturnCoercion,
    /// <summary>A params record couldn't be marshalled into a Lua table.</summary>
    ParamMarshal,
    /// <summary>A table field couldn't coerce to the expected C# member type.</summary>
    FieldCoercion,
    /// <summary>A required (non-Option) member was missing or nil in the table.</summary>
    MissingRequiredField,
    /// <summary>A value wasn't the expected shape (e.g. expected a table, got a number).</summary>
    TypeMismatch,
    /// <summary>The registry hasn't been initialized / pumped yet.</summary>
    RegistryNotReady,
}

/// <summary>
/// A boundary failure carried as a value (never thrown across the boundary).
/// <see cref="Member"/> names the field/method involved when relevant.
/// </summary>
public readonly record struct LuaError(
    LuaErrorKind Kind,
    string Message,
    string? ScriptPath = null,
    string? Member = null)
{
    public override string ToString()
    {
        var where = (ScriptPath, Member) switch
        {
            (not null, not null) => $" [{ScriptPath}::{Member}]",
            (not null, null) => $" [{ScriptPath}]",
            (null, not null) => $" [{Member}]",
            _ => string.Empty,
        };
        return $"{Kind}: {Message}{where}";
    }

    // Builders. The "actual" descriptor is a string so Abstractions stays free
    // of any MoonSharp dependency — callers pass e.g. dynValue.Type.ToString().

    public static LuaError ScriptNotLoaded(string script)
        => new(LuaErrorKind.ScriptNotLoaded, $"no loaded script '{script}'", script);

    public static LuaError MethodMissing(string script, string method)
        => new(LuaErrorKind.MethodMissing, $"module does not export method '{method}'", script, method);

    public static LuaError RuntimeError(string script, string method, string message)
        => new(LuaErrorKind.RuntimeError, message, script, method);

    public static LuaError ReturnCoercion(string script, string method, string detail)
        => new(LuaErrorKind.ReturnCoercion, $"return value could not be coerced: {detail}", script, method);

    public static LuaError ParamMarshal(string detail)
        => new(LuaErrorKind.ParamMarshal, detail);

    public static LuaError FieldCoercion(string type, string field, string actual)
        => new(LuaErrorKind.FieldCoercion, $"field of '{type}' could not be coerced (got {actual})", type, field);

    public static LuaError MissingRequiredField(string type, string field)
        => new(LuaErrorKind.MissingRequiredField, $"required field missing on '{type}'", type, field);

    public static LuaError TypeMismatch(string type, string actual)
        => new(LuaErrorKind.TypeMismatch, $"expected a table for '{type}', got {actual}", type);

    public static LuaError RegistryNotReady(string detail)
        => new(LuaErrorKind.RegistryNotReady, detail);
}
