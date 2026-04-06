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
    public void WrapSelection_SelectionIncludesMarkers_UnwrapsText()
    {
        // Case A: selection is **bold** including the asterisks
        var result = _formatter.WrapSelection("**bold**", Selection(0, 8), "**", "**", "bold text");

        Assert.Equal("bold", result.Text);
        Assert.Equal(0, result.SelectionStart);
        Assert.Equal(4, result.SelectionEnd);
    }

    [Fact]
    public void WrapSelection_SelectionIsInnerText_MarkersOutside_UnwrapsText()
    {
        // Case B: selection is "bold" inside **bold**
        var result = _formatter.WrapSelection("**bold**", Selection(2, 6), "**", "**", "bold text");

        Assert.Equal("bold", result.Text);
        Assert.Equal(0, result.SelectionStart);
        Assert.Equal(4, result.SelectionEnd);
    }

    [Fact]
    public void WrapSelection_InnerTextWithSurroundingContent_UnwrapsCorrectly()
    {
        // "hello **world** today" — "world" is at [8..13], markers "**" are at [6..8] and [13..15]
        var result = _formatter.WrapSelection("hello **world** today", Selection(8, 13), "**", "**", "bold text");

        Assert.Equal("hello world today", result.Text);
        Assert.Equal(6, result.SelectionStart);
        Assert.Equal(11, result.SelectionEnd);
    }

    [Fact]
    public void WrapSelection_AlreadyWrappedIncludingMarkers_WithSurroundingContent_UnwrapsCorrectly()
    {
        // "hello **world** today" — full "**world**" is at [6..15]
        var result = _formatter.WrapSelection("hello **world** today", Selection(6, 15), "**", "**", "bold text");

        Assert.Equal("hello world today", result.Text);
        Assert.Equal(6, result.SelectionStart);
        Assert.Equal(11, result.SelectionEnd);
    }

    [Fact]
    public void InsertFencedCode_NoSelection_InsertsPlaceholder()
    {
        var result = _formatter.InsertFencedCode("", NoSelection(), "");

        Assert.Equal("```\ncode\n```", result.Text);
        Assert.Equal(4, result.SelectionStart);
        Assert.Equal(8, result.SelectionEnd);
    }

    [Fact]
    public void InsertFencedCode_WithSelection_WrapsSelectedText()
    {
        var result = _formatter.InsertFencedCode("var x = 1;", Selection(0, 10), "");

        Assert.Equal("```\nvar x = 1;\n```", result.Text);
        Assert.Equal(4, result.SelectionStart);
        Assert.Equal(14, result.SelectionEnd);
    }

    [Fact]
    public void InsertFencedCode_WithLanguage_AddsLanguageToFence()
    {
        var result = _formatter.InsertFencedCode("", NoSelection(), "csharp");

        Assert.Equal("```csharp\ncode\n```", result.Text);
        Assert.Equal(10, result.SelectionStart);
        Assert.Equal(14, result.SelectionEnd);
    }

    [Fact]
    public void InsertLink_NoSelection_InsertsPlaceholderAndSelectsUrl()
    {
        var result = _formatter.InsertLink("", NoSelection(), "https://example.com", "link text");

        Assert.Equal("[link text](https://example.com)", result.Text);
        // Selection should cover the url "https://example.com"
        Assert.Equal(12, result.SelectionStart);
        Assert.Equal(31, result.SelectionEnd);
    }

    [Fact]
    public void InsertLink_WithSelection_UsesSelectedTextAsLabelAndSelectsUrl()
    {
        var result = _formatter.InsertLink("click here", Selection(0, 10), "https://example.com", "link text");

        Assert.Equal("[click here](https://example.com)", result.Text);
        // Selection should cover the url "https://example.com"
        Assert.Equal(13, result.SelectionStart);
        Assert.Equal(32, result.SelectionEnd);
    }

    [Fact]
    public void InsertImage_NoSelection_InsertsPlaceholderAndSelectsAlt()
    {
        var result = _formatter.InsertImage("", NoSelection(), "path/to/image.png", "alt text");

        Assert.Equal("![alt text](path/to/image.png)", result.Text);
        // No selection → select alt text (positions 2 to 2+8=10)
        Assert.Equal(2, result.SelectionStart);
        Assert.Equal(10, result.SelectionEnd);
    }

    [Fact]
    public void InsertImage_WithSelection_UsesSelectionAsAltAndSelectsPath()
    {
        var result = _formatter.InsertImage("my photo", Selection(0, 8), "path/to/image.png", "alt text");

        Assert.Equal("![my photo](path/to/image.png)", result.Text);
        // With selection → select path (positions: 0 + 8 + 4 = 12, to 12 + 17 = 29)
        Assert.Equal(12, result.SelectionStart);
        Assert.Equal(29, result.SelectionEnd);
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
    public void PrefixSelectedLines_Checkbox_EmptyLine_AppliesPrefix()
    {
        var result = _formatter.PrefixSelectedLines("", NoSelection(), "- [ ] ");

        Assert.Equal("- [ ] ", result.Text);
    }

    [Fact]
    public void PrefixSelectedLines_MultiLineWithBlankSeparator_SkipsBlankLine()
    {
        var result = _formatter.PrefixSelectedLines("foo\n\nbar", Selection(0, 8), "- ");

        Assert.Equal("- foo\n\n- bar", result.Text);
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
    public void GetNextListItemPrefix_UncheckedCheckboxLine_ReturnsCheckboxPrefix()
    {
        string text = "- [ ] item";
        var prefix = _formatter.GetNextListItemPrefix(text, text.Length);

        Assert.Equal("- [ ] ", prefix);
    }

    [Fact]
    public void GetNextListItemPrefix_CheckedCheckboxLine_ReturnsUncheckedCheckboxPrefix()
    {
        string text = "- [x] item";
        var prefix = _formatter.GetNextListItemPrefix(text, text.Length);

        Assert.Equal("- [ ] ", prefix);
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
