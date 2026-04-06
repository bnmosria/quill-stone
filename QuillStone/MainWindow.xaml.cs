using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using QuillStone.Controllers;
using QuillStone.Models;
using QuillStone.Services;

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
    private readonly IAppSettingsService _settingsService;

    private bool _isUpdatingEditorText;
    private bool _closeConfirmed;
    private bool _closingPromptOpen;

    private readonly ViewModeController _viewModeController;
    private readonly PreviewController _previewController;
    private readonly ProjectTreeController _projectTreeController;
    private readonly DragDropController _dragDropController;
    private readonly StatusBarController _statusBarController;
    private readonly WindowChromeController _windowChromeController;
    private readonly RecentProjectsController _recentProjectsController;
    private readonly SidebarController _sidebarController;

    public MainWindow(
        IEditorService editorService,
        IDocumentService documentService,
        IFormatCommandHandler formatHandler,
        IMenuCommandHandler menuHandler,
        IWindowLifecycleManager lifecycleManager,
        IProjectService projectService,
        IWindowDialogService dialogService,
        IAppSettingsService settingsService,
        ViewModeController viewModeController,
        PreviewController previewController,
        ProjectTreeController projectTreeController,
        DragDropController dragDropController,
        StatusBarController statusBarController,
        WindowChromeController windowChromeController,
        SidebarController sidebarController)
    {
        InitializeComponent();

        // SetEditor() must be called after InitializeComponent()
        // because Editor is an AXAML-named control
        editorService.SetEditor(Editor);

        _editorService = editorService;
        _documentService = documentService;
        _formatHandler = formatHandler;
        _menuHandler = menuHandler;
        _lifecycleManager = lifecycleManager;
        _projectService = projectService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _viewModeController = viewModeController;
        _previewController = previewController;
        _projectTreeController = projectTreeController;
        _dragDropController = dragDropController;
        _statusBarController = statusBarController;
        _windowChromeController = windowChromeController;
        _sidebarController = sidebarController;

        // Set owner on services that need a Window reference
        _menuHandler.SetOwner(this);
        _lifecycleManager.SetOwner(this);

        // Wire controllers to AXAML controls
        _windowChromeController.Wire(this, TitleBar, MinimizeButton, MaximizeButton, CloseButton);
        _previewController.Wire(PreviewContainer, PreviewPane, this);
        _viewModeController.Wire(
            EditorPreviewGrid, Editor, PreviewPane, PreviewSplitter,
            ViewEditorOnlyButton, ViewSplitButton, ViewFullPreviewButton, MenuSplitView, MenuFullPreview,
            onEnterPreview: _previewController.RenderIfEmpty, onEnterEditorOnly: _previewController.CancelPendingRender);
        _projectTreeController.Wire(
            ProjectTree, SidebarNoProjectActions, SidebarOpenSection, CurrentFileLabel, this,
            onFileOpened: async p =>
            {
                await RunWithEditorUpdateGuardAsync(() => _menuHandler.OpenFileFromPathAsync(p));
                UpdateWindowTitle();
                _previewController.RenderIfVisible();
            },
            onTitleUpdateNeeded: UpdateWindowTitle);
        _dragDropController.Wire(this, onMoveCompleted: _projectTreeController.RefreshFolderOrSidebar, onTitleUpdateNeeded: UpdateWindowTitle);
        _statusBarController.Wire(StatusMeta, StatusWordCount, Editor);
        _sidebarController.Wire(SidebarEditorGrid, SidebarContent, SidebarSplitter, SidebarToggleButton, SidebarToggleIcon);

        _recentProjectsController = new RecentProjectsController(
            RecentProjectsMenuItem, _settingsService, _projectService, _dialogService, this,
            trySwitchProject: TrySwitchProjectAsync,
            resetEditor: () => RunWithEditorUpdateGuardAsync(ResetEditorAsync),
            onProjectOpened: () => { _projectTreeController.RefreshSidebar(); UpdateWindowTitle(); });

        ProjectTree.ItemsSource = _projectTreeController.ProjectRoots;
        ProjectTree.SelectionChanged += _projectTreeController.OnSelectionChanged;
        _dragDropController.Register(ProjectTree);
        Editor.PointerReleased += (_, _) => { _editorService.UpdateSelection(); _statusBarController.UpdateMeta(); };
        Editor.KeyUp += (_, _) => { _editorService.UpdateSelection(); _statusBarController.UpdateMeta(); };
        _editorService.UpdateSelection();
        UpdateWindowTitle();
        _viewModeController.Apply(ViewMode.EditorOnly);
        _statusBarController.UpdateWordCount();
        _windowChromeController.Configure();
        _windowChromeController.OnWindowStateChanged();
        Loaded += async (_, _) => await _recentProjectsController.InitializeAsync();
    }

    private void Editor_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingEditorText)
            return;
        _editorService.UpdateSelection();
        _documentService.SyncDirtyState(_editorService.GetEditorText());
        UpdateWindowTitle();
        _previewController.OnEditorTextChanged();
        _statusBarController.UpdateMeta();
        _statusBarController.UpdateWordCount();
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
        if (e.KeyModifiers != KeyModifiers.Control)
            return;
        _editorService.UpdateSelection();
        switch (e.Key)
        {
            case Key.B:
                _formatHandler.ApplyBold();
                e.Handled = true;
                break;
            case Key.I:
                _formatHandler.ApplyItalic();
                e.Handled = true;
                break;
            case Key.K:
                e.Handled = true;
                await _formatHandler.InsertLinkAsync(this);
                break;
            case Key.H:
                _formatHandler.ApplyHeading(1);
                e.Handled = true;
                break;
        }
    }

    private void ToolbarBold_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyBold();
    private void ToolbarItalic_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyItalic();
    private void ToolbarStrikethrough_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyStrikethrough();
    private void ToolbarInlineCode_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyInlineCode();
    private void ToolbarCodeBlock_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyCodeBlock();
    private async void ToolbarLink_Click(object? s, RoutedEventArgs e) => await _formatHandler.InsertLinkAsync(this);
    private async void ToolbarImage_Click(object? s, RoutedEventArgs e) => await _formatHandler.InsertImageAsync(this);
    private void ToolbarH1_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyHeading(1);
    private void ToolbarH2_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyHeading(2);
    private void ToolbarH3_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyHeading(3);
    private void ToolbarBulletList_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyBulletList();
    private void ToolbarNumberedList_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyNumberedList();
    private void ToolbarBlockquote_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyBlockquote();
    private void ToolbarCheckbox_Click(object? s, RoutedEventArgs e) => _formatHandler.ApplyCheckbox();

    private async void MenuNew_Click(object? sender, RoutedEventArgs e)
    { await RunWithEditorUpdateGuardAsync(_menuHandler.NewDocumentAsync); UpdateWindowTitle(); _previewController.RenderIfVisible(); _projectTreeController.RefreshSidebar(); }
    private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
    { await RunWithEditorUpdateGuardAsync(_menuHandler.OpenDocumentAsync); UpdateWindowTitle(); _previewController.RenderIfVisible(); _projectTreeController.RefreshSidebar(); }
    private async void SidebarOpenFile_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    { await RunWithEditorUpdateGuardAsync(_menuHandler.OpenDocumentAsync); _projectTreeController.RefreshSidebar(); UpdateWindowTitle(); _previewController.RenderIfVisible(); }
    private async void SidebarOpenFolder_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (!await TrySwitchProjectAsync(() => _projectService.OpenFolderAsync(this)))
            return;
        await _recentProjectsController.RecordAndSaveAsync();
        _projectTreeController.RefreshSidebar();
        UpdateWindowTitle();
    }
    private async void MenuSave_Click(object? sender, RoutedEventArgs e)
    { await _menuHandler.SaveDocumentAsync(); UpdateWindowTitle(); _projectTreeController.RefreshSidebar(); }
    private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e)
    { await _menuHandler.SaveDocumentAsAsync(); UpdateWindowTitle(); _projectTreeController.RefreshSidebar(); }
    private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();
    private async void MenuOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (!await TrySwitchProjectAsync(() => _projectService.OpenFolderAsync(this)))
            return;
        await _recentProjectsController.RecordAndSaveAsync();
        _projectTreeController.RefreshSidebar();
        UpdateWindowTitle();
    }
    private async void MenuNewProject_Click(object? sender, RoutedEventArgs e)
    {
        if (!await TrySwitchProjectAsync(() => _projectService.NewProjectAsync(this, _dialogService)))
            return;
        await _recentProjectsController.RecordAndSaveAsync();
        _projectTreeController.RefreshSidebar();
        UpdateWindowTitle();
    }

    private void MenuToggleTheme_Click(object? sender, RoutedEventArgs e)
    { QuillStone.Styles.Theme.ThemeManager.Toggle(); _previewController.RenderIfVisible(); }
    private void MenuSplitView_Click(object? sender, RoutedEventArgs e)
        => _viewModeController.Apply(_viewModeController.CurrentMode == ViewMode.Split ? ViewMode.EditorOnly : ViewMode.Split);
    private void MenuFullPreview_Click(object? sender, RoutedEventArgs e)
        => _viewModeController.Apply(_viewModeController.CurrentMode == ViewMode.FullPreview ? ViewMode.EditorOnly : ViewMode.FullPreview);
    private void MenuPreviewWindow_Click(object? sender, RoutedEventArgs e) => _previewController.TogglePreviewWindow();
    private async void MenuAbout_Click(object? sender, RoutedEventArgs e)
    { await new Views.AboutDialog().ShowDialog(this); }

    private void ViewEditorOnly_Click(object? sender, RoutedEventArgs e) => _viewModeController.Apply(ViewMode.EditorOnly);
    private void ViewSplit_Click(object? sender, RoutedEventArgs e) => _viewModeController.Apply(ViewMode.Split);
    private void ViewFullPreview_Click(object? sender, RoutedEventArgs e) => _viewModeController.Apply(ViewMode.FullPreview);

    private void FolderContextMenu_NewFile_Click(object? sender, RoutedEventArgs e) => _projectTreeController.OnFolderNewFile(sender, e);
    private void FolderContextMenu_NewFolder_Click(object? sender, RoutedEventArgs e) => _projectTreeController.OnFolderNewFolder(sender, e);
    private void FolderContextMenu_Rename_Click(object? sender, RoutedEventArgs e) => _projectTreeController.OnFolderRename(sender, e);
    private void FolderContextMenu_Delete_Click(object? sender, RoutedEventArgs e) => _projectTreeController.OnFolderDelete(sender, e);
    private void FileContextMenu_Rename_Click(object? sender, RoutedEventArgs e) => _projectTreeController.OnFileRename(sender, e);
    private void FileContextMenu_Delete_Click(object? sender, RoutedEventArgs e) => _projectTreeController.OnFileDelete(sender, e);

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
            { _closeConfirmed = true; Close(); }
        }
        finally { _closingPromptOpen = false; }
    }
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e) => _windowChromeController.OnTitleBarPointerPressed(sender, e);
    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e) => _windowChromeController.OnTitleBarDoubleTapped(sender, e);
    private void CaptionMinimize_Click(object? sender, RoutedEventArgs e) => _windowChromeController.OnMinimize(sender, e);
    private void CaptionMaximize_Click(object? sender, RoutedEventArgs e) => _windowChromeController.OnMaximize(sender, e);
    private void CaptionClose_Click(object? sender, RoutedEventArgs e) => _windowChromeController.OnClose(sender, e);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty)
            _windowChromeController.OnWindowStateChanged();
    }
    private async Task RunWithEditorUpdateGuardAsync(Func<Task> op)
    {
        _isUpdatingEditorText = true;
        try
        { await op(); }
        finally { _isUpdatingEditorText = false; }
    }
    private Task ResetEditorAsync()
    {
        _documentService.NewDocument();
        _editorService.SetEditorText(string.Empty);
        _editorService.SetCaretIndex(0);
        _editorService.UpdateSelection();
        return Task.CompletedTask;
    }
    private async Task<bool> TrySwitchProjectAsync(Func<Task<bool>> operation)
    {
        if (!await _documentService.TrySaveIfDirtyAsync(this, _editorService.GetEditorText()))
            return false;
        if (!await operation())
            return false;
        await RunWithEditorUpdateGuardAsync(ResetEditorAsync);
        return true;
    }
    private void UpdateWindowTitle()
    {
        var dirty = _documentService.IsDirty ? "*" : string.Empty;
        var doc = $"{_documentService.DisplayName}{dirty}";
        Title = _projectService.CurrentProject is { } p ? $"{doc} - {p.ProjectName} - QuillStone" : $"{doc} - QuillStone";
    }
    private void SidebarToggle_Click(object? sender, RoutedEventArgs e)
        => _sidebarController.Toggle();
}
