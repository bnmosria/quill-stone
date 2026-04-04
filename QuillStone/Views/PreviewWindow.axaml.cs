using Avalonia.Controls;

namespace QuillStone.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
    }

    public void UpdateContent(string markdownText)
    {
        PreviewTextBox.Text = markdownText;
    }
}
