using Avalonia.Controls;

namespace QuillStone.Controllers;

internal sealed class StatusBarController
{
    private readonly TextBlock _statusMeta;
    private readonly TextBlock _statusWordCount;
    private readonly TextBox _editor;

    internal StatusBarController(TextBlock statusMeta, TextBlock statusWordCount, TextBox editor)
    {
        _statusMeta = statusMeta;
        _statusWordCount = statusWordCount;
        _editor = editor;
    }

    public void UpdateMeta()
    {
        var text = _editor.Text ?? string.Empty;
        var caret = Math.Clamp(_editor.CaretIndex, 0, text.Length);

        int line = 1, col = 1;
        for (int i = 0; i < caret; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                col = 1;
            }
            else if (text[i] != '\r')
            {
                col++;
            }
        }

        _statusMeta.Text = $"Ln {line}, Col {col}  ·  UTF-8  ·  Markdown";
    }

    public void UpdateWordCount()
    {
        var text = _editor.Text ?? string.Empty;
        var wordCount = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var readingMinutes = (int)Math.Ceiling(wordCount / 200.0);
        _statusWordCount.Text = $"{wordCount} words · {readingMinutes} min read";
    }
}
