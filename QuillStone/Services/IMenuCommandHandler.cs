using Avalonia.Platform.Storage;

namespace QuillStone.Services;

public interface IMenuCommandHandler
{
    Task NewDocumentAsync();
    Task OpenDocumentAsync();
    Task SaveDocumentAsync();
    Task SaveDocumentAsAsync();
}

