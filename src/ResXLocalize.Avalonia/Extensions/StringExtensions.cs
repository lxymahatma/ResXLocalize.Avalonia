using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

        public string ToIdentifierCharacters()
        {
            var sanitized = new string(text.Select(static character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
            return sanitized.Length > 0 && char.IsDigit(sanitized[0]) ? "_" + sanitized : sanitized;
        }

        public string GetValidIdentifier()
        {
            var identifier = text.ToIdentifierCharacters();
            return SyntaxFacts.GetKeywordKind(identifier) == SyntaxKind.None ? identifier : "@" + identifier;
        }

        public string ToLiteral() => SymbolDisplay.FormatLiteral(text, true);
    }
}
