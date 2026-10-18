using CommunityToolkit.Mvvm.DependencyInjection;
using iNKORE.UI.WPF.Modern;
using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using STranslate.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace STranslate.Core;

public class PluginContext(PluginMetaData metaData, string serviceId) : IPluginContext
{
    private IPluginSavable Savable { get => field; set => field = value; } = null!;

    public PluginMetaData MetaData => metaData;

    public ILogger Logger => Ioc.Default.GetRequiredService<ILoggerFactory>().CreateLogger(metaData.AssemblyName);

    public string GetTranslation(string key) => Ioc.Default.GetRequiredService<Internationalization>().GetTranslation(key);

    public IHttpService HttpService => Ioc.Default.GetRequiredService<IHttpService>();

    public IAudioPlayer AudioPlayer => Ioc.Default.GetRequiredService<IAudioPlayer>();

    public ISnackbar Snackbar => Ioc.Default.GetRequiredService<ISnackbar>();

    public INotification Notification => Ioc.Default.GetRequiredService<INotification>();

    public ImageQuality ImageQuality => Ioc.Default.GetRequiredService<Settings>().ImageQuality;

    public Window? GetPromptEditWindow(ObservableCollection<Prompt> prompts, List<string>? roles = default)
    {
        // Prompt 编辑器已随精简版本移除，这里仅提示并返回 null。
        System.Windows.MessageBox.Show(
            "Prompt 编辑器功能已移除。",
            "QTranslate",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return null;
    }

    public T LoadSettingStorage<T>() where T : new()
    {
        var storage = new PluginStorage<T>(metaData, serviceId);
        var data = storage.Load();

        // 初始化时尝试创建配置文件
        if (storage.IsDefaultData)
            storage.Save();

        Savable = storage;
        return data;
    }

    public void SaveSettingStorage<T>() where T : new() => Savable?.Save();

    public void ApplyTheme(Window window)
    {
        if (window == null)
            return;

        ThemeManager.SetRequestedTheme(window, Ioc.Default.GetRequiredService<Settings>().ColorScheme);
    }

    public void Dispose()
    {
        Savable.Delete();
        Savable.Clean();
    }
}