namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateTable(string resxNamespace, LocalizationTableModel table)
    {
        var className = table.ClassName;
        var builder = CreateSourceBuilder();
        builder.AppendLine("using global::System;");
        builder.AppendLine("using global::System.Globalization;");
        builder.AppendLine();
        builder.AppendLine($"namespace {resxNamespace};");
        builder.AppendLine();

        using (builder.Block($"public enum {className}Key : ushort"))
        {
            foreach (var key in table.Keys)
            {
                AppendDocumentation(builder, key.NeutralValue);
                builder.AppendLine($"{key.Identifier},");
            }
        }

        builder.AppendLine();
        using (builder.Block($"public static class {className}"))
        {
            builder.AppendLine($$"""
                                 public static LocalizedString Get({{className}}Key key) => {{className}}Table.Instance.GetLocalizedString((int)key);

                                 public static LocalizedString Get(string key)
                                 {
                                     var index = {{className}}Table.IndexOf(key);
                                     return index < 0 ? new LocalizedString(key) : {{className}}Table.Instance.GetLocalizedString(index);
                                 }
                                 """);
            foreach (var key in table.Keys)
            {
                builder.AppendLine();
                AppendDocumentation(builder, key.NeutralValue);
                if (key.ArgumentCount == 0)
                {
                    builder.AppendLine($"public static LocalizedString {key.Identifier} => Get({className}Key.{key.Identifier});");
                    continue;
                }

                var arguments = Enumerable.Range(0, key.ArgumentCount).Select(static index => $"arg{index}").ToArray();
                var parameters = string.Join(", ", arguments.Select(static argument => $"object? {argument}"));
                builder.AppendLine(
                    $"public static LocalizedString {key.Identifier}({parameters}) => Get({className}Key.{key.Identifier}).Format({string.Join(", ", arguments)});");
            }
        }

        builder.AppendLine();
        using (builder.Block($"internal static partial class {className}Table"))
        {
            builder.AppendLine($"internal static readonly LocalizationTable Instance = new({table.Keys.Length}, Resolve);");
            builder.AppendLine();
            builder.AppendLine("internal static int IndexOf(string key) => key switch");
            builder.AppendLine("{");
            using (builder.Indent())
            {
                foreach (var key in table.Keys)
                {
                    builder.AppendLine($"{key.Name.ToLiteral()} => (int){className}Key.{key.Identifier},");
                }

                builder.AppendLine("_ => -1,");
            }

            builder.AppendLine("};");
            builder.AppendLine();
            AppendResolveMethod(builder, table.Cultures);
            builder.AppendLine();
            AppendValuesClass(builder, "Neutral", table.Keys.Select(static key => key.NeutralValue));
        }

        return builder.ToString();
    }

    private static string GenerateCultureTable(string resxNamespace, LocalizationTableModel table, CultureTableModel culture)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine($"namespace {resxNamespace};");
        builder.AppendLine();

        using (builder.Block($"internal static partial class {table.ClassName}Table"))
        {
            AppendValuesClass(builder, culture.ClassName, culture.Values);
        }

        return builder.ToString();
    }

    private static void AppendResolveMethod(CodeBuilder builder, ImmutableArray<CultureTableModel> cultures)
    {
        if (cultures.IsEmpty)
        {
            builder.AppendLine("private static string[] Resolve(CultureInfo culture) => Neutral.Values;");
            return;
        }

        using (builder.Block("private static string[] Resolve(CultureInfo culture)"))
        {
            using (builder.Block("for (var current = culture; current.Name.Length > 0; current = current.Parent)"))
            {
                for (var index = 0; index < cultures.Length; index++)
                {
                    if (index > 0)
                    {
                        builder.AppendLine();
                    }

                    using (builder.Block($"if (string.Equals(current.Name, {cultures[index].Culture.ToLiteral()}, StringComparison.OrdinalIgnoreCase))"))
                    {
                        builder.AppendLine($"return {cultures[index].ClassName}.Values;");
                    }
                }
            }

            builder.AppendLine();
            builder.AppendLine("return Neutral.Values;");
        }
    }

    private static void AppendValuesClass(CodeBuilder builder, string className, IEnumerable<string> values)
    {
        using (builder.Block($"private static class {className}"))
        {
            builder.AppendLine("internal static readonly string[] Values =");
            builder.AppendLine("[");
            using (builder.Indent())
            {
                foreach (var value in values)
                {
                    builder.AppendLine($"{value.ToLiteral()},");
                }
            }

            builder.AppendLine("];");
        }
    }

    private static void AppendDocumentation(CodeBuilder builder, string value)
    {
        builder.AppendLine("/// <summary>");
        foreach (var line in value.Split('\n'))
        {
            builder.AppendLine($"/// {line.EscapeXmlDoc()}");
        }

        builder.AppendLine("/// </summary>");
    }
}
