using Moq;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class FormatCommandHandlerTests
{
    private readonly Mock<IEditorService> _editorMock = new();
    private readonly MarkdownFormatter _formatter = new();
    private readonly FormatCommandHandler _handler;

    public FormatCommandHandlerTests()
    {
        _handler = new FormatCommandHandler(_editorMock.Object, _formatter);
    }

    private void SetupEditor(string text, int selStart = 0, int selEnd = 0)
    {
        _editorMock.Setup(e => e.GetEditorText()).Returns(text);
        _editorMock.Setup(e => e.GetSavedSelection()).Returns(new TextSelectionRange(selStart, selEnd));
        _editorMock.Setup(e => e.UpdateSelection());
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()));
    }

    [Fact]
    public void ApplyBold_WithSelection_WrapsBold()
    {
        SetupEditor("hello", 0, 5);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyBold();

        Assert.NotNull(result);
        Assert.Equal("**hello**", result!.Text);
    }

    [Fact]
    public void ApplyBold_NoSelection_InsertsBoldPlaceholder()
    {
        SetupEditor("", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyBold();

        Assert.NotNull(result);
        Assert.Equal("**bold text**", result!.Text);
    }

    [Fact]
    public void ApplyItalic_WithSelection_WrapsItalic()
    {
        SetupEditor("word", 0, 4);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyItalic();

        Assert.NotNull(result);
        Assert.Equal("*word*", result!.Text);
    }

    [Fact]
    public void ApplyInlineCode_WithSelection_WrapsCode()
    {
        SetupEditor("var x", 0, 5);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyInlineCode();

        Assert.NotNull(result);
        Assert.Equal("`var x`", result!.Text);
    }

    [Fact]
    public void ApplyHeading_H1_AddsSingleHash()
    {
        SetupEditor("Introduction", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyHeading(1);

        Assert.NotNull(result);
        Assert.Equal("# Introduction", result!.Text);
    }

    [Fact]
    public void ApplyHeading_H2_AddsTwoHashes()
    {
        SetupEditor("Chapter", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyHeading(2);

        Assert.NotNull(result);
        Assert.Equal("## Chapter", result!.Text);
    }

    [Fact]
    public void ApplyHeading_H3_AddsThreeHashes()
    {
        SetupEditor("Section", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyHeading(3);

        Assert.NotNull(result);
        Assert.Equal("### Section", result!.Text);
    }

    [Fact]
    public void ApplyBulletList_PrefixesLine()
    {
        SetupEditor("Item one", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyBulletList();

        Assert.NotNull(result);
        Assert.Equal("- Item one", result!.Text);
    }

    [Fact]
    public void ApplyNumberedList_PrefixesLine()
    {
        SetupEditor("Item one", 0, 8);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyNumberedList();

        Assert.NotNull(result);
        Assert.Equal("1. Item one", result!.Text);
    }

    [Fact]
    public void ApplyBlockquote_PrefixesLine()
    {
        SetupEditor("A wise quote", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyBlockquote();

        Assert.NotNull(result);
        Assert.Equal("> A wise quote", result!.Text);
    }

    [Fact]
    public void InsertLink_NoSelection_InsertsPlaceholderAndSelectsUrl()
    {
        SetupEditor("", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.InsertLink();

        Assert.NotNull(result);
        Assert.Equal("[link text](url)", result!.Text);
        // Selection should cover "url"
        Assert.Equal(12, result!.SelectionStart);
        Assert.Equal(15, result!.SelectionEnd);
    }

    [Fact]
    public void InsertLink_WithSelection_UsesSelectionAsLabelAndSelectsUrl()
    {
        SetupEditor("click here", 0, 10);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.InsertLink();

        Assert.NotNull(result);
        Assert.Equal("[click here](url)", result!.Text);
        // Selection should cover "url"
        Assert.Equal(13, result!.SelectionStart);
        Assert.Equal(16, result!.SelectionEnd);
    }

    [Fact]
    public void InsertImage_NoSelection_InsertsPlaceholderAndSelectsAlt()
    {
        SetupEditor("", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.InsertImage();

        Assert.NotNull(result);
        Assert.Equal("![alt text](path)", result!.Text);
        // Selection should cover "alt text" (start+2 to start+2+8)
        Assert.Equal(2, result!.SelectionStart);
        Assert.Equal(10, result!.SelectionEnd);
    }

    [Fact]
    public void InsertImage_WithSelection_UsesSelectionAsAltAndSelectsPath()
    {
        SetupEditor("my image", 0, 8);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.InsertImage();

        Assert.NotNull(result);
        Assert.Equal("![my image](path)", result!.Text);
        // Selection should cover "path" (start + alt.Length + 4 = 0 + 8 + 4 = 12, to 12 + 4 = 16)
        Assert.Equal(12, result!.SelectionStart);
        Assert.Equal(16, result!.SelectionEnd);
    }

    [Fact]
    public void ApplyStrikethrough_WithSelection_WrapsStrikethrough()
    {
        SetupEditor("word", 0, 4);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyStrikethrough();

        Assert.NotNull(result);
        Assert.Equal("~~word~~", result!.Text);
    }

    [Fact]
    public void ApplyStrikethrough_NoSelection_InsertsPlaceholder()
    {
        SetupEditor("", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyStrikethrough();

        Assert.NotNull(result);
        Assert.Equal("~~strikethrough~~", result!.Text);
    }

    [Fact]
    public void ApplyCodeBlock_NoSelection_InsertsFencedBlock()
    {
        SetupEditor("", 0, 0);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyCodeBlock();

        Assert.NotNull(result);
        Assert.Equal("```\ncode\n```", result!.Text);
    }

    [Fact]
    public void ApplyCodeBlock_WithSelection_WrapsFencedBlock()
    {
        SetupEditor("var x = 1;", 0, 10);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        _handler.ApplyCodeBlock();

        Assert.NotNull(result);
        Assert.Equal("```\nvar x = 1;\n```", result!.Text);
    }
}
