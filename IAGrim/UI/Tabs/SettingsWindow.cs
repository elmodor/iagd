using Avalonia;
using Avalonia.Styling;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using EvilsoftCommons;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.TransferStash;
using IAGrim.Services;
using IAGrim.Settings;
using IAGrim.UI.Controller;
using IAGrim.UI.Misc.CEF;
using IAGrim.UI.Popups;
using IAGrim.Utilities;
using IAGrim.Utilities.HelperClasses;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Overwrites;

namespace IAGrim.UI.Tabs {
    partial class SettingsWindow : UserControl {
        private readonly ISettingsController _controller;
        private readonly Action _itemViewUpdateTrigger;
        private readonly Action _selectItemsTab;
        private readonly IPlayerItemDao _playerItemDao;
        private readonly GDTransferFile[] _modFilter;
        private readonly CefBrowserHandler _cefBrowserHandler;
        private readonly SettingsService _settings;
        private readonly GrimDawnDetector _grimDawnDetector;
        private readonly AutomaticUpdateChecker _automaticUpdateChecker;
        private readonly IItemTagDao _itemTagDao;
        private readonly ParsingService _parsingService;


        public SettingsWindow(
            CefBrowserHandler cefBrowserHandler,
            Action itemViewUpdateTrigger,
            Action selectItemsTab,
            IPlayerItemDao playerItemDao,
            GDTransferFile[] modFilter,
            SettingsService settings, IItemTagDao itemTagDao, ParsingService parsingService,
            GrimDawnDetector grimDawnDetector,
            AutomaticUpdateChecker automaticUpdateChecker) {
            InitializeComponent();
            _controller = new SettingsController(settings);
            this._cefBrowserHandler = cefBrowserHandler;
            this._itemViewUpdateTrigger = itemViewUpdateTrigger;
            this._selectItemsTab = selectItemsTab;
            this._playerItemDao = playerItemDao;
            this._modFilter = modFilter;
            _settings = settings;
            _itemTagDao = itemTagDao;
            _parsingService = parsingService;
            _grimDawnDetector = grimDawnDetector;
            _automaticUpdateChecker = automaticUpdateChecker;

            // /TODO
            // _controller.BindCheckbox(cbMinimizeToTray);

            // _controller.BindCheckbox(cbHideSkills);
            // _controller.LoadDefaults();

            // TODO: Write out the settingscontroller and add logic for updating showskills config

            linkCheckForUpdates.IsVisible = Environment.Is64BitOperatingSystem;
            pbAutomaticUpdates.IsVisible = Environment.Is64BitOperatingSystem;
            SettingsWindow_Load();
        }

        private void SettingsWindow_Load() {
            radioBeta.IsChecked = _settings.GetPersistent().CheckUpdatesDaily;
            radioRelease.IsChecked = !_settings.GetPersistent().CheckUpdatesDaily;
            cbDualComputer.IsChecked = _settings.GetPersistent().UsingDualComputer;
            cbStartMinimized.IsChecked = _settings.GetLocal().StartMinimized;
            cbDarkMode.IsChecked = _settings.GetPersistent().DarkMode;
            cbAutoDismiss.IsChecked = _settings.GetPersistent().AutoDismissNotifications;
            cbTransferAnyMod.IsChecked = _settings.GetPersistent().TransferAnyMod;
            cbDelaySearch.IsChecked = _settings.GetLocal().PreferDelayedSearch;
            cbZipBackups.IsChecked = _settings.GetLocal().BackupCustom;
            cbUseDllHookFiles.IsChecked = _settings.GetLocal().UseDllHookFiles;

        }

        private void buttonViewBackups_Click(object? sender, RoutedEventArgs e)
        {
            _controller.OpenDataFolder();
        }

        private void buttonViewLogs_Click(object? sender, RoutedEventArgs e)
        {
            _controller.OpenLogFolder();
        }

        private void radioRelease_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (radioRelease.IsChecked == true)
                _settings.GetPersistent().CheckUpdatesDaily = false;
        }

        private void radioBeta_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (radioBeta.IsChecked == true)
                _settings.GetPersistent().CheckUpdatesDaily = true;
        }

        // create bindings and stick these into its own settings class
        // unit testable


        private void linkLabel1_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo{FileName = "https://discord.gg/5wuCPbB", UseShellExecute = true});
        }

        private async void copyToolStripMenuItem_Click(object? sender, RoutedEventArgs e) {
            var topLevel = TopLevel.GetTopLevel(this);
            var clipboard = topLevel?.Clipboard;
            if (clipboard != null) {
                var data = new DataTransfer();
                data.Add(DataTransferItem.CreateText("https://discord.gg/5wuCPbB"));
                await clipboard.SetDataAsync(data);
                _ = TooltipHelper.ShowTooltipForControl( RuntimeSettings.Language!.GetTag("iatag_ui_copiedclipboard"), linkLabel1, TooltipHelper.TooltipLocation.TOP);
            }
        }

        private async void buttonLanguageSelect_Click(object sender, RoutedEventArgs e) {
            var languagePackPicker = new LanguagePackPicker(_itemTagDao, _playerItemDao, _parsingService, _settings);
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner != null) {
                await languagePackPicker.Show(_grimDawnDetector.GetGrimLocations(), owner);
            }
            _itemViewUpdateTrigger?.Invoke();
        }

        private async void buttonImportExport_Click(object sender, RoutedEventArgs e) {
            var dialog = new Popups.ImportExport.ImportExportContainer(_modFilter, _playerItemDao);
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner != null) {
                await dialog.ShowDialog(owner);
            }
            else {
                dialog.Show();
            }
        }

        private void cbDisplaySkills_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            _itemViewUpdateTrigger?.Invoke();
        }

        private async void buttonAdvancedSettings_Click(object sender, RoutedEventArgs e) {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null) {
                await new StashTabPicker(_settings, _cefBrowserHandler).ShowDialog(window);
            }
        }

        private async void buttonResetSettings_Click(object sender, RoutedEventArgs e) {
            var body = RuntimeSettings.Language!.GetTag("iatag_ui_resetsettings_body");
            var title = RuntimeSettings.Language!.GetTag("iatag_ui_resetsettings_title");

            var result = await MessageBox.Show(body, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == MessageBoxResult.Yes) {
                StartupService.ResetSettingsAndRestart();
            }
        }

        private void linkSourceCode_Click(object? sender, RoutedEventArgs e)
        {
            // TODO
            // Process.Start(new ProcessStartInfo{FileName = "https://github.com/marius00/iagd", UseShellExecute = true});
        }

        private async void copyToolStripMenuItemSourceCode_Click(object? sender, RoutedEventArgs e) {
            var topLevel = TopLevel.GetTopLevel(this);
            var clipboard = topLevel?.Clipboard;
            if (clipboard != null) {
                // TODO
                // var data = new DataTransfer();
                // data.Add(DataTransferItem.CreateText("https://github.com/marius00/iagd"));
                // await clipboard.SetDataAsync(data);
                _ = TooltipHelper.ShowTooltipForControl( RuntimeSettings.Language!.GetTag("iatag_ui_copiedclipboard"), linkSourceCode, TooltipHelper.TooltipLocation.TOP);
            }
        }


        private async void cbDualComputer_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            var enabled = cbDualComputer.IsChecked == true;
            if (_settings.GetPersistent().UsingDualComputer == enabled)
                return;

            _settings.GetPersistent().UsingDualComputer = enabled;

            await MessageBox.Show("IAGD is restarting to toggle DUAL-PC mode", "Restarting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                Process.Start(new ProcessStartInfo{FileName = processPath, UseShellExecute = true});
            }
            Environment.Exit(0);
        }

        private void helpWhatIsUsingMultiplePc_LinkClicked(object sender, RoutedEventArgs e) {
            _cefBrowserHandler.ShowHelp(IHelpService.HelpType.MultiplePcs);
            _selectItemsTab();
        }

        private void buttonDonate_Click(object? sender, RoutedEventArgs e)
        {
            _controller.DonateNow();
        }

        private void buttonPatreon_Click(object? sender, RoutedEventArgs e)
        {
            // TODO?
            // Process.Start(new ProcessStartInfo{FileName = "https://www.patreon.com/itemassistant", UseShellExecute = true});
        }

        private void helpWhatIsRegularUpdates_LinkClicked(object sender, RoutedEventArgs e) {
            _cefBrowserHandler.ShowHelp(IHelpService.HelpType.RegularUpdates);
            _selectItemsTab();
        }

        private void helpWhatIsExperimentalUpdates_LinkClicked(object sender, RoutedEventArgs e) {
            _cefBrowserHandler.ShowHelp(IHelpService.HelpType.RegularUpdates);
            _selectItemsTab();
        }

        private void cbStartMinimized_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            _settings.GetLocal().StartMinimized = cbStartMinimized.IsChecked == true;
        }

        private async void cbDarkMode_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            var enabled = cbDarkMode.IsChecked == true;
            if (_settings.GetPersistent().DarkMode != enabled) {
                _settings.GetPersistent().DarkMode = enabled;
                _cefBrowserHandler.SetDarkMode(enabled);
                Application.Current!.RequestedThemeVariant = enabled ? ThemeVariant.Dark : ThemeVariant.Light;
            }

        }

        private void linkCheckForUpdates_Click(object? sender, RoutedEventArgs e)
        {
            _automaticUpdateChecker.CheckForUpdates(true);
        }

        private void cbAutoDismiss_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            _settings.GetPersistent().AutoDismissNotifications = cbAutoDismiss.IsChecked == true;
        }


        private void cbTransferAnyMod_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            _settings.GetPersistent().TransferAnyMod = cbTransferAnyMod.IsChecked == true;
        }

        private void helpWhatIsDelayWhenSearching_LinkClicked(object sender, RoutedEventArgs e) {

            _cefBrowserHandler.ShowHelp(IHelpService.HelpType.DelayWhenSearching);
            _selectItemsTab();
        }

        private void cbDelaySearch_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            _settings.GetLocal().PreferDelayedSearch = cbDelaySearch.IsChecked == true;
        }

        private void cbZipBackups_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            _settings.GetLocal().BackupCustom = cbZipBackups.IsChecked == true;
        }

        private async void lbDefineBackupLocation_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title = "Select backup location", AllowMultiple = false});
            if (folders.Count == 0)
                return;
            var path = folders[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path)) {
                _settings.GetLocal().BackupCustomLocation = path;
            }
        }

        private void cbUseDllHookFiles_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            var useHookFiles = cbUseDllHookFiles.IsChecked == true;
            _settings.GetLocal().UseDllHookFiles = useHookFiles;
            if (useHookFiles)
                HookFiles.UpdateHookFiles(_settings.GetLocal().GrimDawnLocation);
            else
                HookFiles.DeleteHookFiles(_settings.GetLocal().GrimDawnLocation);
        }

        private void lbDeleteDllHookFiles_Click(object? sender, RoutedEventArgs e)
        {
            HookFiles.DeleteHookFiles(_settings.GetLocal().GrimDawnLocation);
        }
    }
}
