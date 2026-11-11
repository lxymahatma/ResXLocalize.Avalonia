using ResXLocalize.Avalonia.Analyzers;

namespace ResXLocalize.Avalonia.Tests.Analyzers;

public sealed class ResourceFileAnalyzerTest
{
    [Test]
    public async Task RunAnalyzer_ValidResourceFiles_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.RunAsync<ResourceFileAnalyzer>(
            new Dictionary<string, string> { ["DefaultLocalizationProvider"] = "Interaction" },
            new TestResourceFile("/project/Strings.resx", "", ("Title", "Title"), ("Subtitle", "Subtitle")),
            new TestResourceFile("/project/Strings.de.resx", "de", ("Title", "Titel")),
            new TestResourceFile("/project/Interaction.resx", "", ("Notice", "Notice")));

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task RunAnalyzer_CultureWithoutNeutral_ReportsDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.RunAsync<ResourceFileAnalyzer>(
            null,
            new TestResourceFile("/project/Strings.resx", "", ("Title", "Title")),
            new TestResourceFile("/project/Orphan.de.resx", "de", ("Title", "Titel")));

        await Assert.That(GetIds(diagnostics)).IsEquivalentTo([ResourceFileAnalyzer.MissingNeutralResourceFileRuleId]);
    }

    [Test]
    public async Task RunAnalyzer_KeyMissingFromNeutral_ReportsDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.RunAsync<ResourceFileAnalyzer>(
            null,
            new TestResourceFile("/project/Strings.resx", "", ("Title", "Title")),
            new TestResourceFile("/project/Strings.de.resx", "de", ("Title", "Titel"), ("Typo", "Tippfehler")));

        await Assert.That(GetIds(diagnostics)).IsEquivalentTo([ResourceFileAnalyzer.KeyMissingFromNeutralResourceFileRuleId]);
    }

    [Test]
    public async Task RunAnalyzer_UnknownDefaultLocalizationProvider_ReportsDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.RunAsync<ResourceFileAnalyzer>(
            new Dictionary<string, string> { ["DefaultLocalizationProvider"] = "Missing" },
            new TestResourceFile("/project/Strings.resx", "", ("Title", "Title")));

        await Assert.That(GetIds(diagnostics)).IsEquivalentTo([ResourceFileAnalyzer.UnknownDefaultLocalizationProviderRuleId]);
    }

    [Test]
    public async Task RunAnalyzer_DuplicateNeutralName_ReportsLaterFile()
    {
        var diagnostics = await AnalyzerTestHelper.RunAsync<ResourceFileAnalyzer>(
            null,
            new TestResourceFile("/project/First/Strings.resx", "", ("Title", "Title")),
            new TestResourceFile("/project/Second/Strings.resx", "", ("Title", "Title")));

        using var _ = Assert.Multiple();
        await Assert.That(GetIds(diagnostics)).IsEquivalentTo([ResourceFileAnalyzer.DuplicateNeutralResourceFileRuleId]);
        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Location.GetLineSpan().Path)).IsEquivalentTo(["/project/Second/Strings.resx"]);
    }

    private static IEnumerable<string> GetIds(ImmutableArray<Diagnostic> diagnostics) => diagnostics.Select(static diagnostic => diagnostic.Id);
}
