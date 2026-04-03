namespace QuillStone.Models;

public sealed class ProjectState
{
    public string ProjectName { get; }
    public string RootPath { get; }

    public ProjectState(string projectName, string rootPath)
    {
        ProjectName = projectName;
        RootPath = rootPath;
    }
}
