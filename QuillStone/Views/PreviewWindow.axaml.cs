using Avalonia.Controls;
using Avalonia.VisualTree;

namespace QuillStone.Views;

public partial class PreviewWindow : Window
{
    private ScrollViewer? _scroll;

    public PreviewWindow()
    {
        InitializeComponent();
    }

    public void UpdateContent(string markdownText)
    {
        PreviewTextBox.Text = markdownText;
        _scroll ??= PreviewTextBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        _scroll?.ScrollToHome();
    }
}
