using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using ResXLocalize.Avalonia.Extensions;
using ResXLocalize.Avalonia.StringBuilders;

namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static void GenerateFromData(SourceProductionContext spc, ValueTuple<ImmutableArray<ResourceInfo?>, MsBuildPropertyInfo?> dataCollectionTuple)
    {
        if (dataCollectionTuple is not (var dataCollection, { } msBuildPropertyInfo)
            || dataCollection is []
            || dataCollection.FirstOrDefault(x => x is not null) is not { } first)
        {
            return;
        }

        var resxNamespace = msBuildPropertyInfo.ResxNamespace.IsNullOrEmpty()
            ? msBuildPropertyInfo.RootNamespace
            : msBuildPropertyInfo.ResxNamespace;

        var localizeExtensionBuilder = CreateLocalizeExtensionBuilder(resxNamespace);
        var localizationManagerBuilder = CreateLocalizationManagerBuilder(resxNamespace);
        var providerEnumBuilder = CreateLocalizationProviderBuilder(resxNamespace);

        var defaultLocalizationProvider = msBuildPropertyInfo.DefaultLocalizationProvider.IsNullOrEmpty()
            ? first.ClassName
            : msBuildPropertyInfo.DefaultLocalizationProvider;

        localizeExtensionBuilder.AppendLine($$"""
                                                  public LocalizeExtension(string resourceKey) => _localizedString = new {{defaultLocalizationProvider}}Literal.LocalizedString(resourceKey);

                                                  public LocalizeExtension(string resourceKey, LocalizationProvider provider)
                                                  {
                                                      _localizedString = provider switch
                                                      {
                                              """);
        localizeExtensionBuilder.IncreaseIndent(3);

        foreach (var data in dataCollection)
        {
            if (data is not var (resourceFile, relativeDir, className))
            {
                continue;
            }

            localizationManagerBuilder.AppendLine($"{className}.Culture = Culture;");
            providerEnumBuilder.AppendLine($"{className},");
            localizeExtensionBuilder.AppendLine($"LocalizationProvider.{className} => new {className}Literal.LocalizedString(resourceKey),");

            GenerateDesignerFile(spc, resourceFile, msBuildPropertyInfo.RootNamespace, resxNamespace, relativeDir, className);
            GenerateLiteralFile(spc, resourceFile, resxNamespace, className);
        }

        localizeExtensionBuilder.ResetIndent();
        localizeExtensionBuilder.AppendLine("""
                                                        _ => throw new UnreachableException()
                                                    };
                                                }
                                            }
                                            """);
        spc.AddSource("LocalizeExtension.cs", localizeExtensionBuilder.ToString());

        localizationManagerBuilder.ResetIndent();
        localizationManagerBuilder.AppendLine("""
                                                      }
                                                  }
                                              }
                                              """);
        spc.AddSource("LocalizationManager.cs", localizationManagerBuilder.ToString());

        providerEnumBuilder.ResetIndent();
        providerEnumBuilder.AppendLine("}");
        spc.AddSource("LocalizationProvider.cs", providerEnumBuilder.ToString());

        GenerateInterface(spc, resxNamespace);
        GenerateAssemblyInfo(spc, resxNamespace, msBuildPropertyInfo.XmlnsDefinitionNamespace);
    }

    private static void GenerateInterface(SourceProductionContext spc, string resxNamespace)
    {
        spc.AddSource("ILocalizedString.cs",
            $$"""
              namespace {{resxNamespace}};

              public interface ILocalizedString
              {
                  string Value { get; }
              }
              """);
    }

    private static void GenerateAssemblyInfo(SourceProductionContext spc, string resxNamespace, string? xmlnsNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using global::Avalonia.Metadata;");
        sb.AppendLine();

        if (!xmlnsNamespace.IsNullOrEmpty())
        {
            sb.AppendLine($"""
                           [assembly: XmlnsPrefix("{xmlnsNamespace}", "loc")]
                           [assembly: XmlnsDefinition("{xmlnsNamespace}", "{resxNamespace}")]
                           """);
        }

        sb.AppendLine($"""
                       [assembly: XmlnsDefinition("https://github.com/avaloniaui", "{resxNamespace}.Extensions.MarkupExtensions")]
                       """);
        spc.AddSource("AssemblyInfo.g.cs", sb.ToString());
    }

    #region Designer and Literal

    private static void GenerateDesignerFile(
        SourceProductionContext spc,
        AdditionalText resourceFile,
        string rootNamespace,
        string resxNamespace,
        string relativeDir,
        string className)
    {
        var designerBuilder = new IndentedGeneratorStringBuilder();
        designerBuilder.AppendLine($$"""
                                     namespace {{resxNamespace}};

                                     public static class {{className}}
                                     {
                                         [field: global::System.Diagnostics.CodeAnalysis.AllowNullAttribute()] [field: global::System.Diagnostics.CodeAnalysis.MaybeNullAttribute()]
                                         public static global::System.Resources.ResourceManager ResourceManager =>
                                             field ??= new global::System.Resources.ResourceManager("{{rootNamespace}}.{{relativeDir}}.{{className}}", typeof({{className}}).Assembly);

                                         public static global::System.Globalization.CultureInfo? Culture { get; set; }

                                         public static string GetResourceString(string resourceKey) =>
                                             ResourceManager.GetString(resourceKey, Culture) ?? resourceKey;

                                     """);

        designerBuilder.IncreaseIndent();
        foreach (var (name, value) in ExtractResourceData(resourceFile))
        {
            designerBuilder.AppendLine("/// <summary>");
            foreach (var str in value.Split('\n'))
            {
                designerBuilder.AppendLine($"/// \t{str.EscapeXmlDoc()}");
            }

            designerBuilder.AppendLine("/// </summary>");
            designerBuilder.AppendLine($"public static string {name.GetValidIdentifier()} => GetResourceString(\"{name}\");");
        }

        designerBuilder.ResetIndent();
        designerBuilder.AppendLine("}");
        spc.AddSource($"{className}.Designer.cs", designerBuilder.ToString());
    }

    private static void GenerateLiteralFile(
        SourceProductionContext spc,
        AdditionalText resourceFile,
        string resxNamespace,
        string className)
    {
        var literalBuilder = new GeneratorStringBuilder();

        literalBuilder.AppendLine("""
                                  using global::System.ComponentModel;
                                  using global::System.Runtime.CompilerServices;
                                  using global::Avalonia.Threading;
                                  """);
#if R3
        literalBuilder.AppendLine("using global::R3;");
#endif
        literalBuilder.AppendLine($$"""
                                    namespace {{resxNamespace}};

                                    public static class {{className}}Literal
                                    {
                                    """);

        foreach (var (name, _) in ExtractResourceData(resourceFile))
        {
            literalBuilder.AppendLine($"""
                                           public const string {name.GetValidIdentifier()} = "{name}";

                                       """);
        }


        literalBuilder.AppendLine($$"""
                                        public sealed class LocalizedString : INotifyPropertyChanged, ILocalizedString
                                        {
                                            public string Value => {{className}}.GetResourceString(field);

                                            public LocalizedString(string resourceKey)
                                            {
                                                Value = resourceKey;
                                    """);

#if BASE
        literalBuilder.AppendLine("            LocalizationManager.CultureChanged += (_, _) => OnPropertyChanged(nameof(Value));");
#elif R3
        literalBuilder.AppendLine("""
                                              Observable.FromEventHandler(
                                                      h => LocalizationManager.CultureChanged += h,
                                                      h => LocalizationManager.CultureChanged -= h)
                                                  .Subscribe(this, (_, state) => state.OnPropertyChanged(nameof(Value)));
                                  """);
#endif
        literalBuilder.AppendLine("""
                                          }

                                          public event PropertyChangedEventHandler? PropertyChanged;

                                          public override string ToString() => Value;

                                          public static implicit operator LocalizedString(string resourceKey) => new(resourceKey);

                                          private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
                                              Dispatcher.UIThread.Post(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
                                      }
                                  }
                                  """);
        spc.AddSource($"{className}.Literal.cs", literalBuilder.ToString());
    }

    #endregion Designer and Literal

    #region StringBuilders

    private static IndentedGeneratorStringBuilder CreateLocalizeExtensionBuilder(string resxNamespace)
    {
        var builder = new IndentedGeneratorStringBuilder();
        builder.AppendLine($$"""
                             using global::System.Diagnostics;
                             using global::Avalonia.Data;
                             using global::Avalonia.Data.Core;
                             using global::Avalonia.Markup.Xaml;
                             using global::Avalonia.Markup.Xaml.MarkupExtensions;
                             using global::Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
                             using global::{{resxNamespace}};

                             namespace {{resxNamespace}}.Extensions.MarkupExtensions;

                             public sealed class LocalizeExtension : MarkupExtension
                             {
                                 private readonly ILocalizedString? _localizedString;

                                 public override CompiledBindingExtension ProvideValue(IServiceProvider serviceProvider)
                                 {
                                     var builder = new CompiledBindingPathBuilder();
                                     var clrProperty = new ClrPropertyInfo("Value", o => ((ILocalizedString)o).Value, null, typeof(ILocalizedString));
                                     builder.Property(clrProperty, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);
                                     return new CompiledBindingExtension
                                     {
                                         Mode = BindingMode.OneWay,
                                         Source = _localizedString,
                                         Path = builder.Build()
                                     };
                                 }
                             """);
        return builder;
    }

    private static IndentedGeneratorStringBuilder CreateLocalizationManagerBuilder(string resxNamespace)
    {
        var builder = new IndentedGeneratorStringBuilder();
        builder.AppendLine($$"""
                             using global::System.Globalization;

                             namespace {{resxNamespace}};

                             public static class LocalizationManager
                             {
                                 public static event EventHandler? CultureChanged;

                                 [field: global::System.Diagnostics.CodeAnalysis.AllowNullAttribute()] [field: global::System.Diagnostics.CodeAnalysis.MaybeNullAttribute()]
                                 public static global::System.Globalization.CultureInfo Culture
                                 {
                                     get => field ??= CultureInfo.CurrentUICulture;
                                     set
                                     {
                                         field = value;
                                         CultureChanged?.Invoke(null, EventArgs.Empty);
                                         CultureInfo.CurrentUICulture = value;
                             """);
        builder.IncreaseIndent(3);
        return builder;
    }

    private static IndentedGeneratorStringBuilder CreateLocalizationProviderBuilder(string resxNamespace)
    {
        var builder = new IndentedGeneratorStringBuilder();
        builder.AppendLine($$"""
                             namespace {{resxNamespace}};

                             public enum LocalizationProvider
                             {
                             """);
        return builder;
    }

    #endregion StringBuilders
}