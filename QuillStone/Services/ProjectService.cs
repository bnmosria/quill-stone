using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class ProjectService : IProjectService
{
    private const string AppName = "QuillStone";

    public ProjectState? CurrentProject { get; private set; }

    public async Task OpenFolderAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder as Project",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        var folder = folders[0];
        string? localPath = folder.TryGetLocalPath();
        string name = DeriveFolderName(localPath) ?? folder.Name;
        CurrentProject = new ProjectState(name, localPath ?? folder.Name);
    }

    public async Task NewProjectAsync(Window owner, IWindowDialogService dialogService)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Project Folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        var folder = folders[0];
        string? localPath = folder.TryGetLocalPath();
        string defaultName = DeriveFolderName(localPath) ?? folder.Name;

        string? name = await dialogService.ShowInputDialogAsync(
            owner,
            AppName,
            "Enter a name for this project:",
            defaultName);

        if (string.IsNullOrWhiteSpace(name))
            return;

        CurrentProject = new ProjectState(name.Trim(), localPath ?? folder.Name);
    }

    private static string? DeriveFolderName(string? localPath)
    {
        if (localPath is null)
            return null;

        string trimmed = localPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrEmpty(trimmed) ? null : Path.GetFileName(trimmed);
    }
}
