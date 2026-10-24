using Avalonia;
using Crumb.Core;
using System.Globalization;

namespace Crumb.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // CI exercises the published native binary with its bundled tools, without a desktop session.
        if (args.Length == 4 && args[0] == "--verify-bundle")
        {
            try
            {
                var limit = long.Parse(args[3], CultureInfo.InvariantCulture);
                new VideoCompressor(MediaTools.Resolve()).CompressAsync(new(args[1], args[2], limit))
                    .GetAwaiter().GetResult();
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
