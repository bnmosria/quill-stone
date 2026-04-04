using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using QuillStone.Services;
using QuillStone.ViewModels;

namespace QuillStone.Controllers;

internal sealed class ProjectTreeController
{
    private readonly ObservableCollection<FolderNodeViewModel> _projectRoots = [];
    private readonly TreeView _projectTree; private readonly StackPanel _sidebarNoProjectActions;
    private readonly StackPanel _sidebarOpenSection; private readonly TextBlock _currentFileLabel;
    private readonly IProjectService _projectService; private readonly IDocumentService _documentService;
    private readonly IEditorService _editorService; private readonly IWindowDialogService _dialogService;
    private readonly Window _owner; private readonly Func<string, Task> _onFileOpened;
    private readonly Action _onTitleUpdateNeeded;

    public ObservableCollection<FolderNodeViewModel> ProjectRoots => _projectRoots;

    internal ProjectTreeController(
        TreeView projectTree, StackPanel sidebarNoProjectActions, StackPanel sidebarOpenSection,
        TextBlock currentFileLabel, IProjectService projectService, IDocumentService documentService,
        IEditorService editorService, IWindowDialogService dialogService, Window owner,
        Func<string, Task> onFileOpened, Action onTitleUpdateNeeded)
    {
        _projectTree = projectTree; _sidebarNoProjectActions = sidebarNoProjectActions;
        _sidebarOpenSection = sidebarOpenSection; _currentFileLabel = currentFileLabel;
        _projectService = projectService; _documentService = documentService;
        _editorService = editorService; _dialogService = dialogService;
        _owner = owner; _onFileOpened = onFileOpened; _onTitleUpdateNeeded = onTitleUpdateNeeded;
    }
    public void RefreshSidebar()
    {
        _projectRoots.Clear();
        if (_projectService.CurrentProject is { } project)
        {
            _sidebarNoProjectActions.IsVisible = false; _sidebarOpenSection.IsVisible = false;
            _projectTree.IsVisible = true;
            var root = new FolderNodeViewModel(project.ProjectName, project.RootPath);
            root.IsExpanded = true; _projectRoots.Add(root); return;
        }
        _projectTree.IsVisible = false;
        if (_documentService.CurrentDocument is not null)
        {
            _sidebarNoProjectActions.IsVisible = false; _sidebarOpenSection.IsVisible = true;
            _currentFileLabel.Text = _documentService.DisplayName; return;
        }
        _sidebarNoProjectActions.IsVisible = true; _sidebarOpenSection.IsVisible = false;
    }
    public void RefreshFolderOrSidebar(FolderNodeViewModel? parentFolder)
    {
        if (parentFolder is not null) parentFolder.Refresh();
        else RefreshSidebar();
    }
    public bool IsProjectRoot(FolderNodeViewModel folder)
        => _projectService.CurrentProject is { } project
            && string.Equals(project.RootPath, folder.FullPath, StringComparison.OrdinalIgnoreCase);
    public async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileNodeViewModel fileNode)
            await _onFileOpened(fileNode.FullPath);
        // Clear selection so every subsequent click always fires a new SelectionChanged,
        // regardless of whether the same item or a folder was previously selected.
        _projectTree.SelectedItem = null;
    }
    public async void OnFolderNewFile(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder) return;
        var name = await _dialogService.ShowInputDialogAsync(_owner, "New File", "File name:", "untitled.md");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) name += ".md";
        var filePath = Path.Combine(folder.FullPath, name);
        if (File.Exists(filePath)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A file named '{name}' already exists in this folder."); return; }
        try { File.WriteAllText(filePath, string.Empty, System.Text.Encoding.UTF8); folder.Refresh(); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not create file.\n\n{ex.Message}"); }
    }
    public async void OnFolderNewFolder(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder) return;
        var name = await _dialogService.ShowInputDialogAsync(_owner, "New Folder", "Folder name:", "New Folder");
        if (string.IsNullOrWhiteSpace(name)) return;
        var folderPath = Path.Combine(folder.FullPath, name);
        if (Directory.Exists(folderPath)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A folder named '{name}' already exists."); return; }
        try { Directory.CreateDirectory(folderPath); folder.Refresh(); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not create folder.\n\n{ex.Message}"); }
    }
    public async void OnFolderRename(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder) return;
        if (IsProjectRoot(folder)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot rename the project root folder from the explorer."); return; }
        if (CurrentOpenFileIsInsideFolder(folder)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot rename this folder because it contains the currently open file."); return; }
        var newName = await _dialogService.ShowInputDialogAsync(_owner, "Rename Folder", "New name:", folder.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == folder.Name) return;
        var parentPath = Path.GetDirectoryName(folder.FullPath);
        if (parentPath is null) return;
        var newPath = Path.Combine(parentPath, newName);
        if (Directory.Exists(newPath)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A folder named '{newName}' already exists."); return; }
        try { Directory.Move(folder.FullPath, newPath); RefreshFolderOrSidebar(folder.ParentFolder); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not rename folder.\n\n{ex.Message}"); }
    }
    public async void OnFolderDelete(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder) return;
        if (IsProjectRoot(folder)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot delete the project root folder from the explorer."); return; }
        if (CurrentOpenFileIsInsideFolder(folder)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot delete this folder because it contains the currently open file."); return; }
        var confirmed = await _dialogService.ShowConfirmAsync(
            _owner, "Delete Folder",
            $"Permanently delete the folder '{folder.Name}' and all its contents?", "Delete");
        if (!confirmed) return;
        try { Directory.Delete(folder.FullPath, recursive: true); RefreshFolderOrSidebar(folder.ParentFolder); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not delete folder.\n\n{ex.Message}"); }
    }
    public async void OnFileRename(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FileNodeViewModel>(sender) is not { } fileNode) return;
        bool isCurrentFile = IsCurrentlyOpenFile(fileNode);
        var newName = await _dialogService.ShowInputDialogAsync(_owner, "Rename File", "New name:", fileNode.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == fileNode.Name) return;
        if (!newName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) newName += ".md";
        var parentPath = Path.GetDirectoryName(fileNode.FullPath);
        if (parentPath is null) return;
        var newPath = Path.Combine(parentPath, newName);
        if (File.Exists(newPath)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A file named '{newName}' already exists."); return; }
        try
        {
            File.Move(fileNode.FullPath, newPath);
            if (isCurrentFile)
            {
                bool rebound = await _documentService.RebindCurrentFileAsync(_owner, newPath, _editorService.GetEditorText());
                if (!rebound) return;
                _onTitleUpdateNeeded();
            }
            RefreshFolderOrSidebar(fileNode.ParentFolder);
        }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not rename file.\n\n{ex.Message}"); }
    }
    public async void OnFileDelete(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FileNodeViewModel>(sender) is not { } fileNode) return;
        if (IsCurrentlyOpenFile(fileNode)) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot delete the file that is currently open in the editor."); return; }
        var confirmed = await _dialogService.ShowConfirmAsync(
            _owner, "Delete File", $"Permanently delete '{fileNode.Name}'?", "Delete");
        if (!confirmed) return;
        try { File.Delete(fileNode.FullPath); RefreshFolderOrSidebar(fileNode.ParentFolder); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not delete file.\n\n{ex.Message}"); }
    }
    private static T? GetContextMenuNode<T>(object? sender) where T : class
    {
        if (sender is not MenuItem menuItem) return null;
        if (menuItem.Parent is ContextMenu contextMenu)
            return contextMenu.PlacementTarget?.DataContext as T ?? contextMenu.DataContext as T;
        return menuItem.DataContext as T;
    }
    private bool IsCurrentlyOpenFile(FileNodeViewModel fileNode)
    {
        var currentPath = _documentService.CurrentDocument?.LocalPath;
        return currentPath is not null
            && string.Equals(currentPath, fileNode.FullPath, StringComparison.OrdinalIgnoreCase);
    }
    private bool CurrentOpenFileIsInsideFolder(FolderNodeViewModel folder)
    {
        var currentPath = _documentService.CurrentDocument?.LocalPath;
        if (currentPath is null) return false;
        var normalizedCurrent = Path.GetFullPath(currentPath);
        var normalizedFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder.FullPath))
            + Path.DirectorySeparatorChar;
        return normalizedCurrent.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase);
    }
}
