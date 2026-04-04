using Avalonia.Controls;

namespace QuillStone.Services;

public interface IMarkdownRenderer
{
    /// <summary>Renders <paramref name="markdown"/> into an Avalonia control tree.</summary>
    Control Render(string markdown);
}
