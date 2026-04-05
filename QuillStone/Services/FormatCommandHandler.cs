using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class FormatCommandHandler : IFormatCommandHandler
{
    private readonly IEditorService _editorService;
    private readonly IMarkdownFormatter _formatter;
    private readonly IWindowDialogService _dialogService;

    public FormatCommandHandler(
        IEditorService editorService,
        IMarkdownFormatter formatter,
        IWindowDialogService dialogService)
    {
        _editorService = editorService;
        _formatter = formatter;
        _dialogService = dialogService;
    }

    public void ApplyBold() => ApplyWrap("**", "**", "bold text");

    public void ApplyItalic() => ApplyWrap("*", "*", "italic text");

    public void ApplyInlineCode() => ApplyWrap("`", "`", "code");

    public void ApplyStrikethrough() => ApplyWrap("~~", "~~", "strikethrough");

    public async Task InsertLinkAsync(Window owner)
    {
        string? url = await _dialogService.ShowInputDialogAsync(owner, "Insert Link", "Enter URL:", "https://");
        if (url is null)
            return;

        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        TextEditResult result = _formatter.InsertLink(editorText, _editorService.GetSavedSelection(), url, "link text");
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

    public async Task InsertImageAsync(Window owner)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Insert Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image files")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.svg"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 0)
            return;

        string? localPath = files[0].TryGetLocalPath();
        string path = localPath ?? files[0].Name;

        _editorService.UpdateSelection();
        string editorText = _editorService.GetEditorText();
        var selection = _editorService.GetSavedSelection();

        string alt = selection.HasSelection
            ? editorText[selection.NormalizedStart..selection.NormalizedEnd]
            : "alt text";

        string insertion = $"![{alt}]({path})";
        string newText = editorText[..selection.NormalizedStart]
            + insertion
            + editorText[selection.NormalizedEnd..];

        int altStart = selection.NormalizedStart + 2;
        int altEnd = altStart + alt.Length;
        _editorService.ApplyTextEdit(new TextEditResult(newText, altStart, altEnd));
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
