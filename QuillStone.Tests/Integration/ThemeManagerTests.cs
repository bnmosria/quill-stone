using Avalonia;
using Avalonia.Styling;
using QuillStone.Styles.Theme;

namespace QuillStone.Tests.Integration;

/// <summary>
/// Headless UI tests for <see cref="ThemeManager"/> verifying that toggling light/dark
/// updates <see cref="ThemeManager.Current"/> and <see cref="Application.RequestedThemeVariant"/>.
/// </summary>
public sealed class ThemeManagerTests
{
    [AvaloniaFact]
    public void Apply_Light_SetsCurrent_ToLight()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);

        Assert.Equal(AppThemeVariant.Light, ThemeManager.Current);
    }

    [AvaloniaFact]
    public void Apply_Dark_SetsCurrent_ToDark()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);

        ThemeManager.Apply(AppThemeVariant.Dark);

        Assert.Equal(AppThemeVariant.Dark, ThemeManager.Current);
    }

    [AvaloniaFact]
    public void Apply_Light_SetsRequestedThemeVariant_ToLight()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);

        Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Apply_Dark_SetsRequestedThemeVariant_ToDark()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);

        ThemeManager.Apply(AppThemeVariant.Dark);

        Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Toggle_FromLight_SwitchesToDark()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);

        ThemeManager.Toggle();

        Assert.Equal(AppThemeVariant.Dark, ThemeManager.Current);
        Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Toggle_FromDark_SwitchesToLight()
    {
        ThemeManager.Apply(AppThemeVariant.Dark, forceReload: true);

        ThemeManager.Toggle();

        Assert.Equal(AppThemeVariant.Light, ThemeManager.Current);
        Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Toggle_TwiceFromLight_ReturnsToLight()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);

        ThemeManager.Toggle();
        ThemeManager.Toggle();

        Assert.Equal(AppThemeVariant.Light, ThemeManager.Current);
    }

    [AvaloniaFact]
    public void Apply_SameVariant_WithoutForce_DoesNotFireThemeChanged()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);
        var fireCount = 0;
        EventHandler<AppThemeVariant> handler = (_, _) => fireCount++;
        ThemeManager.ThemeChanged += handler;

        try
        {
            ThemeManager.Apply(AppThemeVariant.Light);

            Assert.Equal(0, fireCount);
        }
        finally
        {
            ThemeManager.ThemeChanged -= handler;
        }
    }

    [AvaloniaFact]
    public void Apply_DifferentVariant_FiresThemeChanged()
    {
        ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);
        AppThemeVariant? observed = null;
        EventHandler<AppThemeVariant> handler = (_, v) => observed = v;
        ThemeManager.ThemeChanged += handler;

        try
        {
            ThemeManager.Apply(AppThemeVariant.Dark);

            Assert.Equal(AppThemeVariant.Dark, observed);
        }
        finally
        {
            ThemeManager.ThemeChanged -= handler;
            ThemeManager.Apply(AppThemeVariant.Light, forceReload: true);
        }
    }
}
