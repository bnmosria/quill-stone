using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Markdig;
using Markdig.Syntax;
using MdInline = Markdig.Syntax.Inlines.Inline;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MdEmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using MdCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MdLinkInline = Markdig.Syntax.Inlines.LinkInline;
using MdLiteralInline = Markdig.Syntax.Inlines.LiteralInline;
using MdLineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;

namespace QuillStone.Services;

public class MarkdownRenderService : IMarkdownRenderService
{
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().Build();

    public IReadOnlyList<Control> Render(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return [];

        try
        {
            var document = Markdig.Markdown.Parse(markdown, _pipeline);
            var controls = new List<Control>();

            foreach (var block in document)
            {
                var control = RenderBlock(block);
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

    private Control? RenderBlock(Block block) => block switch
    {
        HeadingBlock h => RenderHeading(h),
        ParagraphBlock p => RenderParagraph(p),
        QuoteBlock q => RenderQuote(q),
        FencedCodeBlock f => RenderFencedCode(f),
        CodeBlock c => RenderGenericCode(c),
        ListBlock l => RenderList(l),
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

    private static TextBlock RenderParagraph(ParagraphBlock paragraph)
    {
        var tb = new TextBlock();
        tb.Classes.Add("MdBody");
        PopulateInlines(tb, paragraph.Inline);
        return tb;
    }

    private Border RenderQuote(QuoteBlock quote)
    {
        var inner = new StackPanel { Spacing = 6 };
        foreach (var block in quote)
        {
            var control = RenderBlock(block);
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

    private StackPanel RenderList(ListBlock list)
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

            Control itemContent = BuildListItemContent(listItem);

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

    private Control BuildListItemContent(ListItemBlock listItem)
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
            var control = RenderBlock(block);
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
