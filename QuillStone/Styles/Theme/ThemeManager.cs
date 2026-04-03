using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;

namespace MarkdownEditor.Themes;

/// <summary>
/// Manages runtime switching between the "Vellichor" Light and Dark themes.
/// Call ThemeManager.Apply(ThemeVariant.Light) or .Dark from anywhere.
/// </summary>
public static class ThemeManager
{
    private const string BaseUri    = "avares://MarkdownEditor/Themes/";
    private const string LightPath  = "LightTheme.axaml";
    private const string DarkPath   = "DarkTheme.axaml";
    private const string StylesPath = "ControlStyles.axaml";

    private static ResourceInclude? _activeTheme;
    private static ResourceInclude? _controlStyles;
    private static Application      _app = null!;

    public static ThemeVariant Current { get; private set; } = ThemeVariant.Light;

    /// <summary>
    /// Call once in App.axaml.cs OnFrameworkInitializationCompleted().
    /// Loads the initial theme and control styles.
    /// </summary>
    public static void Initialize(Application app, ThemeVariant initial = ThemeVariant.Light)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        Apply(initial, forceReload: true);
    }

    /// <summary>
    /// Switch to the specified theme at runtime. Safe to call multiple times.
    /// </summary>
    public static void Apply(ThemeVariant variant, bool forceReload = false)
    {
        if (variant == Current && !forceReload) return;

        var resources = _app.Resources;

        // ── Remove previous theme ────────────────────────────────
        if (_activeTheme is not null &&
            resources.MergedDictionaries.Contains(_activeTheme))
            resources.MergedDictionaries.Remove(_activeTheme);

        // ── Load new theme ───────────────────────────────────────
        var themePath = variant == ThemeVariant.Dark ? DarkPath : LightPath;
        _activeTheme  = new ResourceInclude(new Uri(BaseUri + themePath))
        {
            Source = new Uri(BaseUri + themePath)
        };
        resources.MergedDictionaries.Insert(0, _activeTheme);

        // ── Ensure control styles are loaded (once) ──────────────
        if (_controlStyles is null)
        {
            _controlStyles = new ResourceInclude(new Uri(BaseUri + StylesPath))
            {
                Source = new Uri(BaseUri + StylesPath)
            };
            resources.MergedDictionaries.Add(_controlStyles);
        }

        Current = variant;

        // ── Sync Avalonia's built-in theme variant ───────────────
        _app.RequestedThemeVariant = variant == ThemeVariant.Dark
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light;

        ThemeChanged?.Invoke(null, variant);
    }

    /// <summary>Toggle between light and dark.</summary>
    public static void Toggle() =>
        Apply(Current == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light);

    /// <summary>Fired after the theme changes. Subscribe in ViewModels to react.</summary>
    public static event EventHandler<ThemeVariant>? ThemeChanged;
}

public enum ThemeVariant { Light, Dark }
