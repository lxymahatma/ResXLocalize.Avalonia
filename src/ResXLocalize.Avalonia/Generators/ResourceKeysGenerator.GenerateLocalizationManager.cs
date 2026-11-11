namespace ResXLocalize.Avalonia.Generators;

public partial class ResourceKeysGenerator
{
    private static string GenerateLocalizationManager(string resxNamespace, ImmutableArray<LocalizationTableModel> tables)
    {
        var builder = CreateSourceBuilder();
        builder.AppendLine("using global::System;");
        builder.AppendLine("using global::System.Globalization;");
        builder.AppendLine();
        builder.AppendLine($"namespace {resxNamespace};");
        builder.AppendLine();

        using (builder.Block("public static class LocalizationManager"))
        {
            builder.AppendLine("public static event EventHandler? CultureChanged;");
            builder.AppendLine();
            using (builder.Block("public static CultureInfo Culture"))
            {
                builder.AppendLine("get => field ??= CultureInfo.CurrentUICulture;");
                using (builder.Block("set"))
                {
                    builder.AppendLine("field = value;");
                    builder.AppendLine("CultureInfo.CurrentUICulture = value;");
                    foreach (var table in tables)
                    {
                        builder.AppendLine($"{table.ClassName}Table.Instance.SetCulture(value);");
                    }

                    builder.AppendLine("CultureChanged?.Invoke(null, EventArgs.Empty);");
                }
            }
        }

        return builder.ToString();
    }
}
