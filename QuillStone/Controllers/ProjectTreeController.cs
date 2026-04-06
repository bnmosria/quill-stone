using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using QuillStone.Services;
using QuillStone.ViewModels;

namespace QuillStone.Controllers;

public sealed class ProjectTreeController
{
    private readonly ObservableCollection<FolderNodeViewModel> _projectRoots = [];
    private TreeView _projectTree = null!;
    private StackPanel _sidebarNoProjectActions = null!;
    private StackPanel _sidebarOpenSection = null!;
    private TextBlock _currentFileLabel = null!;
    private readonly IProjectService _projectService;
    private readonly IDocumentService _documentService;
    private readonly IEditorService _editorService;
    private readonly IWindowDialogService _dialogService;
    private Window _owner = null!;
    private Func<string, Task> _onFileOpened = null!;
    private Action _onTitleUpdateNeeded = null!;
    private FileNodeViewModel? _activeFileNode;
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _externalChangeCts;

    private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".webp",
        ".svg",
    };

    public ObservableCollection<FolderNodeViewModel> ProjectRoots => _projectRoots;

    public ProjectTreeController(
        IProjectService projectService,
        IDocumentService documentService,
        IEditorService editorService,
        IWindowDialogService dialogService)
    {
        _projectService = projectService;
        _documentService = documentService;
        _editorService = editorService;
        _dialogService = dialogService;
    }

    internal void Wire(
        TreeView projectTree,
        StackPanel sidebarNoProjectActions,
        StackPanel sidebarOpenSection,
        TextBlock currentFileLabel,
        Window owner,
        Func<string, Task> onFileOpened,
        Action onTitleUpdateNeeded)
    {
        _projectTree = projectTree;
        _sidebarNoProjectActions = sidebarNoProjectActions;
        _sidebarOpenSection = sidebarOpenSection;
        _currentFileLabel = currentFileLabel;
        _owner = owner;
        _onFileOpened = onFileOpened;
        _onTitleUpdateNeeded = onTitleUpdateNeeded;
    }
    public void RefreshSidebar()
    {
        SetActiveFile(null);
        _projectRoots.Clear();
        if (_projectService.CurrentProject is { } project)
        {
            _sidebarNoProjectActions.IsVisible = false;
            _sidebarOpenSection.IsVisible = false;
            _projectTree.IsVisible = true;
            var root = new FolderNodeViewModel(project.DisplayName, project.RootPath);
            root.IsExpanded = true;
            _projectRoots.Add(root);
            StartWatching(project.RootPath);
            return;
        }
        StopWatching();
        _projectTree.IsVisible = false;
        if (_documentService.CurrentDocument is not null)
        {
            _sidebarNoProjectActions.IsVisible = false;
            _sidebarOpenSection.IsVisible = true;
            _currentFileLabel.Text = _documentService.DisplayName;
            return;
        }
        _sidebarNoProjectActions.IsVisible = true;
        _sidebarOpenSection.IsVisible = false;
    }
    public void RefreshFolderOrSidebar(FolderNodeViewModel? parentFolder)
    {
        if (parentFolder is not null)
            parentFolder.Refresh();
        else
            RefreshSidebar();
    }
    public bool IsProjectRoot(FolderNodeViewModel folder)
        => _projectService.CurrentProject is { } project
            && string.Equals(project.RootPath, folder.FullPath, StringComparison.OrdinalIgnoreCase);
    public async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileNodeViewModel fileNode)
        {
            if (fileNode.FullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                await _onFileOpened(fileNode.FullPath);
                SetActiveFile(fileNode.FullPath);
            }
            else if (IsImageFile(fileNode.FullPath))
            {
                InsertImageSyntax(fileNode.FullPath);
            }
        }
        // Clear selection so every subsequent click always fires a new SelectionChanged,
        // regardless of whether the same item or a folder was previously selected.
        _projectTree.SelectedItem = null;
    }

    private void InsertImageSyntax(string imagePath)
    {
        var currentDocPath = _documentService.CurrentDocument?.LocalPath;
        var syntax = BuildImageSyntax(imagePath, currentDocPath);
        var currentText = _editorService.GetEditorText();
        var caretIndex = _editorService.GetCaretIndex();
        var newText = currentText[..caretIndex] + syntax + currentText[caretIndex..];
        var newCaretPos = caretIndex + syntax.Length;
        _editorService.ApplyTextEdit(new QuillStone.Models.TextEditResult(newText, newCaretPos, newCaretPos));
    }

    private static bool IsImageFile(string path)
        => _imageExtensions.Contains(Path.GetExtension(path));

    internal static string BuildImageSyntax(string imagePath, string? currentDocumentLocalPath)
    {
        var relativePath = BuildRelativePath(imagePath, currentDocumentLocalPath);
        var altText = BuildAltText(Path.GetFileNameWithoutExtension(imagePath));
        return $"![{altText}]({relativePath})";
    }

    internal static string BuildRelativePath(string imagePath, string? currentDocumentLocalPath)
    {
        if (currentDocumentLocalPath is not null)
        {
            var docFolder = Path.GetDirectoryName(currentDocumentLocalPath);
            if (docFolder is not null)
                return Path.GetRelativePath(docFolder, imagePath).Replace(Path.DirectorySeparatorChar, '/');
        }

        return Path.GetFileName(imagePath);
    }

    internal static string BuildAltText(string filenameWithoutExtension)
        => filenameWithoutExtension.Replace('_', ' ').Replace('-', ' ');
    public void OnFolderNewFile(object? sender, RoutedEventArgs e) =>
        FireAndForget(() => OnFolderNewFileAsync(sender, e));
    public void OnFolderNewFolder(object? sender, RoutedEventArgs e) =>
        FireAndForget(() => OnFolderNewFolderAsync(sender, e));
    public void OnFolderRename(object? sender, RoutedEventArgs e) =>
        FireAndForget(() => OnFolderRenameAsync(sender, e));
    public void OnFolderDelete(object? sender, RoutedEventArgs e) =>
        FireAndForget(() => OnFolderDeleteAsync(sender, e));
    public void OnFileRename(object? sender, RoutedEventArgs e) =>
        FireAndForget(() => OnFileRenameAsync(sender, e));
    public void OnFileDelete(object? sender, RoutedEventArgs e) =>
        FireAndForget(() => OnFileDeleteAsync(sender, e));

    public async Task OnFolderNewFileAsync(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;
        var name = await _dialogService.ShowInputDialogAsync(_owner, "New File", "File name:", "untitled.md");
        if (string.IsNullOrWhiteSpace(name))
            return;
        if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            name += ".md";
        var filePath = Path.Combine(folder.FullPath, name);
        if (File.Exists(filePath))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A file named '{name}' already exists in this folder."); return; }
        try
        { File.WriteAllText(filePath, string.Empty, System.Text.Encoding.UTF8); folder.Refresh(); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not create file.\n\n{ex.Message}"); }
    }
    public async Task OnFolderNewFolderAsync(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;
        var name = await _dialogService.ShowInputDialogAsync(_owner, "New Folder", "Folder name:", "New Folder");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var folderPath = Path.Combine(folder.FullPath, name);
        if (Directory.Exists(folderPath))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A folder named '{name}' already exists."); return; }
        try
        { Directory.CreateDirectory(folderPath); folder.Refresh(); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not create folder.\n\n{ex.Message}"); }
    }
    public async Task OnFolderRenameAsync(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;
        if (IsProjectRoot(folder))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot rename the project root folder from the explorer."); return; }
        if (CurrentOpenFileIsInsideFolder(folder))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot rename this folder because it contains the currently open file."); return; }
        var newName = await _dialogService.ShowInputDialogAsync(_owner, "Rename Folder", "New name:", folder.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == folder.Name)
            return;
        var parentPath = Path.GetDirectoryName(folder.FullPath);
        if (parentPath is null)
            return;
        var newPath = Path.Combine(parentPath, newName);
        if (Directory.Exists(newPath))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A folder named '{newName}' already exists."); return; }
        try
        { Directory.Move(folder.FullPath, newPath); RefreshFolderOrSidebar(folder.ParentFolder); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not rename folder.\n\n{ex.Message}"); }
    }
    public async Task OnFolderDeleteAsync(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;
        if (IsProjectRoot(folder))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot delete the project root folder from the explorer."); return; }
        if (CurrentOpenFileIsInsideFolder(folder))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot delete this folder because it contains the currently open file."); return; }
        var confirmed = await _dialogService.ShowConfirmAsync(
            _owner, "Delete Folder",
            $"Permanently delete the folder '{folder.Name}' and all its contents?", "Delete");
        if (!confirmed)
            return;
        try
        { Directory.Delete(folder.FullPath, recursive: true); RefreshFolderOrSidebar(folder.ParentFolder); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not delete folder.\n\n{ex.Message}"); }
    }
    public async Task OnFileRenameAsync(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FileNodeViewModel>(sender) is not { } fileNode)
            return;
        bool isCurrentFile = _documentService.IsCurrentFile(fileNode.FullPath);
        var newName = await _dialogService.ShowInputDialogAsync(_owner, "Rename File", "New name:", fileNode.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == fileNode.Name)
            return;
        if (!newName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            newName += ".md";
        var parentPath = Path.GetDirectoryName(fileNode.FullPath);
        if (parentPath is null)
            return;
        var newPath = Path.Combine(parentPath, newName);
        if (File.Exists(newPath))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"A file named '{newName}' already exists."); return; }
        try
        {
            File.Move(fileNode.FullPath, newPath);
            if (isCurrentFile)
            {
                bool rebound = await _documentService.RebindCurrentFileAsync(_owner, newPath, _editorService.GetEditorText());
                if (!rebound)
                    return;
                _onTitleUpdateNeeded();
            }
            RefreshFolderOrSidebar(fileNode.ParentFolder);
        }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not rename file.\n\n{ex.Message}"); }
    }
    public async Task OnFileDeleteAsync(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FileNodeViewModel>(sender) is not { } fileNode)
            return;
        if (_documentService.IsCurrentFile(fileNode.FullPath))
        { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", "Cannot delete the file that is currently open in the editor."); return; }
        var confirmed = await _dialogService.ShowConfirmAsync(
            _owner, "Delete File", $"Permanently delete '{fileNode.Name}'?", "Delete");
        if (!confirmed)
            return;
        try
        { File.Delete(fileNode.FullPath); RefreshFolderOrSidebar(fileNode.ParentFolder); }
        catch (Exception ex) { await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone", $"Could not delete file.\n\n{ex.Message}"); }
    }
    private static T? GetContextMenuNode<T>(object? sender) where T : class
    {
        if (sender is not MenuItem menuItem)
            return null;
        if (menuItem.Parent is ContextMenu contextMenu)
            return contextMenu.PlacementTarget?.DataContext as T ?? contextMenu.DataContext as T;
        return menuItem.DataContext as T;
    }
    private void SetActiveFile(string? fullPath)
    {
        if (_activeFileNode is not null)
            _activeFileNode.IsActive = false;

        _activeFileNode = fullPath is null ? null :
            _projectRoots
                .SelectMany(r => FindAllFileNodes(r))
                .FirstOrDefault(f => string.Equals(f.FullPath, fullPath,
                    StringComparison.OrdinalIgnoreCase));

        if (_activeFileNode is not null)
            _activeFileNode.IsActive = true;
    }
    private static IEnumerable<FileNodeViewModel> FindAllFileNodes(FolderNodeViewModel folder)
    {
        foreach (var child in folder.Children)
        {
            if (child is FileNodeViewModel file)
                yield return file;
            else if (child is FolderNodeViewModel sub)
                foreach (var f in FindAllFileNodes(sub))
                    yield return f;
        }
    }
    private bool CurrentOpenFileIsInsideFolder(FolderNodeViewModel folder)
    {
        var currentPath = _documentService.CurrentDocument?.LocalPath;
        if (currentPath is null)
            return false;
        var normalizedCurrent = Path.GetFullPath(currentPath);
        var normalizedFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder.FullPath))
            + Path.DirectorySeparatorChar;
        return normalizedCurrent.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase);
    }
    private void StartWatching(string rootPath)
    {
        StopWatching();
        _watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };
        _watcher.Created += OnFileSystemChanged;
        _watcher.Deleted += OnFileSystemChanged;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Changed += OnFileChanged;
    }
    private void StopWatching()
    {
        if (_watcher is null)
            return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
        _externalChangeCts?.Cancel();
        _externalChangeCts?.Dispose();
        _externalChangeCts = null;
    }
    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        _ = Avalonia.Threading.Dispatcher.UIThread
            .InvokeAsync(
                () =>
                {
                    try
                    {
                        RefreshSidebar();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ProjectTreeController] File system refresh failed: {ex.Message}");
                    }
                },
                Avalonia.Threading.DispatcherPriority.Background);
    }
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Atomic-save editors (e.g. VS Code) write to a temp file then rename it over the
        // original, producing a Renamed event instead of a Changed event.  When the
        // rename destination is the currently open file, treat it as an external content
        // change (debounce + reload dialog) rather than a structural tree change — this
        // also prevents the active-file highlight from being needlessly cleared.
        if (_documentService.IsCurrentFile(e.FullPath))
        {
            OnFileChanged(sender, e);
            return;
        }

        // A real structural rename (e.g. another file or folder was renamed by an
        // external tool) — refresh the sidebar tree as normal.
        OnFileSystemChanged(sender, e);
    }
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_documentService.IsCurrentFile(e.FullPath))
            return;

        // Cancel any pending debounce for this file then start a fresh 500 ms window.
        // Capture the local cts so the continuation checks the correct token instance.
        _externalChangeCts?.Cancel();
        _externalChangeCts?.Dispose();
        var cts = new CancellationTokenSource();
        _externalChangeCts = cts;

        _ = Task.Delay(500, cts.Token).ContinueWith(
            t =>
            {
                if (cts.IsCancellationRequested)
                    return;

                // PromptExternalReloadAsync calls ShowDialog which MUST run on the UI thread.
                // Use an async lambda so InvokeAsync properly awaits the full dialog flow.
                _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
                    async () =>
                    {
                        try
                        {
                            await PromptExternalReloadAsync(e.FullPath);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ProjectTreeController] External reload prompt failed: {ex.Message}");
                        }
                    },
                    Avalonia.Threading.DispatcherPriority.Normal);
            },
            TaskScheduler.Default);
    }
    private async Task PromptExternalReloadAsync(string fullPath)
    {
        if (!_documentService.IsCurrentFile(fullPath))
            return;

        string fileName = Path.GetFileName(fullPath);
        string warningLine = _documentService.IsDirty
            ? "Reloading will discard your unsaved changes."
            : "The file on disk has been updated.";
        bool reload = await _dialogService.ShowConfirmAsync(
            _owner,
            "File Changed Externally",
            $"'{fileName}' was modified by another application.\n{warningLine}\n\nReload from disk?",
            "Reload");

        if (!reload)
        {
            // Mark dirty so the user is reminded their in-memory version differs from disk
            _documentService.MarkDirty(true);
            _onTitleUpdateNeeded();
            return;
        }

        string newContent;
        try
        {
            newContent = await ReadFileWithRetryAsync(fullPath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(
                _owner,
                "QuillStone",
                $"Could not read the updated file from disk.\n\n{ex.Message}");
            return;
        }

        _editorService.SetEditorText(newContent);
        _documentService.AcceptExternalReload(newContent);
        _onTitleUpdateNeeded();
    }
    private static async Task<string> ReadFileWithRetryAsync(string path, int retries = 3, int retryDelayMs = 100)
    {
        for (int attempt = 0; attempt < retries; attempt++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (IOException) when (attempt < retries - 1)
            {
                await Task.Delay(retryDelayMs);
            }
        }
        // Final attempt — let exception propagate
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        return await sr.ReadToEndAsync();
    }
    private static void FireAndForget(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            { await action(); }
            catch (Exception ex) { Debug.WriteLine($"[ProjectTreeController] {ex.Message}"); }
        });
    }
}
