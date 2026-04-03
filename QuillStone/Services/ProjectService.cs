using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class ProjectService : IProjectService
{
    private const string AppName = "QuillStone";

    public ProjectState? CurrentProject { get; private set; }

    public async Task<bool> OpenFolderAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder as Project",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return false;

        var folder = folders[0];
        string? localPath = folder.TryGetLocalPath();
        string name = DeriveFolderName(localPath) ?? folder.Name;
        CurrentProject = new ProjectState(name, localPath ?? folder.Name);
        return true;
    }

    public async Task<bool> NewProjectAsync(Window owner, IWindowDialogService dialogService)
    {
        string? name = await dialogService.ShowInputDialogAsync(
            owner,
            AppName,
            "Enter a name for the new project:",
            "MyProject");

        if (string.IsNullOrWhiteSpace(name))
            return false;

        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select where to create the project",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return false;

        var parentFolder = folders[0];
        string? parentPath = parentFolder.TryGetLocalPath();

        if (parentPath is null)
        {
            await dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                "The selected location is not accessible. Please choose a local folder.");
            return false;
        }

        string projectPath = Path.Combine(parentPath, name.Trim());

        try
        {
            Directory.CreateDirectory(projectPath);
        }
        catch (Exception ex)
        {
            await dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                $"Could not create project folder. Check permissions and try again.\n\nDetails: {ex.Message}");
            return false;
        }

        CurrentProject = new ProjectState(name.Trim(), projectPath);
        return true;
    }

    private static string? DeriveFolderName(string? localPath)
    {
        if (localPath is null)
            return null;

        string trimmed = localPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrEmpty(trimmed) ? null : Path.GetFileName(trimmed);
    }

    public void RestoreProject(string name, string rootPath)
        => CurrentProject = new ProjectState(name, rootPath);
}
