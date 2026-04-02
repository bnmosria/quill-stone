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

        int lineEnd = text.IndexOf('\n', end);
        if (lineEnd == -1)
            lineEnd = text.Length;

        string block = text[lineStart..lineEnd];
        string[] lines = block.Split('\n');
        string replacement = string.Join('\n', lines.Select(l => string.IsNullOrEmpty(l) ? l : prefix + l));

        string newText = text[..lineStart] + replacement + text[lineEnd..];
        int newCursorPos = lineStart + replacement.Length;

        return new TextEditResult(newText, newCursorPos, newCursorPos);
    }
}

