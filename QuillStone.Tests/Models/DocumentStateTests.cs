using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Tests.Models;

public sealed class DocumentStateTests
{
    private static Mock<IStorageFile> MakeFile(string name)
    {
        var mock = new Mock<IStorageFile>();
        mock.Setup(f => f.Name).Returns(name);
        return mock;
    }

    [Fact]
    public void DisplayName_DefaultState_ReturnsUntitled()
    {
        var state = new DocumentState();

        Assert.Equal("Untitled", state.DisplayName);
    }

    [Fact]
    public void DisplayName_AfterSetCurrentFile_ReturnsFilename()
    {
        var state = new DocumentState();
        var file = MakeFile("chapter.md");
        state.SetCurrentFile(file.Object, "/books/chapter.md");

        Assert.Equal("chapter.md", state.DisplayName);
    }

    [Fact]
    public void MarkDirty_True_SetsDirty()
    {
        var state = new DocumentState();

        state.MarkDirty(true);

        Assert.True(state.IsDirty);
    }

    [Fact]
    public void MarkDirty_False_ClearsDirty()
    {
        var state = new DocumentState();
        state.MarkDirty(true);

        state.MarkDirty(false);

        Assert.False(state.IsDirty);
    }

    [Fact]
    public void HasUnsavedChanges_ContentMatchesPersisted_ReturnsFalse()
    {
        var state = new DocumentState();
        state.SetPersistedContent("hello");

        Assert.False(state.HasUnsavedChanges("hello"));
    }

    [Fact]
    public void HasUnsavedChanges_ContentDiffers_ReturnsTrue()
    {
        var state = new DocumentState();
        state.SetPersistedContent("hello");

        Assert.True(state.HasUnsavedChanges("hello world"));
    }

    [Fact]
    public void HasUnsavedChanges_DifferentCase_ReturnsTrue()
    {
        var state = new DocumentState();
        state.SetPersistedContent("Hello");

        Assert.True(state.HasUnsavedChanges("hello"));
    }

    [Fact]
    public void SetPersistedContent_UpdatesProperty()
    {
        var state = new DocumentState();

        state.SetPersistedContent("my content");

        Assert.Equal("my content", state.PersistedContent);
    }

    [Fact]
    public void SetCurrentFile_WithLocalPath_UsesLocalPath()
    {
        var state = new DocumentState();
        var file = MakeFile("chapter.md");

        state.SetCurrentFile(file.Object, "/books/chapter.md");

        Assert.Equal("/books/chapter.md", state.CurrentFilePath);
        Assert.Same(file.Object, state.CurrentFile);
    }

    [Fact]
    public void SetCurrentFile_WithNullLocalPath_FallsBackToFileName()
    {
        var state = new DocumentState();
        var file = MakeFile("chapter.md");

        state.SetCurrentFile(file.Object, null);

        Assert.Equal("chapter.md", state.CurrentFilePath);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var state = new DocumentState();
        var file = MakeFile("chapter.md");
        state.SetCurrentFile(file.Object, "/books/chapter.md");
        state.SetPersistedContent("content");
        state.MarkDirty(true);

        state.Reset();

        Assert.Null(state.CurrentFile);
        Assert.Null(state.CurrentFilePath);
        Assert.Equal(string.Empty, state.PersistedContent);
        Assert.False(state.IsDirty);
        Assert.Equal("Untitled", state.DisplayName);
    }

    [Fact]
    public void BuildWindowTitle_CleanDocument_NoDirtyMark()
    {
        var state = new DocumentState();
        var file = MakeFile("draft.md");
        state.SetCurrentFile(file.Object, "/books/draft.md");
        state.MarkDirty(false);

        string title = state.BuildWindowTitle("QuillStone");

        Assert.Equal("draft.md - QuillStone", title);
    }

    [Fact]
    public void BuildWindowTitle_DirtyDocument_PrependsDirtyMark()
    {
        var state = new DocumentState();
        var file = MakeFile("draft.md");
        state.SetCurrentFile(file.Object, "/books/draft.md");
        state.MarkDirty(true);

        string title = state.BuildWindowTitle("QuillStone");

        Assert.Equal("draft.md* - QuillStone", title);
    }

    [Fact]
    public void BuildWindowTitle_NoFile_UsesUntitled()
    {
        var state = new DocumentState();

        string title = state.BuildWindowTitle("QuillStone");

        Assert.Equal("Untitled - QuillStone", title);
    }
}
