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
        if (_editor == null || !_editor.IsFocused)
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

        string editorText = GetEditorText();
        int cursorPos = GetCaretIndex();

        string? nextPrefix = _formatter.GetNextListItemPrefix(editorText, cursorPos);
        if (nextPrefix is null)
            return false;

        int lineStart = cursorPos == 0 ? 0 : editorText.LastIndexOf('\n', cursorPos - 1) + 1;
        int lineEnd = editorText.IndexOf('\n', cursorPos);
        if (lineEnd == -1)
            lineEnd = editorText.Length;

        string currentLine = editorText[lineStart..lineEnd];
        string contentAfterCursor = editorText[cursorPos..lineEnd];

        string lineContent = currentLine.TrimStart();
        string afterListMarker = _formatter.StripListPrefix(lineContent);

        if (string.IsNullOrWhiteSpace(afterListMarker))
        {
            int lineStartOffset = currentLine.Length - lineContent.Length;
            int deleteUntilPos = lineStart + lineStartOffset;
            string newText = editorText[..deleteUntilPos] + editorText[lineEnd..];

            SetEditorText(newText);
            SetCaretIndex(deleteUntilPos);
            UpdateSelection();
            return true;
        }

        string insertText = "\n" + nextPrefix;
        string newEditorText = editorText[..cursorPos] + insertText + contentAfterCursor;

        SetEditorText(newEditorText);
        SetCaretIndex(cursorPos + insertText.Length);
        UpdateSelection();
        return true;
    }
}

