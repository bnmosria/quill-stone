using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class MarkdownFormatterTests
{
    private readonly MarkdownFormatter _formatter = new();

    private static TextSelectionRange NoSelection(int pos = 0) => new(pos, pos);

    private static TextSelectionRange Selection(int start, int end) => new(start, end);

    [Fact]
    public void WrapSelection_NoSelection_InsertsPlaceholder()
    {
        var result = _formatter.WrapSelection("hello", NoSelection(5), "**", "**", "bold text");

        Assert.Equal("hello**bold text**", result.Text);
        Assert.Equal(7, result.SelectionStart);
        Assert.Equal(16, result.SelectionEnd);
    }

    [Fact]
    public void WrapSelection_WithSelection_WrapsSelectedText()
    {
        var result = _formatter.WrapSelection("hello world", Selection(6, 11), "**", "**", "bold text");

        Assert.Equal("hello **world**", result.Text);
        Assert.Equal(15, result.SelectionStart);
        Assert.Equal(15, result.SelectionEnd);
    }

    [Fact]
    public void WrapSelection_Bold_ProducesCorrectMarkup()
    {
        var result = _formatter.WrapSelection("text", Selection(0, 4), "**", "**", "bold text");

        Assert.Equal("**text**", result.Text);
    }

    [Fact]
    public void WrapSelection_Italic_ProducesCorrectMarkup()
    {
        var result = _formatter.WrapSelection("text", Selection(0, 4), "*", "*", "italic text");

        Assert.Equal("*text*", result.Text);
    }

    [Fact]
    public void WrapSelection_InlineCode_ProducesCorrectMarkup()
    {
        var result = _formatter.WrapSelection("text", Selection(0, 4), "`", "`", "code");

        Assert.Equal("`text`", result.Text);
    }

    [Fact]
    public void InsertLink_NoSelection_InsertsPlaceholderLinkText()
    {
        var result = _formatter.InsertLink("", NoSelection(), "https://example.com", "link text");

        Assert.Equal("[link text](https://example.com)", result.Text);
        Assert.Equal(1, result.SelectionStart);
        Assert.Equal(10, result.SelectionEnd);
    }

    [Fact]
    public void InsertLink_WithSelection_UsesSelectedTextAsLabel()
    {
        var result = _formatter.InsertLink("click here", Selection(0, 10), "https://example.com", "link text");

        Assert.Equal("[click here](https://example.com)", result.Text);
        Assert.Equal(33, result.SelectionStart);
    }

    [Fact]
    public void PrefixSelectedLines_BulletList_PrefixesSingleLine()
    {
        var result = _formatter.PrefixSelectedLines("item", NoSelection(), "- ");

        Assert.Equal("- item", result.Text);
    }

    [Fact]
    public void PrefixSelectedLines_BulletList_PrefixesMultipleLines()
    {
        var result = _formatter.PrefixSelectedLines("foo\nbar", Selection(0, 7), "- ");

        Assert.Equal("- foo\n- bar", result.Text);
    }

    [Fact]
    public void PrefixSelectedLines_Blockquote_PrefixesSingleLine()
    {
        var result = _formatter.PrefixSelectedLines("some text", NoSelection(), "> ");

        Assert.Equal("> some text", result.Text);
    }

    [Fact]
    public void PrefixSelectedLines_StripsExistingPrefix_BeforeApplying()
    {
        var result = _formatter.PrefixSelectedLines("- old item", NoSelection(), "- ");

        Assert.Equal("- old item", result.Text);
    }

    [Fact]
    public void ApplyHeading_H1_AddsHash()
    {
        var result = _formatter.ApplyHeadingToSelectedLines("Introduction", NoSelection(), 1);

        Assert.Equal("# Introduction", result.Text);
    }

    [Fact]
    public void ApplyHeading_H2_AddsTwoHashes()
    {
        var result = _formatter.ApplyHeadingToSelectedLines("Chapter One", NoSelection(), 2);

        Assert.Equal("## Chapter One", result.Text);
    }

    [Fact]
    public void ApplyHeading_H3_AddsThreeHashes()
    {
        var result = _formatter.ApplyHeadingToSelectedLines("Section", NoSelection(), 3);

        Assert.Equal("### Section", result.Text);
    }

    [Fact]
    public void ApplyHeading_ReplacesExistingHeading()
    {
        var result = _formatter.ApplyHeadingToSelectedLines("# Old Heading", NoSelection(), 2);

        Assert.Equal("## Old Heading", result.Text);
    }

    [Fact]
    public void ApplyHeading_InvalidLevel_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _formatter.ApplyHeadingToSelectedLines("text", NoSelection(), 0));
    }

    [Fact]
    public void ApplyNumberedList_SingleLine_PrefixesWithOne()
    {
        var result = _formatter.ApplyNumberedListToSelectedLines("First item", Selection(0, 10));

        Assert.Equal("1. First item", result.Text);
    }

    [Fact]
    public void ApplyNumberedList_MultipleLines_NumbersSequentially()
    {
        var result = _formatter.ApplyNumberedListToSelectedLines("Alpha\nBeta\nGamma", Selection(0, 16));

        Assert.Equal("1. Alpha\n2. Beta\n3. Gamma", result.Text);
    }

    [Fact]
    public void GetNextListItemPrefix_BulletLine_ReturnsBulletPrefix()
    {
        string text = "- item one";
        var prefix = _formatter.GetNextListItemPrefix(text, text.Length);

        Assert.Equal("- ", prefix);
    }

    [Fact]
    public void GetNextListItemPrefix_NumberedLine_ReturnsIncrementedNumber()
    {
        string text = "1. first item";
        var prefix = _formatter.GetNextListItemPrefix(text, text.Length);

        Assert.Equal("2. ", prefix);
    }

    [Fact]
    public void GetNextListItemPrefix_BlockquoteLine_ReturnsBlockquotePrefix()
    {
        string text = "> quoted text";
        var prefix = _formatter.GetNextListItemPrefix(text, text.Length);

        Assert.Equal("> ", prefix);
    }

    [Fact]
    public void GetNextListItemPrefix_PlainLine_ReturnsNull()
    {
        string text = "plain text";
        var prefix = _formatter.GetNextListItemPrefix(text, text.Length);

        Assert.Null(prefix);
    }

    [Fact]
    public void GetNextListItemPrefix_EmptyLine_ReturnsNull()
    {
        var prefix = _formatter.GetNextListItemPrefix("", 0);

        Assert.Null(prefix);
    }

    [Fact]
    public void StripListPrefix_BulletLine_StripsPrefix()
    {
        Assert.Equal("item", _formatter.StripListPrefix("- item"));
    }

    [Fact]
    public void StripListPrefix_NumberedLine_StripsPrefix()
    {
        Assert.Equal("item", _formatter.StripListPrefix("1. item"));
    }

    [Fact]
    public void StripListPrefix_PlainLine_ReturnsUnchanged()
    {
        Assert.Equal("plain text", _formatter.StripListPrefix("plain text"));
    }
}
