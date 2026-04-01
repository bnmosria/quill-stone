using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace QuillStone;

public partial class MainWindow : Window
{
    private const string AppName = "Quill-Stone";

    private string? _currentFilePath;
    private bool _isDirty;
    private bool _isUpdatingEditorText;

    public MainWindow()
    {
        InitializeComponent();
        UpdateWindowTitle();
    }

    // ── Editor events ────────────────────────────────────────────────────────

    private void Editor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isUpdatingEditorText)
            return;

        MarkDirty(true);
    }

    // ── Menu handlers ────────────────────────────────────────────────────────

    private void MenuNew_Click(object sender, RoutedEventArgs e)
    {
        if (!TryPromptToSaveIfDirty())
            return;

        ClearEditor();
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        if (!TryPromptToSaveIfDirty())
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Open Markdown File",
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
            DefaultExt = ".md"
        };

        if (dialog.ShowDialog() != true)
            return;

        LoadFromPath(dialog.FileName);
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFilePath is null)
            SaveAs();
        else
            SaveToPath(_currentFilePath);
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e) => SaveAs();

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    // ── Window closing ───────────────────────────────────────────────────────

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!TryPromptToSaveIfDirty())
            e.Cancel = true;
    }

    // ── Core helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Prompts the user to save if there are unsaved changes.
    /// Returns true if the caller may proceed (saved, discarded, or nothing was dirty).
    /// Returns false if the user cancelled.
    /// </summary>
    private bool TryPromptToSaveIfDirty()
    {
        if (!_isDirty)
            return true;

        string docName = _currentFilePath is not null
            ? Path.GetFileName(_currentFilePath)
            : "Untitled";

        var result = MessageBox.Show(
            $"'{docName}' has unsaved changes. Do you want to save before continuing?",
            AppName,
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        return result switch
        {
            MessageBoxResult.Yes    => TrySaveAndReport(),
            MessageBoxResult.No     => true,
            _                       => false   // Cancel
        };
    }

    /// <summary>
    /// Saves the current document (Save As if no path is set).
    /// Returns true if the save succeeded.
    /// </summary>
    private bool TrySaveAndReport()
    {
        if (_currentFilePath is null)
            return SaveAs();

        return SaveToPath(_currentFilePath);
    }

    private bool SaveAs()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Markdown File",
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
            DefaultExt = ".md",
            FileName = _currentFilePath is not null
                ? Path.GetFileName(_currentFilePath)
                : "Untitled.md"
        };

        if (dialog.ShowDialog() != true)
            return false;

        return SaveToPath(dialog.FileName);
    }

    private bool SaveToPath(string path)
    {
        try
        {
            File.WriteAllText(path, Editor.Text, Encoding.UTF8);
            _currentFilePath = path;
            MarkDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save file. Check permissions and try again.\n\nDetails: {ex.Message}",
                AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void LoadFromPath(string path)
    {
        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            _isUpdatingEditorText = true;
            Editor.Text = content;
            _isUpdatingEditorText = false;

            _currentFilePath = path;
            MarkDirty(false);
            Editor.CaretIndex = 0;
            Editor.ScrollToHome();
        }
        catch (Exception ex)
        {
            _isUpdatingEditorText = false;
            MessageBox.Show(
                $"Could not open file. Check that the file exists and you have read access.\n\nDetails: {ex.Message}",
                AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ClearEditor()
    {
        _isUpdatingEditorText = true;
        Editor.Clear();
        _isUpdatingEditorText = false;

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
}