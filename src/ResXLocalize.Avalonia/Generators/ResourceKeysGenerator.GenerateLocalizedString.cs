namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateLocalizedString(string resxNamespace)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine($$"""
                             using global::System.ComponentModel;
                             using global::Avalonia.Utilities;

                             namespace {{resxNamespace}};

                             public sealed class LocalizedString : INotifyPropertyChanged, IWeakEventSubscriber<PropertyChangedEventArgs>
                             {
                                 private static readonly PropertyChangedEventArgs ValueChangedEventArgs = new(nameof(Value));

                                 private readonly LocalizationTable? _table;
                                 private readonly int _index;
                                 private readonly string _text;
                                 private readonly LocalizedString? _format;
                                 private readonly object?[] _arguments = [];
                                 private PropertyChangedEventHandler? _propertyChanged;

                                 internal LocalizedString(LocalizationTable table, int index)
                                 {
                                     _table = table;
                                     _index = index;
                                     _text = string.Empty;
                                 }

                                 internal LocalizedString(string text) => _text = text;

                                 private LocalizedString(LocalizedString format, object?[] arguments)
                                 {
                                     _format = format;
                                     _arguments = arguments;
                                     _text = string.Empty;
                                 }

                                 public string Value =>
                                     _table is not null ? _table.Current[_index]
                                     : _format is not null ? string.Format(LocalizationManager.Culture, _format.Value, _arguments)
                                     : _text;

                                 public event PropertyChangedEventHandler? PropertyChanged
                                 {
                                     add
                                     {
                                         var hadSubscribers = _propertyChanged is not null;
                                         _propertyChanged += value;
                                         if (_format is not null && !hadSubscribers && _propertyChanged is not null)
                                         {
                                             WeakEvents.ThreadSafePropertyChanged.Subscribe(_format, this);
                                         }
                                     }
                                     remove
                                     {
                                         var hadSubscribers = _propertyChanged is not null;
                                         _propertyChanged -= value;
                                         if (_format is not null && hadSubscribers && _propertyChanged is null)
                                         {
                                             WeakEvents.ThreadSafePropertyChanged.Unsubscribe(_format, this);
                                         }
                                     }
                                 }

                                 public static LocalizedString FromText(string text) => new(text);

                                 public static implicit operator string(LocalizedString localizedString) => localizedString.Value;

                                 internal LocalizedString Format(params object?[] arguments) => new(this, arguments);

                                 public override string ToString() => Value;

                                 internal void RaiseValueChanged() => _propertyChanged?.Invoke(this, ValueChangedEventArgs);

                                 void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent weakEvent, PropertyChangedEventArgs eventArgs) =>
                                     RaiseValueChanged();
                             }
                             """);
        return builder.ToString();
    }
}
