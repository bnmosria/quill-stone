using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace QuillStone;

/// <summary>
/// Sets the macOS dock icon via the ObjC runtime.
/// Works both under dotnet run and in a published .app bundle.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacOSDock
{
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr GetClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr GetSel(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr self, IntPtr sel);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr self, IntPtr sel, IntPtr arg);

    public static void SetIcon(string pngPath)
    {
        try
        {
            var pathPtr = Marshal.StringToCoTaskMemUTF8(pngPath);
            try
            {
                // NSString* path = [NSString stringWithUTF8String: pngPath]
                var nsPath = Send(GetClass("NSString"), GetSel("stringWithUTF8String:"), pathPtr);

                // NSImage* img = [[NSImage alloc] initWithContentsOfFile: path]
                var img = Send(Send(GetClass("NSImage"), GetSel("alloc")),
                               GetSel("initWithContentsOfFile:"), nsPath);

                // [NSApplication.sharedApplication setApplicationIconImage: img]
                var app = Send(GetClass("NSApplication"), GetSel("sharedApplication"));
                Send(app, GetSel("setApplicationIconImage:"), img);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }
        catch
        {
            // Best-effort — never crash the app over an icon
        }
    }
}
