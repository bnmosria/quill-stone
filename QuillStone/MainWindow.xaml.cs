using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    private bool _isUpdatingEditorText;
    private bool _closeConfirmed;
    private bool _closingPromptOpen;

    public MainWindow()
        : this(
            new DocumentState(),
            new MarkdownFileService(),
            new WindowDialogService(),
            new MarkdownFormatter())
    {
    }

    internal MainWindow(
        DocumentState documentState,
        IMarkdownFileService fileService,
        IWindowDialogService dialogService,
        IMarkdownFormatter markdownFormatter)
    {
        InitializeComponent();

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

        FormattingToolbar.AddHandler(InputElement.PointerPressedEvent, Toolbar_PointerPressed, RoutingStrategies.Tunnel);
        _editorService.UpdateSelection();
        UpdateWindowTitle();
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
    }

    private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
    {
        await RunWithEditorUpdateGuardAsync(_menuHandler.OpenDocumentAsync);
        UpdateWindowTitle();
    }

    private async void MenuSave_Click(object? sender, RoutedEventArgs e)
    {
        await _menuHandler.SaveDocumentAsync();
        UpdateWindowTitle();
    }

    private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e)
    {
        await _menuHandler.SaveDocumentAsAsync();
        UpdateWindowTitle();
    }

    private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();

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
        Title = $"{_documentService.DisplayName}{(_documentService.IsDirty ? "*" : "")} - QuillStone";
    }

    private void Toolbar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _editorService.UpdateSelection();
    }
}


