using System.IO;
using System.Runtime.InteropServices;
using QuillStone.Controllers;

namespace QuillStone.Tests.Controllers;

public sealed class ProjectTreeControllerTests
{
    // ── BuildAltText ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildAltText_PlainName_ReturnsSameName()
    {
        var result = ProjectTreeController.BuildAltText("hero");

        Assert.Equal("hero", result);
    }

    [Fact]
    public void BuildAltText_Underscores_ReplacedWithSpaces()
    {
        var result = ProjectTreeController.BuildAltText("my_hero_image");

        Assert.Equal("my hero image", result);
    }

    [Fact]
    public void BuildAltText_Hyphens_ReplacedWithSpaces()
    {
        var result = ProjectTreeController.BuildAltText("my-hero-image");

        Assert.Equal("my hero image", result);
    }

    [Fact]
    public void BuildAltText_MixedSeparators_AllReplacedWithSpaces()
    {
        var result = ProjectTreeController.BuildAltText("my_hero-image");

        Assert.Equal("my hero image", result);
    }

    [Fact]
    public void BuildAltText_EmptyString_ReturnsEmpty()
    {
        var result = ProjectTreeController.BuildAltText(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    // ── BuildRelativePath ─────────────────────────────────────────────────────

    [Fact]
    public void BuildRelativePath_NoCurrentDocument_ReturnsFilenameOnly()
    {
        var result = ProjectTreeController.BuildRelativePath("/project/images/hero.png", null);

        Assert.Equal("hero.png", result);
    }

    [Fact]
    public void BuildRelativePath_ImageInSameFolderAsDocument_ReturnsBareFilename()
    {
        var docPath = Path.Combine("project", "docs", "page.md");
        var imgPath = Path.Combine("project", "docs", "hero.png");

        var result = ProjectTreeController.BuildRelativePath(imgPath, docPath);

        Assert.Equal("hero.png", result);
    }

    [Fact]
    public void BuildRelativePath_ImageInSubfolder_ReturnsForwardSlashPath()
    {
        var docPath = Path.Combine("project", "docs", "page.md");
        var imgPath = Path.Combine("project", "docs", "images", "hero.png");

        var result = ProjectTreeController.BuildRelativePath(imgPath, docPath);

        Assert.Equal("images/hero.png", result);
    }

    [Fact]
    public void BuildRelativePath_ImageInSiblingFolder_ReturnsForwardSlashPath()
    {
        var docPath = Path.Combine("project", "docs", "page.md");
        var imgPath = Path.Combine("project", "assets", "hero.png");

        var result = ProjectTreeController.BuildRelativePath(imgPath, docPath);

        Assert.Equal("../assets/hero.png", result);
    }

    [Fact]
    public void BuildRelativePath_AlwaysUsesForwardSlashes()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // backslash separator only applies on Windows

        var docPath = Path.Combine("C:\\project", "docs", "page.md");
        var imgPath = Path.Combine("C:\\project", "docs", "images", "hero.png");

        var result = ProjectTreeController.BuildRelativePath(imgPath, docPath);

        Assert.DoesNotContain('\\', result);
    }

    // ── BuildImageSyntax ──────────────────────────────────────────────────────

    [Fact]
    public void BuildImageSyntax_NoCurrentDocument_UsesBareFilename()
    {
        var result = ProjectTreeController.BuildImageSyntax("/project/images/hero.png", null);

        Assert.Equal("![hero](hero.png)", result);
    }

    [Fact]
    public void BuildImageSyntax_ImageWithUnderscores_AltTextUsesSpaces()
    {
        var docPath = Path.Combine("project", "docs", "page.md");
        var imgPath = Path.Combine("project", "docs", "my_hero_image.png");

        var result = ProjectTreeController.BuildImageSyntax(imgPath, docPath);

        Assert.Equal("![my hero image](my_hero_image.png)", result);
    }

    [Fact]
    public void BuildImageSyntax_ImageInSubfolder_SyntaxIncludesRelativePath()
    {
        var docPath = Path.Combine("project", "docs", "page.md");
        var imgPath = Path.Combine("project", "docs", "images", "screenshot.jpg");

        var result = ProjectTreeController.BuildImageSyntax(imgPath, docPath);

        Assert.Equal("![screenshot](images/screenshot.jpg)", result);
    }

    [Fact]
    public void BuildImageSyntax_SvgFile_SyntaxCorrect()
    {
        var docPath = Path.Combine("project", "page.md");
        var imgPath = Path.Combine("project", "logo.svg");

        var result = ProjectTreeController.BuildImageSyntax(imgPath, docPath);

        Assert.Equal("![logo](logo.svg)", result);
    }
}
