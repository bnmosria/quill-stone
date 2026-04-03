using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.IO;
using QuillStone.Models;
using QuillStone.Services;
using QuillStone.ViewModels;

namespace QuillStone;

public partial class MainWindow : Window
{
    private readonly IEditorService _editorService;
    private readonly IDocumentService _documentService;
    private readonly IFormatCommandHandler _formatHandler;
    private readonly IMenuCommandHandler _menuHandler;
    private readonly IWindowLifecycleManager _lifecycleManager;
    private readonly IProjectService _projectService;
    private readonly IWindowDialogService _dialogService;

    private bool _isUpdatingEditorText;
    private bool _closeConfirmed;
    private bool _closingPromptOpen;

    private readonly ObservableCollection<FolderNodeViewModel> _projectRoots = [];

    public MainWindow()
        : this(
            new DocumentState(),
            new MarkdownFileService(),
            new WindowDialogService(),
            new MarkdownFormatter(),
            new ProjectService())
    {
    }

    internal MainWindow(
        DocumentState documentState,
        IMarkdownFileService fileService,
        IWindowDialogService dialogService,
        IMarkdownFormatter markdownFormatter,
        IProjectService projectService)
    {
        InitializeComponent();
        ConfigureWindowChromeForPlatform();

        var editorService = new EditorService(markdownFormatter);
        editorService.SetEditor(Editor);

        var documentService = new DocumentService(fileService, dialogService, documentState);
        var formatHandler = new FormatCommandHandler(editorService, markdownFormatter, dialogService);
        var menuHandler = new MenuCommandHandler(editorService, documentService, dialogService, this);
        var lifecycleManager = new WindowLifecycleManager(documentService, editorService, this);

        _editorService = editorService;
        _documentService = documentService;
        _formatHandler = formatHandler;
        _menuHandler = menuHandler;
        _lifecycleManager = lifecycleManager;
        _projectService = projectService;
        _dialogService = dialogService;

        ProjectTree.ItemsSource = _projectRoots;
        FormattingToolbar.AddHandler(InputElement.PointerPressedEvent, Toolbar_PointerPressed, RoutingStrategies.Tunnel);
        _editorService.UpdateSelection();
        UpdateWindowTitle();
        UpdateMaximizeButtonTooltip();
    }

    // ── Editor events ────────────────────────────────────────────────────────

    private void Editor_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingEditorText)
            return;

        _editorService.UpdateSelection();
        _documentService.SyncDirtyState(_editorService.GetEditorText());
        UpdateWindowTitle();
    }

    private async void Editor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && e.KeyModifiers == KeyModifiers.None)
        {
            if (_editorService.HandleEnterKey())
            {
                _documentService.SyncDirtyState(_editorService.GetEditorText());
                UpdateWindowTitle();
                e.Handled = true;
            }

            return;
        }

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            _editorService.UpdateSelection();

            switch (e.Key)
            {
                case Key.B:
                    _formatHandler.ApplyBold();
                    _documentService.SyncDirtyState(_editorService.GetEditorText());
                    UpdateWindowTitle();
                    e.Handled = true;
                    break;
                case Key.I:
                    _formatHandler.ApplyItalic();
                    _documentService.SyncDirtyState(_editorService.GetEditorText());
                    UpdateWindowTitle();
                    e.Handled = true;
                    break;
                case Key.K:
                    e.Handled = true;
                    await _formatHandler.InsertLinkAsync(this);
                    _documentService.SyncDirtyState(_editorService.GetEditorText());
                    UpdateWindowTitle();
                    break;
                case Key.H:
                    _formatHandler.ApplyHeading(1);
                    _documentService.SyncDirtyState(_editorService.GetEditorText());
                    UpdateWindowTitle();
                    e.Handled = true;
                    break;
            }
        }
    }

    // ── Toolbar handlers ─────────────────────────────────────────────────────

    private void ToolbarBold_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyBold();
        MarkDirty();
    }

    private void ToolbarItalic_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyItalic();
        MarkDirty();
    }

    private void ToolbarInlineCode_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyInlineCode();
        MarkDirty();
    }

    private async void ToolbarLink_Click(object? sender, RoutedEventArgs e)
    {
        await _formatHandler.InsertLinkAsync(this);
        MarkDirty();
    }

    private void ToolbarH1_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyHeading(1);
        MarkDirty();
    }

    private void ToolbarH2_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyHeading(2);
        MarkDirty();
    }

    private void ToolbarH3_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyHeading(3);
        MarkDirty();
    }

    private void ToolbarBulletList_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyBulletList();
        MarkDirty();
    }

    private void ToolbarNumberedList_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyNumberedList();
        MarkDirty();
    }

    private void ToolbarBlockquote_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyBlockquote();
        MarkDirty();
    }

    private void ToolbarCheckbox_Click(object? sender, RoutedEventArgs e)
    {
        _formatHandler.ApplyCheckbox();
        MarkDirty();
    }

    // ── Menu handlers ────────────────────────────────────────────────────────

    private async void MenuNew_Click(object? sender, RoutedEventArgs e)
    {
        await RunWithEditorUpdateGuardAsync(_menuHandler.NewDocumentAsync);
        UpdateWindowTitle();
        RefreshSidebar();
    }

    private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
    {
        await RunWithEditorUpdateGuardAsync(_menuHandler.OpenDocumentAsync);
        UpdateWindowTitle();
        RefreshSidebar();
    }

    private async void SidebarOpenFile_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        await RunWithEditorUpdateGuardAsync(_menuHandler.OpenDocumentAsync);
        RefreshSidebar();
        UpdateWindowTitle();
    }

    private async void SidebarOpenFolder_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (!await TrySwitchProjectAsync(() => _projectService.OpenFolderAsync(this)))
            return;

        RefreshSidebar();
        UpdateWindowTitle();
    }

    private async void MenuSave_Click(object? sender, RoutedEventArgs e)
    {
        await _menuHandler.SaveDocumentAsync();
        UpdateWindowTitle();
        RefreshSidebar();
    }

    private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e)
    {
        await _menuHandler.SaveDocumentAsAsync();
        UpdateWindowTitle();
        RefreshSidebar();
    }

    private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();

    private async void MenuOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (!await TrySwitchProjectAsync(() => _projectService.OpenFolderAsync(this)))
            return;

        RefreshSidebar();
        UpdateWindowTitle();
    }

    private async void MenuNewProject_Click(object? sender, RoutedEventArgs e)
    {
        if (!await TrySwitchProjectAsync(() => _projectService.NewProjectAsync(this, _dialogService)))
            return;

        RefreshSidebar();
        UpdateWindowTitle();
    }

    private void MenuToggleTheme_Click(object? sender, RoutedEventArgs e)
        => QuillStone.Styles.Theme.ThemeManager.Toggle();

    private async void MenuAbout_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new Views.AboutDialog();
        await dialog.ShowDialog(this);
    }

    // ── Window closing ───────────────────────────────────────────────────────

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_documentService.IsDirty)
            return;

        e.Cancel = true;

        if (_closingPromptOpen)
            return;

        _closingPromptOpen = true;
        try
        {
            if (await _lifecycleManager.HandleClosingAsync())
            {
                _closeConfirmed = true;
                Close();
            }
        }
        finally
        {
            _closingPromptOpen = false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RefreshSidebar()
    {
        _projectRoots.Clear();

        if (_projectService.CurrentProject is { } project)
        {
            SidebarNoProjectActions.IsVisible = false;
            SidebarOpenSection.IsVisible = false;
            ProjectTree.IsVisible = true;

            var root = new FolderNodeViewModel(project.ProjectName, project.RootPath);
            root.IsExpanded = true;
            _projectRoots.Add(root);
            return;
        }

        ProjectTree.IsVisible = false;

        if (_documentService.CurrentDocument is not null)
        {
            SidebarNoProjectActions.IsVisible = false;
            SidebarOpenSection.IsVisible = true;
            CurrentFileLabel.Text = _documentService.DisplayName;
            return;
        }

        SidebarNoProjectActions.IsVisible = true;
        SidebarOpenSection.IsVisible = false;
    }

    private async void ProjectTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileNodeViewModel fileNode)
        {
            await RunWithEditorUpdateGuardAsync(() => _menuHandler.OpenFileFromPathAsync(fileNode.FullPath));
            UpdateWindowTitle();
        }

        // Clear selection so every subsequent click always fires a new SelectionChanged,
        // regardless of whether the same item or a folder was previously selected.
        ProjectTree.SelectedItem = null;
    }

    // ── Tree context menu handlers ────────────────────────────────────────────

    private async void FolderContextMenu_NewFile_Click(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;

        var name = await _dialogService.ShowInputDialogAsync(this, "New File", "File name:", "untitled.md");
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            name += ".md";

        var filePath = Path.Combine(folder.FullPath, name);
        if (File.Exists(filePath))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"A file named '{name}' already exists in this folder.");
            return;
        }

        try
        {
            File.WriteAllText(filePath, string.Empty, System.Text.Encoding.UTF8);
            folder.Refresh();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"Could not create file.\n\n{ex.Message}");
        }
    }

    private async void FolderContextMenu_NewFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;

        var name = await _dialogService.ShowInputDialogAsync(this, "New Folder", "Folder name:", "New Folder");
        if (string.IsNullOrWhiteSpace(name))
            return;

        var folderPath = Path.Combine(folder.FullPath, name);
        if (Directory.Exists(folderPath))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"A folder named '{name}' already exists.");
            return;
        }

        try
        {
            Directory.CreateDirectory(folderPath);
            folder.Refresh();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"Could not create folder.\n\n{ex.Message}");
        }
    }

    private async void FolderContextMenu_Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;

        if (IsProjectRoot(folder))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                "Cannot rename the project root folder from the explorer.");
            return;
        }

        if (CurrentOpenFileIsInsideFolder(folder))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                "Cannot rename this folder because it contains the currently open file.");
            return;
        }

        var newName = await _dialogService.ShowInputDialogAsync(this, "Rename Folder", "New name:", folder.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == folder.Name)
            return;

        var parentPath = Path.GetDirectoryName(folder.FullPath);
        if (parentPath is null)
            return;

        var newPath = Path.Combine(parentPath, newName);
        if (Directory.Exists(newPath))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"A folder named '{newName}' already exists.");
            return;
        }

        try
        {
            Directory.Move(folder.FullPath, newPath);
            RefreshFolderOrSidebar(folder.ParentFolder);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"Could not rename folder.\n\n{ex.Message}");
        }
    }

    private async void FolderContextMenu_Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FolderNodeViewModel>(sender) is not { } folder)
            return;

        if (IsProjectRoot(folder))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                "Cannot delete the project root folder from the explorer.");
            return;
        }

        if (CurrentOpenFileIsInsideFolder(folder))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                "Cannot delete this folder because it contains the currently open file.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            this,
            "Delete Folder",
            $"Permanently delete the folder '{folder.Name}' and all its contents?",
            "Delete");

        if (!confirmed)
            return;

        try
        {
            Directory.Delete(folder.FullPath, recursive: true);
            RefreshFolderOrSidebar(folder.ParentFolder);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"Could not delete folder.\n\n{ex.Message}");
        }
    }

    private async void FileContextMenu_Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FileNodeViewModel>(sender) is not { } fileNode)
            return;

        bool isCurrentFile = IsCurrentlyOpenFile(fileNode);

        var newName = await _dialogService.ShowInputDialogAsync(this, "Rename File", "New name:", fileNode.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == fileNode.Name)
            return;

        if (!newName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            newName += ".md";

        var parentPath = Path.GetDirectoryName(fileNode.FullPath);
        if (parentPath is null)
            return;

        var newPath = Path.Combine(parentPath, newName);
        if (File.Exists(newPath))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"A file named '{newName}' already exists.");
            return;
        }

        try
        {
            File.Move(fileNode.FullPath, newPath);

            if (isCurrentFile)
            {
                bool rebound = await _documentService.RebindCurrentFileAsync(this, newPath, _editorService.GetEditorText());
                if (!rebound)
                    return;

                UpdateWindowTitle();
            }

            RefreshFolderOrSidebar(fileNode.ParentFolder);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"Could not rename file.\n\n{ex.Message}");
        }
    }

    private async void FileContextMenu_Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (GetContextMenuNode<FileNodeViewModel>(sender) is not { } fileNode)
            return;

        if (IsCurrentlyOpenFile(fileNode))
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                "Cannot delete the file that is currently open in the editor.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            this,
            "Delete File",
            $"Permanently delete '{fileNode.Name}'?",
            "Delete");

        if (!confirmed)
            return;

        try
        {
            File.Delete(fileNode.FullPath);
            RefreshFolderOrSidebar(fileNode.ParentFolder);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(this, "QuillStone",
                $"Could not delete file.\n\n{ex.Message}");
        }
    }

    private static T? GetContextMenuNode<T>(object? sender) where T : class
    {
        if (sender is not MenuItem menuItem)
            return null;

        if (menuItem.Parent is ContextMenu contextMenu)
            return contextMenu.PlacementTarget?.DataContext as T
                ?? contextMenu.DataContext as T;

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
        if (currentPath is null)
            return false;

        var normalizedCurrent = Path.GetFullPath(currentPath);
        var normalizedFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder.FullPath))
            + Path.DirectorySeparatorChar;

        return normalizedCurrent.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsProjectRoot(FolderNodeViewModel folder)
        => _projectService.CurrentProject is { } project
            && string.Equals(project.RootPath, folder.FullPath, StringComparison.OrdinalIgnoreCase);

    private void RefreshFolderOrSidebar(FolderNodeViewModel? parentFolder)
    {
        if (parentFolder is not null)
            parentFolder.Refresh();
        else
            RefreshSidebar();
    }

    private void MarkDirty()
    {
        _documentService.SyncDirtyState(_editorService.GetEditorText());
        UpdateWindowTitle();
    }

    private async Task RunWithEditorUpdateGuardAsync(Func<Task> operation)
    {
        _isUpdatingEditorText = true;
        try
        {
            await operation();
        }
        finally
        {
            _isUpdatingEditorText = false;
        }
    }

    private async Task<bool> TrySwitchProjectAsync(Func<Task<bool>> operation)
    {
        if (!await _documentService.TrySaveIfDirtyAsync(this, _editorService.GetEditorText()))
            return false;

        bool changed = await operation();
        if (!changed)
            return false;

        await RunWithEditorUpdateGuardAsync(() =>
        {
            _documentService.NewDocument();
            _editorService.SetEditorText(string.Empty);
            _editorService.SetCaretIndex(0);
            _editorService.UpdateSelection();
            return Task.CompletedTask;
        });

        return true;
    }

    private void UpdateWindowTitle()
    {
        string dirtyMark = _documentService.IsDirty ? "*" : string.Empty;
        string docPart = $"{_documentService.DisplayName}{dirtyMark}";
        Title = _projectService.CurrentProject is { } project
            ? $"{docPart} - {project.ProjectName} - QuillStone"
            : $"{docPart} - QuillStone";
    }

    private void Toolbar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _editorService.UpdateSelection();
    }

    private void WindowSurface_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual sourceVisual)
            return;

        if (sourceVisual == Editor || Editor.IsVisualAncestorOf(sourceVisual))
            return;

        if (sourceVisual.FindAncestorOfType<Button>() is not null
            || sourceVisual.FindAncestorOfType<MenuItem>() is not null
            || sourceVisual.FindAncestorOfType<TreeViewItem>() is not null)
        {
            return;
        }

        WindowSurface.Focus();
    }

    // ── Window chrome ─────────────────────────────────────────────────────────

    private void ConfigureWindowChromeForPlatform()
    {
        Classes.Remove("platform-windows");
        Classes.Remove("platform-native");

        if (OperatingSystem.IsWindows())
        {
            ConfigureWindowsChrome();
            return;
        }

        ConfigureNativeChrome();
    }

    private void ConfigureWindowsChrome()
    {
        Classes.Add("platform-windows");
        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;
        TitleBar.IsVisible = true;

        TransparencyLevelHint =
        [
            WindowTransparencyLevel.Mica,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Blur,
            WindowTransparencyLevel.None,
        ];
    }

    private void ConfigureNativeChrome()
    {
        Classes.Add("platform-native");
        SystemDecorations = SystemDecorations.Full;
        ExtendClientAreaToDecorationsHint = false;
        ExtendClientAreaTitleBarHeightHint = 0;
        TitleBar.IsVisible = false;
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CaptionMinimize_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CaptionMaximize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CaptionClose_Click(object? sender, RoutedEventArgs e) => Close();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty)
            UpdateMaximizeButtonTooltip();
    }

    private void UpdateMaximizeButtonTooltip()
    {
        if (MaximizeButton is not null)
            ToolTip.SetTip(MaximizeButton, WindowState == WindowState.Maximized ? "Restore" : "Maximize");
    }
}


