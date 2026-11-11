namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static ImmutableArray<LocalizationTableModel> BuildTables(ImmutableArray<ResourceFile> files) =>
        [..ResourceTable.Create(files).Select(BuildTable)];

    private static LocalizationTableModel BuildTable(ResourceTable table)
    {
        var keys = table.Neutral.Entries
            .Select(static entry => new LocalizationKeyModel(entry.Name, entry.Name.GetValidIdentifier(), entry.Value))
            .ToImmutableArray();

        var valuesByCulture = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cultureFile in table.CultureFiles)
        {
            if (!valuesByCulture.TryGetValue(cultureFile.Culture, out var values))
            {
                values = new Dictionary<string, string>(StringComparer.Ordinal);
                valuesByCulture.Add(cultureFile.Culture, values);
            }

            foreach (var entry in cultureFile.Entries)
            {
                values[entry.Name] = entry.Value;
            }
        }

        var cultures = valuesByCulture.Keys
            .OrderBy(static culture => culture, StringComparer.OrdinalIgnoreCase)
            .Select(culture => new CultureTableModel(
                culture,
                "Culture_" + culture.Replace('-', '_'),
                [..keys.Select(key => ResolveValue(culture, key, valuesByCulture))]))
            .ToImmutableArray();

        return new LocalizationTableModel(table.Name, keys, cultures);
    }

    private static string ResolveValue(string culture, LocalizationKeyModel key, Dictionary<string, Dictionary<string, string>> valuesByCulture)
    {
        for (var current = culture; current.Length > 0; current = GetParentCulture(current))
        {
            if (valuesByCulture.TryGetValue(current, out var values) && values.TryGetValue(key.Name, out var value))
            {
                return value;
            }
        }

        return key.NeutralValue;
    }

    private static string GetParentCulture(string culture)
    {
        var separatorIndex = culture.LastIndexOf('-');
        return separatorIndex < 0 ? string.Empty : culture[..separatorIndex];
    }
}
