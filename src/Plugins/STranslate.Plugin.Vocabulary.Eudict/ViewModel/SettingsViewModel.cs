using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace STranslate.Plugin.Vocabulary.Eudict.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    [ObservableProperty] public partial string BookName { get; set; }
    [ObservableProperty] public partial string Token { get; set; }
    [ObservableProperty] public partial string BookID { get; set; }

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        BookName = settings.BookName;
        Token = settings.Token;
        BookID = settings.BookID;

        PropertyChanged += OnPropertyChanged;
    }

    public void Dispose() => PropertyChanged -= OnPropertyChanged;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case (nameof(BookName)):
                _settings.BookName = BookName;
                break;
            case (nameof(Token)):
                _settings.Token = Token;
                break;
            case (nameof(BookID)):
                _settings.BookID = BookID;
                break;
            default:
                return;
        }

        _context.SaveSettingStorage<Settings>();
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        const string url = "https://api.frdic.com/api/open/v1/studylist/category";
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(BookName, $"生词本服务: {BookName} 中生词本名称为空");

            var option = new Options
            {
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", _settings.Token }
                },
                QueryParams = new Dictionary<string, string>
                {
                    { "language", "en" }
                }
            };
            var bookList = await _context.HttpService.GetAsync(url, option);
            var bookId = GetIdByNameInArray(bookList, BookName);
            if (string.IsNullOrWhiteSpace(bookId))
            {
                var headerOption = new Options
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", _settings.Token }
                    }
                };
                var content = new
                {
                    language = "en",
                    name = BookName
                };
                var resp = await _context.HttpService.PostAsync(url, content, headerOption);
                bookId = GetIdByName(resp);
                ArgumentException.ThrowIfNullOrWhiteSpace(bookId,
                    $"创建生词本服务: {BookName}->生词本名称: {BookName} 失败, 接口回复: {resp}");
            }

            BookID = bookId;
            _context.Snackbar.ShowSuccess("检查成功");
        }
        catch (Exception ex)
        {
            _context.Snackbar.ShowError("检查失败");
            _context.Logger.LogError(ex, $"检查生词本服务： {BookName} 配置失败");
        }
    }

    private static string GetIdByName(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement
            .TryGetProperty("data", out var data) &&
            data.TryGetProperty("id", out var id)
                ? id.GetString() ?? string.Empty
                : string.Empty;
    }

    private static string GetIdByNameInArray(string json, string name)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var item in dataElement.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameElement) &&
                nameElement.GetString() == name)
            {
                return item.TryGetProperty("id", out var idElement)
                    ? idElement.GetString() ?? string.Empty
                    : string.Empty;
            }
        }

        return string.Empty;
    }
}