using Avalonia.Controls;

namespace QuillStone.Services;

public interface IMarkdownRenderService
{
    IReadOnlyList<Control> Render(string markdown, string? basePath = null, Func<string, Task>? onLocalFileLink = null);
}
