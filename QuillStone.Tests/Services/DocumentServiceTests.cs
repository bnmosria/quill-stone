using Avalonia.Platform.Storage;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Services;

public sealed class DocumentServiceTests
{
    private readonly Mock<IMarkdownFileService> _fileServiceMock = new();
    private readonly Mock<IWindowDialogService> _dialogMock = new();
    private readonly DocumentState _state = new();
    private readonly DocumentService _svc;

    public DocumentServiceTests()
    {
        _svc = new DocumentService(_fileServiceMock.Object, _dialogMock.Object, _state);
    }

    private static Mock<IStorageFile> MakeStorageFile(string name)
    {
        var mock = new Mock<IStorageFile>();
        mock.Setup(f => f.Name).Returns(name);
        return mock;
    }

    [Fact]
    public void NewDocument_ClearsCurrentDocument()
    {
        _svc.NewDocument();

        Assert.Null(_svc.CurrentDocument);
    }

    [Fact]
    public void NewDocument_ClearsIsDirty()
    {
        _svc.MarkDirty(true);

        _svc.NewDocument();

        Assert.False(_svc.IsDirty);
    }

    [Fact]
    public void MarkDirty_True_SetsIsDirty()
    {
        _svc.MarkDirty(true);

        Assert.True(_svc.IsDirty);
    }

    [Fact]
    public void MarkDirty_False_ClearsIsDirty()
    {
        _svc.MarkDirty(true);

        _svc.MarkDirty(false);

        Assert.False(_svc.IsDirty);
    }

    [Fact]
    public void SyncDirtyState_MatchingContent_NotDirty()
    {
        _state.SetPersistedContent("hello");

        _svc.SyncDirtyState("hello");

        Assert.False(_svc.IsDirty);
    }

    [Fact]
    public void SyncDirtyState_DifferentContent_Dirty()
    {
        _state.SetPersistedContent("hello");

        _svc.SyncDirtyState("hello world");

        Assert.True(_svc.IsDirty);
    }

    [Fact]
    public void DisplayName_DefaultState_ReturnsUntitled()
    {
        Assert.Equal("Untitled", _svc.DisplayName);
    }

    [Fact]
    public async Task LoadAsync_SetsCurrentDocument()
    {
        var storageFile = MakeStorageFile("note.md");
        var loaded = new LoadedDocument(storageFile.Object, "/docs/note.md", "# Hello");
        _fileServiceMock.Setup(f => f.LoadAsync(storageFile.Object)).ReturnsAsync(loaded);

        await _svc.LoadAsync(storageFile.Object);

        Assert.NotNull(_svc.CurrentDocument);
        Assert.Equal("# Hello", _svc.CurrentDocument!.Content);
    }

    [Fact]
    public async Task LoadAsync_ClearsIsDirty()
    {
        _svc.MarkDirty(true);
        var storageFile = MakeStorageFile("note.md");
        var loaded = new LoadedDocument(storageFile.Object, "/docs/note.md", "content");
        _fileServiceMock.Setup(f => f.LoadAsync(storageFile.Object)).ReturnsAsync(loaded);

        await _svc.LoadAsync(storageFile.Object);

        Assert.False(_svc.IsDirty);
    }

    [Fact]
    public async Task LoadAsync_WhenFileServiceThrows_WrapsException()
    {
        var storageFile = MakeStorageFile("note.md");
        _fileServiceMock.Setup(f => f.LoadAsync(storageFile.Object))
            .ThrowsAsync(new IOException("disk error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _svc.LoadAsync(storageFile.Object));

        Assert.Contains("disk error", ex.Message);
    }

    [Fact]
    public async Task TrySaveIfDirtyAsync_NotDirty_ReturnsTrueWithoutPrompting()
    {
        _state.SetPersistedContent("same content");

        bool result = await _svc.TrySaveIfDirtyAsync(null!, "same content");

        Assert.True(result);
        _dialogMock.Verify(
            d => d.ShowConfirmDialogAsync(It.IsAny<Avalonia.Controls.Window>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task TrySaveIfDirtyAsync_DirtyAndCancel_ReturnsFalse()
    {
        _state.SetPersistedContent("original");
        _dialogMock
            .Setup(d => d.ShowConfirmDialogAsync(It.IsAny<Avalonia.Controls.Window>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(DialogChoice.Cancel);

        bool result = await _svc.TrySaveIfDirtyAsync(null!, "modified");

        Assert.False(result);
    }

    [Fact]
    public async Task TrySaveIfDirtyAsync_DirtyAndDontSave_ReturnsTrue()
    {
        _state.SetPersistedContent("original");
        _dialogMock
            .Setup(d => d.ShowConfirmDialogAsync(It.IsAny<Avalonia.Controls.Window>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(DialogChoice.Secondary);

        bool result = await _svc.TrySaveIfDirtyAsync(null!, "modified");

        Assert.True(result);
    }

    [Fact]
    public async Task TrySaveIfDirtyAsync_DirtyAndSave_WithCurrentFile_SavesAndReturnsTrue()
    {
        var storageFile = MakeStorageFile("note.md");
        _state.SetCurrentFile(storageFile.Object, "/docs/note.md");
        _state.SetPersistedContent("original");
        _fileServiceMock
            .Setup(f => f.SaveAsync(storageFile.Object, "modified"))
            .ReturnsAsync("/docs/note.md");
        _dialogMock
            .Setup(d => d.ShowConfirmDialogAsync(It.IsAny<Avalonia.Controls.Window>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(DialogChoice.Primary);

        bool result = await _svc.TrySaveIfDirtyAsync(null!, "modified");

        Assert.True(result);
        _fileServiceMock.Verify(f => f.SaveAsync(storageFile.Object, "modified"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithCurrentFile_UpdatesCurrentDocumentAndClearsDirty()
    {
        var storageFile = MakeStorageFile("note.md");
        _state.SetCurrentFile(storageFile.Object, "/docs/note.md");
        _fileServiceMock
            .Setup(f => f.SaveAsync(storageFile.Object, "new content"))
            .ReturnsAsync("/docs/note.md");

        bool result = await _svc.SaveAsync(null!, "new content");

        Assert.True(result);
        Assert.False(_svc.IsDirty);
        Assert.NotNull(_svc.CurrentDocument);
        Assert.Equal("new content", _svc.CurrentDocument!.Content);
    }

    [Fact]
    public async Task SaveAsync_WhenFileServiceThrows_ShowsMessageAndReturnsFalse()
    {
        var storageFile = MakeStorageFile("note.md");
        _state.SetCurrentFile(storageFile.Object, "/docs/note.md");
        _fileServiceMock
            .Setup(f => f.SaveAsync(storageFile.Object, It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("no permission"));
        _dialogMock
            .Setup(d => d.ShowMessageDialogAsync(It.IsAny<Avalonia.Controls.Window>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        bool result = await _svc.SaveAsync(null!, "content");

        Assert.False(result);
        _dialogMock.Verify(
            d => d.ShowMessageDialogAsync(It.IsAny<Avalonia.Controls.Window>(),
                It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RebindCurrentFileAsync_NullCurrentDocument_ReturnsFalse()
    {
        bool result = await _svc.RebindCurrentFileAsync(null!, "/new/path.md", "content");

        Assert.False(result);
    }

    [Fact]
    public void IsCurrentFile_WhenNoCurrentDocument_ReturnsFalse()
    {
        _svc.NewDocument();

        Assert.False(_svc.IsCurrentFile("/some/path/file.md"));
    }

    [Fact]
    public void IsCurrentFile_WithNonMatchingPath_ReturnsFalse()
    {
        // No document loaded — any path should return false
        Assert.False(_svc.IsCurrentFile("/other/path/other.md"));
    }
}
