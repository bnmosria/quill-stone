using Avalonia.Controls;
using Avalonia.Input;

namespace QuillStone.Services;

public sealed class WindowDialogService : IWindowDialogService
{
    public async Task<DialogChoice> ShowConfirmDialogAsync(
        Window owner,
        string title,
        string message,
        string primaryButton,
        string secondaryButton,
        string cancelButton)
    {
        var dialog = CreateDialogWindow(title);
        var result = DialogChoice.Cancel;

        void CloseWith(DialogChoice choice)
        {
            result = choice;
            dialog.Close();
        }

        var primary = new Button { Content = primaryButton, MinWidth = 96 };
        var secondary = new Button { Content = secondaryButton, MinWidth = 96 };
        var cancel = new Button { Content = cancelButton, MinWidth = 96 };

        primary.Click += (_, _) => CloseWith(DialogChoice.Primary);
        secondary.Click += (_, _) => CloseWith(DialogChoice.Secondary);
        cancel.Click += (_, _) => CloseWith(DialogChoice.Cancel);

        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { primary, secondary, cancel }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }

    public async Task<bool> ShowConfirmAsync(Window owner, string title, string message, string confirmButton)
    {
        var dialog = CreateDialogWindow(title);
        var confirmed = false;

        var confirm = new Button { Content = confirmButton, MinWidth = 96 };
        var cancel = new Button { Content = "Cancel", MinWidth = 96 };

        confirm.Click += (_, _) => { confirmed = true; dialog.Close(); };
        cancel.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { confirm, cancel }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return confirmed;
    }

    public async Task ShowMessageDialogAsync(Window owner, string title, string message)
    {
        var dialog = CreateDialogWindow(title);

        var ok = new Button { Content = "OK", MinWidth = 96 };
        ok.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { ok }
                }
            }
        };

        await dialog.ShowDialog(owner);
    }

    public async Task<string?> ShowInputDialogAsync(Window owner, string title, string prompt, string defaultValue)
    {
        var dialog = CreateDialogWindow(title);
        string? result = null;

        var input = new TextBox { Text = defaultValue, MinWidth = 320 };
        var ok = new Button { Content = "OK", MinWidth = 80 };
        var cancel = new Button { Content = "Cancel", MinWidth = 80 };

        void Accept()
        {
            result = input.Text;
            dialog.Close();
        }

        void Dismiss() => dialog.Close();

        ok.Click += (_, _) => Accept();
        cancel.Click += (_, _) => Dismiss();

        input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Accept();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Dismiss();
                e.Handled = true;
            }
        };

        dialog.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = prompt, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                input,
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { ok, cancel }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }

    private static Window CreateDialogWindow(string title) => new()
    {
        Title = title,
        Width = 520,
        MinWidth = 420,
        SizeToContent = SizeToContent.Height,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ShowInTaskbar = false
    };
}

