using Avalonia.Controls;
using QuillStone.Controllers;

namespace QuillStone.Tests.Integration;

/// <summary>
/// Headless UI tests for <see cref="ViewModeController"/> verifying column widths,
/// control visibility, and toolbar button active states after each view mode switch.
/// </summary>
public sealed class ViewModeControllerTests
{
    private sealed class Fixture
    {
        public Grid Grid { get; }
        public TextBox Editor { get; }
        public Border PreviewPane { get; }
        public GridSplitter Splitter { get; }
        public Button EditorOnlyButton { get; }
        public Button SplitButton { get; }
        public Button FullPreviewButton { get; }
        public MenuItem MenuSplitView { get; }
        public MenuItem MenuFullPreview { get; }
        public List<string> Callbacks { get; } = [];
        public ViewModeController Controller { get; }

        public Fixture()
        {
            Grid = new Grid();
            Grid.ColumnDefinitions.Add(new ColumnDefinition(2, GridUnitType.Star));
            Grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

            Editor = new TextBox();
            PreviewPane = new Border();
            Splitter = new GridSplitter();
            EditorOnlyButton = new Button();
            SplitButton = new Button();
            FullPreviewButton = new Button();
            MenuSplitView = new MenuItem();
            MenuFullPreview = new MenuItem();

            Controller = new ViewModeController();
            Controller.Wire(
                Grid, Editor, PreviewPane, Splitter,
                EditorOnlyButton, SplitButton, FullPreviewButton,
                MenuSplitView, MenuFullPreview,
                onEnterPreview: () => Callbacks.Add("preview"),
                onEnterEditorOnly: () => Callbacks.Add("editorOnly"));
        }
    }

    [AvaloniaFact]
    public void Apply_EditorOnly_SetsEditorVisiblePreviewHidden()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.EditorOnly);

        Assert.Equal(ViewMode.EditorOnly, f.Controller.CurrentMode);
        Assert.Equal(2.0, f.Grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(GridUnitType.Star, f.Grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(0.0, f.Grid.ColumnDefinitions[2].Width.Value);
        Assert.Equal(GridUnitType.Pixel, f.Grid.ColumnDefinitions[2].Width.GridUnitType);
        Assert.True(f.Editor.IsVisible);
        Assert.False(f.PreviewPane.IsVisible);
        Assert.False(f.Splitter.IsVisible);
    }

    [AvaloniaFact]
    public void Apply_Split_SetsBothPanesVisible()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.Split);

        Assert.Equal(ViewMode.Split, f.Controller.CurrentMode);
        Assert.Equal(1.0, f.Grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(GridUnitType.Star, f.Grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(1.0, f.Grid.ColumnDefinitions[2].Width.Value);
        Assert.Equal(GridUnitType.Star, f.Grid.ColumnDefinitions[2].Width.GridUnitType);
        Assert.True(f.Editor.IsVisible);
        Assert.True(f.PreviewPane.IsVisible);
        Assert.True(f.Splitter.IsVisible);
    }

    [AvaloniaFact]
    public void Apply_FullPreview_HidesEditorShowsPreview()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.FullPreview);

        Assert.Equal(ViewMode.FullPreview, f.Controller.CurrentMode);
        Assert.Equal(0.0, f.Grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(GridUnitType.Pixel, f.Grid.ColumnDefinitions[0].Width.GridUnitType);
        Assert.Equal(1.0, f.Grid.ColumnDefinitions[2].Width.Value);
        Assert.Equal(GridUnitType.Star, f.Grid.ColumnDefinitions[2].Width.GridUnitType);
        Assert.False(f.Editor.IsVisible);
        Assert.True(f.PreviewPane.IsVisible);
        Assert.False(f.Splitter.IsVisible);
    }

    [AvaloniaFact]
    public void Apply_EditorOnly_SetsEditorOnlyButtonActive()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.EditorOnly);

        Assert.Contains("ViewModeActive", f.EditorOnlyButton.Classes);
        Assert.DoesNotContain("ViewModeActive", f.SplitButton.Classes);
        Assert.DoesNotContain("ViewModeActive", f.FullPreviewButton.Classes);
    }

    [AvaloniaFact]
    public void Apply_Split_SetsSplitButtonActive()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.Split);

        Assert.DoesNotContain("ViewModeActive", f.EditorOnlyButton.Classes);
        Assert.Contains("ViewModeActive", f.SplitButton.Classes);
        Assert.DoesNotContain("ViewModeActive", f.FullPreviewButton.Classes);
    }

    [AvaloniaFact]
    public void Apply_FullPreview_SetsFullPreviewButtonActive()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.FullPreview);

        Assert.DoesNotContain("ViewModeActive", f.EditorOnlyButton.Classes);
        Assert.DoesNotContain("ViewModeActive", f.SplitButton.Classes);
        Assert.Contains("ViewModeActive", f.FullPreviewButton.Classes);
    }

    [AvaloniaFact]
    public void Apply_Cycle_EditorOnlyToSplitToFullPreviewAndBack()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.EditorOnly);
        Assert.Equal(ViewMode.EditorOnly, f.Controller.CurrentMode);
        Assert.True(f.Editor.IsVisible);
        Assert.False(f.PreviewPane.IsVisible);

        f.Controller.Apply(ViewMode.Split);
        Assert.Equal(ViewMode.Split, f.Controller.CurrentMode);
        Assert.True(f.Editor.IsVisible);
        Assert.True(f.PreviewPane.IsVisible);

        f.Controller.Apply(ViewMode.FullPreview);
        Assert.Equal(ViewMode.FullPreview, f.Controller.CurrentMode);
        Assert.False(f.Editor.IsVisible);
        Assert.True(f.PreviewPane.IsVisible);

        f.Controller.Apply(ViewMode.EditorOnly);
        Assert.Equal(ViewMode.EditorOnly, f.Controller.CurrentMode);
        Assert.True(f.Editor.IsVisible);
        Assert.False(f.PreviewPane.IsVisible);
    }

    [AvaloniaFact]
    public void Apply_Split_InvokesOnEnterPreviewCallback()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.Split);

        Assert.Contains("preview", f.Callbacks);
        Assert.DoesNotContain("editorOnly", f.Callbacks);
    }

    [AvaloniaFact]
    public void Apply_EditorOnly_InvokesOnEnterEditorOnlyCallback()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.EditorOnly);

        Assert.Contains("editorOnly", f.Callbacks);
        Assert.DoesNotContain("preview", f.Callbacks);
    }

    [AvaloniaFact]
    public void Apply_FullPreview_InvokesOnEnterPreviewCallback()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.FullPreview);

        Assert.Contains("preview", f.Callbacks);
        Assert.DoesNotContain("editorOnly", f.Callbacks);
    }

    [AvaloniaFact]
    public void Apply_Split_UpdatesMenuHeaders()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.Split);

        Assert.Equal("✓ _Split View", f.MenuSplitView.Header);
        Assert.Equal("_Full Preview", f.MenuFullPreview.Header);
    }

    [AvaloniaFact]
    public void Apply_FullPreview_UpdatesMenuHeaders()
    {
        var f = new Fixture();

        f.Controller.Apply(ViewMode.FullPreview);

        Assert.Equal("_Split View", f.MenuSplitView.Header);
        Assert.Equal("✓ _Full Preview", f.MenuFullPreview.Header);
    }

    [AvaloniaFact]
    public void Apply_EditorOnly_ClearsMenuCheckmarks()
    {
        var f = new Fixture();
        f.Controller.Apply(ViewMode.Split);

        f.Controller.Apply(ViewMode.EditorOnly);

        Assert.Equal("_Split View", f.MenuSplitView.Header);
        Assert.Equal("_Full Preview", f.MenuFullPreview.Header);
    }
}
