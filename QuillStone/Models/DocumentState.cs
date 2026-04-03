using Avalonia.Platform.Storage;

namespace QuillStone.Models;

public sealed class DocumentState
{
    public IStorageFile? CurrentFile { get; private set; }
    public string? CurrentFilePath { get; private set; }
    public bool IsDirty { get; private set; }
    public string PersistedContent { get; private set; } = string.Empty;

    public string DisplayName => CurrentFilePath is not null
        ? Path.GetFileName(CurrentFilePath)
        : "Untitled";

    public void MarkDirty(bool isDirty) => IsDirty = isDirty;

    public bool HasUnsavedChanges(string content)
        => !string.Equals(content, PersistedContent, StringComparison.Ordinal);

    public void SetCurrentFile(IStorageFile file, string? localPath)
    {
        CurrentFile = file;
        CurrentFilePath = localPath ?? file.Name;
    }

    public void SetPersistedContent(string content)
    {
        PersistedContent = content;
    }

    public void Reset()
    {
        CurrentFile = null;
        CurrentFilePath = null;
        PersistedContent = string.Empty;
        IsDirty = false;
    }

    public string BuildWindowTitle(string appName)
    {
        string dirtyMark = IsDirty ? "*" : string.Empty;
        return $"{DisplayName}{dirtyMark} - {appName}";
    }
}

