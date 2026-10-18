using Microsoft.Extensions.Logging;
using STranslate.Helpers;
using STranslate.Plugin;
using STranslate.ViewModels;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace STranslate.Core;

public class ExternalCallService(
    ILogger<ExternalCallService> logger,
    MainWindowViewModel viewModel,
    Internationalization i18n,
    INotification notification)
{
    private HttpListener? _listener;
    private readonly SemaphoreSlim _externalCallLock = new(1, 1);

    public bool IsStarted { get; private set; }

    public bool StartService(string prefix)
    {
        StopService();

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);

            _listener.Start();
            _listener.BeginGetContext(Callback, _listener);
            IsStarted = true;

            OnActionOccurred?.Invoke(string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "启动服务失败请重新配置端口: {Prefix}", prefix);
            var msg = string.Format(i18n.GetTranslation("ExternalCallStartFailed"), prefix);
            OnActionOccurred?.Invoke(msg);
            notification.Show(i18n.GetTranslation("Prompt"), msg);

            return false;
        }
    }

    public void StopService()
    {
        if (!IsStarted)
            return;

        OnActionOccurred?.Invoke(string.Empty);
        _listener?.Close();
        _listener = null;
        IsStarted = false;
    }

    private void Callback(IAsyncResult ar)
    {
        if (!IsStarted || _listener == null || !_listener.IsListening)
            return;

        HttpListenerContext context;
        try
        {
            context = _listener.EndGetContext(ar);
        }
        catch (Exception)
        {
            // HttpListener has been disposed, no need to handle the request
            return;
        }

        _listener.BeginGetContext(Callback, _listener);

        try
        {
            var request = context.Request;

            // Get the URL from the request
            var uri = request.Url ?? throw new Exception("get url is null");

            if (uri.Segments.Length > 2)
                throw new Exception("path does not meet the requirements");

            // Get the path from the URL
            var path = uri.AbsolutePath.TrimStart('/');
            path = path == "" ? "translate" : path;

            // Get the external call action based on the path
            var ecAction = GetExternalCallAction(path);

            // Read the content of the request
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var content = reader.ReadToEnd();

            //Please use GET like `curl localhost:50020/translate -d \"hello world\"`"
            switch (request.HttpMethod)
            {
                case "GET":
                    ExecuteExternalCall(ecAction, "");
                    break;

                case "POST":
                    ExecuteExternalCall(ecAction, content);
                    break;

                default:
                    throw new Exception("Method Not Allowed");
            }

            ResponseHandler(context.Response);
        }
        catch (Exception e)
        {
            ResponseHandler(context.Response, e.Message);
            logger.LogError(e, $"ExternalCall Error, {e.Message}");
        }
    }

    private void ExecuteExternalCall(ExternalCallAction action, string content)
    {
        var dispatcher = App.Current?.Dispatcher;
        if (dispatcher == null)
        {
            logger.LogWarning("Dispatcher is unavailable, skip external action: {Action}", action);
            return;
        }

        // 外部接口会被并发请求，串行执行可避免静默任务重叠导致全局光标状态错乱。
        _ = dispatcher.InvokeAsync(() => ExecuteExternalCallAsync(action, content));
    }

    /// <summary>
    /// 在 UI 线程串行执行外部调用，确保后台静默任务不会并发改写全局状态。
    /// </summary>
    /// <param name="action">外部调用动作类型</param>
    /// <param name="content">外部调用内容</param>
    /// <returns>异步任务</returns>
    private async Task ExecuteExternalCallAsync(ExternalCallAction action, string content)
    {
        await _externalCallLock.WaitAsync();
        try
        {
            using var _ = WindowActivationContext.Push(WindowActivationMode.ForceForeground);

            switch (action)
            {
                case ExternalCallAction.translate:
                    if (string.IsNullOrWhiteSpace(content))
                        viewModel.InputClearCommand.Execute(null);
                    else
                        viewModel.ExecuteTranslate(content);
                    break;
                case ExternalCallAction.translate_force:
                    if (string.IsNullOrWhiteSpace(content))
                        viewModel.InputClearCommand.Execute(null);
                    else
                        viewModel.ExecuteTranslate(content, "force");
                    break;
                case ExternalCallAction.translate_input:
                    viewModel.InputClearCommand.Execute(null);
                    break;
                case ExternalCallAction.translate_crossword:
                    await viewModel.CrosswordTranslateCommand.ExecuteAsync(null);
                    break;
                case ExternalCallAction.translate_mousehook:
                    viewModel.ToggleMouseSelectionTranslationCommand.Execute(null);
                    break;
                case ExternalCallAction.translate_replace:
                    {
                        if (viewModel.ReplaceTranslateCommand.IsRunning)
                        {
                            viewModel.ReplaceTranslateCancelCommand.Execute(null);
                            return;
                        }
                        await viewModel.ReplaceTranslateCommand.ExecuteAsync(null);
                    }
                    break;
                case ExternalCallAction.open_window:
                    viewModel.ToggleAppCommand.Execute(null);
                    break;
                case ExternalCallAction.open_preference:
                    await viewModel.OpenSettingsAndNavigateAsync(null);
                    break;
                case ExternalCallAction.open_history:
                    await viewModel.OpenHistoryInternalAsync();
                    break;
                case ExternalCallAction.forbiddenhotkey:
                    viewModel.ToggleGlobalHotkey();
                    break;
                case ExternalCallAction.tts_silence:
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        if (viewModel.SilentTtsCommand.IsRunning)
                        {
                            viewModel.SilentTtsCancelCommand.Execute(null);
                            return;
                        }
                        await viewModel.SilentTtsCommand.ExecuteAsync(null);
                    }
                    else
                        await viewModel.SilentTtsHandlerAsync(content);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "External action execution failed: {Action}", action);
        }
        finally
        {
            _externalCallLock.Release();
        }
    }

    /// <summary>
    ///     字符串=>外部调用枚举
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    private ExternalCallAction GetExternalCallAction(string source)
    {
        return Enum.TryParse<ExternalCallAction>(source, true, out var eAction)
            ? eAction
            : throw new Exception("path does not meet the requirements");
    }

    private void ResponseHandler(HttpListenerResponse response, string? error = null)
    {
        response.StatusCode = HttpStatusCode.OK.GetHashCode();
        response.ContentType = "application/json;charset=UTF-8";
        response.ContentEncoding = Encoding.UTF8;
        response.AppendHeader("Content-Type", "application/json;charset=UTF-8");

        var data = new
        {
            code = error is null ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
            data = error ?? "Call Succeed"
        };

        using StreamWriter writer = new(response.OutputStream, Encoding.UTF8);
        writer.Write(JsonSerializer.Serialize(data));
        writer.Close();
        response.Close();
    }

    public Action<string>? OnActionOccurred;
}

/// <summary>
///     外部调用功能
/// </summary>
public enum ExternalCallAction
{
    translate = 1,
    translate_force,
    translate_input,
    translate_ocr,
    translate_ocr_image,
    translate_crossword,
    translate_mousehook,
    translate_replace,
    ocr,
    ocr_silence,
    ocr_qrcode,
    open_window,
    open_preference,
    open_history,
    forbiddenhotkey,
    tts_silence
}
