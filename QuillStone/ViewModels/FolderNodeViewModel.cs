using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuillStone.ViewModels;

public sealed class FolderNodeViewModel : FileSystemNodeViewModel, INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isLoaded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FileSystemNodeViewModel> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
            if (_isExpanded)
                EnsureChildrenLoaded();
        }
    }

    public FolderNodeViewModel(string name, string fullPath) : base(name, fullPath)
    {
        Children.Add(new PlaceholderNodeViewModel());
    }

    /// <summary>Reloads the immediate children from disk.</summary>
    public void Refresh()
    {
        _isLoaded = false;
        Children.Clear();

        if (_isExpanded)
            EnsureChildrenLoaded();
        else
            Children.Add(new PlaceholderNodeViewModel());
    }

    private void EnsureChildrenLoaded()
    {
        if (_isLoaded)
            return;

        _isLoaded = true;
        Children.Clear();

        try
        {
            var dir = new DirectoryInfo(FullPath);

            foreach (var subDir in dir.GetDirectories()
                         .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                         .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                var folderVm = new FolderNodeViewModel(subDir.Name, subDir.FullName);
                folderVm.ParentFolder = this;
                Children.Add(folderVm);
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".md", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" };

            foreach (var file in dir.GetFiles()
                         .Where(f => allowedExtensions.Contains(f.Extension))
                         .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                var fileVm = new FileNodeViewModel(file.Name, file.FullName);
                fileVm.ParentFolder = this;
                Children.Add(fileVm);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Access-denied folders appear empty in the tree — standard file-explorer behaviour.
        }
        catch (IOException)
        {
            // Broken symlinks or I/O errors are skipped; the folder appears empty.
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class PlaceholderNodeViewModel : FileSystemNodeViewModel
    {
        public PlaceholderNodeViewModel() : base(string.Empty, string.Empty) { }
    }
}
