using System.IO;
using Avalonia.Controls;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Controllers;

internal sealed class RecentProjectsController
{
    private readonly MenuItem _menuItem;
    private readonly IAppSettingsService _settingsService;
    private readonly IProjectService _projectService;
    private readonly IWindowDialogService _dialogService;
    private readonly Window _owner;
    private readonly Func<Func<Task<bool>>, Task<bool>> _trySwitchProject;
    private readonly Func<Task> _resetEditor;
    private readonly Action _onProjectOpened;

    internal RecentProjectsController(
        MenuItem menuItem,
        IAppSettingsService settingsService,
        IProjectService projectService,
        IWindowDialogService dialogService,
        Window owner,
        Func<Func<Task<bool>>, Task<bool>> trySwitchProject,
        Func<Task> resetEditor,
        Action onProjectOpened)
    {
        _menuItem = menuItem;
        _settingsService = settingsService;
        _projectService = projectService;
        _dialogService = dialogService;
        _owner = owner;
        _trySwitchProject = trySwitchProject;
        _resetEditor = resetEditor;
        _onProjectOpened = onProjectOpened;
    }

    internal async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        Populate();
        await TryRestoreLastProjectAsync();
    }

    internal async Task RecordAndSaveAsync()
    {
        if (_projectService.CurrentProject is not { } project)
            return;
        _settingsService.RecordProject(project.ProjectName, project.RootPath);
        try
        {
            await _settingsService.SaveAsync();
        }
        catch { /* settings save failures are non-fatal */ }
        Populate();
    }

    internal void Populate()
    {
        _menuItem.Items.Clear();
        var recent = _settingsService.Settings.RecentProjects;
        if (recent.Count == 0)
        {
            _menuItem.Items.Add(new MenuItem { Header = "(No recent projects)", IsEnabled = false });
            return;
        }
        foreach (var project in recent)
        {
            var capturedProject = project;
            var item = new MenuItem { Header = project.Name };
            ToolTip.SetTip(item, project.Path);
            item.Click += async (_, _) =>
            {
                if (!await _trySwitchProject(async () =>
                {
                    if (!Directory.Exists(capturedProject.Path))
                    {
                        await _dialogService.ShowMessageDialogAsync(
                            _owner, "QuillStone", $"The project folder no longer exists:\n{capturedProject.Path}");
                        _settingsService.Settings.RecentProjects.RemoveAll(p =>
                            string.Equals(p.Path, capturedProject.Path, StringComparison.OrdinalIgnoreCase));
                        try
                        {
                            await _settingsService.SaveAsync();
                        }
                        catch { /* non-fatal */ }
                        Populate();
                        return false;
                    }
                    _projectService.RestoreProject(capturedProject.Name, capturedProject.Path);
                    return true;
                }))
                    return;
                await RecordAndSaveAsync();
                _onProjectOpened();
            };
            _menuItem.Items.Add(item);
        }
    }

    private async Task TryRestoreLastProjectAsync()
    {
        var path = _settingsService.Settings.LastOpenedProjectPath;
        if (path is null || !Directory.Exists(path))
            return;
        var recent = _settingsService.Settings.RecentProjects
            .FirstOrDefault(p => string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase));
        string name =
            recent?.Name
            ?? Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            ?? path;
        _projectService.RestoreProject(name, path);
        await _resetEditor();
        _onProjectOpened();
    }
}
