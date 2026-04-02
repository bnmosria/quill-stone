namespace QuillStone.Services;

public interface IWindowLifecycleManager
{
    Task<bool> HandleClosingAsync();
}

