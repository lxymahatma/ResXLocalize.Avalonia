namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static ImmutableArray<LocalizationTableModel> BuildTables(ImmutableArray<ResourceFile> files) =>
        [..ResourceTable.Create(files).Select(BuildTable)];

    private static LocalizationTableModel BuildTable(ResourceTable table)
    {
        var keys = table.Neutral.Entries
            .Select(static entry => new LocalizationKeyModel(entry.Name, entry.Name.GetValidIdentifier(), entry.Value, GetArgumentCount(entry.Value)))
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

    /// <summary>Returns one more than the highest argument index in a composite format string such as "{0} of {1}".</summary>
    private static int GetArgumentCount(string format)
    {
        var count = 0;
        for (var index = 0; index < format.Length; index++)
        {
            if (format[index] != '{')
            {
                continue;
            }

            if (index + 1 < format.Length && format[index + 1] == '{')
            {
                index++;
                continue;
            }

            var end = index + 1;
            while (end < format.Length && format[end] is >= '0' and <= '9')
            {
                end++;
            }

            if (end > index + 1 && int.TryParse(format.Substring(index + 1, end - index - 1), out var argument))
            {
                count = Math.Max(count, argument + 1);
            }
        }

        return count;
    }
}
