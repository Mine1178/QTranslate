using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace STranslate.Plugin;

/// <summary>
/// 提供插件设置存储的加载和保存功能的接口。
/// </summary>
public interface IPluginContext : IDisposable
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    PluginMetaData MetaData { get; }

    /// <summary>
    /// 日志
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// 获取翻译
    /// </summary>
    string GetTranslation(string key);

    /// <summary>
    /// Http服务
    /// </summary>
    IHttpService HttpService {get; }

    /// <summary>
    /// 音频播放
    /// </summary>
    IAudioPlayer AudioPlayer { get; }

    /// <summary>
    /// 消息弹窗
    /// </summary>
    ISnackbar Snackbar { get; }

    /// <summary>
    /// 通知
    /// </summary>
    INotification Notification { get; }

    /// <summary>
    /// 图片质量
    /// </summary>
    ImageQuality ImageQuality { get; }

    /// <summary>
    /// 获取Prompt编辑窗口
    /// </summary>
    /// <param name="prompts"></param>
    /// <param name="roles"></param>
    /// <returns></returns>
    Window? GetPromptEditWindow(ObservableCollection<Prompt> prompts, List<string>? roles = default);

    /// <summary>
    /// 加载插件的设置存储。
    /// </summary>
    /// <typeparam name="T">设置存储的类型。</typeparam>
    /// <returns>设置存储对象。</returns>
    T LoadSettingStorage<T>() where T : new();

    /// <summary>
    /// 保存插件的设置存储。
    /// </summary>
    /// <typeparam name="T">设置存储的类型。</typeparam>
    void SaveSettingStorage<T>() where T : new();

    /// <summary>
    /// 将当前应用主题应用到指定窗口，使插件窗口与主程序保持一致的视觉风格。
    /// </summary>
    /// <param name="window">需要应用主题的窗口实例</param>
    void ApplyTheme(Window window);
}