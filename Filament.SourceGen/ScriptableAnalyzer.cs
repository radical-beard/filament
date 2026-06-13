namespace Filament.SourceGen;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScriptableAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        ScriptableDiagnostics.MustBePartial,
        ScriptableDiagnostics.UnsupportedMemberType,
        ScriptableDiagnostics.NoUsableConstructor,
        ScriptableDiagnostics.DuplicateName,
        ScriptableDiagnostics.UseOptionNotNullable,
        ScriptableDiagnostics.MissingReadableMember);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            // (luaName, location) seen across the compilation, for duplicate detection.
            var names = new ConcurrentBag<(string name, Location location)>();

            start.RegisterSymbolAction(ctx =>
            {
                var type = (INamedTypeSymbol)ctx.Symbol;
                var attr = TypeAnalysis.GetAttribute(type, TypeAnalysis.ScriptableAttr);
                if (attr is null) return;

                CheckType(ctx, type, attr, names);
            }, SymbolKind.NamedType);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (var group in names.GroupBy(n => n.name).Where(g => g.Count() > 1))
                    foreach (var (name, location) in group)
                        end.ReportDiagnostic(Diagnostic.Create(ScriptableDiagnostics.DuplicateName, location, name));
            });
        });
    }

    private static void CheckType(
        SymbolAnalysisContext ctx,
        INamedTypeSymbol type,
        AttributeData attr,
        ConcurrentBag<(string, Location)> names)
    {
        var location = type.Locations.FirstOrDefault() ?? Location.None;

        // FILA0001 — must be partial.
        var isPartial = type.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(d => d.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));
        if (!isPartial)
            ctx.ReportDiagnostic(Diagnostic.Create(ScriptableDiagnostics.MustBePartial, location, type.Name));

        // Record the lua name for duplicate detection (explicit arg or type name).
        var luaName = attr.ConstructorArguments.Length > 0
            && attr.ConstructorArguments[0].Value is string n && !string.IsNullOrEmpty(n)
            ? n : type.Name;
        names.Add((luaName, location));

        // FILA0003 — needs a parameterful ctor for FromLua to call.
        var ctor = TypeAnalysis.PrimaryConstructor(type);
        if (ctor is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(ScriptableDiagnostics.NoUsableConstructor, location, type.Name));
            return;
        }

        // FILA0002 / FILA0005 — per marshalled member (ctor parameters).
        foreach (var p in ctor.Parameters)
        {
            var memberLoc = p.Locations.FirstOrDefault() ?? location;
            var shape = TypeAnalysis.Classify(p.Type);
            var prop = TypeAnalysis.MatchingProperty(type, p);

            if (prop is null || prop.GetMethod is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ScriptableDiagnostics.MissingReadableMember, memberLoc, p.Name, type.Name));
            }

            if (!shape.IsSupported)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ScriptableDiagnostics.UnsupportedMemberType, memberLoc,
                    p.Name, type.Name, p.Type.ToDisplayString()));
                continue;
            }

            // FILA0005 — nullable reference instead of Option<T>.
            if (shape.Wrap == TypeAnalysis.Wrap.None
                && p.Type.IsReferenceType
                && p.NullableAnnotation == NullableAnnotation.Annotated)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ScriptableDiagnostics.UseOptionNotNullable, memberLoc, p.Name, type.Name));
            }
        }
    }
}
