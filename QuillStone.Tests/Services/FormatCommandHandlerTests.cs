using Moq;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class FormatCommandHandlerTests
{
    private readonly Mock<IEditorService> _editorMock = new();
    private readonly Mock<IWindowDialogService> _dialogMock = new();
    private readonly MarkdownFormatter _formatter = new();
    private readonly FormatCommandHandler _handler;

    public FormatCommandHandlerTests()
    {
        _handler = new FormatCommandHandler(_editorMock.Object, _formatter, _dialogMock.Object);
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
    public async Task InsertLinkAsync_UserCancels_DoesNotModifyEditor()
    {
        _dialogMock
            .Setup(d => d.ShowInputDialogAsync(null!, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);
        SetupEditor("some text", 0, 0);

        await _handler.InsertLinkAsync(null!);

        _editorMock.Verify(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()), Times.Never);
    }

    [Fact]
    public async Task InsertLinkAsync_UserProvidesUrl_InsertsLink()
    {
        _dialogMock
            .Setup(d => d.ShowInputDialogAsync(null!, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://example.com");
        SetupEditor("click here", 0, 10);
        TextEditResult? result = null;
        _editorMock.Setup(e => e.ApplyTextEdit(It.IsAny<TextEditResult>()))
            .Callback<TextEditResult>(r => result = r);

        await _handler.InsertLinkAsync(null!);

        Assert.NotNull(result);
        Assert.Equal("[click here](https://example.com)", result!.Text);
    }
}
