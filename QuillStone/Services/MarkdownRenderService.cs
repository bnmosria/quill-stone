using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Markdig;
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
        new MarkdownPipelineBuilder().Build();

    public IReadOnlyList<Control> Render(string markdown, string? basePath = null)
    {
        if (string.IsNullOrEmpty(markdown))
            return [];

        try
        {
            var document = Markdig.Markdown.Parse(markdown, _pipeline);
            var controls = new List<Control>();

            foreach (var block in document)
            {
                var control = RenderBlock(block, basePath);
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

    private Control? RenderBlock(Block block, string? basePath) => block switch
    {
        HeadingBlock h => RenderHeading(h),
        ParagraphBlock p => RenderParagraph(p, basePath),
        QuoteBlock q => RenderQuote(q, basePath),
        FencedCodeBlock f => RenderFencedCode(f),
        CodeBlock c => RenderGenericCode(c),
        ListBlock l => RenderList(l, basePath),
        ThematicBreakBlock => RenderHr(),
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

    private static Control RenderParagraph(ParagraphBlock paragraph, string? basePath)
    {
        if (TryGetSingleImageLink(paragraph, out var imageLink))
            return RenderImage(imageLink!, basePath);

        var tb = new TextBlock();
        tb.Classes.Add("MdBody");
        PopulateInlines(tb, paragraph.Inline);
        return tb;
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

    private Border RenderQuote(QuoteBlock quote, string? basePath)
    {
        var inner = new StackPanel { Spacing = 6 };
        foreach (var block in quote)
        {
            var control = RenderBlock(block, basePath);
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

    private StackPanel RenderList(ListBlock list, string? basePath)
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

            Control itemContent = BuildListItemContent(listItem, basePath);

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

    private Control BuildListItemContent(ListItemBlock listItem, string? basePath)
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
            var control = RenderBlock(block, basePath);
            if (control is not null)
                inner.Children.Add(control);
        }
        return inner;
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
