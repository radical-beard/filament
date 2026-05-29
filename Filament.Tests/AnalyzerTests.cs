namespace Filament.Tests;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Filament.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

[TestFixture]
public class AnalyzerTests
{
    private static ImmutableArray<Diagnostic> Analyze(string body)
    {
        var source = "using Filament;\nusing System.Collections.Generic;\nnamespace Probe {\n" + body + "\n}";
        var tree = CSharpSyntaxTree.ParseText(source);

        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var refs = tpa.Where(p => p.Length > 0)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
        refs.Add(MetadataReference.CreateFromFile(typeof(ScriptableAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "AnalyzerProbe", new[] { tree }, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        return compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ScriptableAnalyzer()))
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    private static bool Has(ImmutableArray<Diagnostic> d, string id) => d.Any(x => x.Id == id);

    [Test]
    public void GoodType_NoFilaDiagnostics()
    {
        var d = Analyze("[Scriptable] public partial record Good(string Id, int N, IReadOnlyList<int> Xs, Option<float> W);");
        Assert.That(d.Any(x => x.Id.StartsWith("FILA")), Is.False, string.Join(", ", d.Select(x => x.Id)));
    }

    [Test]
    public void NonPartial_FILA0001()
    {
        var d = Analyze("[Scriptable] public record NotPartial(string Id);");
        Assert.That(Has(d, "FILA0001"), Is.True);
    }

    [Test]
    public void UnsupportedMember_FILA0002()
    {
        var d = Analyze("[Scriptable] public partial record Bad(object Thing);");
        Assert.That(Has(d, "FILA0002"), Is.True);
    }

    [Test]
    public void NoConstructor_FILA0003()
    {
        var d = Analyze("[Scriptable] public partial struct NoCtor { public int X { get; set; } }");
        Assert.That(Has(d, "FILA0003"), Is.True);
    }

    [Test]
    public void DuplicateName_FILA0004()
    {
        var d = Analyze(
            "[Scriptable(\"dup\")] public partial record A(int X);\n" +
            "[Scriptable(\"dup\")] public partial record B(int Y);");
        Assert.That(Has(d, "FILA0004"), Is.True);
    }

    [Test]
    public void MissingReadableMember_FILA0006()
    {
        var d = Analyze("[Scriptable] public partial class Bad { public Bad(int x) {} }");
        Assert.That(Has(d, "FILA0006"), Is.True);
    }
}
