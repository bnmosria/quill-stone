using QuillStone.Models;

namespace QuillStone.Services;

public sealed class FormatCommandHandler : IFormatCommandHandler
{
    private readonly IEditorService _editorService;
    private readonly IMarkdownFormatter _formatter;

    public FormatCommandHandler(
        IEditorService editorService,
        IMarkdownFormatter formatter)
    {
        _editorService = editorService;
        _formatter = formatter;
    }

    public void ApplyBold() => ApplyWrap("**", "**", "bold text");

    public void ApplyItalic() => ApplyWrap("*", "*", "italic text");

    public void ApplyInlineCode() => ApplyWrap("`", "`", "code");

    public void ApplyStrikethrough() => ApplyWrap("~~", "~~", "strikethrough");

    public void InsertLink()
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.InsertLink(editorText, _editorService.GetSavedSelection(), "url", "link text");
        _editorService.ApplyTextEdit(result);
    }

    public void ApplyHeading(int level)
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.ApplyHeadingToSelectedLines(editorText, _editorService.GetSavedSelection(), level);
        _editorService.ApplyTextEdit(result);
    }

    public void ApplyBulletList() => ApplyLinePrefix("- ");

    public void ApplyNumberedList()
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.ApplyNumberedListToSelectedLines(editorText, _editorService.GetSavedSelection());
        _editorService.ApplyTextEdit(result);
    }

    public void ApplyBlockquote() => ApplyLinePrefix("> ");

    public void ApplyCheckbox() => ApplyLinePrefix("- [ ] ");

    public void ApplyCodeBlock()
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.InsertFencedCode(editorText, _editorService.GetSavedSelection());
        _editorService.ApplyTextEdit(result);
    }

    public void InsertImage()
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.InsertImage(editorText, _editorService.GetSavedSelection(), "path", "alt text");
        _editorService.ApplyTextEdit(result);
    }

    private void ApplyWrap(string prefix, string suffix, string placeholder)
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.WrapSelection(editorText, _editorService.GetSavedSelection(), prefix, suffix, placeholder);
        _editorService.ApplyTextEdit(result);
    }

    private void ApplyLinePrefix(string prefix)
    {
        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.PrefixSelectedLines(editorText, _editorService.GetSavedSelection(), prefix);
        _editorService.ApplyTextEdit(result);
    }
}
