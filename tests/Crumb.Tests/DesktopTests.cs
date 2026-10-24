using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Crumb.Desktop;
using Crumb.Desktop.ViewModels;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Crumb.Tests.TestApplication))]

namespace Crumb.Tests;

public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().WithInterFont().UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed class DesktopTests
{
    [AvaloniaFact]
    public void WindowRendersAndInvalidInputStaysDisabled()
    {
        var window = new MainWindow();
        try
        {
            window.Show();
            var button = window.FindControl<Button>("CompressButton")!;
            var limit = window.FindControl<TextBox>("LimitInput")!;
            Assert.False(button.IsEnabled);
            limit.Text = "invalid";
            Dispatcher.UIThread.RunJobs();
            var model = Assert.IsType<MainViewModel>(window.DataContext);
            Assert.Equal("invalid", model.LimitMb);
            Assert.False(button.IsEnabled);
            Assert.Contains("Enter a size", model.CompressionHint);
            limit.Text = "25";
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame.PixelSize.Width >= 680);
            if (Environment.GetEnvironmentVariable("CRUMB_SCREENSHOT_DIR") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                frame.Save(Path.Combine(directory, "crumb.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            }
        }
        finally { window.Close(); }
    }
}
