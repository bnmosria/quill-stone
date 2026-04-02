using Avalonia.Controls;

namespace QuillStone.Services;

public sealed class WindowLifecycleManager : IWindowLifecycleManager
{
    private readonly IDocumentService _documentService;
    private readonly IEditorService _editorService;
    private readonly Window _owner;

    public WindowLifecycleManager(IDocumentService documentService, IEditorService editorService, Window owner)
    {
        _documentService = documentService;
        _editorService = editorService;
        _owner = owner;
    }

    public async Task<bool> HandleClosingAsync()
    {
        return await _documentService.TrySaveIfDirtyAsync(_owner, _editorService.GetEditorText());
    }
}

