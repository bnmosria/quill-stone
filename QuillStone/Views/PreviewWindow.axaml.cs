using Avalonia.Controls;
using QuillStone.Services;

namespace QuillStone.Views;

public partial class PreviewWindow : Window
{
    private readonly IMarkdownRenderer _renderer;

    // Parameterless constructor for Avalonia design-time tooling.
    public PreviewWindow() : this(new MarkdownRenderer()) { }

    public PreviewWindow(IMarkdownRenderer renderer)
    {
        InitializeComponent();
        _renderer = renderer;
    }

    public void UpdateContent(string markdownText)
    {
        PreviewContent.Children.Clear();
        var rendered = _renderer.Render(markdownText);

        if (rendered is Panel panel)
        {
            foreach (var child in panel.Children.ToList())
                PreviewContent.Children.Add(child);
        }
        else
        {
            PreviewContent.Children.Add(rendered);
        }
    }
}
