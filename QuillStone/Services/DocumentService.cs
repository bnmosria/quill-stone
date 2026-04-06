using System.IO;
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
        SyncDirtyState(content);

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
            _documentState.SetPersistedContent(content);
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
            _documentState.SetPersistedContent(document.Content);
            MarkDirty(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not open file: {ex.Message}", ex);
        }
    }

    public async Task<bool> RebindCurrentFileAsync(Window owner, string newPath, string content)
    {
        if (CurrentDocument is null)
            return false;

        IStorageFile? file;
        try
        {
            file = await owner.StorageProvider.TryGetFileFromPathAsync(newPath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                $"The file was renamed, but QuillStone could not reconnect to the new path.\n\nDetails: {ex.Message}");
            return false;
        }

        if (file is null)
        {
            await _dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                "The file was renamed, but QuillStone could not reconnect to the new path.");
            return false;
        }

        string localPath = file.TryGetLocalPath() ?? newPath;
        _documentState.SetCurrentFile(file, localPath);
        CurrentDocument = new LoadedDocument(file, localPath, content);
        SyncDirtyState(content);
        return true;
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

    public void SyncDirtyState(string content)
    {
        _documentState.MarkDirty(_documentState.HasUnsavedChanges(content));
    }

    public bool IsCurrentFile(string path) =>
        CurrentDocument?.LocalPath is { } current &&
        string.Equals(Path.GetFullPath(current),
                      Path.GetFullPath(path),
                      StringComparison.OrdinalIgnoreCase);

    public void AcceptExternalReload(string newContent)
    {
        if (CurrentDocument is null)
            return;
        CurrentDocument = new LoadedDocument(CurrentDocument.File, CurrentDocument.LocalPath, newContent);
        _documentState.SetPersistedContent(newContent);
        MarkDirty(false);
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
            _documentState.SetPersistedContent(content);
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

