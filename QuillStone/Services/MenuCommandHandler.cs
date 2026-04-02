using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class MenuCommandHandler : IMenuCommandHandler
{
    private const string AppName = "QuillStone";

    private readonly IEditorService _editorService;
    private readonly IDocumentService _documentService;
    private readonly IWindowDialogService _dialogService;
    private readonly Window _owner;

    public MenuCommandHandler(
        IEditorService editorService,
        IDocumentService documentService,
        IWindowDialogService dialogService,
        Window owner)
    {
        _editorService = editorService;
        _documentService = documentService;
        _dialogService = dialogService;
        _owner = owner;
    }

    public async Task NewDocumentAsync()
    {
        if (!await _documentService.TrySaveIfDirtyAsync(_owner, _editorService.GetEditorText()))
            return;

        _documentService.NewDocument();
        _editorService.SetEditorText(string.Empty);
        _editorService.SetCaretIndex(0);
        _editorService.UpdateSelection();
    }

    public async Task OpenDocumentAsync()
    {
        if (!await _documentService.TrySaveIfDirtyAsync(_owner, _editorService.GetEditorText()))
            return;

        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown files") { Patterns = ["*.md"] },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 0)
            return;

        try
        {
            await _documentService.LoadAsync(files[0]);
            var doc = _documentService.CurrentDocument;
            if (doc != null)
            {
                _editorService.SetEditorText(doc.Content);
                _editorService.SetCaretIndex(0);
                _editorService.UpdateSelection();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(
                _owner,
                AppName,
                $"Could not open file. Check that the file exists and you have read access.\n\nDetails: {ex.Message}");
        }
    }

    public async Task SaveDocumentAsync()
    {
        await _documentService.SaveAsync(_owner, _editorService.GetEditorText());
    }

    public async Task SaveDocumentAsAsync()
    {
        await _documentService.SaveAsAsync(_owner, _editorService.GetEditorText());
    }
}

