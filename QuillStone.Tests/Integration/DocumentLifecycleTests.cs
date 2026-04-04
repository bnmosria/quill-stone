using Avalonia.Platform.Storage;
using QuillStone.Models;
using QuillStone.Services;

namespace QuillStone.Tests.Integration;

/// <summary>
/// Integration tests for the document lifecycle: new document, load, edit (dirty),
/// and save. These tests exercise DocumentState and DocumentService together with
/// mocked I/O, verifying the combined title and dirty-flag behaviour.
/// </summary>
public sealed class DocumentLifecycleTests
{
    private readonly Mock<IMarkdownFileService> _fileService = new();
    private readonly Mock<IWindowDialogService> _dialogService = new();

    private DocumentService CreateService(out DocumentState state)
    {
        state = new DocumentState();
        return new DocumentService(_fileService.Object, _dialogService.Object, state);
    }

    private static Mock<IStorageFile> StorageFile(string name, string localPath)
    {
        var mock = new Mock<IStorageFile>();
        mock.Setup(f => f.Name).Returns(name);
        mock.Setup(f => f.Path).Returns(new Uri("file://" + localPath));
        return mock;
    }

    // ── New document ──────────────────────────────────────────────────────────

    [Fact]
    public void NewDocument_DisplayNameIsUntitled()
    {
        var svc = CreateService(out _);

        svc.NewDocument();

        Assert.Equal("Untitled", svc.DisplayName);
    }

    [Fact]
    public void NewDocument_IsDirtyIsFalse()
    {
        var svc = CreateService(out _);
        svc.MarkDirty(true);

        svc.NewDocument();

        Assert.False(svc.IsDirty);
    }

    [Fact]
    public void NewDocument_CurrentDocumentIsNull()
    {
        var svc = CreateService(out _);

        svc.NewDocument();

        Assert.Null(svc.CurrentDocument);
    }

    // ── Open file ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_ContentMatchesFile_TitleUpdatesToFileName()
    {
        var svc = CreateService(out _);
        var file = StorageFile("notes.md", "/tmp/notes.md");
        const string content = "# Hello World";
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/tmp/notes.md", content));

        await svc.LoadAsync(file.Object);

        Assert.Equal("notes.md", svc.DisplayName);
        Assert.Equal(content, svc.CurrentDocument?.Content);
    }

    [Fact]
    public async Task LoadAsync_ClearsDirtyState()
    {
        var svc = CreateService(out _);
        svc.MarkDirty(true);
        var file = StorageFile("doc.md", "/tmp/doc.md");
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/tmp/doc.md", "content"));

        await svc.LoadAsync(file.Object);

        Assert.False(svc.IsDirty);
    }

    // ── Edit → dirty ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncDirtyState_AfterEdit_SetsDirtyTrue()
    {
        var svc = CreateService(out _);
        var file = StorageFile("doc.md", "/tmp/doc.md");
        const string original = "original content";
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/tmp/doc.md", original));
        await svc.LoadAsync(file.Object);

        svc.SyncDirtyState("changed content");

        Assert.True(svc.IsDirty);
    }

    [Fact]
    public async Task SyncDirtyState_SameContent_DirtyRemainsFalse()
    {
        var svc = CreateService(out _);
        var file = StorageFile("doc.md", "/tmp/doc.md");
        const string content = "same content";
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/tmp/doc.md", content));
        await svc.LoadAsync(file.Object);

        svc.SyncDirtyState(content);

        Assert.False(svc.IsDirty);
    }

    [Fact]
    public void BuildWindowTitle_DirtyDocument_IncludesAsterisk()
    {
        var svc = CreateService(out var state);
        svc.NewDocument();
        svc.MarkDirty(true);

        string title = state.BuildWindowTitle("QuillStone");

        Assert.Contains("*", title);
        Assert.StartsWith("Untitled*", title);
    }

    [Fact]
    public void BuildWindowTitle_CleanDocument_NoAsterisk()
    {
        var svc = CreateService(out var state);
        svc.NewDocument();

        string title = state.BuildWindowTitle("QuillStone");

        Assert.DoesNotContain("*", title);
        Assert.StartsWith("Untitled", title);
    }

    // ── Save → clean ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_ClearsDirtyState()
    {
        var svc = CreateService(out _);
        var file = StorageFile("doc.md", "/tmp/doc.md");
        const string content = "# Saved";
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/tmp/doc.md", content));
        await svc.LoadAsync(file.Object);
        svc.MarkDirty(true);

        _fileService.Setup(f => f.SaveAsync(file.Object, content))
            .ReturnsAsync("/tmp/doc.md");

        await svc.SaveAsync(null!, content);

        Assert.False(svc.IsDirty);
    }

    [Fact]
    public async Task SaveAsync_UpdatesCurrentDocument()
    {
        var svc = CreateService(out _);
        var file = StorageFile("doc.md", "/tmp/doc.md");
        const string content = "# Updated";
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/tmp/doc.md", "# Original"));
        await svc.LoadAsync(file.Object);

        _fileService.Setup(f => f.SaveAsync(file.Object, content))
            .ReturnsAsync("/tmp/doc.md");

        await svc.SaveAsync(null!, content);

        Assert.Equal(content, svc.CurrentDocument?.Content);
    }

    // ── Full lifecycle ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_NewOpenEditSave_TitleAndDirtyCorrect()
    {
        var svc = CreateService(out var state);

        // 1. New document
        svc.NewDocument();
        Assert.Equal("Untitled", svc.DisplayName);
        Assert.False(svc.IsDirty);
        Assert.DoesNotContain("*", state.BuildWindowTitle("QuillStone"));

        // 2. Open file
        var file = StorageFile("chapter1.md", "/project/chapter1.md");
        const string loaded = "# Chapter 1";
        _fileService.Setup(f => f.LoadAsync(file.Object))
            .ReturnsAsync(new LoadedDocument(file.Object, "/project/chapter1.md", loaded));
        await svc.LoadAsync(file.Object);
        Assert.Equal("chapter1.md", svc.DisplayName);
        Assert.False(svc.IsDirty);

        // 3. Edit → dirty
        svc.SyncDirtyState("# Chapter 1\n\nNew paragraph.");
        Assert.True(svc.IsDirty);
        Assert.Contains("*", state.BuildWindowTitle("QuillStone"));

        // 4. Save → clean
        const string saved = "# Chapter 1\n\nNew paragraph.";
        _fileService.Setup(f => f.SaveAsync(file.Object, saved))
            .ReturnsAsync("/project/chapter1.md");
        await svc.SaveAsync(null!, saved);
        Assert.False(svc.IsDirty);
        Assert.DoesNotContain("*", state.BuildWindowTitle("QuillStone"));
    }
}
