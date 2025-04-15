using System.Diagnostics.CodeAnalysis;

namespace ResXLocalize.Avalonia.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? text) => string.IsNullOrEmpty(text);

    public static string EscapeXmlDoc(this string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

    public static string GetValidIdentifier(this string text) =>
        new(text.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}