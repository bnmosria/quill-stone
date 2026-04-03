namespace QuillStone.Models;

public sealed class AppSettings
{
    public string? LastOpenedProjectPath { get; set; }
    public List<RecentProject> RecentProjects { get; set; } = [];
}

public sealed class RecentProject
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTimeOffset LastOpened { get; set; }
}
