using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using STranslate.Services;
using STranslate.Views;
using System.Diagnostics;
using System.IO;

namespace STranslate.ViewModels.Pages;

public partial class AboutViewModel(
    Settings settings,
    HotkeySettings hotkeySettings,
    ServiceSettings serviceSettings,
    DataProvider dataProvider,
    INotification notification,
    ISnackbar snackbar,
    Internationalization i18n,
    BackupService backupService) : ObservableObject
{
    private const int PortableModeOperationDelaySeconds = 3;

    public Settings Settings { get; } = settings;
    public DataProvider DataProvider { get; } = dataProvider;
    public bool IsPortableMode => DataLocation.PortableDataLocationInUse();
    public string Version => Constant.Version switch
    {
        "1.0.0" => Constant.Dev,
        _ => $"V{Constant.Version}"
    };

    #region ICommand

    [RelayCommand]
    private void Donate() => new SponsorWindow().ShowDialog();

    [RelayCommand]
    private void LocateUserData() => Locate(Path.GetDirectoryName(Path.Combine(DataLocation.SettingsDirectory)));

    [RelayCommand]
    private void LocateSettings() => Locate(DataLocation.SettingsDirectory);

    [RelayCommand]
    private void LocateLog() => Locate(Path.Combine(DataLocation.LogDirectory, Constant.Version));

    [RelayCommand]
    private void LocateCache() => Locate(DataLocation.CacheDirectory);

    [RelayCommand]
    private async Task TogglePortableModeAsync()
    {
        var isPortableMode = IsPortableMode;

        if (await new ContentDialog
        {
            Title = i18n.GetTranslation("Prompt"),
            PrimaryButtonText = i18n.GetTranslation("Confirm"),
            CloseButtonText = i18n.GetTranslation("Cancel"),
            DefaultButton = ContentDialogButton.Close,
            Content = i18n.GetTranslation(isPortableMode
                ? "ConfirmRestartAndDisablePortableMode"
                : "ConfirmRestartAndEnablePortableMode")
        }.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        if (isPortableMode && Directory.Exists(DataLocation.RoamingDataPath))
        {
            notification.ShowWithButton(
                i18n.GetTranslation("Prompt"),
                i18n.GetTranslation("OpenDirectory"),
                () => FilesFolders.OpenPath(DataLocation.RoamingDataPath),
                i18n.GetTranslation("PortableModeDisableRoamingConflict"));
            return;
        }

        Settings.Save();
        hotkeySettings.Save();
        serviceSettings.Save();

        var mode = isPortableMode ? "disable" : "enable";
        var sourcePath = isPortableMode ? DataLocation.PortableDataPath : DataLocation.RoamingDataPath;
        var targetPath = isPortableMode ? DataLocation.RoamingDataPath : DataLocation.PortableDataPath;
        var successMessage = i18n.GetTranslation(isPortableMode
            ? "PortableModeDisableSuccess"
            : "PortableModeEnableSuccess");
        var failurePrefix = i18n.GetTranslation(isPortableMode
            ? "PortableModeDisableFailed"
            : "PortableModeEnableFailed");

        var args = new List<string>
        {
            "portable",
            "-m", mode,
            "-s", sourcePath,
            "-t", targetPath,
            "-d", PortableModeOperationDelaySeconds.ToString(),
            "-p", DataLocation.AppExePath,
            "-i", DataLocation.InfoFilePath,
            "-w", successMessage,
            "-f", failurePrefix
        };

        var executeResult = Utilities.ExecuteProgram(DataLocation.HostExePath, [.. args]);
        if (!executeResult.Success)
        {
            snackbar.ShowError(string.Format(i18n.GetTranslation("PortableModeHostStartFailed"), executeResult.ErrorMessage));
            return;
        }

        App.RequestShutdown(AppShutdownReason.PortableModeChange);
    }

    private void Locate(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        Process.Start("explorer.exe", folder);
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        if (Settings.Backup.Type == BackupType.Local)
            await backupService.LocalBackupAsync();
        else
            await backupService.PreWebDavBackupAsync();
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (Settings.Backup.Type == BackupType.Local)
            await backupService.LocalRestoreAsync();
        else
            await backupService.WebDavRestoreAsync();
    }

    #endregion
}
