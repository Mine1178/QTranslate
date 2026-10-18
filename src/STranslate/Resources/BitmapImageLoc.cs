using System.Windows.Media.Imaging;

namespace STranslate.Resources;

internal class BitmapImageLoc
{
    public static readonly BitmapImage DevIcon = new(new Uri("pack://application:,,,/Resources/dev.ico"));
    public static readonly BitmapImage AppIcon = new(new Uri("pack://application:,,,/Resources/app.png"));
    public static readonly BitmapImage NoHotkeyIcon = new(new Uri("pack://application:,,,/Resources/app.png"));
    public static readonly BitmapImage IgnoreOnFullScreenIcon = new(new Uri("pack://application:,,,/Resources/app.png"));
}
