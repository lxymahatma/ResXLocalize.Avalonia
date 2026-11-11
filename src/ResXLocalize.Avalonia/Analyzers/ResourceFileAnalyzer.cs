using Microsoft.CodeAnalysis.Text;

namespace ResXLocalize.Avalonia.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResourceFileAnalyzer : DiagnosticAnalyzer
{
    public const string MissingNeutralResourceFileRuleId = "RXL0001";
    public const string KeyMissingFromNeutralResourceFileRuleId = "RXL0002";
    public const string UnknownDefaultLocalizationProviderRuleId = "RXL0003";
    public const string DuplicateNeutralResourceFileRuleId = "RXL0004";

    private const string Category = "ResXLocalize";

    private static readonly DiagnosticDescriptor MissingNeutralResourceFileRule = new(
        MissingNeutralResourceFileRuleId,
        "Culture-specific resource file has no neutral resource file",
        "Resource file '{0}' is for culture '{1}' but no neutral resource file '{2}.resx' exists; it is ignored",
        Category,
        DiagnosticSeverity.Warning,
        true);

    private static readonly DiagnosticDescriptor KeyMissingFromNeutralResourceFileRule = new(
        KeyMissingFromNeutralResourceFileRuleId,
        "Key is missing from the neutral resource file",
        "Key '{0}' in resource file '{1}' does not exist in the neutral resource file '{2}.resx'; it is ignored",
        Category,
        DiagnosticSeverity.Warning,
        true);

    private static readonly DiagnosticDescriptor UnknownDefaultLocalizationProviderRule = new(
        UnknownDefaultLocalizationProviderRuleId,
        "DefaultLocalizationProvider does not match a resource file",
        "DefaultLocalizationProvider '{0}' does not match any neutral resource file; '{1}' is used instead",
        Category,
        DiagnosticSeverity.Error,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor DuplicateNeutralResourceFileRule = new(
        DuplicateNeutralResourceFileRuleId,
        "Multiple neutral resource files share a name",
        "Neutral resource file '{0}' has the same name as '{1}'; it is ignored",
        Category,
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        MissingNeutralResourceFileRule,
        KeyMissingFromNeutralResourceFileRule,
        UnknownDefaultLocalizationProviderRule,
        DuplicateNeutralResourceFileRule
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        // The resource files are read on first use, so analysis that involves no resource file reads none of them.
        var options = context.Options;
        ResourceIndex? index = null;
        ResourceIndex GetIndex(CancellationToken cancellationToken) =>
            LazyInitializer.EnsureInitialized(ref index, () => new ResourceIndex(options, cancellationToken));

        context.RegisterAdditionalFileAction(fileContext =>
        {
            if (ResourceFile.IsResourceFile(fileContext.AdditionalFile))
            {
                AnalyzeResourceFile(fileContext, GetIndex(fileContext.CancellationToken));
            }
        });
        context.RegisterCompilationEndAction(endContext => AnalyzeDefaultLocalizationProvider(endContext, GetIndex(endContext.CancellationToken)));
    }

    private static void AnalyzeDefaultLocalizationProvider(CompilationAnalysisContext context, ResourceIndex index)
    {
        if (!index.Tables.IsEmpty
            && context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.DefaultLocalizationProvider", out var defaultLocalizationProvider)
            && defaultLocalizationProvider.Length > 0
            && !index.Tables.Any(table => table.Name == defaultLocalizationProvider))
        {
            context.ReportDiagnostic(Diagnostic.Create(UnknownDefaultLocalizationProviderRule, Location.None, defaultLocalizationProvider, index.Tables[0].Name));
        }
    }

    private static void AnalyzeResourceFile(AdditionalFileAnalysisContext context, ResourceIndex index)
    {
        var file = index.FilesByPath[context.AdditionalFile.Path];
        var location = Location.Create(file.Path, default, default(LinePositionSpan));
        if (!index.TablesByName.TryGetValue(file.BaseName, out var table))
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingNeutralResourceFileRule, location, file.Path, file.Culture, file.BaseName));
        }
        else if (file.IsNeutral)
        {
            if (file.Path != table.Neutral.Path)
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateNeutralResourceFileRule, location, file.Path, table.Neutral.Path));
            }
        }
        else
        {
            var neutralKeys = new HashSet<string>(table.Neutral.Entries.Select(static entry => entry.Name), StringComparer.Ordinal);
            foreach (var entry in file.Entries.Where(entry => !neutralKeys.Contains(entry.Name)))
            {
                context.ReportDiagnostic(Diagnostic.Create(KeyMissingFromNeutralResourceFileRule, location, entry.Name, file.Path, table.Neutral.BaseName));
            }
        }
    }

    private sealed class ResourceIndex
    {
        public ResourceIndex(AnalyzerOptions options, CancellationToken cancellationToken)
        {
            ImmutableArray<ResourceFile> files =
            [
                ..options.AdditionalFiles
                    .Where(ResourceFile.IsResourceFile)
                    .Select(file => ResourceFile.Read(file, options.AnalyzerConfigOptionsProvider.GetOptions(file), cancellationToken))
            ];

            foreach (var file in files)
            {
                FilesByPath[file.Path] = file;
            }

            Tables = ResourceTable.Create(files);
            TablesByName = Tables.ToDictionary(static table => table.Neutral.BaseName, StringComparer.Ordinal);
        }

        public Dictionary<string, ResourceFile> FilesByPath { get; } = new(StringComparer.Ordinal);

        public ImmutableArray<ResourceTable> Tables { get; }

        public Dictionary<string, ResourceTable> TablesByName { get; }
    }
}
