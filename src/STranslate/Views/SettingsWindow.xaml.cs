using CommunityToolkit.Mvvm.DependencyInjection;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using STranslate.ViewModels;
using STranslate.ViewModels.Pages;
using STranslate.Views.Pages;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Views;

public partial class SettingsWindow
{
    private readonly SettingsWindowViewModel _viewModel;
    // 独立 DI scope：SettingsWindow 内的各 Page 及其 Page VM（HistoryViewModel 等）为 Scoped 注册。
    // 从 root provider 解析则被 root scope 跟踪、关闭时 Dispose 永不触发（root 永不释放），
    // 导致 Page VM 的事件订阅（如 HistoryViewModel 订阅 SelectedItems.CollectionChanged、
    // SearchViewModelBase 订阅 i18n.OnLanguageChanged）泄漏。用独立 scope 解析并在窗口关闭时释放。
    private readonly IServiceScope _serviceScope;
    private bool _isCodeNavi;

    public SettingsWindow()
    {
        _serviceScope = Ioc.Default.CreateScope();
        try
        {
            _viewModel = _serviceScope.ServiceProvider.GetRequiredService<SettingsWindowViewModel>();
            DataContext = _viewModel;

            InitializeComponent();
        }
        catch
        {
            _serviceScope.Dispose();
            throw;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (!e.Cancel)
        {
            RunWithNavigationProtection(
                (INavigation)App.Current,
                () => ModernWindowLifecycle.DetachModernWindowStyle(this));
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // 先解绑导航事件并清空 Frame，再统一释放视觉树与 DI scope。
        // 释放 scope 会触发当前已解析 Page VM 的 Dispose()（HistoryViewModel 等）。
        RootNavigation.SelectionChanged -= OnNaviSelectionChanged;
        RunWithNavigationProtection(
            (INavigation)App.Current,
            () =>
            {
                RootFrame.Content = null;
                ModernWindowLifecycle.Release(this, _serviceScope.Dispose);
            });
        base.OnClosed(e);
    }

    private void OnNaviSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_isCodeNavi)
        {
            _isCodeNavi = false;
            return;
        }

        var selectedItem = args.SelectedItemContainer;
        var tag = selectedItem.Tag?.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        Navigate(tag, false);
    }

    /// <summary>
    /// 导航到指定设置页面，并可在服务页面中选中目标服务实例。
    /// </summary>
    /// <param name="tag">目标页面的导航标识。</param>
    /// <param name="isCodeBehinde">是否同步更新左侧导航选中项。</param>
    /// <param name="selectedService">需要在目标服务页面中选中的服务实例。</param>
    public void Navigate(string tag, bool isCodeBehinde = true, Service? selectedService = null)
    {
        // Page 及其 Page VM 为 Scoped 注册，统一从窗口独有 scope 解析，
        // 关闭窗口时释放 scope 即可触发 Page VM 的 Dispose()。
        var sp = _serviceScope.ServiceProvider;
        var content = tag switch
        {
            nameof(GeneralPage) => sp.GetRequiredService<GeneralPage>(),
            nameof(TranslatePage) => sp.GetRequiredService<TranslatePage>(),
            nameof(TtsPage) => sp.GetRequiredService<TtsPage>(),
            nameof(VocabularyPage) => sp.GetRequiredService<VocabularyPage>(),
            nameof(StandalonePage) => sp.GetRequiredService<StandalonePage>(),
            nameof(HistoryPage) => sp.GetRequiredService<HistoryPage>(),
            nameof(PluginPage) => sp.GetRequiredService<PluginPage>(),
            nameof(HotkeyPage) => sp.GetRequiredService<HotkeyPage>(),
            nameof(AboutPage) => sp.GetRequiredService<AboutPage>(),
            _ => default(iNKORE.UI.WPF.Modern.Controls.Page)
        };
        if (isCodeBehinde)
        {
            _isCodeNavi = true;
            switch (tag)
            {
                case nameof(GeneralPage):
                    RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
                    break;
                case nameof(TranslatePage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[0];
                    break;
                case nameof(TtsPage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[1];
                    break;
                case nameof(VocabularyPage):
                    RootNavigation.SelectedItem = (RootNavigation.MenuItems[1] as NavigationViewItem)?.MenuItems[2];
                    break;
                case nameof(StandalonePage):
                    RootNavigation.SelectedItem = RootNavigation.MenuItems[2];
                    break;
                case nameof(HistoryPage):
                    RootNavigation.SelectedItem = RootNavigation.MenuItems[3];
                    break;
                default:
                    break;
            }
        }
        RunWithNavigationProtection(
            (INavigation)App.Current,
            () => RootFrame.Content = content);
        ApplySelectedService(content?.DataContext, selectedService);
    }

    /// <summary>
    /// 在可能导致 PasswordBox 被 WPF 清空的视觉树变更期间启用导航保护，
    /// 并在操作结束或抛出异常后恢复调用前的状态。
    /// </summary>
    internal static void RunWithNavigationProtection(INavigation navigation, Action action)
    {
        var originalState = navigation.IsNavigated;
        navigation.IsNavigated = true;
        try
        {
            action();
        }
        finally
        {
            navigation.IsNavigated = originalState;
        }
    }

    internal static void ApplySelectedService(object? dataContext, Service? selectedService)
    {
        if (selectedService is not null && dataContext is IServiceSelectionViewModel viewModel)
            viewModel.SelectedItem = selectedService;
    }

    private void OnKeyDown(object _, KeyEventArgs e)
    {
        if (e.Key is not Key.F || Keyboard.Modifiers is not ModifierKeys.Control) return;

        switch (RootFrame.Content)
        {
            case GeneralPage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
            case StandalonePage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
            case HistoryPage page:
                FocusAndSelectAll(page.PART_SearchBox);
                break;
            case PluginPage page:
                FocusAndSelectAll(page.ViewModel.IsMarketView ? page.MarketFilterTextbox : page.PluginFilterTextbox);
                break;
            case HotkeyPage page:
                FocusAndSelectAll(page.PART_AutoSuggestBox);
                break;
        }
    }

    private static void FocusAndSelectAll(Control control)
    {
        control.Focus();

        if (control is TextBox textBox)
            textBox.SelectAll();
        else if (control is AutoSuggestBox autoSuggestBox)
            Utilities.FindVisualChild<TextBox>(autoSuggestBox, "TextBox")?.SelectAll();
    }
}
