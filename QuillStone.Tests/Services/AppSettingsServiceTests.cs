using System.Text.Json;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class AppSettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public AppSettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuillStoneTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private AppSettingsService CreateService() => new(_settingsPath);

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        var svc = CreateService();

        await svc.LoadAsync();

        Assert.NotNull(svc.Settings);
        Assert.Empty(svc.Settings.RecentProjects);
        Assert.Null(svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsDefaults()
    {
        await File.WriteAllTextAsync(_settingsPath, "{ this is not valid json }", System.Text.Encoding.UTF8);
        var svc = CreateService();

        await svc.LoadAsync();

        Assert.NotNull(svc.Settings);
        Assert.Empty(svc.Settings.RecentProjects);
        Assert.Null(svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public async Task LoadAsync_ValidJson_DeserializesCorrectly()
    {
        var expected = new AppSettings
        {
            LastOpenedProjectPath = _tempDir,
            RecentProjects =
            [
                new RecentProject
                {
                    Name = "My Book",
                    Path = _tempDir,
                    LastOpened = DateTimeOffset.UtcNow,
                },
            ],
        };
        string json = JsonSerializer.Serialize(expected, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json, System.Text.Encoding.UTF8);

        var svc = CreateService();
        await svc.LoadAsync();

        Assert.Equal(_tempDir, svc.Settings.LastOpenedProjectPath);
        Assert.Single(svc.Settings.RecentProjects);
        Assert.Equal("My Book", svc.Settings.RecentProjects[0].Name);
    }

    [Fact]
    public async Task SaveAsync_WritesSettingsToDisk()
    {
        var svc = CreateService();
        svc.Settings.LastOpenedProjectPath = "/saved/path";

        await svc.SaveAsync();

        Assert.True(File.Exists(_settingsPath));
        string saved = await File.ReadAllTextAsync(_settingsPath, System.Text.Encoding.UTF8);
        Assert.Contains("/saved/path", saved);
    }

    [Fact]
    public async Task SaveAsync_DoesNotLeaveTemporaryFile()
    {
        var svc = CreateService();

        await svc.SaveAsync();

        Assert.False(File.Exists(_settingsPath + ".tmp"));
    }

    [Fact]
    public void RecordProject_AddsProjectToRecentList()
    {
        var svc = CreateService();

        svc.RecordProject("My Book", "/projects/my-book");

        Assert.Single(svc.Settings.RecentProjects);
        Assert.Equal("My Book", svc.Settings.RecentProjects[0].Name);
        Assert.Equal("/projects/my-book", svc.Settings.RecentProjects[0].Path);
    }

    [Fact]
    public void RecordProject_DeduplicatesByPath()
    {
        var svc = CreateService();
        svc.RecordProject("Old Name", "/projects/same-path");

        svc.RecordProject("New Name", "/projects/same-path");

        Assert.Single(svc.Settings.RecentProjects);
        Assert.Equal("New Name", svc.Settings.RecentProjects[0].Name);
    }

    [Fact]
    public void RecordProject_PathComparisonIsCaseInsensitive()
    {
        var svc = CreateService();
        svc.RecordProject("Book", "/Projects/MyBook");

        svc.RecordProject("Book Updated", "/projects/mybook");

        Assert.Single(svc.Settings.RecentProjects);
    }

    [Fact]
    public void RecordProject_NewestFirst()
    {
        var svc = CreateService();
        svc.RecordProject("First", "/projects/first");

        svc.RecordProject("Second", "/projects/second");

        Assert.Equal("Second", svc.Settings.RecentProjects[0].Name);
        Assert.Equal("First", svc.Settings.RecentProjects[1].Name);
    }

    [Fact]
    public void RecordProject_CapsAtMaxTenProjects()
    {
        var svc = CreateService();
        for (int i = 1; i <= 11; i++)
            svc.RecordProject($"Book {i}", $"/projects/book{i}");

        Assert.Equal(10, svc.Settings.RecentProjects.Count);
        Assert.Equal("Book 11", svc.Settings.RecentProjects[0].Name);
    }

    [Fact]
    public void RecordProject_SetsLastOpenedProjectPath()
    {
        var svc = CreateService();

        svc.RecordProject("My Book", "/projects/my-book");

        Assert.Equal("/projects/my-book", svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public void RemoveStale_RemovesNonExistentPaths()
    {
        var svc = CreateService();
        svc.Settings.RecentProjects.Add(new RecentProject
        {
            Name = "Gone",
            Path = "/nonexistent/path/that/does/not/exist",
            LastOpened = DateTimeOffset.UtcNow,
        });
        svc.Settings.RecentProjects.Add(new RecentProject
        {
            Name = "Present",
            Path = _tempDir,
            LastOpened = DateTimeOffset.UtcNow,
        });

        svc.RemoveStale();

        Assert.Single(svc.Settings.RecentProjects);
        Assert.Equal("Present", svc.Settings.RecentProjects[0].Name);
    }

    [Fact]
    public void RemoveStale_ClearsLastOpenedProjectPathIfMissing()
    {
        var svc = CreateService();
        svc.Settings.LastOpenedProjectPath = "/nonexistent/path";

        svc.RemoveStale();

        Assert.Null(svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public void RemoveStale_KeepsLastOpenedProjectPathIfExists()
    {
        var svc = CreateService();
        svc.Settings.LastOpenedProjectPath = _tempDir;

        svc.RemoveStale();

        Assert.Equal(_tempDir, svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_PreservesRecentProjects()
    {
        var svc = CreateService();
        svc.RecordProject("Kept", "/projects/kept");

        await svc.ResetToDefaultsAsync();

        Assert.Single(svc.Settings.RecentProjects);
        Assert.Equal("Kept", svc.Settings.RecentProjects[0].Name);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_PreservesLastOpenedProjectPath()
    {
        var svc = CreateService();
        svc.Settings.LastOpenedProjectPath = "/projects/kept";

        await svc.ResetToDefaultsAsync();

        Assert.Equal("/projects/kept", svc.Settings.LastOpenedProjectPath);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_WritesToDisk()
    {
        var svc = CreateService();

        await svc.ResetToDefaultsAsync();

        Assert.True(File.Exists(_settingsPath));
    }
}
