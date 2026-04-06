using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace QuillStone.Controllers;

public sealed class SidebarController
{
    private Grid _sidebarEditorGrid = null!;
    private Border _sidebarContent = null!;
    private Control _sidebarSplitter = null!;
    private Button _toggleButton = null!;
    private PathIcon _toggleIcon = null!;
    private bool _isVisible = true;

    public bool IsVisible => _isVisible;

    internal void Wire(
        Grid sidebarEditorGrid,
        Border sidebarContent,
        Control sidebarSplitter,
        Button toggleButton,
        PathIcon toggleIcon)
    {
        _sidebarEditorGrid = sidebarEditorGrid;
        _sidebarContent = sidebarContent;
        _sidebarSplitter = sidebarSplitter;
        _toggleButton = toggleButton;
        _toggleIcon = toggleIcon;
    }

    public void EnsureVisible()
    {
        if (!_isVisible)
            Toggle();
    }

    public void EnsureHidden()
    {
        if (_isVisible)
            Toggle();
    }

    public void Toggle()
    {
        _isVisible = !_isVisible;
        var cols = _sidebarEditorGrid.ColumnDefinitions;

        if (_isVisible)
        {
            cols[1].Width = new GridLength(210);
            cols[1].MinWidth = 150;
            cols[2].Width = GridLength.Auto;
            _sidebarContent.IsVisible = true;
            _sidebarSplitter.IsVisible = true;
            _toggleIcon.Data = (StreamGeometry?)Application.Current!.FindResource("Icon.SidebarCollapse");
            ToolTip.SetTip(_toggleButton, "Collapse sidebar");
        }
        else
        {
            cols[1].MinWidth = 0;
            cols[1].Width = new GridLength(0);
            cols[2].Width = new GridLength(0);
            _sidebarContent.IsVisible = false;
            _sidebarSplitter.IsVisible = false;
            _toggleIcon.Data = (StreamGeometry?)Application.Current!.FindResource("Icon.SidebarExpand");
            ToolTip.SetTip(_toggleButton, "Expand sidebar");
        }
    }
}
