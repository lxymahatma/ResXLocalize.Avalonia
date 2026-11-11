namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateLocalizationTable(string resxNamespace)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine($$"""
                             using global::System;
                             using global::System.Globalization;
                             using global::System.Threading;

                             namespace {{resxNamespace}};

                             internal sealed class LocalizationTable
                             {
                                 private readonly Func<CultureInfo, string[]> _resolve;
                                 private readonly LocalizedString?[] _localizedStrings;

                                 internal LocalizationTable(int count, Func<CultureInfo, string[]> resolve)
                                 {
                                     _resolve = resolve;
                                     _localizedStrings = new LocalizedString?[count];
                                     Current = resolve(LocalizationManager.Culture);
                                 }

                                 internal string[] Current { get; private set; }

                                 internal LocalizedString GetLocalizedString(int index) => _localizedStrings[index] ?? CreateLocalizedString(index);

                                 internal void SetCulture(CultureInfo culture)
                                 {
                                     Current = _resolve(culture);
                                     foreach (var localizedString in _localizedStrings)
                                     {
                                         localizedString?.RaiseValueChanged();
                                     }
                                 }

                                 private LocalizedString CreateLocalizedString(int index)
                                 {
                                     var created = new LocalizedString(this, index);
                                     return Interlocked.CompareExchange(ref _localizedStrings[index], created, null) ?? created;
                                 }
                             }
                             """);
        return builder.ToString();
    }
}
