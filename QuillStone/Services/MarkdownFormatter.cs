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
}

