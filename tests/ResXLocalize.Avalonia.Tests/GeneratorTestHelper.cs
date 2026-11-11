using System.Collections.Concurrent;
using System.Reflection;
using Avalonia;
using Avalonia.Markup.Xaml;
using ResXLocalize.Avalonia.Generators;

namespace ResXLocalize.Avalonia.Tests;

internal sealed record GeneratorTestOutput(GeneratorDriverRunResult RunResult, Compilation OutputCompilation);

internal static class GeneratorTestHelper
{
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp14);

    private static readonly ImmutableArray<MetadataReference> References =
    [
        ..Net110.References.All,
        ..new[] { typeof(AvaloniaObject).Assembly, typeof(MarkupExtension).Assembly }
            .Select(static assembly => Path.GetDirectoryName(assembly.Location)!)
            .Distinct()
            .SelectMany(static directory => Directory.GetFiles(directory, "Avalonia*.dll"))
            .Select(static path => MetadataReference.CreateFromFile(path))
    ];

    // Assembly-unification warnings that the .NET SDK suppresses in every C# project (NoWarn 1701;1702).
    private static readonly Dictionary<string, ReportDiagnostic> SdkSuppressedDiagnostics = new()
    {
        ["CS1701"] = ReportDiagnostic.Suppress,
        ["CS1702"] = ReportDiagnostic.Suppress
    };

    // Avalonia raises binding updates and weak property-change events synchronously only on its UI thread, the first thread
    // that uses a dispatcher, so every program runs on one dedicated thread.
    private static readonly BlockingCollection<Action> UIThreadQueue = StartUIThread();

    public static GeneratorTestOutput Run(IReadOnlyDictionary<string, string>? properties, params TestResourceFile[] files)
    {
        var project = TestProject.Create(properties, files);
        var compilation = CSharpCompilation.Create(
            "Tests",
            [],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
                .WithSpecificDiagnosticOptions(SdkSuppressedDiagnostics));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ResourceKeysGenerator().AsSourceGenerator()],
            project.AdditionalTexts,
            ParseOptions,
            project.OptionsProvider);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        return new GeneratorTestOutput(driver.GetRunResult(), outputCompilation);
    }

    /// <summary>
    /// Compiles the generated code together with a <c>Program</c> class in the <c>Sample</c> namespace, runs
    /// <c>Program.Run</c> in a freshly loaded assembly on the Avalonia UI thread and returns its result.
    /// </summary>
    /// <param name="body">Body of <c>Program.Run</c>.</param>
    /// <param name="declarations">Further members of <c>Program</c>.</param>
    public static Task<T> ExecuteAsync<T>(IReadOnlyDictionary<string, string>? properties, TestResourceFile[] files, string body, string declarations = "")
    {
        var program = $$"""
                        using System;
                        using System.Collections.Generic;
                        using System.ComponentModel;
                        using System.Globalization;
                        using System.Linq;
                        using Avalonia;
                        using Avalonia.Data;
                        using Sample.Extensions.MarkupExtensions;

                        namespace Sample;

                        public static class Program
                        {
                            public static object? Run()
                            {
                                {{body}}
                            }

                            {{declarations}}
                        }
                        """;

        var compilation = Run(properties, files).OutputCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(program, ParseOptions));
        var diagnostics = compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).ToArray();
        if (diagnostics.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        using var stream = new MemoryStream();
        compilation.Emit(stream);
        var run = System.Reflection.Assembly.Load(stream.ToArray()).GetType("Sample.Program")!.GetMethod("Run")!;
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        UIThreadQueue.Add(() =>
        {
            try
            {
                result.SetResult((T)run.Invoke(null, BindingFlags.DoNotWrapExceptions, null, null, null)!);
            }
            catch (Exception exception)
            {
                result.SetException(exception);
            }
        });

        return result.Task;
    }

    private static BlockingCollection<Action> StartUIThread()
    {
        var queue = new BlockingCollection<Action>();
        new Thread(() =>
        {
            foreach (var action in queue.GetConsumingEnumerable())
            {
                action();
            }
        }) { IsBackground = true }.Start();
        return queue;
    }
}
