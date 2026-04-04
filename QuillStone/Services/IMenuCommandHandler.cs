using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace QuillStone.Services;

public interface IMenuCommandHandler
{
    void SetOwner(Window owner);
    Task NewDocumentAsync();
    Task OpenDocumentAsync();
    Task SaveDocumentAsync();
    Task SaveDocumentAsAsync();
    Task OpenFileFromPathAsync(string path);
}

