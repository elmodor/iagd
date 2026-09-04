using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IAGrim.Backup.FileWriter;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities;
using System;
using System.IO;
using System.Linq;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Utilities.HelperClasses;
using IAGrim.Overwrites.LinuxConfig;

namespace IAGrim.UI.Popups.ImportExport.Panels {
    public partial class ExportMode : UserControl {
        private readonly GDTransferFile[] _modSelection;
        private readonly IPlayerItemDao _playerItemDao;
        private readonly Action onClose;
        private string? _filename;
        private bool _isGdstashFormat = false;

        public ExportMode(GDTransferFile[] modSelection, IPlayerItemDao playerItemDao, Action onClose) {
            InitializeComponent();
            this._modSelection = modSelection;
            this._playerItemDao = playerItemDao;
            this.onClose = onClose;

            Loaded += ExportMode_Load;
        }

        private enum FilterType {
            IAS = 1,
            GDS = 2
        }

        private async void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) {
                return;
            }

            var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                    DefaultExtension = "ias",
                    SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(LinuxConfig.DataDirectory),
                    FileTypeChoices = new[] {
                        new FilePickerFileType("IA Stash exports") {Patterns = new[] { "*.ias" }},
                        new FilePickerFileType("GD Stash exports") {Patterns = new[] { "*.gds" }}},
                    Title = "Choose filename for export"
                }
            );

            if (files != null) {
                var filename = files.Path.LocalPath;
                if (!string.IsNullOrEmpty(filename)) {
                    buttonExport.IsEnabled = true;
                    _isGdstashFormat = filename.EndsWith(".gds", StringComparison.OrdinalIgnoreCase);
                    cbItemSelection.IsVisible = _isGdstashFormat;
                    _filename = filename;
                }
            }

            // For IA exports, we can skip the manual export step, since we don't have the list view to worry about.
            if (_filename != null && !cbItemSelection.IsVisible) {
                buttonExport_Click(sender, e);
            }
        }

        private void ExportMode_Load(object? sender, RoutedEventArgs e) {
            cbItemSelection.Items.Add("All items");

            foreach (var mod in _modSelection) {
                cbItemSelection.Items.Add(mod);
            }

            cbItemSelection.SelectedIndex = 0;
            buttonExport.IsEnabled = false;
            cbItemSelection.IsVisible = false;

            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
        }

        private async void buttonExport_Click(object? sender, RoutedEventArgs e) {
            if (buttonExport.IsEnabled && _filename != null) {
                if (_isGdstashFormat) {
                    var io = new GDFileExporter(_filename, string.Empty); // Params are not used for writing

                    GDTransferFile? settings = cbItemSelection.SelectedItem as GDTransferFile;
                    if (settings == null) {
                        var items = _playerItemDao.ListAll();
                        io.Write(items);
                    }
                    else {
                        var items = _playerItemDao.ListAll()
                            .Where(item => item.IsHardcore == settings.IsHardcore/* && item.IsExpansion1 == settings.IsExpansion1*/);

                        if (string.IsNullOrEmpty(settings.Mod)) {
                            io.Write(items.Where(item => string.IsNullOrEmpty(item.Mod)).ToList());
                        }
                        else {
                            io.Write(items.Where(item => item.Mod == settings.Mod).ToList());
                        }
                    }
                }
                else {
                    var io = new IAFileExporter(_filename);
                    var items = _playerItemDao.ListAll();
                    io.Write(items);
                }

                await MessageBox.Show(RuntimeSettings.Language!.GetTag("iatag_ui_exportsuccess"), RuntimeSettings.Language.GetTag("iatag_ui_exportsuccess"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                onClose();
            }
        }
    }
}
