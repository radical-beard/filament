namespace Filament.SourceGen;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

/// <summary>
/// Shared analysis for the <c>[Scriptable]</c> generator + analyzer: recognises
/// the Filament attributes by well-known full name, classifies a member's type
/// into a marshalling shape, and finds the constructor <c>FromLua</c> calls.
/// </summary>
internal static class TypeAnalysis
{
    public const string ScriptableAttr = "Filament.ScriptableAttribute";
    public const string ScriptMemberAttr = "Filament.ScriptMemberAttribute";
    public const string OptionType = "Filament.Option<T>";

    /// <summary>Wrapper around a scalar: a bare value, an <c>Option&lt;T&gt;</c>, or a list.</summary>
    public enum Wrap { None, Option, List }

    /// <summary>The leaf value kind under any wrapper.</summary>
    public enum Scalar { Unsupported, Bool, Int, Long, Float, Double, String, Enum, Scriptable }

    public readonly struct MemberShape
    {
        public readonly Wrap Wrap;
        public readonly Scalar Scalar;
        public readonly ITypeSymbol Inner;
        public MemberShape(Wrap wrap, Scalar scalar, ITypeSymbol inner)
        {
            Wrap = wrap;
            Scalar = scalar;
            Inner = inner;
        }
        public bool IsSupported => Scalar != Scalar.Unsupported;
    }

    public static bool HasAttribute(ISymbol symbol, string fullName)
    {
        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass?.ToDisplayString() == fullName) return true;
        return false;
    }

    public static AttributeData? GetAttribute(ISymbol symbol, string fullName)
    {
        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass?.ToDisplayString() == fullName) return attr;
        return null;
    }

    public static bool IsOption(ITypeSymbol t, out ITypeSymbol inner)
    {
        inner = null!;
        if (t is INamedTypeSymbol n && n.IsGenericType && n.TypeArguments.Length == 1
            && n.OriginalDefinition.ToDisplayString() == OptionType)
        {
            inner = n.TypeArguments[0];
            return true;
        }
        return false;
    }

    public static bool IsList(ITypeSymbol t, out ITypeSymbol inner)
    {
        inner = null!;
        if (t is INamedTypeSymbol n && n.IsGenericType && n.TypeArguments.Length == 1)
        {
            switch (n.ConstructedFrom.ToDisplayString())
            {
                case "System.Collections.Generic.IReadOnlyList<T>":
                case "System.Collections.Generic.IList<T>":
                case "System.Collections.Generic.List<T>":
                case "System.Collections.Generic.IReadOnlyCollection<T>":
                case "System.Collections.Generic.ICollection<T>":
                case "System.Collections.Generic.IEnumerable<T>":
                    inner = n.TypeArguments[0];
                    return true;
            }
        }
        return false;
    }

    public static Scalar ClassifyScalar(ITypeSymbol t)
    {
        if (t.TypeKind == TypeKind.Enum) return Scalar.Enum;
        switch (t.SpecialType)
        {
            case SpecialType.System_Boolean: return Scalar.Bool;
            case SpecialType.System_Int32: return Scalar.Int;
            case SpecialType.System_Int64: return Scalar.Long;
            case SpecialType.System_Single: return Scalar.Float;
            case SpecialType.System_Double: return Scalar.Double;
            case SpecialType.System_String: return Scalar.String;
        }
        return HasAttribute(t, ScriptableAttr) ? Scalar.Scriptable : Scalar.Unsupported;
    }

    public static MemberShape Classify(ITypeSymbol t)
    {
        if (IsOption(t, out var oi)) return new MemberShape(Wrap.Option, ClassifyScalar(oi), oi);
        if (IsList(t, out var li)) return new MemberShape(Wrap.List, ClassifyScalar(li), li);
        return new MemberShape(Wrap.None, ClassifyScalar(t), t);
    }

    /// <summary>The constructor <c>FromLua</c> will call: the accessible instance
    /// ctor with the most parameters (the positional/primary ctor for records).
    /// Null when there's no usable parameterful ctor.</summary>
    public static IMethodSymbol? PrimaryConstructor(INamedTypeSymbol type)
        => type.InstanceConstructors
            .Where(c => c.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
                        && c.Parameters.Length > 0)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

    /// <summary>The property matching a constructor parameter (records pair them by name).</summary>
    public static IPropertySymbol? MatchingProperty(INamedTypeSymbol type, IParameterSymbol param)
        => type.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => string.Equals(p.Name, param.Name, System.StringComparison.OrdinalIgnoreCase));

    /// <summary>Lua table key for a ctor parameter: explicit <c>[ScriptMember("k")]</c>
    /// on the matching property, else snake_case of the parameter name.</summary>
    public static string ResolveKey(INamedTypeSymbol type, IParameterSymbol param)
    {
        var prop = MatchingProperty(type, param);
        if (prop is not null && GetAttribute(prop, ScriptMemberAttr) is { } attr
            && attr.ConstructorArguments.Length > 0
            && attr.ConstructorArguments[0].Value is string key
            && !string.IsNullOrEmpty(key))
        {
            return key;
        }
        return ToSnakeCase(param.Name);
    }

    /// <summary>PascalCase / camelCase → snake_case, acronym-aware (HUDOverlay → hud_overlay).</summary>
    public static string ToSnakeCase(string member)
    {
        if (string.IsNullOrEmpty(member)) return member;
        var sb = new StringBuilder(member.Length + 4);
        for (int i = 0; i < member.Length; i++)
        {
            var c = member[i];
            if (char.IsUpper(c))
            {
                bool prevLower = i > 0 && char.IsLower(member[i - 1]);
                bool nextLower = i + 1 < member.Length && char.IsLower(member[i + 1]);
                if (i > 0 && (prevLower || nextLower)) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
