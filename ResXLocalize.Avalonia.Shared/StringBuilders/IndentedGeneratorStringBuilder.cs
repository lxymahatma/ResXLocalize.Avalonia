using static ResXLocalize.Avalonia.SourceGenerationTexts;

namespace ResXLocalize.Avalonia.StringBuilders;

public sealed class IndentedGeneratorStringBuilder : IndentedStringBuilder
{
    public IndentedGeneratorStringBuilder()
    {
        StringBuilder.AppendLine(Header);
    }

    public override string ToString() => StringBuilder.ToString();
}