using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(QuillStone.Tests.TestAppBuilder))]

namespace QuillStone.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<QuillStone.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
