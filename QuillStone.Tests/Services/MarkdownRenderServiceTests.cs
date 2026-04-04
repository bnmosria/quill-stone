using Avalonia.Controls;
using Avalonia.Controls.Documents;
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

    [Fact]
    public void Render_H1_ReturnsTextBlockWithMdH1Class()
    {
        var result = _service.Render("# Heading One");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH1", tb.Classes);
    }

    [Fact]
    public void Render_H2_ReturnsTextBlockWithMdH2Class()
    {
        var result = _service.Render("## Heading Two");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH2", tb.Classes);
    }

    [Fact]
    public void Render_H3_ReturnsTextBlockWithMdH3Class()
    {
        var result = _service.Render("### Heading Three");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH3", tb.Classes);
    }

    [Fact]
    public void Render_H4OrDeeper_ReturnsTextBlockWithMdH4Class()
    {
        var result = _service.Render("#### Heading Four");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdH4", tb.Classes);
    }

    [Fact]
    public void Render_Paragraph_ReturnsTextBlockWithMdBodyClass()
    {
        var result = _service.Render("Some regular paragraph text.");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
    }

    [Fact]
    public void Render_BoldText_SpanHasBoldFontWeight()
    {
        var result = _service.Render("**bold**");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.NotNull(tb.Inlines);
        var span = tb.Inlines!.OfType<Span>().FirstOrDefault();
        Assert.NotNull(span);
        Assert.Equal(FontWeight.Bold, span!.FontWeight);
    }

    [Fact]
    public void Render_ItalicText_SpanHasItalicFontStyle()
    {
        var result = _service.Render("*italic*");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.NotNull(tb.Inlines);
        var span = tb.Inlines!.OfType<Span>().FirstOrDefault();
        Assert.NotNull(span);
        Assert.Equal(FontStyle.Italic, span!.FontStyle);
    }

    [Fact]
    public void Render_Blockquote_ReturnsBorderWithMdBlockquoteClass()
    {
        var result = _service.Render("> A quoted line");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdBlockquote", border.Classes);
    }

    [Fact]
    public void Render_Blockquote_ChildIsStackPanel()
    {
        var result = _service.Render("> A quoted line");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.IsType<StackPanel>(border.Child);
    }

    [Fact]
    public void Render_FencedCodeBlock_ReturnsBorderWithMdCodeBlockClass()
    {
        var result = _service.Render("```\nvar x = 1;\n```");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdCodeBlock", border.Classes);
    }

    [Fact]
    public void Render_FencedCodeBlockWithLanguage_IncludesLangLabel()
    {
        var result = _service.Render("```csharp\nvar x = 1;\n```");

        var border = Assert.IsType<Border>(Assert.Single(result));
        var panel = Assert.IsType<StackPanel>(border.Child);
        var langLabel = panel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Classes.Contains("MdCodeLang"));
        Assert.NotNull(langLabel);
        Assert.Equal("csharp", langLabel!.Text);
    }

    [Fact]
    public void Render_IndentedCodeBlock_ReturnsBorderWithMdCodeBlockClass()
    {
        var result = _service.Render("    var x = 1;");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdCodeBlock", border.Classes);
    }

    [Fact]
    public void Render_HorizontalRule_ReturnsBorderWithMdHrClass()
    {
        var result = _service.Render("---");

        var border = Assert.IsType<Border>(Assert.Single(result));
        Assert.Contains("MdHr", border.Classes);
    }

    [Fact]
    public void Render_UnorderedList_ReturnsStackPanel()
    {
        var result = _service.Render("- Item A\n- Item B");

        var panel = Assert.IsType<StackPanel>(Assert.Single(result));
        Assert.Equal(2, panel.Children.Count);
    }

    [Fact]
    public void Render_OrderedList_ReturnsStackPanel()
    {
        var result = _service.Render("1. First\n2. Second");

        var panel = Assert.IsType<StackPanel>(Assert.Single(result));
        Assert.Equal(2, panel.Children.Count);
    }

    [Fact]
    public void Render_MalformedMarkdown_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Render("**unclosed bold"));

        Assert.Null(exception);
    }

    [Fact]
    public void Render_MixedContent_ReturnsMultipleControls()
    {
        string md = "# Title\n\nParagraph.\n\n---";

        var result = _service.Render(md);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Render_VeryLongInput_DoesNotThrow()
    {
        string md = string.Concat(Enumerable.Repeat("Line of text.\n", 1000));

        var exception = Record.Exception(() => _service.Render(md));

        Assert.Null(exception);
    }

    [Fact]
    public void Render_ImageOnlyParagraph_NoBasePath_ReturnsFallbackTextBlock()
    {
        var result = _service.Render("![alt text](image.png)");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
        Assert.Equal("[alt text]", tb.Text);
    }

    [Fact]
    public void Render_ImageOnlyParagraph_MissingFile_ReturnsFallbackTextBlock()
    {
        var result = _service.Render("![alt text](missing.png)", "/nonexistent/dir");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
        Assert.Equal("[alt text]", tb.Text);
    }

    [Fact]
    public void Render_ImageOnlyParagraph_NoAltAndMissingFile_ReturnsFallbackWithUrl()
    {
        var result = _service.Render("![](image.png)", "/nonexistent/dir");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
        Assert.Equal("image.png", tb.Text);
    }

    [Fact]
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

    [Fact]
    public void Render_AbsoluteImageUrl_ReturnsFallback()
    {
        var result = _service.Render("![remote](https://example.com/image.png)", "/some/dir");

        var tb = Assert.IsType<TextBlock>(Assert.Single(result));
        Assert.Contains("MdBody", tb.Classes);
    }

    // Minimal 1×1 transparent PNG (67 bytes) used for image load tests.
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
}
