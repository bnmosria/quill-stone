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
                Children.Add(new FolderNodeViewModel(subDir.Name, subDir.FullName));
            }

            foreach (var file in dir.GetFiles("*.md")
                         .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                Children.Add(new FileNodeViewModel(file.Name, file.FullName));
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class PlaceholderNodeViewModel : FileSystemNodeViewModel
    {
        public PlaceholderNodeViewModel() : base(string.Empty, string.Empty) { }
    }
}
