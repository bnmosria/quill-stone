using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using MdCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MdEmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using MdInline = Markdig.Syntax.Inlines.Inline;
using MdLineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
using MdLinkInline = Markdig.Syntax.Inlines.LinkInline;
using MdLiteralInline = Markdig.Syntax.Inlines.LiteralInline;

namespace QuillStone.Services;

public class MarkdownRenderService : IMarkdownRenderService
{
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder()
            .UsePipeTables()
            .Build();

    public IReadOnlyList<Control> Render(string markdown, string? basePath = null, Func<string, Task>? onLocalFileLink = null)
    {
        if (string.IsNullOrEmpty(markdown))
            return [];

        try
        {
            var document = Markdig.Markdown.Parse(markdown, _pipeline);
            var controls = new List<Control>();

            foreach (var block in document)
            {
                var control = RenderBlock(block, basePath, onLocalFileLink);
                if (control is not null)
                    controls.Add(control);
            }

            return controls;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MarkdownRenderService] Render error: {ex.Message}");
            return [];
        }
    }

    private Control? RenderBlock(Block block, string? basePath, Func<string, Task>? onLocalFileLink) => block switch
    {
        HeadingBlock h => RenderHeading(h),
        ParagraphBlock p => RenderParagraph(p, basePath, onLocalFileLink),
        QuoteBlock q => RenderQuote(q, basePath, onLocalFileLink),
        FencedCodeBlock f => RenderFencedCode(f),
        CodeBlock c => RenderGenericCode(c),
        ListBlock l => RenderList(l, basePath, onLocalFileLink),
        ThematicBreakBlock => RenderHr(),
        Table t => RenderTable(t),
        _ => RenderFallback(block),
    };

    private static TextBlock RenderHeading(HeadingBlock heading)
    {
        var tb = new TextBlock();
        tb.Classes.Add(heading.Level switch
        {
            1 => "MdH1",
            2 => "MdH2",
            3 => "MdH3",
            _ => "MdH4",
        });
        PopulateInlines(tb, heading.Inline);
        return tb;
    }

    private static Control RenderParagraph(ParagraphBlock paragraph, string? basePath, Func<string, Task>? onLocalFileLink)
    {
        if (TryGetSingleImageLink(paragraph, out var imageLink))
            return RenderImage(imageLink!, basePath);

        if (ContainsLinks(paragraph.Inline))
            return RenderParagraphWithLinks(paragraph, basePath, onLocalFileLink);

        var tb = new TextBlock();
        tb.Classes.Add("MdBody");
        PopulateInlines(tb, paragraph.Inline);
        return tb;
    }

    private static bool ContainsLinks(MdContainerInline? container)
    {
        if (container is null)
            return false;

        foreach (var inline in container)
        {
            if (inline is MdLinkInline link && !link.IsImage)
                return true;
        }

        return false;
    }

    private static Control RenderParagraphWithLinks(ParagraphBlock paragraph, string? basePath, Func<string, Task>? onLocalFileLink)
    {
        var wrap = new WrapPanel();

        foreach (var inline in paragraph.Inline ?? Enumerable.Empty<MdInline>())
        {
            if (inline is MdLinkInline link && !link.IsImage)
            {
                var uri = ResolveLinkUrl(link.Url ?? string.Empty, basePath);
                var text = GetRawText(link);

                if (uri is not null)
                    wrap.Children.Add(BuildLinkButton(text, uri, onLocalFileLink));
                else
                {
                    var tb = new TextBlock { Text = text };
                    tb.Classes.Add("MdBody");
                    wrap.Children.Add(tb);
                }
            }
            else
            {
                var tb = new TextBlock();
                tb.Classes.Add("MdBody");
                if (inline is MdContainerInline container)
                    PopulateInlines(tb, container);
                else
                {
                    var built = BuildInline(inline);
                    if (built is not null)
                        tb.Inlines!.Add(built);
                }
                wrap.Children.Add(tb);
            }
        }

        return wrap;
    }

    private static Uri? ResolveLinkUrl(string url, string? basePath)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (url.StartsWith('#'))
        {
            Debug.WriteLine($"[MarkdownRenderService] Anchor link ignored: {url}");
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme is "http" or "https")
                return absolute;

            Debug.WriteLine($"[MarkdownRenderService] Blocked protocol: {absolute.Scheme}");
            return null;
        }

        if (basePath is not null)
        {
            try
            {
                string fullPath = Path.GetFullPath(url, basePath);
                return new Uri(fullPath);
            }
            catch (Exception ex) when (ex is ArgumentException
                                           or NotSupportedException
                                           or PathTooLongException
                                           or System.Security.SecurityException)
            {
                return null;
            }
        }

        return null;
    }

    private static Button BuildLinkButton(string text, Uri uri, Func<string, Task>? onLocalFileLink)
    {
        var label = new TextBlock { Text = text };
        var button = new Button { Content = label };
        button.Classes.Add("MdLink");
        button.Click += async (sender, _) =>
        {
            if (uri.IsFile && onLocalFileLink is not null)
            {
                await onLocalFileLink(uri.LocalPath);
            }
            else
            {
                var topLevel = TopLevel.GetTopLevel(sender as Control);
                if (topLevel?.Launcher is { } launcher)
                    await launcher.LaunchUriAsync(uri);
            }
        };
        return button;
    }

    private static bool TryGetSingleImageLink(ParagraphBlock paragraph, out MdLinkInline? imageLink)
    {
        imageLink = null;
        if (paragraph.Inline is null)
            return false;

        var inlines = paragraph.Inline.ToList();
        if (inlines.Count == 1 && inlines[0] is MdLinkInline link && link.IsImage)
        {
            imageLink = link;
            return true;
        }

        return false;
    }

    private static Control RenderImage(MdLinkInline imageLink, string? basePath)
    {
        string url = imageLink.Url ?? string.Empty;
        string alt = GetRawText(imageLink);

        string? resolvedPath = ResolveImagePath(url, basePath);
        if (resolvedPath is not null)
        {
            try
            {
                var bitmap = new Bitmap(resolvedPath);
                return new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MaxWidth = bitmap.Size.Width,
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MarkdownRenderService] Image load error: {ex.Message}");
            }
        }

        var fallback = new TextBlock { Text = string.IsNullOrEmpty(alt) ? url : $"[{alt}]" };
        fallback.Classes.Add("MdBody");
        return fallback;
    }

    private static string? ResolveImagePath(string url, string? basePath)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(basePath))
            return null;

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return null;

        try
        {
            string fullPath = Path.GetFullPath(url, basePath);
            return File.Exists(fullPath) ? fullPath : null;
        }
        catch
        {
            return null;
        }
    }

    private Border RenderQuote(QuoteBlock quote, string? basePath, Func<string, Task>? onLocalFileLink)
    {
        var inner = new StackPanel { Spacing = 6 };
        foreach (var block in quote)
        {
            var control = RenderBlock(block, basePath, onLocalFileLink);
            if (control is not null)
                inner.Children.Add(control);
        }

        var border = new Border { Child = inner };
        border.Classes.Add("MdBlockquote");
        return border;
    }

    private static Border RenderFencedCode(FencedCodeBlock fenced)
    {
        var panel = new StackPanel();

        if (!string.IsNullOrWhiteSpace(fenced.Info))
        {
            var lang = new TextBlock { Text = fenced.Info };
            lang.Classes.Add("MdCodeLang");
            panel.Children.Add(lang);
        }

        var code = new TextBlock { Text = fenced.Lines.ToString().TrimEnd() };
        code.Classes.Add("MdCodeText");
        panel.Children.Add(code);

        var border = new Border { Child = panel };
        border.Classes.Add("MdCodeBlock");
        return border;
    }

    private static Border RenderGenericCode(CodeBlock codeBlock)
    {
        var code = new TextBlock { Text = codeBlock.Lines.ToString().TrimEnd() };
        code.Classes.Add("MdCodeText");

        var border = new Border { Child = code };
        border.Classes.Add("MdCodeBlock");
        return border;
    }

    private StackPanel RenderList(ListBlock list, string? basePath, Func<string, Task>? onLocalFileLink)
    {
        var panel = new StackPanel { Spacing = 4 };

        int index = list.IsOrdered
            ? (int.TryParse(list.OrderedStart, out var s) ? s : 1)
            : 0;

        foreach (var rawItem in list)
        {
            if (rawItem is not ListItemBlock listItem)
                continue;

            var markerText = list.IsOrdered ? $"{index}." : "•";
            var markerClass = list.IsOrdered ? "MdListNum" : "MdListBullet";
            index++;

            var marker = new TextBlock { Text = markerText };
            marker.Classes.Add(markerClass);

            Control itemContent = BuildListItemContent(listItem, basePath, onLocalFileLink);

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Avalonia.Thickness(0),
            };
            Grid.SetColumn(marker, 0);
            Grid.SetColumn(itemContent, 1);
            row.Children.Add(marker);
            row.Children.Add(itemContent);

            panel.Children.Add(row);
        }

        return panel;
    }

    private Control BuildListItemContent(ListItemBlock listItem, string? basePath, Func<string, Task>? onLocalFileLink)
    {
        var blocks = listItem.OfType<Block>().ToList();

        if (blocks is [ParagraphBlock singlePara])
        {
            var tb = new TextBlock();
            tb.Classes.Add("MdListText");
            PopulateInlines(tb, singlePara.Inline);
            return tb;
        }

        var inner = new StackPanel { Spacing = 4 };
        foreach (var block in blocks)
        {
            var control = RenderBlock(block, basePath, onLocalFileLink);
            if (control is not null)
                inner.Children.Add(control);
        }
        return inner;
    }

    private static Border RenderTable(Table table)
    {
        var firstRow = table.OfType<TableRow>().FirstOrDefault();
        int colCount = firstRow?.Count ?? 0;

        if (colCount == 0)
        {
            var empty = new Border();
            empty.Classes.Add("MdTable");
            return empty;
        }

        var colDefsString = string.Join(",", Enumerable.Repeat("*", colCount));

        var outerPanel = new StackPanel();

        int rowIndex = 0;
        foreach (var row in table.OfType<TableRow>())
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(colDefsString) };

            int colIndex = 0;
            foreach (var cell in row.OfType<TableCell>())
            {
                var alignment = table.ColumnDefinitions.ElementAtOrDefault(colIndex)
                    ?.Alignment ?? TableColumnAlign.Left;

                var textAlign = alignment switch
                {
                    TableColumnAlign.Center => TextAlignment.Center,
                    TableColumnAlign.Right => TextAlignment.Right,
                    _ => TextAlignment.Left,
                };

                var cellText = new TextBlock
                {
                    TextAlignment = textAlign,
                    TextWrapping = TextWrapping.Wrap,
                };
                cellText.Classes.Add(row.IsHeader ? "MdTableHeadCell" : "MdTableCell");

                var para = cell.OfType<ParagraphBlock>().FirstOrDefault();
                if (para?.Inline is not null)
                    PopulateInlines(cellText, para.Inline);

                Grid.SetColumn(cellText, colIndex);
                grid.Children.Add(cellText);
                colIndex++;
            }

            var rowBorder = new Border { Child = grid };
            rowBorder.Classes.Add(
                row.IsHeader ? "MdTableHeader" :
                rowIndex % 2 == 0 ? "MdTableRow" :
                                    "MdTableRowAlt");

            outerPanel.Children.Add(rowBorder);
            if (!row.IsHeader)
                rowIndex++;
        }

        var outer = new Border { Child = outerPanel };
        outer.Classes.Add("MdTable");
        return outer;
    }

    private static Border RenderHr()
    {
        var border = new Border();
        border.Classes.Add("MdHr");
        return border;
    }

    private static TextBlock? RenderFallback(Block block)
    {
        string text = string.Empty;
        if (block is LeafBlock leaf && leaf.Lines.Count > 0)
            text = leaf.Lines.ToString().Trim();

        if (string.IsNullOrEmpty(text))
            return null;

        var tb = new TextBlock { Text = text };
        tb.Classes.Add("MdBody");
        return tb;
    }

    private static void PopulateInlines(TextBlock textBlock, MdContainerInline? container)
    {
        if (container is null)
            return;

        foreach (var inline in container)
        {
            var inlineControl = BuildInline(inline);
            if (inlineControl is not null)
                textBlock.Inlines!.Add(inlineControl);
        }
    }

    private static Inline? BuildInline(MdInline inline)
    {
        switch (inline)
        {
            case MdLiteralInline literal:
                return new Run(literal.Content.ToString());

            case MdEmphasisInline emphasis:
                var span = new Span();
                if (emphasis.DelimiterCount >= 2)
                    span.FontWeight = FontWeight.Bold;
                else
                    span.FontStyle = FontStyle.Italic;
                foreach (var child in emphasis)
                {
                    var childInline = BuildInline(child);
                    if (childInline is not null)
                        span.Inlines.Add(childInline);
                }
                return span;

            case MdCodeInline code:
                return new Run(code.Content)
                {
                    FontFamily = new FontFamily("avares://QuillStone/Assets/Fonts#JetBrains Mono, Consolas, monospace"),
                };

            case MdLinkInline link when link.IsImage:
                var alt = GetRawText(link);
                return new Run(string.IsNullOrEmpty(alt) ? link.Url ?? string.Empty : $"[{alt}]");

            case MdLinkInline link:
                return new Run(GetRawText(link));

            case MdLineBreakInline lineBreak:
                return lineBreak.IsHard ? new LineBreak() : new Run(" ");

            case MdContainerInline container:
                var wrapper = new Span();
                foreach (var child in container)
                {
                    var childInline = BuildInline(child);
                    if (childInline is not null)
                        wrapper.Inlines.Add(childInline);
                }
                return wrapper;

            default:
                return null;
        }
    }

    private static string GetRawText(MdContainerInline? container)
    {
        if (container is null)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case MdLiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case MdCodeInline code:
                    sb.Append(code.Content);
                    break;
                case MdLineBreakInline:
                    sb.Append(' ');
                    break;
                case MdContainerInline inner:
                    sb.Append(GetRawText(inner));
                    break;
            }
        }
        return sb.ToString();
    }
}
