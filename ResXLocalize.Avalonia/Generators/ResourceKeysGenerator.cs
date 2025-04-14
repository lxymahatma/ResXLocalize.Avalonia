using System.Xml.Linq;

namespace ResXLocalize.Avalonia.Generators;

[Generator(LanguageNames.CSharp)]
public sealed partial class ResourceKeysGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var namespaceInfo = context
            .AnalyzerConfigOptionsProvider
            .Select((c, _) =>
            {
                if (!c.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
                {
                    return null;
                }

                c.GlobalOptions.TryGetValue("build_property.ResxNamespace", out var resxNamespace);
                c.GlobalOptions.TryGetValue("build_property.XmlnsDefinitionNamespace", out var xmlnsDefinitionNamespace);
                c.GlobalOptions.TryGetValue("build_property.DefaultLocalizationProvider", out var defaultLocalizationProvider);
                return new MsBuildPropertyInfo(rootNamespace, resxNamespace, xmlnsDefinitionNamespace, defaultLocalizationProvider);
            });

        var resourceFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase));

        var resources = resourceFiles.Combine(context.AnalyzerConfigOptionsProvider)
            .Select((resourceFileAndOptions, _) =>
            {
                var (resourceFile, optionsProvider) = resourceFileAndOptions;
                var options = optionsProvider.GetOptions(resourceFile);
                var resourceFileName = Path.GetFileNameWithoutExtension(resourceFile.Path);

                if (!options.TryGetValue("build_metadata.AdditionalFiles.RelativeDir", out var relativeDir))
                {
                    return null;
                }

                relativeDir = relativeDir
                    .Replace(Path.DirectorySeparatorChar.ToString(), string.Empty)
                    .Replace(Path.AltDirectorySeparatorChar.ToString(), string.Empty);
                return new ResourceInfo(resourceFile, relativeDir, resourceFileName);
            })
            .Collect();

        context.RegisterSourceOutput(resources.Combine(namespaceInfo), GenerateFromData);
    }

    private static IEnumerable<(string Name, string Value)> ExtractResourceData(AdditionalText resourceFile)
    {
        var text = resourceFile.GetText()?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var xdoc = XDocument.Parse(text);
        var dataElements = xdoc.Descendants("data");

        foreach (var element in dataElements)
        {
            var nameAttribute = element.Attribute("name");
            var valueElement = element.Element("value");

            if (nameAttribute != null && valueElement != null)
            {
                yield return (nameAttribute.Value, valueElement.Value.Trim());
            }
        }
    }
}