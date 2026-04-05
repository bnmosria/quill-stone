using QuillStone.Models;

namespace QuillStone.Services;

public sealed class EditorService : IEditorService
{
    private readonly IMarkdownFormatter _formatter;
    private TextSelectionRange _savedSelection;
    private Avalonia.Controls.TextBox? _editor;

    public EditorService(IMarkdownFormatter formatter)
    {
        _formatter = formatter;
        _savedSelection = new TextSelectionRange(0, 0);
    }

    public void SetEditor(Avalonia.Controls.TextBox editor)
    {
        _editor = editor;
    }

    public string GetEditorText() => _editor?.Text ?? string.Empty;

    public void SetEditorText(string text)
    {
        if (_editor != null)
            _editor.Text = text;
    }

    public int GetCaretIndex() => _editor?.CaretIndex ?? 0;

    public void SetCaretIndex(int index)
    {
        if (_editor != null)
            _editor.CaretIndex = index;
    }

    public void UpdateSelection()
    {
        if (_editor == null)
            return;
        _savedSelection = new TextSelectionRange(_editor.SelectionStart, _editor.SelectionEnd);
    }

    public TextSelectionRange GetSavedSelection() => _savedSelection;

    public void ApplyTextEdit(TextEditResult result)
    {
        if (_editor == null)
            return;

        _editor.Text = result.Text;
        _editor.SelectionStart = result.SelectionStart;
        _editor.SelectionEnd = result.SelectionEnd;
        UpdateSelection();
    }

    public bool HandleEnterKey()
    {
        if (_editor == null)
            return false;

        var edit = ComputeEnterKeyEdit(GetEditorText(), GetCaretIndex(), _formatter);
        if (edit is null)
            return false;

        SetEditorText(edit.Value.NewText);
        SetCaretIndex(edit.Value.NewCaretIndex);
        UpdateSelection();
        return true;
    }

    internal static (string NewText, int NewCaretIndex)? ComputeEnterKeyEdit(
        string text, int cursorPos, IMarkdownFormatter formatter)
    {
        string? nextPrefix = formatter.GetNextListItemPrefix(text, cursorPos);
        if (nextPrefix is null)
            return null;

        int lineStart = cursorPos == 0 ? 0 : text.LastIndexOf('\n', cursorPos - 1) + 1;
        int lineEnd = text.IndexOf('\n', cursorPos);
        if (lineEnd == -1)
            lineEnd = text.Length;

        string currentLine = text[lineStart..lineEnd];
        string contentAfterCursor = text[cursorPos..lineEnd];

        string lineContent = currentLine.TrimStart();
        string afterListMarker = formatter.StripListPrefix(lineContent);

        if (string.IsNullOrWhiteSpace(afterListMarker))
        {
            int lineStartOffset = currentLine.Length - lineContent.Length;
            int deleteUntilPos = lineStart + lineStartOffset;
            string newText = text[..deleteUntilPos] + text[lineEnd..];
            return (newText, deleteUntilPos);
        }

        string insertText = "\n" + nextPrefix;
        string newEditorText = text[..cursorPos] + insertText + contentAfterCursor;
        return (newEditorText, cursorPos + insertText.Length);
    }
}

