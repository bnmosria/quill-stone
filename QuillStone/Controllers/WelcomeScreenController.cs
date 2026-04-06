using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Controllers;

public sealed class WelcomeScreenController
{
    private readonly IAppSettingsService _settingsService;

    private Views.WelcomeScreenView _welcomeScreen = null!;
    private Control _editor = null!;
    private Border _formattingToolbarContainer = null!;
    private SidebarController _sidebarController = null!;
    private Func<RecentProject, Task> _onOpenRecentProject = null!;
    private bool _sidebarWasVisible;

    public bool IsWelcomeVisible { get; private set; }

    public WelcomeScreenController(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Wire(
        Views.WelcomeScreenView welcomeScreen,
        Control editor,
        Border formattingToolbarContainer,
        SidebarController sidebarController,
        Func<Task<bool>> onNewDocument,
        Func<Task<bool>> onOpenFile,
        Func<Task<bool>> onOpenProject,
        Func<Task<bool>> onOpenFolder,
        Func<RecentProject, Task> onOpenRecentProject)
    {
        _welcomeScreen = welcomeScreen;
        _editor = editor;
        _formattingToolbarContainer = formattingToolbarContainer;
        _sidebarController = sidebarController;
        _onOpenRecentProject = onOpenRecentProject;

        _welcomeScreen.NewDocumentButton.Click += async (_, _) => { if (await onNewDocument()) Hide(); };
        _welcomeScreen.OpenFileButton.Click += async (_, _) => { if (await onOpenFile()) Hide(); };
        _welcomeScreen.OpenProjectButton.Click += async (_, _) => { if (await onOpenProject()) Hide(); };
        _welcomeScreen.OpenFolderButton.Click += async (_, _) => { if (await onOpenFolder()) Hide(); };
    }

    public void Show()
    {
        IsWelcomeVisible = true;
        _welcomeScreen.IsVisible = true;
        _editor.IsVisible = false;
        _formattingToolbarContainer.IsVisible = false;
        _sidebarWasVisible = _sidebarController.IsVisible;
        _sidebarController.EnsureHidden();
        RefreshRecentProjects();
    }

    public void Hide()
    {
        IsWelcomeVisible = false;
        _welcomeScreen.IsVisible = false;
        _editor.IsVisible = true;
        _formattingToolbarContainer.IsVisible = true;
        if (_sidebarWasVisible)
            _sidebarController.EnsureVisible();
    }

    public void RefreshRecentProjects()
    {
        var projects = _settingsService.Settings.RecentProjects;
        _welcomeScreen.RecentProjectsPanel.Children.Clear();

        if (projects.Count == 0)
        {
            _welcomeScreen.NoRecentProjectsHint.IsVisible = true;
            return;
        }

        _welcomeScreen.NoRecentProjectsHint.IsVisible = false;

        foreach (var project in projects)
        {
            var btn = new Button { Classes = { "WelcomeRecentItem" } };
            var captured = project;
            btn.Click += async (_, _) =>
            {
                await _onOpenRecentProject(captured);
                Hide();
            };

            var nameBlock = new TextBlock
            {
                Text = project.Name,
                FontWeight = FontWeight.Bold,
            };
            if (Application.Current!.TryGetResource("Brush.Text.Primary", Application.Current.ActualThemeVariant, out var primary))
                nameBlock.Foreground = (IBrush?)primary;

            var pathBlock = new TextBlock { Text = project.Path, Margin = new Thickness(0, 2, 0, 0) };
            if (Application.Current!.TryGetResource("FontSize.SM", Application.Current.ActualThemeVariant, out var sm))
                pathBlock.FontSize = (double)sm!;
            if (Application.Current!.TryGetResource("Brush.Text.Tertiary", Application.Current.ActualThemeVariant, out var tertiary))
                pathBlock.Foreground = (IBrush?)tertiary;

            btn.Content = new StackPanel { Children = { nameBlock, pathBlock } };
            _welcomeScreen.RecentProjectsPanel.Children.Add(btn);
        }
    }
}
