namespace ResXLocalize.Avalonia.Generators;

[Generator(LanguageNames.CSharp)]
public sealed partial class ResourceKeysGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => ReadOptions(provider.GlobalOptions));

        var resourceFiles = context.AdditionalTextsProvider
            .Where(ResourceFile.IsResourceFile)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, cancellationToken) => ResourceFile.Read(pair.Left, pair.Right.GetOptions(pair.Left), cancellationToken))
            .Collect();

        context.RegisterSourceOutput(resourceFiles.Combine(options), static (context, pair) => Generate(context, pair.Left, pair.Right));
    }

    private static GeneratorOptions ReadOptions(AnalyzerConfigOptions options)
    {
        options.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        options.TryGetValue("build_property.ResxNamespace", out var resxNamespace);
        options.TryGetValue("build_property.XmlnsDefinitionNamespace", out var xmlnsDefinitionNamespace);
        options.TryGetValue("build_property.DefaultLocalizationProvider", out var defaultLocalizationProvider);
        return new GeneratorOptions(
            string.IsNullOrEmpty(resxNamespace) ? rootNamespace! : resxNamespace,
            xmlnsDefinitionNamespace,
            defaultLocalizationProvider);
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<ResourceFile> files, GeneratorOptions options)
    {
        var tables = BuildTables(files);
        if (tables.IsEmpty)
        {
            return;
        }

        var resxNamespace = options.ResxNamespace;
        var defaultTable = tables.FirstOrDefault(table => table.ClassName == options.DefaultLocalizationProvider) ?? tables[0];

        foreach (var table in tables)
        {
            context.AddSource($"{table.ClassName}.g.cs", GenerateTable(resxNamespace, table));
            foreach (var culture in table.Cultures)
            {
                context.AddSource($"{table.ClassName}.{culture.Culture}.g.cs", GenerateCultureTable(resxNamespace, table, culture));
            }
        }

        context.AddSource("LocalizedString.g.cs", GenerateLocalizedString(resxNamespace));
        context.AddSource("LocalizationTable.g.cs", GenerateLocalizationTable(resxNamespace));
        context.AddSource("LocalizationManager.g.cs", GenerateLocalizationManager(resxNamespace, tables));
        context.AddSource("LocalizeExtension.g.cs", GenerateLocalizeExtensions(resxNamespace, tables, defaultTable));
        context.AddSource("LocalizedStringBinding.g.cs", GenerateLocalizedStringBinding(resxNamespace));
        context.AddSource("AssemblyInfo.g.cs", GenerateAssemblyInfo(resxNamespace, options.XmlnsDefinitionNamespace));
    }

    private static CodeBuilder CreateSourceBuilder() => new CodeBuilder().Append(Header).AppendLine();
}
