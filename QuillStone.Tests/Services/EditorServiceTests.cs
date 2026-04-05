using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class EditorServiceTests
{
    private readonly MarkdownFormatter _formatter = new();

    [Fact]
    public void ComputeEnterKeyEdit_PlainLine_ReturnsNull()
    {
        var result = EditorService.ComputeEnterKeyEdit("plain text", 10, _formatter);

        Assert.Null(result);
    }

    [Fact]
    public void ComputeEnterKeyEdit_EmptyString_ReturnsNull()
    {
        var result = EditorService.ComputeEnterKeyEdit(string.Empty, 0, _formatter);

        Assert.Null(result);
    }

    [Fact]
    public void ComputeEnterKeyEdit_BulletLine_InsertsNextBullet()
    {
        string text = "- item one";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("- item one\n- ", result!.Value.NewText);
        Assert.Equal(cursor + "\n- ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_NumberedLine_InsertsIncrementedNumber()
    {
        string text = "1. first item";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("1. first item\n2. ", result!.Value.NewText);
        Assert.Equal(cursor + "\n2. ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_BlockquoteLine_InsertsNextBlockquote()
    {
        string text = "> quoted text";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("> quoted text\n> ", result!.Value.NewText);
        Assert.Equal(cursor + "\n> ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_EmptyBulletItem_RemovesMarker()
    {
        string text = "- ";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.Value.NewText);
        Assert.Equal(0, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_CursorMidLine_IncludesTrailingTextOnNewLine()
    {
        string text = "- item text";
        int cursor = "- item".Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("- item\n-  text", result!.Value.NewText);
        Assert.Equal(cursor + "\n- ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_SecondLineOfMultiLine_InsertsAtCorrectPosition()
    {
        string text = "- first\n- second";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("- first\n- second\n- ", result!.Value.NewText);
        Assert.Equal(cursor + "\n- ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_UncheckedCheckboxLine_InsertsNextUncheckedCheckbox()
    {
        string text = "- [ ] item";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("- [ ] item\n- [ ] ", result!.Value.NewText);
        Assert.Equal(cursor + "\n- [ ] ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_CheckedCheckboxLine_InsertsNextUncheckedCheckbox()
    {
        string text = "- [x] item";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal("- [x] item\n- [ ] ", result!.Value.NewText);
        Assert.Equal(cursor + "\n- [ ] ".Length, result.Value.NewCaretIndex);
    }

    [Fact]
    public void ComputeEnterKeyEdit_EmptyUncheckedCheckbox_RemovesMarker()
    {
        string text = "- [ ] ";
        int cursor = text.Length;

        var result = EditorService.ComputeEnterKeyEdit(text, cursor, _formatter);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.Value.NewText);
        Assert.Equal(0, result.Value.NewCaretIndex);
    }
}
