using System.ComponentModel;

namespace QuillStone.ViewModels;

public sealed class FileNodeViewModel : FileSystemNodeViewModel, INotifyPropertyChanged
{
    private bool _isActive;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;
            _isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    public FileNodeViewModel(string name, string fullPath) : base(name, fullPath) { }
}
