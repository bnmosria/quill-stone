using Avalonia.Controls;

namespace QuillStone.Controllers;

internal sealed class ViewModeController
{
    private ViewMode _viewMode = ViewMode.EditorOnly;

    private readonly Grid _editorPreviewGrid;
    private readonly Control _editor;
    private readonly Border _previewPane;
    private readonly Control _previewSplitter;
    private readonly Button _viewEditorOnlyButton;
    private readonly Button _viewSplitButton;
    private readonly Button _viewFullPreviewButton;
    private readonly MenuItem _menuSplitView;
    private readonly MenuItem _menuFullPreview;
    private readonly Action _onEnterPreview;
    private readonly Action _onEnterEditorOnly;

    public ViewMode CurrentMode => _viewMode;

    internal ViewModeController(
        Grid editorPreviewGrid,
        Control editor,
        Border previewPane,
        Control previewSplitter,
        Button viewEditorOnlyButton,
        Button viewSplitButton,
        Button viewFullPreviewButton,
        MenuItem menuSplitView,
        MenuItem menuFullPreview,
        Action onEnterPreview,
        Action onEnterEditorOnly)
    {
        _editorPreviewGrid = editorPreviewGrid;
        _editor = editor;
        _previewPane = previewPane;
        _previewSplitter = previewSplitter;
        _viewEditorOnlyButton = viewEditorOnlyButton;
        _viewSplitButton = viewSplitButton;
        _viewFullPreviewButton = viewFullPreviewButton;
        _menuSplitView = menuSplitView;
        _menuFullPreview = menuFullPreview;
        _onEnterPreview = onEnterPreview;
        _onEnterEditorOnly = onEnterEditorOnly;
    }

    public void Apply(ViewMode mode)
    {
        _viewMode = mode;
        var cols = _editorPreviewGrid.ColumnDefinitions;

        switch (mode)
        {
            case ViewMode.EditorOnly:
                cols[0].Width = new GridLength(2, GridUnitType.Star);
                cols[2].Width = new GridLength(0, GridUnitType.Pixel);
                _editor.IsVisible = true;
                _previewPane.IsVisible = false;
                _previewSplitter.IsVisible = false;
                _onEnterEditorOnly();
                break;

            case ViewMode.Split:
                cols[0].Width = new GridLength(1, GridUnitType.Star);
                cols[2].Width = new GridLength(1, GridUnitType.Star);
                _editor.IsVisible = true;
                _previewPane.IsVisible = true;
                _previewSplitter.IsVisible = true;
                _onEnterPreview();
                break;

            case ViewMode.FullPreview:
                cols[0].Width = new GridLength(0, GridUnitType.Pixel);
                cols[2].Width = new GridLength(1, GridUnitType.Star);
                _editor.IsVisible = false;
                _previewPane.IsVisible = true;
                _previewSplitter.IsVisible = false;
                _onEnterPreview();
                break;
        }

        UpdateViewModeButtons();
        UpdateViewMenuHeaders();
    }

    private void UpdateViewModeButtons()
    {
        SetViewModeActiveClass(_viewEditorOnlyButton, _viewMode == ViewMode.EditorOnly);
        SetViewModeActiveClass(_viewSplitButton, _viewMode == ViewMode.Split);
        SetViewModeActiveClass(_viewFullPreviewButton, _viewMode == ViewMode.FullPreview);
    }

    private static void SetViewModeActiveClass(Button button, bool active)
    {
        if (active && !button.Classes.Contains("ViewModeActive"))
            button.Classes.Add("ViewModeActive");
        else if (!active)
            button.Classes.Remove("ViewModeActive");
    }

    private void UpdateViewMenuHeaders()
    {
        _menuSplitView.Header = _viewMode == ViewMode.Split ? "✓ _Split View" : "_Split View";
        _menuFullPreview.Header = _viewMode == ViewMode.FullPreview ? "✓ _Full Preview" : "_Full Preview";
    }
}
