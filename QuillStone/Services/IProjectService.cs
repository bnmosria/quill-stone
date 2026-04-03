using Avalonia.Controls;
using QuillStone.Models;

namespace QuillStone.Services;

public interface IProjectService
{
    ProjectState? CurrentProject { get; }
    Task OpenFolderAsync(Window owner);
    Task NewProjectAsync(Window owner, IWindowDialogService dialogService);
}
