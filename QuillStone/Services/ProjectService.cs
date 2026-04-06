using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class ProjectService : IProjectService
{
    private const string AppName = "QuillStone";
    internal const string MarkerDirectory = ".quillstone";
    internal const string MarkerFileName = "project.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ProjectState? CurrentProject { get; private set; }

    public async Task<bool> OpenFolderAsync(Window owner, IWindowDialogService dialogService)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return false;

        var folder = folders[0];
        string? localPath = folder.TryGetLocalPath();

        if (localPath is null)
        {
            CurrentProject = new ProjectState(folder.Name, folder.Name, isProject: false);
            return true;
        }

        string folderName = DeriveFolderName(localPath) ?? folder.Name;

        string? markerName = await TryReadProjectNameAsync(localPath);
        if (markerName is not null)
        {
            CurrentProject = new ProjectState(markerName, localPath, isProject: true);
            return true;
        }

        bool saveAsProject = await dialogService.ShowConfirmAsync(
            owner,
            AppName,
            $"Save \"{folderName}\" as a QuillStone project?",
            "Save as Project");

        if (saveAsProject)
        {
            try
            {
                await WriteProjectMarkerAsync(localPath, folderName);
            }
            catch (Exception ex)
            {
                await dialogService.ShowMessageDialogAsync(
                    owner,
                    AppName,
                    $"Could not create project marker. Opening as plain folder.\n\nDetails: {ex.Message}");
                CurrentProject = new ProjectState(folderName, localPath, isProject: false);
                return true;
            }
            CurrentProject = new ProjectState(folderName, localPath, isProject: true);
        }
        else
        {
            CurrentProject = new ProjectState(folderName, localPath, isProject: false);
        }

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

        try
        {
            await WriteProjectMarkerAsync(projectPath, name.Trim());
        }
        catch (Exception ex)
        {
            await dialogService.ShowMessageDialogAsync(
                owner,
                AppName,
                $"Project folder created but could not write project marker. Check permissions and try again.\n\nDetails: {ex.Message}");
            return false;
        }

        CurrentProject = new ProjectState(name.Trim(), projectPath, isProject: true);
        return true;
    }

    public void RestoreProject(string name, string rootPath, bool isProject)
        => CurrentProject = new ProjectState(name, rootPath, isProject);

    private static async Task<string?> TryReadProjectNameAsync(string rootPath)
    {
        string markerPath = Path.Combine(rootPath, MarkerDirectory, MarkerFileName);
        if (!File.Exists(markerPath))
            return null;

        try
        {
            string json = await File.ReadAllTextAsync(markerPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("name", out var nameEl)
                && nameEl.GetString() is { } n
                && !string.IsNullOrWhiteSpace(n))
                return n;
        }
        catch (IOException) { }
        catch (JsonException) { }

        return null;
    }

    private static async Task WriteProjectMarkerAsync(string rootPath, string projectName)
    {
        string markerDir = Path.Combine(rootPath, MarkerDirectory);
        Directory.CreateDirectory(markerDir);
        string markerPath = Path.Combine(markerDir, MarkerFileName);
        string json = JsonSerializer.Serialize(new { name = projectName }, JsonOptions);
        await File.WriteAllTextAsync(markerPath, json, Encoding.UTF8);
    }

    private static string? DeriveFolderName(string? localPath)
    {
        if (localPath is null)
            return null;

        string trimmed = localPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrEmpty(trimmed) ? null : Path.GetFileName(trimmed);
    }
}
