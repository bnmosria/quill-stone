using QuillStone.Models;

namespace QuillStone.Services;

public interface IMarkdownFormatter
{
    TextEditResult WrapSelection(string text, TextSelectionRange selection, string prefix, string suffix, string placeholder);
    TextEditResult InsertLink(string text, TextSelectionRange selection, string url, string placeholder);
    TextEditResult PrefixSelectedLines(string text, TextSelectionRange selection, string prefix);
    TextEditResult ApplyHeadingToSelectedLines(string text, TextSelectionRange selection, int level);
    TextEditResult ApplyNumberedListToSelectedLines(string text, TextSelectionRange selection);
    string? GetNextListItemPrefix(string text, int cursorPosition);
    string StripListPrefix(string line);
    TextEditResult InsertFencedCode(string text, TextSelectionRange selection, string language = "");
}
