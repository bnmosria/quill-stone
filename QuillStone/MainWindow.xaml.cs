using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace QuillStone;

public partial class MainWindow : Window
{
    private const string AppName = "QuillStone";

    private IStorageFile? _currentFile;
    private string? _currentFilePath;
    private bool _isDirty;
    private bool _isUpdatingEditorText;
    private bool _closeConfirmed;
    private bool _closingPromptOpen;

    public MainWindow()
    {
        InitializeComponent();
        UpdateWindowTitle();
    }

    // ── Editor events ────────────────────────────────────────────────────────

    private void Editor_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingEditorText)
            return;

        MarkDirty(true);
    }

    // ── Toolbar handlers ─────────────────────────────────────────────────────

    private void ToolbarBold_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarItalic_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarInlineCode_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarLink_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarH1_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarH2_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarH3_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarBulletList_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarNumberedList_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarBlockquote_Click(object? sender, RoutedEventArgs e) { }

    private void ToolbarCheckbox_Click(object? sender, RoutedEventArgs e) { }

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
        if (_currentFile is null)
            await SaveAsAsync();
        else
            await SaveToFileAsync(_currentFile);
    }

    private async void MenuSaveAs_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();

    private void MenuExit_Click(object? sender, RoutedEventArgs e) => Close();

    // ── Window closing ───────────────────────────────────────────────────────

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_isDirty)
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

    /// <summary>
    /// Prompts the user to save if there are unsaved changes.
    /// Returns true if the caller may proceed (saved, discarded, or nothing was dirty).
    /// Returns false if the user cancelled.
    /// </summary>
    private async Task<bool> TryPromptToSaveIfDirtyAsync()
    {
        if (!_isDirty)
            return true;

        string docName = _currentFilePath is not null
            ? Path.GetFileName(_currentFilePath)
            : "Untitled";

        var result = await ShowConfirmDialogAsync(
            AppName,
            $"'{docName}' has unsaved changes. Do you want to save before continuing?",
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
        if (_currentFile is null)
            return await SaveAsAsync();

        return await SaveToFileAsync(_currentFile);
    }

    private async Task<bool> SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Markdown File",
            SuggestedFileName = _currentFilePath is not null
                ? Path.GetFileName(_currentFilePath)
                : "Untitled",
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
            string? localPath = file.TryGetLocalPath();

            if (localPath is not null)
            {
                await File.WriteAllTextAsync(localPath, Editor.Text ?? string.Empty, Encoding.UTF8);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                if (stream.CanSeek)
                    stream.SetLength(0);

                await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false);
                await writer.WriteAsync(Editor.Text ?? string.Empty);
                await writer.FlushAsync();
            }

            _currentFile = file;
            _currentFilePath = localPath ?? file.Name;
            MarkDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            await ShowMessageDialogAsync(
                AppName,
                $"Could not save file. Check permissions and try again.\n\nDetails: {ex.Message}");
            return false;
        }
    }

    private async Task LoadFromFileAsync(IStorageFile file)
    {
        try
        {
            string content;
            string? localPath = file.TryGetLocalPath();

            if (localPath is not null)
            {
                content = await File.ReadAllTextAsync(localPath, Encoding.UTF8);
            }
            else
            {
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                content = await reader.ReadToEndAsync();
            }

            _isUpdatingEditorText = true;
            Editor.Text = content;
            _isUpdatingEditorText = false;

            _currentFile = file;
            _currentFilePath = localPath ?? file.Name;
            MarkDirty(false);
            Editor.CaretIndex = 0;
        }
        catch (Exception ex)
        {
            _isUpdatingEditorText = false;
            await ShowMessageDialogAsync(
                AppName,
                $"Could not open file. Check that the file exists and you have read access.\n\nDetails: {ex.Message}");
        }
    }

    private void ClearEditor()
    {
        _isUpdatingEditorText = true;
        Editor.Clear();
        _isUpdatingEditorText = false;

        _currentFile = null;
        _currentFilePath = null;
        MarkDirty(false);
    }

    private void MarkDirty(bool dirty)
    {
        _isDirty = dirty;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        string docName = _currentFilePath is not null
            ? Path.GetFileName(_currentFilePath)
            : "Untitled";

        string dirtyMark = _isDirty ? "*" : string.Empty;
        Title = $"{docName}{dirtyMark} - {AppName}";
    }

    private enum DialogChoice
    {
        Primary,
        Secondary,
        Cancel
    }

    private async Task<DialogChoice> ShowConfirmDialogAsync(
        string title,
        string message,
        string primaryButton,
        string secondaryButton,
        string cancelButton)
    {
        var dialog = CreateDialogWindow(title);
        var result = DialogChoice.Cancel;

        void CloseWith(DialogChoice choice)
        {
            result = choice;
            dialog.Close();
        }

        var primary = new Button { Content = primaryButton, MinWidth = 96 };
        var secondary = new Button { Content = secondaryButton, MinWidth = 96 };
        var cancel = new Button { Content = cancelButton, MinWidth = 96 };

        primary.Click += (_, _) => CloseWith(DialogChoice.Primary);
        secondary.Click += (_, _) => CloseWith(DialogChoice.Secondary);
        cancel.Click += (_, _) => CloseWith(DialogChoice.Cancel);

        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { primary, secondary, cancel }
                }
            }
        };

        await dialog.ShowDialog(this);
        return result;
    }

    private async Task ShowMessageDialogAsync(string title, string message)
    {
        var dialog = CreateDialogWindow(title);

        var ok = new Button { Content = "OK", MinWidth = 96 };
        ok.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { ok }
                }
            }
        };

        await dialog.ShowDialog(this);
    }

    private static Window CreateDialogWindow(string title) => new()
    {
        Title = title,
        Width = 520,
        MinWidth = 420,
        SizeToContent = SizeToContent.Height,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ShowInTaskbar = false
    };
}