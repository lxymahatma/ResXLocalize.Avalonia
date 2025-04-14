using Microsoft.CodeAnalysis;

namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private sealed record MsBuildPropertyInfo(
        string RootNamespace,
        string? ResxNamespace,
        string? XmlnsDefinitionNamespace,
        string? DefaultLocalizationProvider);

    private sealed record ResourceInfo(AdditionalText ResourceFile, string RelativeDir, string ClassName);
}