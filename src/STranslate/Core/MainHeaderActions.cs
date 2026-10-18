namespace STranslate.Core;

public static class MainHeaderActions
{
    public const string ClipboardMonitor = "clipboard_monitor";
    public const string AutoTranslate = "auto_translate";
    public const string Ocr = "ocr";
    public const string ImageTranslate = "image_translate";
    public const string ScreenshotTranslate = "screenshot_translate";
    public const string MouseSelectionTranslation = "mouse_selection_translation";
    public const string ColorScheme = "color_scheme";
    public const string HideInput = "hide_input";
    public const string ServiceSwitcher = "service_switcher";
    public const string HistoryNavigation = "history_navigation";

    public static IReadOnlyList<string> DefaultOrder { get; } =
    [
        ClipboardMonitor,
        AutoTranslate,
        MouseSelectionTranslation,
        ColorScheme,
        HideInput,
        ServiceSwitcher,
        HistoryNavigation
    ];

    public static IReadOnlyList<string> All => DefaultOrder;

    private static readonly Dictionary<string, string> CanonicalActionMap = DefaultOrder
        .ToDictionary(action => action, StringComparer.OrdinalIgnoreCase);

    public static List<string> Normalize(IEnumerable<string>? actions)
    {
        if (actions == null)
        {
            return [];
        }

        var normalizedActions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var action in actions)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                continue;
            }

            var trimmedAction = action.Trim();
            if (!CanonicalActionMap.TryGetValue(trimmedAction, out var canonicalAction))
            {
                continue;
            }

            if (seen.Add(canonicalAction))
            {
                normalizedActions.Add(canonicalAction);
            }
        }

        return normalizedActions;
    }
}
