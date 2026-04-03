using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
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
        _documentService.MarkDirty(true);
        UpdateWindowTitle();
    }

    private async void Editor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && e.KeyModifiers == KeyModifiers.None)
        {
            if (_editorService.HandleEnterKey())
            {
                _documentService.MarkDirty(true);
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
                    _documentService.MarkDirty(true);
                    UpdateWindowTitle();
                    e.Handled = true;
                    break;
                case Key.I:
                    _formatHandler.ApplyItalic();
                    _documentService.MarkDirty(true);
                    UpdateWindowTitle();
                    e.Handled = true;
                    break;
                case Key.K:
                    e.Handled = true;
                    await _formatHandler.InsertLinkAsync(this);
                    _documentService.MarkDirty(true);
                    UpdateWindowTitle();
                    break;
                case Key.H:
                    _formatHandler.ApplyHeading(1);
                    _documentService.MarkDirty(true);
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

    private async void SidebarHint_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        await RunWithEditorUpdateGuardAsync(_menuHandler.OpenDocumentAsync);
        UpdateWindowTitle();
        RefreshSidebar();
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
        await _projectService.OpenFolderAsync(this);
        RefreshSidebar();
        UpdateWindowTitle();
    }

    private async void MenuNewProject_Click(object? sender, RoutedEventArgs e)
    {
        await _projectService.NewProjectAsync(this, _dialogService);
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
            SidebarNoProjectHint.IsVisible = false;
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
            SidebarNoProjectHint.IsVisible = false;
            SidebarOpenSection.IsVisible = true;
            CurrentFileLabel.Text = _documentService.DisplayName;
            return;
        }

        SidebarNoProjectHint.IsVisible = true;
        SidebarOpenSection.IsVisible = false;
    }

    private async void ProjectTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileNodeViewModel fileNode)
        {
            await RunWithEditorUpdateGuardAsync(() => _menuHandler.OpenFileFromPathAsync(fileNode.FullPath));
            UpdateWindowTitle();
        }
    }

    private void MarkDirty()
    {
        _documentService.MarkDirty(true);
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


