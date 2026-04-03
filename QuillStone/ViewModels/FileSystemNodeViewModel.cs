namespace QuillStone.ViewModels;

public abstract class FileSystemNodeViewModel
{
    public string Name { get; }
    public string FullPath { get; }
    public FolderNodeViewModel? ParentFolder { get; internal set; }

    protected FileSystemNodeViewModel(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }
}
