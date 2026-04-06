using QuillStone.Models;

namespace QuillStone.Tests.Models;

public sealed class ProjectStateTests
{
    [Fact]
    public void IsProject_True_WhenConstructedWithIsProjectTrue()
    {
        var state = new ProjectState("My Book", "/projects/my-book", isProject: true);

        Assert.True(state.IsProject);
    }

    [Fact]
    public void IsProject_False_WhenConstructedWithIsProjectFalse()
    {
        var state = new ProjectState("my-book", "/projects/my-book", isProject: false);

        Assert.False(state.IsProject);
    }

    [Fact]
    public void DisplayName_IsProject_ReturnsProjectName()
    {
        var state = new ProjectState("My Book", "/projects/my-book", isProject: true);

        Assert.Equal("My Book", state.DisplayName);
    }

    [Fact]
    public void DisplayName_PlainFolder_ReturnsFolderName()
    {
        var state = new ProjectState("my-book", "/projects/my-book", isProject: false);

        Assert.Equal("my-book", state.DisplayName);
    }

    [Fact]
    public void DisplayName_PlainFolder_StripsTrailingSlash()
    {
        var rootPath = $"{Path.DirectorySeparatorChar}projects{Path.DirectorySeparatorChar}my-book{Path.DirectorySeparatorChar}";
        var state = new ProjectState("ignored", rootPath, isProject: false);

        Assert.Equal("my-book", state.DisplayName);
    }

    [Fact]
    public void DisplayName_IsProject_ProjectNameDiffersFromFolderName()
    {
        var state = new ProjectState("The Great Novel", "/projects/tgn-draft", isProject: true);

        Assert.Equal("The Great Novel", state.DisplayName);
    }
}
