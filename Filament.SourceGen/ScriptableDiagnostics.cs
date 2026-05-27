namespace Filament.SourceGen;

using Microsoft.CodeAnalysis;

/// <summary>The <c>FILA####</c> diagnostic catalogue for <c>[Scriptable]</c>.</summary>
internal static class ScriptableDiagnostics
{
    private const string Category = "Filament";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        "FILA0001",
        "[Scriptable] type must be partial",
        "'{0}' is [Scriptable] but not declared 'partial'; the generator emits its converter as a sibling",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedMemberType = new(
        "FILA0002",
        "[Scriptable] member type is not marshallable",
        "member '{0}' on '{1}' has type '{2}' which can't be marshalled (allowed: bool/int/long/float/double/string/enum, a [Scriptable] type, Option<T> of those, or a list of those)",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoUsableConstructor = new(
        "FILA0003",
        "[Scriptable] type has no constructor FromLua can call",
        "'{0}' is [Scriptable] but has no accessible constructor with parameters; use a record or a positional/primary constructor so FromLua can build it",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateName = new(
        "FILA0004",
        "duplicate [Scriptable] name",
        "the [Scriptable] name '{0}' is used by more than one type; names must be unique",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor UseOptionNotNullable = new(
        "FILA0005",
        "use Option<T> instead of a nullable reference for [Scriptable] members",
        "member '{0}' on '{1}' is a nullable reference; model optional values as Option<T> so absence never becomes null",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
