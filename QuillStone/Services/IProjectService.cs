using Avalonia.Controls;
using QuillStone.Models;

namespace QuillStone.Services;

public interface IProjectService
{
    ProjectState? CurrentProject { get; }
    Task<bool> OpenFolderAsync(Window owner);
    Task<bool> NewProjectAsync(Window owner, IWindowDialogService dialogService);
}
