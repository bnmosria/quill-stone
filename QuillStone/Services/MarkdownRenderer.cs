using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace QuillStone.Services;

/// <summary>
/// Converts a Markdown string into an Avalonia control tree using design-system tokens.
/// Supports: ATX headings H1–H3, paragraphs, blockquotes, fenced code blocks,
/// unordered/ordered lists, horizontal rules, and inline bold/italic/code.
/// </summary>
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private static readonly Regex FencedCodeOpenRe = new(@"^(`{3,}|~{3,})", RegexOptions.Compiled);
    private static readonly Regex OrderedListRe = new(@"^(\d+)\.\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex UnorderedListRe = new(@"^[-*+]\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex HeadingRe = new(@"^(#{1,3})\s+(.*)", RegexOptions.Compiled);
    private static readonly Regex HrRe = new(@"^(---+|___+|\*\*\*+)\s*$", RegexOptions.Compiled);

    public Control Render(string markdown)
    {
        var root = new StackPanel { Spacing = 0 };
        if (string.IsNullOrEmpty(markdown))
            return root;

        var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var blocks = ParseBlocks(lines);

        foreach (var block in blocks)
            root.Children.Add(BlockToControl(block));

        return root;
    }

    // ── Resource helpers ─────────────────────────────────────────────────────

    private static IBrush GetBrush(string key, IBrush fallback)
    {
        if (Application.Current?.TryGetResource(key, ThemeVariant.Default, out var value) == true
            && value is IBrush brush)
            return brush;
        return fallback;
    }

    private static readonly IBrush FallbackFg   = Brushes.Black;
    private static readonly IBrush FallbackMuted = new SolidColorBrush(Color.FromRgb(200, 200, 200));

    // ── Block Parsing ────────────────────────────────────────────────────────

    private static List<Block> ParseBlocks(string[] lines)
    {
        var blocks = new List<Block>();
        int i = 0;

        while (i < lines.Length)
        {
            string line = lines[i];

            // Fenced code block
            var fenceMatch = FencedCodeOpenRe.Match(line);
            if (fenceMatch.Success)
            {
                string fence = fenceMatch.Value;
                var codeLines = new List<string>();
                i++;
                while (i < lines.Length && !lines[i].StartsWith(fence, StringComparison.Ordinal))
                {
                    codeLines.Add(lines[i]);
                    i++;
                }
                i++; // consume closing fence
                blocks.Add(new Block(BlockKind.Code, string.Join("\n", codeLines)));
                continue;
            }

            // Horizontal rule
            if (HrRe.IsMatch(line))
            {
                blocks.Add(new Block(BlockKind.HorizontalRule, string.Empty));
                i++;
                continue;
            }

            // Heading
            var headingMatch = HeadingRe.Match(line);
            if (headingMatch.Success)
            {
                int level = headingMatch.Groups[1].Length;
                string text = headingMatch.Groups[2].Value;
                var kind = level switch
                {
                    1 => BlockKind.H1,
                    2 => BlockKind.H2,
                    _ => BlockKind.H3
                };
                blocks.Add(new Block(kind, text));
                i++;
                continue;
            }

            // Blockquote — collect consecutive quote lines
            if (line.StartsWith("> ", StringComparison.Ordinal) || line == ">")
            {
                var quoteLines = new List<string>();
                while (i < lines.Length &&
                       (lines[i].StartsWith("> ", StringComparison.Ordinal) || lines[i] == ">"))
                {
                    quoteLines.Add(lines[i].Length >= 2 ? lines[i][2..] : string.Empty);
                    i++;
                }
                blocks.Add(new Block(BlockKind.Blockquote, string.Join("\n", quoteLines)));
                continue;
            }

            // Unordered list — collect consecutive list items
            if (UnorderedListRe.IsMatch(line))
            {
                var items = new List<string>();
                while (i < lines.Length && UnorderedListRe.IsMatch(lines[i]))
                {
                    items.Add(UnorderedListRe.Match(lines[i]).Groups[1].Value);
                    i++;
                }
                blocks.Add(new Block(BlockKind.BulletList, string.Join("\n", items)));
                continue;
            }

            // Ordered list — collect consecutive list items
            if (OrderedListRe.IsMatch(line))
            {
                var items = new List<string>();
                while (i < lines.Length && OrderedListRe.IsMatch(lines[i]))
                {
                    items.Add(OrderedListRe.Match(lines[i]).Groups[2].Value);
                    i++;
                }
                blocks.Add(new Block(BlockKind.OrderedList, string.Join("\n", items)));
                continue;
            }

            // Blank line — paragraph break (skip silently)
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Paragraph — collect non-blank, non-special lines
            var paraLines = new List<string>();
            while (i < lines.Length
                   && !string.IsNullOrWhiteSpace(lines[i])
                   && !HeadingRe.IsMatch(lines[i])
                   && !HrRe.IsMatch(lines[i])
                   && !FencedCodeOpenRe.IsMatch(lines[i])
                   && !lines[i].StartsWith("> ", StringComparison.Ordinal)
                   && !UnorderedListRe.IsMatch(lines[i])
                   && !OrderedListRe.IsMatch(lines[i]))
            {
                paraLines.Add(lines[i]);
                i++;
            }

            if (paraLines.Count > 0)
                blocks.Add(new Block(BlockKind.Paragraph, string.Join(" ", paraLines)));
        }

        return blocks;
    }

    // ── Block → Control ──────────────────────────────────────────────────────

    private Control BlockToControl(Block block) => block.Kind switch
    {
        BlockKind.H1 => BuildHeading(block.Text, 28, FontWeight.Bold, new Thickness(0, 4, 0, 6)),
        BlockKind.H2 => BuildHeading(block.Text, 20, FontWeight.SemiBold, new Thickness(0, 12, 0, 6)),
        BlockKind.H3 => BuildHeading(block.Text, 16, FontWeight.SemiBold, new Thickness(0, 10, 0, 4)),
        BlockKind.Paragraph => BuildParagraph(block.Text),
        BlockKind.Blockquote => BuildBlockquote(block.Text),
        BlockKind.Code => BuildCodeBlock(block.Text),
        BlockKind.BulletList => BuildBulletList(block.Text),
        BlockKind.OrderedList => BuildOrderedList(block.Text),
        BlockKind.HorizontalRule => BuildHorizontalRule(),
        _ => new TextBlock { Text = block.Text }
    };

    private static readonly FontFamily PreviewFont = new("avares://QuillStone/Assets/Fonts#Lora, Georgia, serif");
    private static readonly FontFamily EditorFont  = new("avares://QuillStone/Assets/Fonts#JetBrains Mono, Consolas, monospace");

    private TextBlock BuildHeading(string text, double size, FontWeight weight, Thickness margin)
    {
        var tb = new TextBlock
        {
            FontFamily = PreviewFont,
            FontSize = size,
            FontWeight = weight,
            TextWrapping = TextWrapping.Wrap,
            Margin = margin,
            Foreground = GetBrush("Brush.Text.Primary", FallbackFg),
        };
        AddInlines(tb, text);
        return tb;
    }

    private TextBlock BuildParagraph(string text)
    {
        var tb = new TextBlock
        {
            FontFamily = PreviewFont,
            FontSize = 14.5,
            LineHeight = 1.6 * 14.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = GetBrush("Brush.Text.Secondary", FallbackFg),
        };
        AddInlines(tb, text);
        return tb;
    }

    private Border BuildBlockquote(string text)
    {
        var inner = new TextBlock
        {
            FontFamily = PreviewFont,
            FontSize = 14,
            FontStyle = FontStyle.Italic,
            LineHeight = 1.6 * 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetBrush("Brush.Text.Secondary", FallbackFg),
        };
        AddInlines(inner, text);

        return new Border
        {
            BorderBrush = GetBrush("Brush.Accent.Primary", FallbackFg),
            BorderThickness = new Thickness(3, 0, 0, 0),
            Background = GetBrush("Brush.Accent.Muted", FallbackMuted),
            Padding = new Thickness(12, 8),
            CornerRadius = new CornerRadius(0, 4, 4, 0),
            Margin = new Thickness(0, 4, 0, 10),
            Child = inner,
        };
    }

    private Border BuildCodeBlock(string text)
    {
        var codeText = new TextBlock
        {
            FontFamily = EditorFont,
            FontSize = 12.5,
            TextWrapping = TextWrapping.NoWrap,
            Text = text,
            Foreground = GetBrush("Brush.Text.Secondary", FallbackFg),
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = codeText,
        };

        return new Border
        {
            Background = GetBrush("Brush.Background.Overlay", FallbackMuted),
            BorderBrush = GetBrush("Brush.Border.Default", FallbackMuted),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(14, 10),
            Margin = new Thickness(0, 4, 0, 10),
            Child = scrollViewer,
        };
    }

    private StackPanel BuildBulletList(string text)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 10) };
        var accent = GetBrush("Brush.Accent.Primary", FallbackFg);
        var body   = GetBrush("Brush.Text.Secondary", FallbackFg);

        foreach (var item in text.Split('\n'))
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("16,*") };
            var bullet = new TextBlock
            {
                Text = "•",
                FontSize = 14.5,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = accent,
            };

            var tb = new TextBlock
            {
                FontFamily = PreviewFont,
                FontSize = 14.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = body,
            };
            AddInlines(tb, item);

            Grid.SetColumn(bullet, 0);
            Grid.SetColumn(tb, 1);
            row.Children.Add(bullet);
            row.Children.Add(tb);
            panel.Children.Add(row);
        }
        return panel;
    }

    private StackPanel BuildOrderedList(string text)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 10) };
        var items  = text.Split('\n');
        var accent = GetBrush("Brush.Accent.Primary", FallbackFg);
        var body   = GetBrush("Brush.Text.Secondary", FallbackFg);

        for (int n = 0; n < items.Length; n++)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("22,*") };
            var number = new TextBlock
            {
                Text = $"{n + 1}.",
                FontFamily = PreviewFont,
                FontSize = 14.5,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = accent,
            };

            var tb = new TextBlock
            {
                FontFamily = PreviewFont,
                FontSize = 14.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = body,
            };
            AddInlines(tb, items[n]);

            Grid.SetColumn(number, 0);
            Grid.SetColumn(tb, 1);
            row.Children.Add(number);
            row.Children.Add(tb);
            panel.Children.Add(row);
        }
        return panel;
    }

    private Border BuildHorizontalRule()
    {
        return new Border
        {
            Background = GetBrush("Brush.Border.Separator", FallbackMuted),
            Height = 1,
            Margin = new Thickness(0, 10, 0, 10),
        };
    }

    // ── Inline Parsing ───────────────────────────────────────────────────────

    // Matches (in priority order): **bold**, __bold__, *italic*, _italic_, `code`, [text](url)
    private static readonly Regex InlineRe = new(
        @"\*\*(.+?)\*\*|__(.+?)__|" +   // bold
        @"\*(.+?)\*|_(.+?)_|" +          // italic
        @"`(.+?)`|" +                     // inline code
        @"\[([^\]]+)\]\([^\)]+\)",        // link
        RegexOptions.Compiled | RegexOptions.Singleline);

    private void AddInlines(TextBlock tb, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            tb.Text = string.Empty;
            return;
        }

        var inlines = new InlineCollection();
        int pos = 0;
        foreach (Match m in InlineRe.Matches(text))
        {
            if (m.Index > pos)
                inlines.Add(new Run(text[pos..m.Index]));

            // Bold (**..** or __..__)
            if (m.Groups[1].Success)
                inlines.Add(new Run(m.Groups[1].Value) { FontWeight = FontWeight.Bold });
            else if (m.Groups[2].Success)
                inlines.Add(new Run(m.Groups[2].Value) { FontWeight = FontWeight.Bold });
            // Italic (*..* or _.._)
            else if (m.Groups[3].Success)
                inlines.Add(new Run(m.Groups[3].Value) { FontStyle = FontStyle.Italic });
            else if (m.Groups[4].Success)
                inlines.Add(new Run(m.Groups[4].Value) { FontStyle = FontStyle.Italic });
            // Inline code
            else if (m.Groups[5].Success)
            {
                inlines.Add(new Run(m.Groups[5].Value)
                {
                    FontFamily = EditorFont,
                    FontSize = 13,
                    Foreground = GetBrush("Brush.Accent.Primary", FallbackFg),
                    Background = GetBrush("Brush.Accent.Muted", FallbackMuted),
                });
            }
            // Link: [text](url)
            else if (m.Groups[6].Success)
            {
                inlines.Add(new Run(m.Groups[6].Value)
                {
                    TextDecorations = TextDecorations.Underline,
                    Foreground = GetBrush("Brush.Syntax.Link", FallbackFg),
                });
            }

            pos = m.Index + m.Length;
        }

        if (pos < text.Length)
            inlines.Add(new Run(text[pos..]));

        tb.Inlines = inlines;
    }

    // ── Internal Model ───────────────────────────────────────────────────────

    private enum BlockKind
    {
        H1, H2, H3,
        Paragraph, Blockquote,
        Code, BulletList, OrderedList,
        HorizontalRule
    }

    private sealed record Block(BlockKind Kind, string Text);
}

