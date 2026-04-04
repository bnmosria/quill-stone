using Avalonia.Controls;

namespace QuillStone.Services;

public sealed class WindowLifecycleManager : IWindowLifecycleManager
{
    private readonly IDocumentService _documentService;
    private readonly IEditorService _editorService;
    private Window _owner = null!;

    public WindowLifecycleManager(IDocumentService documentService, IEditorService editorService)
    {
        _documentService = documentService;
        _editorService = editorService;
    }

    public void SetOwner(Window owner) => _owner = owner;

    public async Task<bool> HandleClosingAsync()
    {
        return await _documentService.TrySaveIfDirtyAsync(_owner, _editorService.GetEditorText());
    }
}

