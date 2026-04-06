using Avalonia.Controls;
using QuillStone.Models;

namespace QuillStone.Services;

public interface IProjectService
{
    ProjectState? CurrentProject { get; }
    Task<bool> OpenFolderAsync(Window owner, IWindowDialogService dialogService);
    Task<bool> NewProjectAsync(Window owner, IWindowDialogService dialogService);
    void RestoreProject(string name, string rootPath, bool isProject);
}
