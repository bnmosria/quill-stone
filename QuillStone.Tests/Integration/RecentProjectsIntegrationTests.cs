using Avalonia.Controls;
using QuillStone.Controllers;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Integration;

/// <summary>
/// Integration tests for recent-projects functionality.
/// Pure unit tests verify recording and capping via <see cref="AppSettingsService"/>;
/// headless UI tests verify menu population via <see cref="RecentProjectsController"/>.
/// </summary>
public sealed class RecentProjectsIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public RecentProjectsIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QS_RPTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private AppSettingsService CreateSettingsService() => new(_settingsPath);

    private string MakeProjectDir(string name)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    // ── Recording projects ────────────────────────────────────────────────────

    [Fact]
    public void RecordProject_AppearsInRecentProjects()
    {
        var svc = CreateSettingsService();
        var path = MakeProjectDir("ProjectA");

        svc.RecordProject("Project A", path);

        Assert.Single(svc.Settings.RecentProjects);
        Assert.Equal("Project A", svc.Settings.RecentProjects[0].Name);
        Assert.Equal(path, svc.Settings.RecentProjects[0].Path);
    }

    [Fact]
    public void RecordProject_SameProjectAgain_MovedToTop()
    {
        var svc = CreateSettingsService();
        var pathA = MakeProjectDir("ProjectA");
        var pathB = MakeProjectDir("ProjectB");

        svc.RecordProject("Project B", pathB);
        svc.RecordProject("Project A", pathA);
        svc.RecordProject("Project B", pathB);

        Assert.Equal("Project B", svc.Settings.RecentProjects[0].Name);
        Assert.Equal("Project A", svc.Settings.RecentProjects[1].Name);
        Assert.Equal(2, svc.Settings.RecentProjects.Count);
    }

    [Fact]
    public void RecordProject_ExceedsMaxCap_ListTrimmedImmediately()
    {
        var svc = CreateSettingsService();

        for (var i = 0; i < 12; i++)
        {
            var dir = MakeProjectDir($"p{i}");
            svc.RecordProject($"Project {i}", dir);
        }

        Assert.Equal(10, svc.Settings.RecentProjects.Count);
    }

    [Fact]
    public void RecordProject_ExceedsMaxCap_MostRecentAreKept()
    {
        var svc = CreateSettingsService();

        for (var i = 0; i < 12; i++)
        {
            var dir = MakeProjectDir($"proj{i}");
            svc.RecordProject($"Project {i}", dir);
        }

        // Most-recently recorded projects are at the top
        Assert.Equal("Project 11", svc.Settings.RecentProjects[0].Name);
        Assert.Equal("Project 10", svc.Settings.RecentProjects[1].Name);
    }

    [Fact]
    public void RecordProject_UpdatesLastOpenedProjectPath()
    {
        var svc = CreateSettingsService();
        var path = MakeProjectDir("MyProject");

        svc.RecordProject("My Project", path);

        Assert.Equal(path, svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public void RecordProject_CaseInsensitiveDeduplicate()
    {
        var svc = CreateSettingsService();
        var path = MakeProjectDir("Alpha");

        svc.RecordProject("Alpha", path);
        svc.RecordProject("Alpha", path.ToUpperInvariant());

        Assert.Single(svc.Settings.RecentProjects);
    }

    // ── Menu population ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Populate_WithProjects_AddsMenuItemsForEach()
    {
        var svc = CreateSettingsService();
        var pathA = MakeProjectDir("MenuA");
        var pathB = MakeProjectDir("MenuB");
        svc.RecordProject("Menu B", pathB);
        svc.RecordProject("Menu A", pathA);

        var menuItem = new MenuItem();
        var controller = CreateController(menuItem, svc);

        controller.Populate();

        Assert.Equal(2, menuItem.Items.Count);
        var first = Assert.IsType<MenuItem>(menuItem.Items[0]);
        Assert.Equal("Menu A", first.Header);
        var second = Assert.IsType<MenuItem>(menuItem.Items[1]);
        Assert.Equal("Menu B", second.Header);
    }

    [AvaloniaFact]
    public void Populate_NoProjects_ShowsNoRecentPlaceholder()
    {
        var svc = CreateSettingsService();
        var menuItem = new MenuItem();
        var controller = CreateController(menuItem, svc);

        controller.Populate();

        Assert.Single(menuItem.Items);
        var placeholder = Assert.IsType<MenuItem>(menuItem.Items[0]);
        Assert.Equal("(No recent projects)", placeholder.Header);
        Assert.False(placeholder.IsEnabled);
    }

    private RecentProjectsController CreateController(MenuItem menuItem, IAppSettingsService settingsService)
    {
        var projectService = new Mock<IProjectService>();
        var dialogService = new Mock<IWindowDialogService>();
        var owner = new Window();

        return new RecentProjectsController(
            menuItem,
            settingsService,
            projectService.Object,
            dialogService.Object,
            owner,
            trySwitchProject: _ => Task.FromResult(false),
            resetEditor: () => Task.CompletedTask,
            onProjectOpened: () => { });
    }
}
