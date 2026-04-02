using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public interface IMarkdownFileService
{
    Task<LoadedDocument> LoadAsync(IStorageFile file);
    Task<string?> SaveAsync(IStorageFile file, string content);
}

