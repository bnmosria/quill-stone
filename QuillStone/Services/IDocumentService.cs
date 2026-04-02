using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public interface IDocumentService
{
    bool IsDirty { get; }
    LoadedDocument? CurrentDocument { get; }
    string DisplayName { get; }
    Task<bool> TrySaveIfDirtyAsync(Window owner, string content);
    Task<bool> SaveAsync(Window owner, string content);
    Task<bool> SaveAsAsync(Window owner, string content);
    Task LoadAsync(IStorageFile file);
    void NewDocument();
    void MarkDirty(bool dirty);
}

