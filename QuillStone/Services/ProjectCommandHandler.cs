using Avalonia.Controls;

namespace QuillStone.Services;

public sealed class ProjectCommandHandler : IProjectCommandHandler
{
    private readonly IDocumentService _documentService;
    private readonly IEditorService _editorService;
    private readonly IProjectService _projectService;
    private readonly IWindowDialogService _dialogService;
    private Window _owner = null!;

    public ProjectCommandHandler(
        IDocumentService documentService,
        IEditorService editorService,
        IProjectService projectService,
        IWindowDialogService dialogService)
    {
        _documentService = documentService;
        _editorService = editorService;
        _projectService = projectService;
        _dialogService = dialogService;
    }

    public void SetOwner(Window owner) => _owner = owner;

    public Task<bool> OpenFolderAsync()
        => SwitchProjectAsync(() => _projectService.OpenFolderAsync(_owner));

    public Task<bool> NewProjectAsync()
        => SwitchProjectAsync(() => _projectService.NewProjectAsync(_owner, _dialogService));

    public async Task<bool> SwitchProjectAsync(Func<Task<bool>> operation)
    {
        if (!await _documentService.TrySaveIfDirtyAsync(_owner, _editorService.GetEditorText()))
            return false;

        if (!await operation())
            return false;

        _documentService.NewDocument();
        _editorService.SetEditorText(string.Empty);
        _editorService.SetCaretIndex(0);
        _editorService.UpdateSelection();
        return true;
    }
}
