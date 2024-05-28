using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using NUnit.Framework;

namespace LoadError;

internal sealed class Main : TestBase
{
    [Test]
    public async Task Load_Error()
    {
        // language=cs
        const string source = """
            public static class Test
            {
                public static int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """;

        AssemblyLoadContext? context = AssemblyLoadContext.GetLoadContext(s_analyzerAssembly);
        Assert.That(context, Is.Not.Null);
        foreach (Assembly assembly in context.Assemblies.Where(static a =>
            a.FullName!.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)))
        {
            await TestContext.Out.WriteLineAsync($"{assembly} - {assembly.Location}");
        }

        await Verify(source);
    }
}

internal abstract class TestBase
{
    private static readonly string s_dllDirectory =
        Path.GetDirectoryName(typeof(object).Assembly.Location)
        ?? throw new DirectoryNotFoundException("Cannot find object assembly directory");

    private static readonly string s_outputPath =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new DirectoryNotFoundException("Cannot find executing assembly directory");

    protected static readonly Assembly s_analyzerAssembly =
        Assembly.LoadFrom(Path.Combine(s_outputPath, "analyzers", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll"));

    private static readonly ImmutableArray<DiagnosticAnalyzer> s_analyzers = s_analyzerAssembly
        .GetTypes()
        .Where(static type => type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
        .Select(static type => Unsafe.As<DiagnosticAnalyzer>(Activator.CreateInstance(type))!)
        .ToImmutableArray();

    private static readonly Dictionary<string, ReportDiagnostic> s_diagnosticsOptions = s_analyzers
        .SelectMany(static analyzer => analyzer.SupportedDiagnostics, static (_, descriptor) => descriptor.Id)
        .Distinct()
        .Where(static id => id.StartsWith("CA", StringComparison.Ordinal))
        .ToDictionary(static id => id, _ => ReportDiagnostic.Warn);

    private static readonly CSharpCompilationOptions s_compilationOptions =
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable)
            .WithSpecificDiagnosticOptions(s_diagnosticsOptions);

    private static readonly CSharpParseOptions s_parseOptions = new(LanguageVersion.CSharp11);

    private static readonly MetadataReference[] s_metadataReferences =
    {
        MetadataReference.CreateFromFile(Path.Combine(s_dllDirectory, "System.Private.CoreLib.dll"))
    };

    protected static Task Verify(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(GetSource(source), options: s_parseOptions);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "FooBar",
            syntaxTrees: new[] { syntaxTree },
            references: s_metadataReferences,
            options: s_compilationOptions);

        AssertCompilation(compilation);
        return AssertCompilationWithAnalyzers(compilation);
    }

    private static void AssertCompilation(Compilation compilation)
    {
        using MemoryStream output = new();
        EmitResult result = compilation.Emit(output);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics.WarningOrWorse(), Is.Empty);
        });
    }

    private static async Task AssertCompilationWithAnalyzers(Compilation compilation)
    {
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(s_analyzers);
        ImmutableArray<Diagnostic> diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        Assert.That(diagnostics.WarningOrWorse(), Is.Empty);
    }

    private static string GetSource(string source)
    {
        // language=cs
        return $"""
            using System;
            using System.Reflection;
            using System.Runtime.InteropServices;

            [assembly: AssemblyVersion("1.0")]
            [assembly: CLSCompliant(false)]
            [assembly: ComVisible(false)]

            {source}
            """;
    }
}

internal static class Extensions
{
    public static IEnumerable<Diagnostic> WarningOrWorse(this IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics.Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
    }
}
