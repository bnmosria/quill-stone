using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone;

public partial class MainWindow : Window
{
    private const string AppName = "QuillStone";

    private readonly DocumentState _documentState;
    private readonly IMarkdownFileService _fileService;
    private readonly IWindowDialogService _dialogService;
    private readonly IMarkdownFormatter _markdownFormatter;

    private bool _isUpdatingEditorText;
    private bool _closeConfirmed;
    private bool _closingPromptOpen;
    private TextSelectionRange _savedSelection;

    public MainWindow()
        : this(new DocumentState(), new MarkdownFileService(), new WindowDialogService(), new MarkdownFormatter())
    {
    }

    internal MainWindow(
        DocumentState documentState,
        IMarkdownFileService fileService,
        IWindowDialogService dialogService,
        IMarkdownFormatter markdownFormatter)
    {
        _documentState = documentState;
        _fileService = fileService;
        _dialogService = dialogService;
        _markdownFormatter = markdownFormatter;

        InitializeComponent();
        FormattingToolbar.AddHandler(InputElement.PointerPressedEvent, Toolbar_PointerPressed, RoutingStrategies.Tunnel);
        CaptureEditorSelection();
        UpdateWindowTitle();
    }

    // ── Editor events ────────────────────────────────────────────────────────

    private void Editor_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingEditorText)
            return;

        CaptureEditorSelection();
        MarkDirty(true);
    }

    // ── Toolbar handlers ─────────────────────────────────────────────────────

    private void ToolbarBold_Click(object? sender, RoutedEventArgs e) =>
        ApplyWrapFormatting("**", "**", "bold text");

    private void ToolbarItalic_Click(object? sender, RoutedEventArgs e) =>
        ApplyWrapFormatting("*", "*", "italic text");

    private void ToolbarInlineCode_Click(object? sender, RoutedEventArgs e) =>
        ApplyWrapFormatting("`", "`", "code");

    private async void ToolbarLink_Click(object? sender, RoutedEventArgs e)
    {
        string? url = await _dialogService.ShowInputDialogAsync(this, "Insert Link", "Enter URL:", "https://");
        if (url is null)
            return;

        string editorText = Editor.Text ?? string.Empty;
        TextEditResult result = _markdownFormatter.InsertLink(editorText, _savedSelection, url, "link text");
        ApplyTextEdit(result);
        MarkDirty(true);
        Editor.Focus();
    }

    private void Editor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            CaptureEditorSelection();

            switch (e.Key)
            {
                case Key.B:
                    ApplyWrapFormatting("**", "**", "bold text");
                    e.Handled = true;
                    break;
                case Key.I:
                    ApplyWrapFormatting("*", "*", "italic text");
                    e.Handled = true;
                    break;
            }
        }
    }

    private void ToolbarH1_Click(object? sender, RoutedEventArgs e) =>
        ApplyHeadingFormatting(1);

    private void ToolbarH2_Click(object? sender, RoutedEventArgs e) =>
        ApplyHeadingFormatting(2);

    private void ToolbarH3_Click(object? sender, RoutedEventArgs e) =>
        ApplyHeadingFormatting(3);

    private void ToolbarBulletList_Click(object? sender, RoutedEventArgs e) =>
        ApplyLinePrefixFormatting("- ");

    private void ToolbarNumberedList_Click(object? sender, RoutedEventArgs e) =>
        ApplyLinePrefixFormatting("1. ");

    private void ToolbarBlockquote_Click(object? sender, RoutedEventArgs e) =>
        ApplyLinePrefixFormatting("> ");

    private void ToolbarCheckbox_Click(object? sender, RoutedEventArgs e) =>
        ApplyLinePrefixFormatting("- [ ] ");

    // ── Menu handlers ────────────────────────────────────────────────────────

    private async void MenuNew_Click(object? sender, RoutedEventArgs e)
    {
        if (!await TryPromptToSaveIfDirtyAsync())
            return;

        ClearEditor();
    }

    private async void MenuOpen_Click(object? sender, RoutedEventArgs e)
    {
        if (!await TryPromptToSaveIfDirtyAsync())
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown files") { Patterns = ["*.md"] },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 0)
            return;

        await LoadFromFileAsync(files[0]);
    }

    private async void MenuSave_Click(object? sender, RoutedEventArgs e)
    {
        if (_documentState.CurrentFile is null)
            await SaveAsAsync();
        else
            await SaveToFileAsync(_documentState.CurrentFile);
    }

    private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();

    private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();

    // ── Window closing ───────────────────────────────────────────────────────

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_documentState.IsDirty)
            return;

        e.Cancel = true;

        if (_closingPromptOpen)
            return;

        _closingPromptOpen = true;
        try
        {
            if (await TryPromptToSaveIfDirtyAsync())
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

    // ── Core helpers ─────────────────────────────────────────────────────────

    private void ApplyWrapFormatting(string prefix, string suffix, string placeholder)
    {
        string editorText = Editor.Text ?? string.Empty;
        TextEditResult result = _markdownFormatter.WrapSelection(editorText, _savedSelection, prefix, suffix, placeholder);
        ApplyTextEdit(result);
        MarkDirty(true);
        Editor.Focus();
    }

    private void ApplyLinePrefixFormatting(string prefix)
    {
        string editorText = Editor.Text ?? string.Empty;
        TextEditResult result = _markdownFormatter.PrefixSelectedLines(editorText, _savedSelection, prefix);
        ApplyTextEdit(result);
        MarkDirty(true);
        Editor.Focus();
    }

    private void ApplyHeadingFormatting(int level)
    {
        string editorText = Editor.Text ?? string.Empty;
        TextEditResult result = _markdownFormatter.ApplyHeadingToSelectedLines(editorText, _savedSelection, level);
        ApplyTextEdit(result);
        MarkDirty(true);
        Editor.Focus();
    }

    private void ApplyTextEdit(TextEditResult result)
    {
        _isUpdatingEditorText = true;
        try
        {
            Editor.Text = result.Text;
            Editor.SelectionStart = result.SelectionStart;
            Editor.SelectionEnd = result.SelectionEnd;
            CaptureEditorSelection();
        }
        finally
        {
            _isUpdatingEditorText = false;
        }
    }

    /// <summary>
    /// Prompts the user to save if there are unsaved changes.
    /// Returns true if the caller may proceed (saved, discarded, or nothing was dirty).
    /// Returns false if the user cancelled.
    /// </summary>
    private async Task<bool> TryPromptToSaveIfDirtyAsync()
    {
        if (!_documentState.IsDirty)
            return true;

        var result = await _dialogService.ShowConfirmDialogAsync(
            this,
            AppName,
            $"'{_documentState.DisplayName}' has unsaved changes. Do you want to save before continuing?",
            "Save",
            "Don't Save",
            "Cancel");

        return result switch
        {
            DialogChoice.Primary => await TrySaveAndReportAsync(),
            DialogChoice.Secondary => true,
            _ => false
        };
    }

    /// <summary>
    /// Saves the current document (Save As if no path is set).
    /// Returns true if the save succeeded.
    /// </summary>
    private async Task<bool> TrySaveAndReportAsync()
    {
        if (_documentState.CurrentFile is null)
            return await SaveAsAsync();

        return await SaveToFileAsync(_documentState.CurrentFile);
    }

    private async Task<bool> SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Markdown File",
            SuggestedFileName = _documentState.DisplayName,
            DefaultExtension = "md",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("Markdown files") { Patterns = ["*.md"] },
                FilePickerFileTypes.All
            ]
        });

        if (file is null)
            return false;

        return await SaveToFileAsync(file);
    }

    private async Task<bool> SaveToFileAsync(IStorageFile file)
    {
        try
        {
            string content = Editor.Text ?? string.Empty;
            string? localPath = await _fileService.SaveAsync(file, content);
            _documentState.SetCurrentFile(file, localPath);
            MarkDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(
                this,
                AppName,
                $"Could not save file. Check permissions and try again.\n\nDetails: {ex.Message}");
            return false;
        }
    }

    private async Task LoadFromFileAsync(IStorageFile file)
    {
        try
        {
            LoadedDocument document = await _fileService.LoadAsync(file);

            _isUpdatingEditorText = true;
            Editor.Text = document.Content;
            Editor.SelectionStart = 0;
            Editor.SelectionEnd = 0;
            CaptureEditorSelection();
            _isUpdatingEditorText = false;

            _documentState.SetCurrentFile(document.File, document.LocalPath);
            MarkDirty(false);
            Editor.CaretIndex = 0;
        }
        catch (Exception ex)
        {
            _isUpdatingEditorText = false;
            await _dialogService.ShowMessageDialogAsync(
                this,
                AppName,
                $"Could not open file. Check that the file exists and you have read access.\n\nDetails: {ex.Message}");
        }
    }

    private void ClearEditor()
    {
        _isUpdatingEditorText = true;
        Editor.Clear();
        Editor.SelectionStart = 0;
        Editor.SelectionEnd = 0;
        _isUpdatingEditorText = false;

        _documentState.Reset();
        CaptureEditorSelection();
        UpdateWindowTitle();
    }

    private void MarkDirty(bool dirty)
    {
        _documentState.MarkDirty(dirty);
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle() => Title = _documentState.BuildWindowTitle(AppName);

    private void Toolbar_PointerPressed(object? sender, PointerPressedEventArgs e) => CaptureEditorSelection();

    private void CaptureEditorSelection() => _savedSelection = new TextSelectionRange(Editor.SelectionStart, Editor.SelectionEnd);
}

