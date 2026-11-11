namespace ResXLocalize.Avalonia.Models;

internal sealed record ResourceTable(ResourceFile Neutral, ImmutableArray<ResourceFile> CultureFiles)
{
    public string Name => Neutral.BaseName.ToIdentifierCharacters();

    /// <summary>
    /// Creates one table per neutral file name, in file order. The first neutral file of a name defines the table,
    /// and culture files join the table that has their name.
    /// </summary>
    public static ImmutableArray<ResourceTable> Create(ImmutableArray<ResourceFile> files)
    {
        var cultureFiles = files
            .Where(static file => !file.IsNeutral)
            .ToLookup(static file => file.BaseName, StringComparer.Ordinal);

        return
        [
            ..files
                .Where(static file => file.IsNeutral)
                .GroupBy(static file => file.BaseName, StringComparer.Ordinal)
                .Select(group => new ResourceTable(group.First(), [..cultureFiles[group.Key]]))
        ];
    }
}
