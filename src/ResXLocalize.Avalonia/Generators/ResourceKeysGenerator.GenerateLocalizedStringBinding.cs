namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateLocalizedStringBinding(string resxNamespace)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine($$"""
                             using global::System;
                             using global::System.Collections.Generic;
                             using global::System.Globalization;
                             using global::Avalonia;
                             using global::Avalonia.Data;
                             using global::Avalonia.Data.Converters;
                             using global::Avalonia.Data.Core;
                             using global::Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

                             namespace {{resxNamespace}}.Extensions.MarkupExtensions;

                             internal static class LocalizedStringBinding
                             {
                                 private static readonly CompiledBindingPath ValuePath = new CompiledBindingPathBuilder()
                                     .Property(
                                         new ClrPropertyInfo(
                                             nameof(global::{{resxNamespace}}.LocalizedString.Value),
                                             static target => ((global::{{resxNamespace}}.LocalizedString)target).Value,
                                             null,
                                             typeof(string)),
                                         PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
                                     .Build();

                                 internal static BindingBase Create(global::{{resxNamespace}}.LocalizedString localizedString, IReadOnlyList<BindingBase> arguments)
                                 {
                                     var valueBinding = new CompiledBinding(ValuePath) { Source = localizedString, Mode = BindingMode.OneWay };
                                     if (arguments.Count == 0)
                                     {
                                         return valueBinding;
                                     }

                                     var formatBinding = new MultiBinding { Converter = FormatConverter.Instance, Mode = BindingMode.OneWay };
                                     formatBinding.Bindings.Add(valueBinding);
                                     foreach (var argument in arguments)
                                     {
                                         formatBinding.Bindings.Add(argument);
                                     }

                                     return formatBinding;
                                 }

                                 private sealed class FormatConverter : IMultiValueConverter
                                 {
                                     internal static readonly FormatConverter Instance = new();

                                     public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
                                     {
                                         if (values[0] is not string format)
                                         {
                                             return AvaloniaProperty.UnsetValue;
                                         }

                                         var arguments = new object?[values.Count - 1];
                                         for (var index = 1; index < values.Count; index++)
                                         {
                                             if (values[index] is UnsetValueType)
                                             {
                                                 return AvaloniaProperty.UnsetValue;
                                             }

                                             arguments[index - 1] = values[index];
                                         }

                                         return string.Format(global::{{resxNamespace}}.LocalizationManager.Culture, format, arguments);
                                     }
                                 }
                             }
                             """);
        return builder.ToString();
    }
}
