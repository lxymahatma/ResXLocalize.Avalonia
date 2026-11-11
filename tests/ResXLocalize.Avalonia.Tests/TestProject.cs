using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace ResXLocalize.Avalonia.Tests;

internal sealed record TestResourceFile(string Path, string Culture, params (string Name, string Value)[] Entries)
{
    public string ToResx()
    {
        var entries = string.Concat(Entries.Select(static entry =>
            $"""
                 <data name="{SecurityElement.Escape(entry.Name)}" xml:space="preserve">
                     <value>{SecurityElement.Escape(entry.Value)}</value>
                 </data>

             """));
        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <root>
                    <resheader name="resmimetype">
                        <value>text/microsoft-resx</value>
                    </resheader>
                {entries}</root>
                """;
    }
}

internal sealed record TestProject(ImmutableArray<AdditionalText> AdditionalTexts, AnalyzerConfigOptionsProvider OptionsProvider)
{
    public static TestProject Create(IReadOnlyDictionary<string, string>? properties, TestResourceFile[] files)
    {
        var globalOptions = new Dictionary<string, string> { ["build_property.RootNamespace"] = "Sample" };
        foreach (var (key, value) in properties ?? new Dictionary<string, string>())
        {
            globalOptions[$"build_property.{key}"] = value;
        }

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            new TestAnalyzerConfigOptions(globalOptions),
            files.ToDictionary(
                static file => file.Path,
                static file => new TestAnalyzerConfigOptions(new Dictionary<string, string> { ["build_metadata.AdditionalFiles.Culture"] = file.Culture })));

        return new TestProject([..files.Select(static file => new TestAdditionalText(file.Path, file.ToResx()))], optionsProvider);
    }

    private sealed class TestAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }

    private sealed class TestAnalyzerConfigOptionsProvider(
        AnalyzerConfigOptions globalOptions,
        Dictionary<string, TestAnalyzerConfigOptions> fileOptions) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => fileOptions[textFile.Path];
    }
}
