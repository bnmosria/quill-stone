using QuillStone.Models;

namespace QuillStone.Services;

public interface IEditorService
{
    string GetEditorText();
    void SetEditorText(string text);
    int GetCaretIndex();
    void SetCaretIndex(int index);
    void UpdateSelection();
    TextSelectionRange GetSavedSelection();
    void ApplyTextEdit(TextEditResult result);
    bool HandleEnterKey();
}

