using Avalonia.Controls;

namespace QuillStone.Services;

public interface IWindowDialogService
{
    Task<DialogChoice> ShowConfirmDialogAsync(Window owner, string title, string message, string primaryButton, string secondaryButton, string cancelButton);
    Task<bool> ShowConfirmAsync(Window owner, string title, string message, string confirmButton);
    Task ShowMessageDialogAsync(Window owner, string title, string message);
    Task<string?> ShowInputDialogAsync(Window owner, string title, string prompt, string defaultValue);
}

