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
                _ = EnsureChildrenLoadedAsync();
        }
    }

    public FolderNodeViewModel(string name, string fullPath) : base(name, fullPath)
    {
        Children.Add(new LoadingPlaceholderViewModel());
    }

    /// <summary>Reloads the immediate children from disk.</summary>
    public void Refresh()
    {
        _isLoaded = false;
        Children.Clear();

        if (_isExpanded)
            _ = EnsureChildrenLoadedAsync();
        else
            Children.Add(new LoadingPlaceholderViewModel());
    }

    private async Task EnsureChildrenLoadedAsync()
    {
        if (_isLoaded)
            return;

        _isLoaded = true;
        Children.Clear();

        try
        {
            var (dirs, files) = await Task.Run(() =>
            {
                var dir = new DirectoryInfo(FullPath);

                var subDirs = dir.GetDirectories()
                    .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                    .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { ".md", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" };

                var mdFiles = dir.GetFiles()
                    .Where(f => allowedExtensions.Contains(f.Extension))
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return (subDirs, mdFiles);
            });

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var subDir in dirs)
                {
                    var vm = new FolderNodeViewModel(subDir.Name, subDir.FullName);
                    vm.ParentFolder = this;
                    Children.Add(vm);
                }

                foreach (var file in files)
                {
                    var vm = new FileNodeViewModel(file.Name, file.FullName);
                    vm.ParentFolder = this;
                    Children.Add(vm);
                }

                if (Children.Count == 0)
                    Children.Add(new EmptyPlaceholderViewModel());
            });
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

    private sealed class LoadingPlaceholderViewModel : FileSystemNodeViewModel
    {
        public LoadingPlaceholderViewModel() : base(string.Empty, string.Empty) { }
    }

    public sealed class EmptyPlaceholderViewModel : FileSystemNodeViewModel
    {
        public EmptyPlaceholderViewModel() : base("(empty)", string.Empty) { }
    }
}
