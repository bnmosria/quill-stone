using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using QuillStone.Styles.Theme;
using System.Runtime.Versioning;

namespace QuillStone;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		ThemeManager.Initialize(this);

		if (OperatingSystem.IsMacOS())
			SetMacOSDockIcon();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow();
		}

		base.OnFrameworkInitializationCompleted();
	}

	[SupportedOSPlatform("macos")]
	private static void SetMacOSDockIcon()
	{
		try
		{
			var tmp = Path.Combine(Path.GetTempPath(), "QuillStone_dock.png");
			using var stream = AssetLoader.Open(new Uri("avares://QuillStone/Assets/Icons/icon.png"));
			using var file = File.Create(tmp);
			stream.CopyTo(file);
			file.Close();
			MacOSDock.SetIcon(tmp);
		}
		catch { /* best-effort */ }
	}
}

