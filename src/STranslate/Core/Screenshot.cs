using ScreenGrab;
using System.Drawing;
using System.Windows;

namespace STranslate.Core;

public class Screenshot(Settings settings) : IScreenshot
{
    private const int DefaultCaptureDelayMs = 150;

    public Bitmap? GetScreenshot()
    {
        if (ScreenGrabber.IsCapturing)
            return default;
        var bitmap = ScreenGrabber.CaptureDialog(settings.ShowScreenshotAuxiliaryLines);
        if (bitmap == null)
            return default;
        return bitmap;
    }

    public async Task<Bitmap?> GetScreenshotAsync()
    {
        return await CaptureBitmapAsync();
    }

    public async Task<ScreenshotCaptureResult?> GetScreenshotCaptureAsync()
    {
        if (ScreenGrabber.IsCapturing)
            return default;

        var wasMainWindowVisible = App.Current.MainWindow.Visibility == Visibility.Visible &&
                                   !App.Current.MainWindow.Topmost;
        if (wasMainWindowVisible)
            App.Current.MainWindow.Visibility = Visibility.Collapsed;

        try
        {
            // Allow UI to update before capturing
            await Task.Delay(DefaultCaptureDelayMs);

            var padImage = ShouldPadImage(settings.ImageTranslateWindowMode);
            var capture = await ScreenGrabber.CaptureWithRegionAsync(settings.ShowScreenshotAuxiliaryLines, padImage);
            if (capture == null)
                return default;

            return new ScreenshotCaptureResult(capture.Bitmap, capture.Region);
        }
        finally
        {
            if (wasMainWindowVisible)
                App.Current.MainWindow.Visibility = Visibility.Visible;
        }
    }

    internal static bool ShouldPadImage(ImageTranslateWindowMode mode) =>
        mode != ImageTranslateWindowMode.Compact;

    private async Task<Bitmap?> CaptureBitmapAsync()
    {
        if (ScreenGrabber.IsCapturing)
            return default;

        var wasMainWindowVisible = App.Current.MainWindow.Visibility == Visibility.Visible &&
                                   !App.Current.MainWindow.Topmost;
        if (wasMainWindowVisible)
            App.Current.MainWindow.Visibility = Visibility.Collapsed;

        try
        {
            // Allow UI to update before capturing
            await Task.Delay(DefaultCaptureDelayMs);

            var bitmap = await ScreenGrabber.CaptureAsync(settings.ShowScreenshotAuxiliaryLines);
            if (bitmap == null)
                return default;

            return bitmap;
        }
        finally
        {
            if (wasMainWindowVisible)
                App.Current.MainWindow.Visibility = Visibility.Visible;
        }
    }
}
