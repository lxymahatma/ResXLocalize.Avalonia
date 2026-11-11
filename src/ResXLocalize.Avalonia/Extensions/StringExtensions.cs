using System.Diagnostics.CodeAnalysis;

namespace ResXLocalize.Avalonia.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? text) => string.IsNullOrEmpty(text);

    extension(string text)
    {
        public string EscapeXmlDoc() =>
            text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

        public string GetValidIdentifier()
        {
            var sanitized = new string(text.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return sanitized.Length > 0 && char.IsDigit(sanitized[0]) ? "_" + sanitized : sanitized;
        }
    }
}