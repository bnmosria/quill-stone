using System.Text.RegularExpressions;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class MarkdownFormatter : IMarkdownFormatter
{
    public TextEditResult WrapSelection(string text, TextSelectionRange selection, string prefix, string suffix, string placeholder)
    {
        int start = selection.NormalizedStart;
        int end = selection.NormalizedEnd;
        bool hasSelection = selection.HasSelection;

        string inner = hasSelection ? text[start..end] : placeholder;
        string replacement = prefix + inner + suffix;
        string updatedText = text[..start] + replacement + text[end..];

        return hasSelection
            ? new TextEditResult(updatedText, start + replacement.Length, start + replacement.Length)
            : new TextEditResult(updatedText, start + prefix.Length, start + prefix.Length + placeholder.Length);
    }

    public TextEditResult InsertLink(string text, TextSelectionRange selection, string url, string placeholder)
    {
        int start = selection.NormalizedStart;
        int end = selection.NormalizedEnd;
        bool hasSelection = selection.HasSelection;

        string inner = hasSelection ? text[start..end] : placeholder;
        string replacement = $"[{inner}]({url})";
        string updatedText = text[..start] + replacement + text[end..];

        return hasSelection
            ? new TextEditResult(updatedText, start + replacement.Length, start + replacement.Length)
            : new TextEditResult(updatedText, start + 1, start + 1 + inner.Length);
    }

    public TextEditResult PrefixSelectedLines(string text, TextSelectionRange selection, string prefix)
    {
        int start = selection.NormalizedStart;
        int end = selection.NormalizedEnd;

        int lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;

        int effectiveEnd = (end > start && end > 0 && text[end - 1] == '\n') ? end - 1 : end;
        int lineEnd = text.IndexOf('\n', effectiveEnd);
        if (lineEnd == -1)
            lineEnd = text.Length;

        string block = text[lineStart..lineEnd];
        string[] lines = block.Split('\n');
        string replacement = string.Join('\n', lines.Select(l => string.IsNullOrWhiteSpace(l) ? l : prefix + StripLinePrefix(l)));

        string newText = text[..lineStart] + replacement + text[lineEnd..];
        int newCursorPos = lineStart + replacement.Length;

        return new TextEditResult(newText, newCursorPos, newCursorPos);
    }

    public TextEditResult ApplyHeadingToSelectedLines(string text, TextSelectionRange selection, int level)
    {
        if (level is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(level));

        int start = selection.NormalizedStart;
        int end = selection.NormalizedEnd;

        int lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;

        int effectiveEnd = (end > start && end > 0 && text[end - 1] == '\n') ? end - 1 : end;
        int lineEnd = text.IndexOf('\n', effectiveEnd);
        if (lineEnd == -1)
            lineEnd = text.Length;

        string block = text[lineStart..lineEnd];
        string[] lines = block.Split('\n');
        string replacement = string.Join('\n', lines.Select(line => ReplaceHeading(line, level)));

        string newText = text[..lineStart] + replacement + text[lineEnd..];
        int newCursorPos = lineStart + replacement.Length;

        return new TextEditResult(newText, newCursorPos, newCursorPos);
    }

    // Ordered by longest match first so "- [ ] " is checked before "- ".
    private static readonly string[] LinePrefixes =
    [
        "- [ ] ", "- [x] ", "> ", "- ", "* ", "+ ",
    ];

    private static readonly Regex OrderedListPrefix = new(@"^\d+\.\s+", RegexOptions.Compiled);

    private static string StripLinePrefix(string line)
    {
        foreach (string p in LinePrefixes)
        {
            if (line.StartsWith(p, StringComparison.Ordinal))
                return line[p.Length..];
        }

        Match m = OrderedListPrefix.Match(line);
        if (m.Success)
            return line[m.Length..];

        return line;
    }

    private static string ReplaceHeading(string line, int level)
    {
        if (string.IsNullOrWhiteSpace(line))
            return line;

        int contentStart = 0;
        while (contentStart < line.Length && (line[contentStart] == ' ' || line[contentStart] == '\t'))
            contentStart++;

        string indent = line[..contentStart];
        string content = StripAtxHeading(line[contentStart..]);

        return $"{indent}{new string('#', level)} {content}";
    }

    private static string StripAtxHeading(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] != '#')
            return value;

        int hashCount = 0;
        while (hashCount < value.Length && hashCount < 6 && value[hashCount] == '#')
            hashCount++;

        if (hashCount == 0 || (hashCount < value.Length && value[hashCount] != ' '))
            return value;

        int bodyStart = hashCount;
        while (bodyStart < value.Length && value[bodyStart] == ' ')
            bodyStart++;

        return value[bodyStart..];
    }

    public TextEditResult ApplyNumberedListToSelectedLines(string text, TextSelectionRange selection)
    {
        int start = selection.NormalizedStart;
        int end = selection.NormalizedEnd;

        int lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;

        int effectiveEnd = (end > start && end > 0 && text[end - 1] == '\n') ? end - 1 : end;
        int lineEnd = text.IndexOf('\n', effectiveEnd);
        if (lineEnd == -1)
            lineEnd = text.Length;

        string block = text[lineStart..lineEnd];
        string[] lines = block.Split('\n');

        int listNumber = 1;
        string[] formatted = lines.Select(l =>
        {
            if (string.IsNullOrWhiteSpace(l))
                return l;

            string stripped = StripLinePrefix(l);
            return $"{listNumber++}. {stripped}";
        }).ToArray();

        string replacement = string.Join('\n', formatted);
        string newText = text[..lineStart] + replacement + text[lineEnd..];
        int newCursorPos = lineStart + replacement.Length;

        return new TextEditResult(newText, newCursorPos, newCursorPos);
    }

    public string? GetNextListItemPrefix(string text, int cursorPosition)
    {
        if (cursorPosition < 0 || cursorPosition > text.Length)
            return null;

        int lineStart = cursorPosition == 0 ? 0 : text.LastIndexOf('\n', cursorPosition - 1) + 1;
        int lineEnd = text.IndexOf('\n', cursorPosition);
        if (lineEnd == -1)
            lineEnd = text.Length;

        string fullLine = text[lineStart..lineEnd];
        string lineContent = fullLine.TrimStart();

        if (string.IsNullOrWhiteSpace(lineContent))
            return null;

        Match orderedMatch = OrderedListPrefix.Match(lineContent);
        if (orderedMatch.Success)
        {
            string numPart = lineContent[..orderedMatch.Length].Trim();
            if (int.TryParse(numPart.TrimEnd('.'), out int number))
                return $"{number + 1}. ";
        }

        if (lineContent.StartsWith("- ", StringComparison.Ordinal))
            return "- ";
        if (lineContent.StartsWith("* ", StringComparison.Ordinal))
            return "* ";
        if (lineContent.StartsWith("+ ", StringComparison.Ordinal))
            return "+ ";
        if (lineContent.StartsWith("> ", StringComparison.Ordinal))
            return "> ";
        if (lineContent.StartsWith("- [ ] ", StringComparison.Ordinal))
            return "- [ ] ";
        if (lineContent.StartsWith("- [x] ", StringComparison.Ordinal))
            return "- [ ] ";

        return null;
    }

    public string StripListPrefix(string line)
    {
        return StripLinePrefix(line);
    }
}
