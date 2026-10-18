using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Launcher;

internal static class Program
{
    private const string RequiredRuntime = "10.";
    private const string DownloadUrl = "https://dotnet.microsoft.com/download/dotnet/10.0";
    private const string AppExe = "STranslate.exe";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private const uint MB_YESNOCANCEL = 0x00000003;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONINFORMATION = 0x00000040;
    private const uint MB_OK = 0x00000000;
    private const int IDYES = 6;
    private const int IDCANCEL = 2;

    private static void Main()
    {
        if (!HasDesktopRuntime10())
        {
            var ret = MessageBox(IntPtr.Zero,
                "QTranslate 需要 .NET 10 桌面运行时才能运行。\n\n是否现在打开下载页面？\n\n点\"是\"打开浏览器下载，点\"否\"退出。",
                "QTranslate - 缺少运行时",
                MB_YESNOCANCEL | MB_ICONWARNING);

            if (ret == IDYES)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DownloadUrl,
                    UseShellExecute = true
                });
            }
            return;
        }

        var dir = AppContext.BaseDirectory;
        var exePath = Path.Combine(dir, AppExe);
        if (!File.Exists(exePath))
        {
            MessageBox(IntPtr.Zero, $"未找到主程序 {AppExe}", "QTranslate", MB_OK | MB_ICONERROR);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            WorkingDirectory = dir
        });
    }

    private static bool HasDesktopRuntime10()
    {
        try
        {
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrEmpty(dotnetRoot))
            {
                dotnetRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "dotnet");
            }

            var desktopDir = Path.Combine(dotnetRoot, "shared", "Microsoft.WindowsDesktop.App");
            if (!Directory.Exists(desktopDir)) return false;

            foreach (var dir in Directory.GetDirectories(desktopDir))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith(RequiredRuntime, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
        }
        return false;
    }
}
