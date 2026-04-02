using QuillStone.Models;

namespace QuillStone.Services;

public interface IMarkdownFormatter
{
    TextEditResult WrapSelection(string text, TextSelectionRange selection, string prefix, string suffix, string placeholder);
    TextEditResult InsertLink(string text, TextSelectionRange selection, string url, string placeholder);
}

