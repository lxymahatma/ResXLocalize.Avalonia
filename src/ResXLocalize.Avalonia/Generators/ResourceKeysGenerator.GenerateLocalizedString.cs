namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateLocalizedString(string resxNamespace)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine($$"""
                             using global::System.ComponentModel;

                             namespace {{resxNamespace}};

                             public sealed class LocalizedString : INotifyPropertyChanged
                             {
                                 private static readonly PropertyChangedEventArgs ValueChangedEventArgs = new(nameof(Value));

                                 private readonly LocalizationTable? _table;
                                 private readonly int _index;
                                 private readonly string _text;

                                 internal LocalizedString(LocalizationTable table, int index)
                                 {
                                     _table = table;
                                     _index = index;
                                     _text = string.Empty;
                                 }

                                 internal LocalizedString(string text) => _text = text;

                                 public string Value => _table is null ? _text : _table.Current[_index];

                                 public event PropertyChangedEventHandler? PropertyChanged;

                                 public static LocalizedString FromText(string text) => new(text);

                                 public static implicit operator string(LocalizedString localizedString) => localizedString.Value;

                                 public override string ToString() => Value;

                                 internal void RaiseValueChanged() => PropertyChanged?.Invoke(this, ValueChangedEventArgs);
                             }
                             """);
        return builder.ToString();
    }
}
