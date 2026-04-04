using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace QuillStone.Controllers;

internal sealed class WindowChromeController
{
    private Window _owner = null!;
    private Grid _titleBar = null!;
    private Button _maximizeButton = null!;

    internal void Wire(
        Window owner,
        Grid titleBar,
        Button minimizeButton,
        Button maximizeButton,
        Button closeButton)
    {
        _owner = owner;
        _titleBar = titleBar;
        _maximizeButton = maximizeButton;
        _ = minimizeButton;
        _ = closeButton;
    }

    public void Configure()
    {
        _owner.Classes.Remove("platform-windows");
        _owner.Classes.Remove("platform-native");

        if (OperatingSystem.IsWindows())
        {
            ConfigureWindowsChrome();
            return;
        }

        ConfigureNativeChrome();
    }

    public void OnWindowStateChanged()
    {
        if (_maximizeButton is not null)
            ToolTip.SetTip(_maximizeButton, _owner.WindowState == WindowState.Maximized ? "Restore" : "Maximize");
    }

    public void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed)
            _owner.BeginMoveDrag(e);
    }

    public void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        _owner.WindowState = _owner.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public void OnMinimize(object? sender, RoutedEventArgs e)
        => _owner.WindowState = WindowState.Minimized;

    public void OnMaximize(object? sender, RoutedEventArgs e)
    {
        _owner.WindowState = _owner.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public void OnClose(object? sender, RoutedEventArgs e) => _owner.Close();

    private void ConfigureWindowsChrome()
    {
        _owner.Classes.Add("platform-windows");
        _owner.SystemDecorations = SystemDecorations.None;
        _owner.ExtendClientAreaToDecorationsHint = true;
        _owner.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        _owner.ExtendClientAreaTitleBarHeightHint = -1;
        _titleBar.IsVisible = true;

        _owner.TransparencyLevelHint =
        [
            WindowTransparencyLevel.Mica,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Blur,
            WindowTransparencyLevel.None,
        ];
    }

    private void ConfigureNativeChrome()
    {
        _owner.Classes.Add("platform-native");
        _owner.SystemDecorations = SystemDecorations.Full;
        _owner.ExtendClientAreaToDecorationsHint = false;
        _owner.ExtendClientAreaTitleBarHeightHint = 0;
        _titleBar.IsVisible = false;
    }
}
