namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private sealed record GeneratorOptions(string ResxNamespace, string? XmlnsDefinitionNamespace, string? DefaultLocalizationProvider);

    private sealed record LocalizationTableModel(
        string ClassName,
        ImmutableArray<LocalizationKeyModel> Keys,
        ImmutableArray<CultureTableModel> Cultures);

    private sealed record LocalizationKeyModel(string Name, string Identifier, string NeutralValue, int ArgumentCount);

    private sealed record CultureTableModel(string Culture, string ClassName, ImmutableArray<string> Values);
}
