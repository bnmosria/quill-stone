using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class DocumentService : IDocumentService
{
    private const string AppName = "QuillStone";

    private readonly IMarkdownFileService _fileService;
    private readonly IWindowDialogService _dialogService;
    private readonly DocumentState _documentState;

    public bool IsDirty => _documentState.IsDirty;
    public LoadedDocument? CurrentDocument { get; private set; }
    public string DisplayName => _documentState.DisplayName;

    public DocumentService(
        IMarkdownFileService fileService,
        IWindowDialogService dialogService,
        DocumentState documentState)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _documentState = documentState;
    }

    public async Task<bool> TrySaveIfDirtyAsync(Window owner, string content)
    {
        if (!IsDirty)
            return true;

        var result = await _dialogService.ShowConfirmDialogAsync(
            owner,
            AppName,
            $"'{DisplayName}' has unsaved changes. Do you want to save before continuing?",
            "Save",
            "Don't Save",
            "Cancel");

        return result switch
        {
            DialogChoice.Primary => await SaveAsync(owner, content),
            DialogChoice.Secondary => true,
            _ => false
        };
    }

    public async Task<bool> SaveAsync(Window owner, string content)
    {
        try
        {
            if (_documentState.CurrentFile is null)
                return await SaveAsAsync(owner, content);

            string? localPath = await _fileService.SaveAsync(_documentState.CurrentFile, content);
            _documentState.SetCurrentFile(_documentState.CurrentFile, localPath);
            CurrentDocument = new LoadedDocument(_documentState.CurrentFile, localPath, content);
            MarkDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                $"Could not save file. Check permissions and try again.\n\nDetails: {ex.Message}");
            return false;
        }
    }

    public async Task LoadAsync(IStorageFile file)
    {
        try
        {
            LoadedDocument document = await _fileService.LoadAsync(file);
            CurrentDocument = document;
            _documentState.SetCurrentFile(document.File, document.LocalPath);
            MarkDirty(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not open file: {ex.Message}", ex);
        }
    }

    public void NewDocument()
    {
        CurrentDocument = null;
        _documentState.Reset();
        MarkDirty(false);
    }

    public void MarkDirty(bool dirty)
    {
        _documentState.MarkDirty(dirty);
    }

    public async Task<bool> SaveAsAsync(Window owner, string content)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Markdown File",
            SuggestedFileName = DisplayName,
            DefaultExtension = "md",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("Markdown files") { Patterns = ["*.md"] },
                FilePickerFileTypes.All
            ]
        });

        if (file is null)
            return false;

        try
        {
            string? localPath = await _fileService.SaveAsync(file, content);
            _documentState.SetCurrentFile(file, localPath);
            CurrentDocument = new LoadedDocument(file, localPath, content);
            MarkDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                $"Could not save file. Check permissions and try again.\n\nDetails: {ex.Message}");
            return false;
        }
    }
}

