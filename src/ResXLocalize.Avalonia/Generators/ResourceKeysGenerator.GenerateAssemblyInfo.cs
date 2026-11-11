namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateAssemblyInfo(string resxNamespace, string? xmlnsNamespace)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine("using global::Avalonia.Metadata;");
        builder.AppendLine();

        if (!xmlnsNamespace.IsNullOrEmpty())
        {
            builder.AppendLine($"""[assembly: XmlnsPrefix("{xmlnsNamespace}", "loc")]""");
            builder.AppendLine($"""[assembly: XmlnsDefinition("{xmlnsNamespace}", "{resxNamespace}")]""");
        }

        builder.AppendLine($"""[assembly: XmlnsDefinition("https://github.com/avaloniaui", "{resxNamespace}.Extensions.MarkupExtensions")]""");
        return builder.ToString();
    }
}
