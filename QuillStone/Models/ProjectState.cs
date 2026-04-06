namespace QuillStone.Models;

public sealed class ProjectState
{
    public string ProjectName { get; }
    public string RootPath { get; }
    public bool IsProject { get; }

    public string DisplayName
    {
        get
        {
            if (IsProject)
                return ProjectName;
            string trimmed = RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrEmpty(trimmed) ? RootPath : Path.GetFileName(trimmed);
        }
    }

    public ProjectState(string projectName, string rootPath, bool isProject)
    {
        ProjectName = projectName;
        RootPath = rootPath;
        IsProject = isProject;
    }
}
