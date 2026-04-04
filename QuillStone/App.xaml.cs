using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using QuillStone.Controllers;
using QuillStone.Models;
using QuillStone.Services;
using QuillStone.Styles.Theme;

namespace QuillStone;

public partial class App : Application
{
    private ServiceProvider? _provider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // ── Infrastructure ───────────────────────────────────────────
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IMarkdownFileService, MarkdownFileService>();
        services.AddSingleton<IMarkdownFormatter, MarkdownFormatter>();
        services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();

        // ── Document ─────────────────────────────────────────────────
        services.AddSingleton<DocumentState>();
        services.AddSingleton<IDocumentService, DocumentService>();

        // ── Project ──────────────────────────────────────────────────
        services.AddSingleton<IProjectService, ProjectService>();

        // ── Editor ───────────────────────────────────────────────────
        services.AddSingleton<IEditorService, EditorService>();
        services.AddSingleton<IFormatCommandHandler, FormatCommandHandler>();
        services.AddSingleton<IMenuCommandHandler, MenuCommandHandler>();
        services.AddSingleton<IWindowLifecycleManager, WindowLifecycleManager>();

        // ── Dialog (window-scoped, owner set post-resolve) ────────────
        services.AddTransient<IWindowDialogService, WindowDialogService>();

        // ── Controllers (Ticket 1) ────────────────────────────────────
        services.AddTransient<ViewModeController>();
        services.AddTransient<PreviewController>();
        services.AddTransient<ProjectTreeController>();
        services.AddTransient<DragDropController>();
        services.AddTransient<StatusBarController>();
        services.AddTransient<WindowChromeController>();

        // ── Window ────────────────────────────────────────────────────
        services.AddTransient<MainWindow>();

        _provider = services.BuildServiceProvider();

        ThemeManager.Initialize(this);

        if (OperatingSystem.IsMacOS())
            SetMacOSDockIcon();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _provider.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) => _provider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    [SupportedOSPlatform("macos")]
    private static void SetMacOSDockIcon()
    {
        try
        {
            var tmp = Path.Combine(Path.GetTempPath(), "QuillStone_dock.png");
            using var stream = AssetLoader.Open(new Uri("avares://QuillStone/Assets/Icons/icon.png"));
            using var file = File.Create(tmp);
            stream.CopyTo(file);
            file.Close();
            MacOSDock.SetIcon(tmp);
        }
        catch { /* best-effort */ }
    }
}

