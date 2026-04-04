using System.Text.Json;
using System.Text.Json.Serialization;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private const int MaxRecentProjects = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _settingsPath;

    public AppSettings Settings { get; private set; } = new();

    public AppSettingsService()
    {
        string configDir = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuillStone")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "QuillStone");

        _settingsPath = Path.Combine(configDir, "settings.json");
    }

    internal AppSettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            Settings = new AppSettings();
            return;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_settingsPath, System.Text.Encoding.UTF8);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            Settings = new AppSettings();
        }

        RemoveStale();
    }

    public async Task SaveAsync()
    {
        string dir = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(dir);

        string tmpPath = _settingsPath + ".tmp";
        string json = JsonSerializer.Serialize(Settings, JsonOptions);

        await File.WriteAllTextAsync(tmpPath, json, System.Text.Encoding.UTF8);
        File.Move(tmpPath, _settingsPath, overwrite: true);
    }

    public void RecordProject(string name, string path)
    {
        Settings.RecentProjects.RemoveAll(p =>
            string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase));

        Settings.RecentProjects.Insert(0, new RecentProject
        {
            Name = name,
            Path = path,
            LastOpened = DateTimeOffset.UtcNow,
        });

        if (Settings.RecentProjects.Count > MaxRecentProjects)
            Settings.RecentProjects.RemoveRange(MaxRecentProjects, Settings.RecentProjects.Count - MaxRecentProjects);

        Settings.LastOpenedProjectPath = path;
    }

    public void RemoveStale()
    {
        Settings.RecentProjects.RemoveAll(p => !Directory.Exists(p.Path));

        if (Settings.LastOpenedProjectPath is not null && !Directory.Exists(Settings.LastOpenedProjectPath))
            Settings.LastOpenedProjectPath = null;
    }

    public async Task ResetToDefaultsAsync()
    {
        var preserved = Settings.RecentProjects;
        var lastPath = Settings.LastOpenedProjectPath;

        Settings = new AppSettings
        {
            RecentProjects = preserved,
            LastOpenedProjectPath = lastPath,
        };

        await SaveAsync();
    }
}
