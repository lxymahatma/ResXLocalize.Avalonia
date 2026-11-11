namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private const int MaxPositionalArguments = 4;

    private static string GenerateLocalizeExtensions(string resxNamespace, ImmutableArray<LocalizationTableModel> tables, LocalizationTableModel defaultTable)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine($"""
                            using global::System;
                            using global::System.Collections.Generic;
                            using global::Avalonia.Data;
                            using global::Avalonia.Markup.Xaml;
                            using global::Avalonia.Metadata;

                            namespace {resxNamespace}.Extensions.MarkupExtensions;
                            """);

        AppendLocalizeExtension(builder, resxNamespace, "LocalizeExtension", defaultTable);
        foreach (var table in tables.Where(table => table != defaultTable))
        {
            AppendLocalizeExtension(builder, resxNamespace, $"Localize{table.ClassName}Extension", table);
        }

        return builder.ToString();
    }

    private static void AppendLocalizeExtension(CodeBuilder builder, string resxNamespace, string extensionName, LocalizationTableModel table)
    {
        var keyType = $"global::{resxNamespace}.{table.ClassName}Key";
        builder.AppendLine();
        using (builder.Block($"public sealed class {extensionName} : MarkupExtension"))
        {
            builder.AppendLine($$"""
                                 public {{extensionName}}()
                                 {
                                 }

                                 public {{extensionName}}({{keyType}} key) => Key = key;
                                 """);
            for (var count = 1; count <= MaxPositionalArguments; count++)
            {
                var names = Enumerable.Range(0, count).Select(static index => $"arg{index}").ToArray();
                var parameters = string.Join(", ", names.Select(static name => $"BindingBase {name}"));
                builder.AppendLine();
                builder.AppendLine($"public {extensionName}({keyType} key, {parameters}) : this(key) => Arguments.AddRange([{string.Join(", ", names)}]);");
            }

            builder.AppendLine($$"""

                                 public {{keyType}} Key { get; set; }

                                 [Content]
                                 public List<BindingBase> Arguments { get; } = [];

                                 public override BindingBase ProvideValue(IServiceProvider serviceProvider) =>
                                     LocalizedStringBinding.Create(global::{{resxNamespace}}.{{table.ClassName}}.Get(Key), Arguments);
                                 """);
        }
    }
}
