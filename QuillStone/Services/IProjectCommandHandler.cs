using Avalonia.Controls;

namespace QuillStone.Services;

public interface IProjectCommandHandler
{
    void SetOwner(Window owner);
    Task<bool> OpenFolderAsync();
    Task<bool> NewProjectAsync();
    Task<bool> SwitchProjectAsync(Func<Task<bool>> operation);
}
