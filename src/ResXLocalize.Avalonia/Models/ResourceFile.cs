using System.Xml.Linq;

namespace ResXLocalize.Avalonia.Models;

internal sealed record ResourceFile(string Path, string BaseName, string Culture, EquatableArray<ResourceEntry> Entries)
{
    public bool IsNeutral => Culture.Length == 0;

    public static bool IsResourceFile(AdditionalText file) => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase);

    public static ResourceFile Read(AdditionalText file, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        options.TryGetValue("build_metadata.AdditionalFiles.Culture", out var culture);
        var fileName = System.IO.Path.GetFileNameWithoutExtension(file.Path);
        var baseName = culture!.Length > 0 && fileName.EndsWith("." + culture, StringComparison.OrdinalIgnoreCase)
            ? fileName[..^(culture.Length + 1)]
            : fileName;

        var text = file.GetText(cancellationToken)?.ToString();
        var entries = text.IsNullOrEmpty() ? [] : ReadEntries(text);
        return new ResourceFile(file.Path, baseName, culture, new EquatableArray<ResourceEntry>(entries));
    }

    private static ImmutableArray<ResourceEntry> ReadEntries(string text)
    {
        var document = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        var entries = ImmutableArray.CreateBuilder<ResourceEntry>();

        foreach (var element in document.Root!.Elements("data"))
        {
            if (element.Attribute("name") is not { } nameAttribute
                || element.Element("value") is not { } valueElement
                || element.Attribute("mimetype") is not null
                || element.Attribute("type") is { } typeAttribute && !typeAttribute.Value.StartsWith("System.String", StringComparison.Ordinal))
            {
                continue;
            }

            entries.Add(new ResourceEntry(nameAttribute.Value, valueElement.Value));
        }

        return entries.ToImmutable();
    }
}

internal readonly record struct ResourceEntry(string Name, string Value);
