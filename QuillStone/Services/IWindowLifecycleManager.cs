using Avalonia.Controls;

namespace QuillStone.Services;

public interface IWindowLifecycleManager
{
    void SetOwner(Window owner);
    Task<bool> HandleClosingAsync();
}

