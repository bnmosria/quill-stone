using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using System;

namespace QuillStone.Styles.Theme;

/// <summary>
/// Manages runtime switching between the "Vellichor" Light and Dark themes.
/// Call ThemeManager.Apply(AppThemeVariant.Light) or .Dark from anywhere.
/// </summary>
public static class ThemeManager
{
    private const string BaseUri   = "avares://QuillStone/Styles/Theme/";
    private const string LightPath = "LightTheme.axaml";
    private const string DarkPath  = "DarkTheme.axaml";

    private static ResourceInclude? _activeTheme;
    private static Application      _app = null!;

    public static AppThemeVariant Current { get; private set; } = AppThemeVariant.Light;

    /// <summary>
    /// Call once in App.xaml.cs OnFrameworkInitializationCompleted().
    /// Loads the initial theme into the application resource dictionary.
    /// </summary>
    public static void Initialize(Application app, AppThemeVariant initial = AppThemeVariant.Light)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        Apply(initial, forceReload: true);
    }

    /// <summary>
    /// Switch to the specified theme at runtime. Safe to call multiple times.
    /// </summary>
    public static void Apply(AppThemeVariant variant, bool forceReload = false)
    {
        if (variant == Current && !forceReload) return;

        var resources = _app.Resources;

        if (_activeTheme is not null &&
            resources.MergedDictionaries.Contains(_activeTheme))
            resources.MergedDictionaries.Remove(_activeTheme);

        var themePath = variant == AppThemeVariant.Dark ? DarkPath : LightPath;
        var themeUri  = new Uri(BaseUri + themePath);
        _activeTheme  = new ResourceInclude(themeUri) { Source = themeUri };
        resources.MergedDictionaries.Add(_activeTheme);

        Current = variant;

        _app.RequestedThemeVariant = variant == AppThemeVariant.Dark
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light;

        ThemeChanged?.Invoke(null, variant);
    }

    /// <summary>Toggle between light and dark.</summary>
    public static void Toggle() =>
        Apply(Current == AppThemeVariant.Light ? AppThemeVariant.Dark : AppThemeVariant.Light);

    /// <summary>Fired after the theme changes.</summary>
    public static event EventHandler<AppThemeVariant>? ThemeChanged;
}

public enum AppThemeVariant { Light, Dark }
