using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Input;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.UI.Model;
using IAGrim.UI.Service;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using IAGrim.Parsers.Arz;
using IAGrim.Services;
using IAGrim.Settings;
using IAGrim.Utilities;
// using DllInjector;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Overwrites;

namespace IAGrim.UI {
    public partial class ModsDatabaseConfig : UserControl {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ModsDatabaseConfig));

        private readonly Action _itemViewUpdateTrigger;
        private readonly IPlayerItemDao _playerItemDao;
        private readonly ParsingService _parsingService;
        private readonly DatabaseModSelectionService _databaseModSelectionService;

        private readonly GrimDawnDetector _grimDawnDetector;
        private readonly SettingsService _settingsService;
        private readonly IHelpService _helpService;
        private readonly IDatabaseItemDao _databaseItemDao;
        private readonly IReplicaItemDao _replicaItemDao;
        private readonly IComputedItemStatDao _computedItemStatDao;

        public ModsDatabaseConfig(
            Action itemViewUpdateTrigger,
            IPlayerItemDao playerItemDao,
            ParsingService parsingService,
            GrimDawnDetector grimDawnDetector,
            SettingsService settingsService,
            IHelpService helpService, IDatabaseItemDao databaseItemDao, IReplicaItemDao replicaItemDao,
            IComputedItemStatDao computedItemStatDao) {
            InitializeComponent();
            _itemViewUpdateTrigger = itemViewUpdateTrigger;
            _playerItemDao = playerItemDao;
            _parsingService = parsingService;
            _grimDawnDetector = grimDawnDetector;
            _settingsService = settingsService;
            _helpService = helpService;
            _databaseItemDao = databaseItemDao;
            _databaseModSelectionService = new DatabaseModSelectionService();
            _replicaItemDao = replicaItemDao;
            _computedItemStatDao = computedItemStatDao;

            AttachedToVisualTree += ModsDatabaseConfig_AttachedToVisualTree;
        }

        private void UpdateListView(IEnumerable<string> paths) {
            var installs = _databaseModSelectionService.GetGrimDawnInstalls(paths).ToList();
            var mods = _databaseModSelectionService.GetInstalledMods(paths).ToList();

            listViewInstalls.ItemsSource = installs;
            listViewMods.ItemsSource = mods;

            if (listViewInstalls.Items.Count > 0) {
                listViewInstalls.SelectedIndex = 0;
            }

            if (listViewMods.Items.Count > 0) {
                listViewMods.SelectedIndex = 0;
            }

            // Show help linklabel?
            helpFindGrimdawnInstall.IsVisible = listViewInstalls.Items.Count == 0;

            buttonForceUpdate.IsEnabled = listViewInstalls.SelectedItem != null;
        }

        private void ModsDatabaseConfig_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) {
            AttachedToVisualTree -= ModsDatabaseConfig_AttachedToVisualTree;
            ModsDatabaseConfig_Load();
        }

        private void ModsDatabaseConfig_Load() {
            var localSettings = _settingsService.GetLocal();
            wineUserProfilePath.Text = localSettings.GrimDawnWineUserProfilePath;

            var paths = _grimDawnDetector.GetGrimLocations();

            // Ensure that we store all known paths.
            foreach (var path in paths) {
                _settingsService.GetLocal().AddGrimDawnLocation(path);
                if (_settingsService.GetLocal().UseDllHookFiles)
                    HookFiles.UpdateHookFiles(path);
            }

            if (paths.Count == 0) {
                listViewInstalls.IsEnabled = false;
                buttonForceUpdate.IsEnabled = false;
            }
            else {
                UpdateListView(paths);
            }

            buttonForceUpdate.IsEnabled = listViewInstalls.SelectedItem != null;
        }

        /// <summary>
        /// Sets the "last database update" timestamp to 0 to force an update
        /// Queues a database update, followed by an item stat update.
        /// </summary>
        public void ForceDatabaseUpdate(string? location, string? modLocation) {
            var parsed = false;

            if (!string.IsNullOrEmpty(location) && Directory.Exists(location)) {
                _parsingService.Update(location, modLocation ?? string.Empty);
                _parsingService.Execute();
                parsed = true;

                // The tags were just dropped and rebuilt, in whatever language is currently selected.
                // The language has to be reloaded from them before the item names below are generated,
                // or the names come out ordered for the language that was parsed previously.
                _settingsService.GetLocal().ParsedLanguageCode = _settingsService.GetLocal().LanguageCode;
                RuntimeSettings.InitializeLanguage(_settingsService.GetLocal().LanguageCode, _databaseItemDao.GetTagDictionary());
            }
            else {
                Logger.Warn("Could not find the Grim Dawn install location");
            }

            // Update item stats as well
            var updatingPlayerItemsScreen = new UpdatingPlayerItemsScreen(_playerItemDao);
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner != null) {
                updatingPlayerItemsScreen.ShowDialog(owner);
            }
            _itemViewUpdateTrigger?.Invoke();

            // Icons go last. Extraction is memory hungry, so running it alongside the database
            // parse spikes peak memory (out of memory on lower end machines) and slows the parse
            // down. Everything above is modal/blocking, so the parse is complete by this point.
            // A game update can add items whose icons we have never extracted, and the startup
            // icon check is a file-count heuristic that will not notice those.
            if (parsed) {
                ArzParser.QueueIconExtraction(location, modLocation);
            }
        }

        private ListViewEntry? GetSelectedInstall() {
            return listViewInstalls.SelectedItem as ListViewEntry;
        }

        private ListViewEntry? GetSelectedMod() {
            return listViewMods.SelectedItem as ListViewEntry;
        }

        private void ButtonForceUpdate_Click(object? sender, RoutedEventArgs e) {
            // Grim Dawn holds its .arc resources open for the whole session, but only with
            // FILE_SHARE_READ -- they remain readable, so there is no need to block parsing here.
            _databaseItemDao.Clean();

            var isGdParsed2 = _databaseItemDao.GetRowCount() > 0;

            var mod = GetSelectedMod();
            var entry = GetSelectedInstall();

            if (entry == null) {
                Logger.Warn("ForceDatabaseUpdate requested with no install selected, aborting.");
                return;
            }

            // Icons (base game, expansions and the selected mod) are queued by ForceDatabaseUpdate.
            ForceDatabaseUpdate(entry.Path, mod?.Path);
            _settingsService.GetLocal().CurrentGrimdawnLocation = entry.Path ?? string.Empty;

            // Remembered so an automatic re-parse doesn't silently downgrade a modded database to vanilla.
            _settingsService.GetLocal().CurrentGrimdawnMod = mod?.Path ?? string.Empty;

            // Store the loaded GD path, so we can poll it for updates later.
            //_settingsService.GetLocal().GrimDawnLocation = new List<string> { entry.Path }; // TODO: Wtf is this? Why overwrite any existing?
            _settingsService.GetLocal().GrimDawnLocationLastModified = ParsingService.GetHighestTimestamp(entry.Path ?? string.Empty);
            _settingsService.GetLocal().HasWarnedGrimDawnUpdate = false;

            var isGdParsed = _databaseItemDao.GetRowCount() > 0;
            _settingsService.GetLocal().IsGrimDawnParsed = isGdParsed;
            _helpService.SetIsGrimParsed(isGdParsed);
        }

        private void ListViewInstalls_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
            buttonForceUpdate.IsEnabled = listViewInstalls.SelectedItem != null;
        }

        private void ButtonUpdateItemStats_Click(object? sender, RoutedEventArgs e) {
            var updatingPlayerItemsScreen = new UpdatingPlayerItemsScreen(_playerItemDao);
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner != null) {
                updatingPlayerItemsScreen.ShowDialog(owner);
            }

            _replicaItemDao.DeleteAll();
            _computedItemStatDao.DeleteAll();

            _itemViewUpdateTrigger?.Invoke();
        }

        private void HelpFindGrimdawnInstall_Click(object? sender, RoutedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.CannotFindGrimdawn);
        }

        private void ButtonClean_Click(object? sender, RoutedEventArgs e) {
            _databaseItemDao.Clean();
            ButtonUpdateItemStats_Click(sender, e);

            MessageBox.Show(RuntimeSettings.Language!.GetTag("iatag_ui_clean_body"),
                RuntimeSettings.Language.GetTag("iatag_ui_clean_caption"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

            var isGdParsed = _databaseItemDao.GetRowCount() > 0;
            _settingsService.GetLocal().IsGrimDawnParsed = isGdParsed;
            _helpService.SetIsGrimParsed(isGdParsed);
        }

        private async void ButtonConfigure_Click(object? sender, RoutedEventArgs e) {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) {
                Logger.Warn("Could not find TopLevel for folder selection.");
                return;
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {Title = "Select Grim Dawn installation folder", AllowMultiple = false});
            if (folders.Count == 0) {
                return;
            }

            var selectedPath = folders[0].Path.LocalPath;
            if (File.Exists(Path.Combine(selectedPath, "Grim Dawn.exe"))) {
                _settingsService.GetLocal().AddGrimDawnLocation(selectedPath);
                Logger.Info($"Added {selectedPath} to the known Grim Dawn locations");
                ModsDatabaseConfig_Load();
                if (_settingsService.GetLocal().UseDllHookFiles)
                    HookFiles.UpdateHookFiles(selectedPath);
                // TODO: Kill the task that keeps looking for GD.
            }
            else {
                var text = RuntimeSettings.Language!.GetTag("iatag_ui_db_invalidlocation_body");
                var title = RuntimeSettings.Language.GetTag("iatag_ui_db_invalidlocation_title");
                MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void SetWineUserProfilePath(string? path) {
            if (GlobalPaths.HasGrimDawnWineUserProfilePath) {
                if (GlobalPaths.GrimDawnWineUserProfilePath == path)
                    return;
            }
            if (!string.IsNullOrWhiteSpace(path)) {
                try {
                    GlobalPaths.GrimDawnWineUserProfilePath = path;
                    _settingsService.GetLocal().GrimDawnWineUserProfilePath = path;
                    wineUserProfilePath.Text = path;
                    Logger.Info($"Set grim dawn wine user profile path {path}");
                    Dispatcher.UIThread.Post(async () => {
                        var result = await MessageBox.Show(
                            "Changing the prefix path requires an application restart. Restart now?",
                            "Application Restart",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == MessageBoxResult.Yes) {
                            var processPath = Environment.ProcessPath;
                            if (!string.IsNullOrEmpty(processPath)) {
                                Process.Start(new ProcessStartInfo{FileName = processPath, UseShellExecute = true});
                            }
                            Environment.Exit(0);
                        }
                    });
                }
                catch (ArgumentException ex) {
                    if (GlobalPaths.HasGrimDawnWineUserProfilePath)
                        wineUserProfilePath.Text = GlobalPaths.GrimDawnWineUserProfilePath;
                    await Dispatcher.UIThread.InvokeAsync(() => MessageBox.Show(ex.Message, "Invalid Wine user profile", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
            }
        }

        private void WineUserProfilePath_LostFocus(object? sender, RoutedEventArgs e) {
            var path = wineUserProfilePath.Text?.Trim() ?? string.Empty;
            SetWineUserProfilePath(path);
        }

        private void WineUserProfilePath_KeyDown(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                e.Handled = true;
                var path = wineUserProfilePath.Text?.Trim() ?? string.Empty;
                SetWineUserProfilePath(path);
            }
        }

        private async void ButtonConfigureWineUserProfile_Click(object? sender, RoutedEventArgs e) {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) {
                Logger.Warn("Could not find TopLevel for folder selection.");
                return;
            }
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {Title = "Select Grim Dawn Wine user profile folder", AllowMultiple = false});
            if (folders.Count != 1) {
                return;
            }
            var selectedPath = folders[0].Path.LocalPath;
            SetWineUserProfilePath(selectedPath);
        }
    }
}
