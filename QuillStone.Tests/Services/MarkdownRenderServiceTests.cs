using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class MarkdownRenderServiceTests
{
    private readonly MarkdownRenderService _service = new();

    [Fact]
    public void Render_EmptyString_ReturnsEmptyList()
    {
        var result = _service.Render(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Render_WhitespaceOnly_ReturnsEmptyList()
    {
        var result = _service.Render("   \n  ");

        Assert.Empty(result);
    }

    [AvaloniaFact]
    public void Render_H1_ReturnsTextBlockWithMdH1Class()
    {
        var result = _service.Render("# Heading One");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH1", tb.Classes);
    }

    [AvaloniaFact]
    public void Render_H2_ReturnsTextBlockWithMdH2Class()
    {
        var result = _service.Render("## Heading Two");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH2", tb.Classes);
    }

    [AvaloniaFact]
    public void Render_H3_ReturnsTextBlockWithMdH3Class()
    {
        var result = _service.Render("### Heading Three");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH3", tb.Classes);
    }

    [AvaloniaFact]
    public void Render_H4OrDeeper_ReturnsTextBlockWithMdH4Class()
    {
        var result = _service.Render("#### Heading Four");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH4", tb.Classes);
    }

    [AvaloniaFact]
    public void Render_Paragraph_ReturnsTextBlockWithMdBodyClass()
    {
        var result = _service.Render("Some regular paragraph text.");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
    }

    [AvaloniaFact]
    public void Render_BoldText_SpanHasBoldFontWeight()
    {
        var result = _service.Render("**bold**");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.NotNull(tb.Inlines);
        var span = tb.Inlines!.OfType<Span>().FirstOrDefault();
        Assert.NotNull(span);
        Assert.Equal(FontWeight.Bold, span!.FontWeight);
    }

    [AvaloniaFact]
    public void Render_ItalicText_SpanHasItalicFontStyle()
    {
        var result = _service.Render("*italic*");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.NotNull(tb.Inlines);
        var span = tb.Inlines!.OfType<Span>().FirstOrDefault();
        Assert.NotNull(span);
        Assert.Equal(FontStyle.Italic, span!.FontStyle);
    }

    [AvaloniaFact]
    public void Render_StrikethroughText_SpanHasStrikethroughDecoration()
    {
        var result = _service.Render("~~strikethrough~~");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.NotNull(tb.Inlines);
        var span = tb.Inlines!.OfType<Span>().FirstOrDefault();
        Assert.NotNull(span);
        Assert.NotNull(span!.TextDecorations);
        Assert.Contains(span.TextDecorations!, d => d.Location == TextDecorationLocation.Strikethrough);
    }

    [AvaloniaFact]
    public void Render_Blockquote_ReturnsBorderWithMdBlockquoteClass()
    {
        var result = _service.Render("> A quoted line");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdBlockquote", border.Classes);
    }

    [AvaloniaFact]
    public void Render_Blockquote_ChildIsStackPanel()
    {
        var result = _service.Render("> A quoted line");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.IsType<StackPanel>(border.Child);
    }

    [AvaloniaFact]
    public void Render_FencedCodeBlock_ReturnsBorderWithMdCodeBlockClass()
    {
        var result = _service.Render("```\nvar x = 1;\n```");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdCodeBlock", border.Classes);
    }

    [AvaloniaFact]
    public void Render_FencedCodeBlockWithLanguage_IncludesLangLabel()
    {
        var result = _service.Render("```csharp\nvar x = 1;\n```");

        var border = Assert.IsType<Border>(Assert.Single(result));
        var panel = Assert.IsType<StackPanel>(border.Child);
        var langLabel = panel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Classes.Contains("MdCodeLang"));
        Assert.NotNull(langLabel);
        Assert.Equal("csharp", langLabel!.Text);
    }

    [AvaloniaFact]
    public void Render_IndentedCodeBlock_ReturnsBorderWithMdCodeBlockClass()
    {
        var result = _service.Render("    var x = 1;");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdCodeBlock", border.Classes);
    }

    [AvaloniaFact]
    public void Render_HorizontalRule_ReturnsBorderWithMdHrClass()
    {
        var result = _service.Render("---");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdHr", border.Classes);
    }

    [AvaloniaFact]
    public void Render_UnorderedList_ReturnsStackPanel()
    {
        var result = _service.Render("- Item A\n- Item B");

        var panel = Assert.IsType<StackPanel>(Assert.Single(result));
        Assert.Equal(2, panel.Children.Count);
    }

    [AvaloniaFact]
    public void Render_OrderedList_ReturnsStackPanel()
    {
        var result = _service.Render("1. First\n2. Second");

        var panel = Assert.IsType<StackPanel>(Assert.Single(result));
        Assert.Equal(2, panel.Children.Count);
    }

    [AvaloniaFact]
    public void Render_MalformedMarkdown_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Render("**unclosed bold"));

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Render_MixedContent_ReturnsMultipleControls()
    {
        string md = "# Title\n\nParagraph.\n\n---";

        var result = _service.Render(md);

        Assert.Equal(3, result.Count);
    }

    [AvaloniaFact]
    public void Render_VeryLongInput_DoesNotThrow()
    {
        string md = string.Concat(Enumerable.Repeat("Line of text.\n", 1000));

        var exception = Record.Exception(() => _service.Render(md));

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Render_ImageOnlyParagraph_NoBasePath_ReturnsFallbackTextBlock()
    {
        var result = _service.Render("![alt text](image.png)");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
        Assert.Equal("[alt text]", tb.Text);
    }

    [AvaloniaFact]
    public void Render_ImageOnlyParagraph_MissingFile_ReturnsFallbackTextBlock()
    {
        var result = _service.Render("![alt text](missing.png)", "/nonexistent/dir");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
        Assert.Equal("[alt text]", tb.Text);
    }

    [AvaloniaFact]
    public void Render_ImageOnlyParagraph_NoAltAndMissingFile_ReturnsFallbackWithUrl()
    {
        var result = _service.Render("![](image.png)", "/nonexistent/dir");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
        Assert.Equal("image.png", tb.Text);
    }

    [AvaloniaFact]
    public void Render_ImageOnlyParagraph_ExistingFile_ReturnsControl()
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        string imgPath = Path.Combine(dir, "test.png");
        try
        {
            File.WriteAllBytes(imgPath, MinimalPng);

            var exception = Record.Exception(() => _service.Render("![photo](test.png)", dir));

            Assert.Null(exception);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public void Render_AbsoluteImageUrl_ReturnsFallback()
    {
        var result = _service.Render("![remote](https://example.com/image.png)", "/some/dir");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
    }

    // Minimal 1×1 transparent PNG (67 bytes) used for image load tests.
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    [AvaloniaFact]
    public void Render_ParagraphWithNoLinks_StillRendersAsSingleTextBlock()
    {
        var result = _service.Render("Just plain text without any links.");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
    }

    [AvaloniaFact]
    public void Render_HttpLink_ReturnsWrapPanelWithMdLinkButton()
    {
        var result = _service.Render("[Visit](https://example.com)");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        var button = wrap.Children.OfType<Button>().FirstOrDefault();
        Assert.NotNull(button);
        Assert.Contains("MdLink", button!.Classes);
        var label = Assert.IsType<TextBlock>(button.Content);
        Assert.Equal("Visit", label.Text);
    }

    [AvaloniaFact]
    public void Render_HttpsLink_ReturnsWrapPanelWithMdLinkButton()
    {
        var result = _service.Render("[Secure](https://secure.example.com)");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        var button = wrap.Children.OfType<Button>().FirstOrDefault();
        Assert.NotNull(button);
        Assert.Contains("MdLink", button!.Classes);
    }

    [AvaloniaFact]
    public void Render_AnchorLink_RendersAsPlainText()
    {
        var result = _service.Render("[Section](#section)");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        // Anchor links are resolved to null — rendered as TextBlock.MdBody, not Button
        Assert.Empty(wrap.Children.OfType<Button>());
        var tb = wrap.Children.OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(tb);
        Assert.Contains("MdBody", tb!.Classes);
    }

    [AvaloniaFact]
    public void Render_BlockedProtocolLink_RendersAsPlainText()
    {
        var result = _service.Render("[Email](mailto:user@example.com)");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        Assert.Empty(wrap.Children.OfType<Button>());
        var tb = wrap.Children.OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(tb);
        Assert.Contains("MdBody", tb!.Classes);
    }

    [AvaloniaFact]
    public void Render_RelativeLink_WithBasePath_ReturnsWrapPanelWithButton()
    {
        var result = _service.Render("[Read more](other.md)", "/some/dir");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        var button = wrap.Children.OfType<Button>().FirstOrDefault();
        Assert.NotNull(button);
        Assert.Contains("MdLink", button!.Classes);
        var label = Assert.IsType<TextBlock>(button.Content);
        Assert.Equal("Read more", label.Text);
    }

    [AvaloniaFact]
    public void Render_RelativeLink_WithoutBasePath_RendersAsPlainText()
    {
        var result = _service.Render("[Read more](other.md)");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        Assert.Empty(wrap.Children.OfType<Button>());
        var tb = wrap.Children.OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(tb);
        Assert.Contains("MdBody", tb!.Classes);
    }

    [AvaloniaFact]
    public void Render_MixedTextAndLink_ReturnsWrapPanelWithTextBlockAndButton()
    {
        var result = _service.Render("Click [here](https://example.com) now.");

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        Assert.NotEmpty(wrap.Children.OfType<Button>());
        Assert.NotEmpty(wrap.Children.OfType<TextBlock>());
    }

    [AvaloniaFact]
    public async Task Render_LocalFileLink_InvokesCallbackInsteadOfLauncher()
    {
        string? capturedPath = null;
        Task OnLocalFileLink(string path)
        {
            capturedPath = path;
            return Task.CompletedTask;
        }

        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var result = _service.Render("[Notes](notes.md)", dir, OnLocalFileLink);

        var wrap = Assert.IsType<WrapPanel>(Assert.Single(result));
        var button = wrap.Children.OfType<Button>().Single();

        // Simulate button click by invoking the command
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(50); // let async handler complete

        Assert.NotNull(capturedPath);
        Assert.EndsWith("notes.md", capturedPath, StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public void Render_PipeTable_ReturnsBorderWithMdTableClass()
    {
        string md = "| A | B |\n|---|---|\n| 1 | 2 |";

        var result = _service.Render(md);

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdTable", border.Classes);
    }

    [AvaloniaFact]
    public void Render_PipeTable_HeaderRowHasMdTableHeaderClass()
    {
        string md = "| Name | Age |\n|------|-----|\n| Alice | 30 |";

        var result = _service.Render(md);

        var outer = Assert.IsType<Border>(Assert.Single(result));
        var panel = Assert.IsType<StackPanel>(outer.Child);
        var headerRow = panel.Children.OfType<Border>().FirstOrDefault();
        Assert.NotNull(headerRow);
        Assert.Contains("MdTableHeader", headerRow!.Classes);
    }

    [AvaloniaFact]
    public void Render_PipeTable_HeaderCellsHaveMdTableHeadCellClass()
    {
        string md = "| Col1 | Col2 |\n|------|------|\n| a | b |";

        var result = _service.Render(md);

        var outer = Assert.IsType<Border>(Assert.Single(result));
        var panel = Assert.IsType<StackPanel>(outer.Child);
        var headerRow = panel.Children.OfType<Border>().First(b => b.Classes.Contains("MdTableHeader"));
        var grid = Assert.IsType<Grid>(headerRow.Child);
        var headCells = grid.Children.OfType<TextBlock>().ToList();
        Assert.All(headCells, tb => Assert.Contains("MdTableHeadCell", tb.Classes));
    }

    [AvaloniaFact]
    public void Render_PipeTable_BodyRowsAlternate_MdTableRow_And_MdTableRowAlt()
    {
        string md = "| X |\n|---|\n| r0 |\n| r1 |\n| r2 |";

        var result = _service.Render(md);

        var outer = Assert.IsType<Border>(Assert.Single(result));
        var panel = Assert.IsType<StackPanel>(outer.Child);
        var bodyRows = panel.Children.OfType<Border>()
            .Where(b => !b.Classes.Contains("MdTableHeader"))
            .ToList();

        Assert.Equal(3, bodyRows.Count);
        Assert.Contains("MdTableRow", bodyRows[0].Classes);
        Assert.Contains("MdTableRowAlt", bodyRows[1].Classes);
        Assert.Contains("MdTableRow", bodyRows[2].Classes);
    }

    [AvaloniaFact]
    public void Render_PipeTable_EmptyTable_DoesNotCrash()
    {
        // A table AST with no rows — simulate by rendering degenerate input
        var exception = Record.Exception(() => _service.Render("| |\n|---|"));

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Render_PipeTable_CellContentIsRendered()
    {
        string md = "| Hello |\n|-------|\n| World |";

        var result = _service.Render(md);

        var outer = Assert.IsType<Border>(Assert.Single(result));
        var panel = Assert.IsType<StackPanel>(outer.Child);
        var bodyRow = panel.Children.OfType<Border>()
            .First(b => b.Classes.Contains("MdTableRow") || b.Classes.Contains("MdTableRowAlt"));
        var grid = Assert.IsType<Grid>(bodyRow.Child);
        var cell = grid.Children.OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(cell);
        Assert.Contains("MdTableCell", cell!.Classes);
    }
}
