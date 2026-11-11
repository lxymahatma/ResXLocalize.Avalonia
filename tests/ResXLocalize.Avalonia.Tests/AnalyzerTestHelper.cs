namespace ResXLocalize.Avalonia.Tests;

internal static class AnalyzerTestHelper
{
    public static async Task<ImmutableArray<Diagnostic>> RunAsync<TAnalyzer>(IReadOnlyDictionary<string, string>? properties, params TestResourceFile[] files)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var project = TestProject.Create(properties, files);
        var compilation = CSharpCompilation.Create(
            "Tests",
            [],
            Net110.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await compilation
            .WithAnalyzers([new TAnalyzer()], new AnalyzerOptions(project.AdditionalTexts, project.OptionsProvider))
            .GetAnalyzerDiagnosticsAsync();
    }
}
