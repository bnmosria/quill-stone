using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QuillStone.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "1.0.0";

        VersionLabel.Text = $"Version {version}";
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}
