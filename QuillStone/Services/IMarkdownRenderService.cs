using Avalonia.Controls;

namespace QuillStone.Services;

public interface IMarkdownRenderService
{
    IReadOnlyList<Control> Render(string markdown);
}
