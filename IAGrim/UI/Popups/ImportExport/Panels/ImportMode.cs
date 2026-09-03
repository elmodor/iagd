using EvilsoftCommons.Exceptions;
using IAGrim.Backup;
using IAGrim.Backup.FileWriter;
using IAGrim.Database;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Parsers.TransferStash;
using IAGrim.Services;
using IAGrim.Utilities;
using IAGrim.Utilities.HelperClasses;
using log4net;
using System.IO.Compression;
using System.Text;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Overwrites.LinuxConfig;

namespace IAGrim.UI.Popups.ImportExport.Panels {
    partial class ImportMode : UserControl {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ImportMode));
        private readonly GDTransferFile[] _modSelection;
        private readonly IPlayerItemDao _playerItemDao;
        private string? _filename;
        private volatile bool isLocked = false;

        public ImportMode(GDTransferFile[] modSelection, IPlayerItemDao playerItemDao) {
            InitializeComponent();
            this._modSelection = modSelection;
            this._playerItemDao = playerItemDao;
        }

        private void ImportMode_Loaded(object sender, RoutedEventArgs e) {
            this.buttonImport.IsEnabled = false;
            cbItemSelection.IsVisible = false;
            cbItemSelection.IsEnabled = false;

            foreach (var item in _modSelection) {
                cbItemSelection.Items.Add(item);
            }

            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);

            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null) {
                window.Closing += Form1_FormClosing;
            }
        }

        private void Form1_FormClosing(object? sender, WindowClosingEventArgs e) {
            e.Cancel = isLocked;
        }

        private void radioIAStash_CheckedChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
            if (e.Property == RadioButton.IsCheckedProperty && radioIAStash.IsChecked == true) {
                cbItemSelection.IsVisible = false;
            }
        }

        private void radioGDStash_CheckedChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
            if (e.Property == RadioButton.IsCheckedProperty && radioGDStash.IsChecked == true) {
                cbItemSelection.IsVisible = true;
            }
        }

        private async void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            if (buttonBrowse.IsEnabled) {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                    return;

                var options = new FilePickerOpenOptions {
                    AllowMultiple = false,
                    Title = RuntimeSettings.Language!.GetTag("iatag_ui_importexport_selectfile"),
                    SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(LinuxConfig.DataDirectory)
                };

                if (radioGDStash.IsChecked == true) {
                    options.FileTypeFilter = new[] {
                        new FilePickerFileType("GD Stash exports (*.gds)") {Patterns = new[] { "*.gds" }}
                    };
                }
                else {
                    options.FileTypeFilter = new[] {
                        new FilePickerFileType("IA Stash exports (*.ias)") {Patterns = new[] { "*.ias", "*.zip" }}
                    };
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

                if (files.Count > 0) {
                    var filename = files[0].TryGetLocalPath();
                    if (filename == null)
                        return;

                    if (IsValid(filename)) {
                        radioGDStash.IsEnabled = false;
                        radioIAStash.IsEnabled = false;
                        buttonImport.IsEnabled = true;
                        cbItemSelection.IsEnabled = true;
                        this._filename = filename;
                    }
                    else {
                        await MessageBox.Show(
                            RuntimeSettings.Language.GetTag("iatag_ui_importexport_nothinginzip_body"),
                            RuntimeSettings.Language.GetTag("iatag_ui_importexport_nothinginzip_title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            }
        }

        private static bool IsValid(string filename) {
            // Attempt to read ias/gds from zip file
            if (filename.ToLowerInvariant().EndsWith(".zip")) {
                using var zip = ZipFile.Open(filename, ZipArchiveMode.Read);
                return zip.Entries.Any(fn => fn.Name.EndsWith(".ias") || fn.Name.EndsWith(".gds"));
            }

            // Regular ias/gds file
            return filename.EndsWith(".ias") || filename.EndsWith(".gds");
        }

        private static byte[] Read(string filename) {
            // Attempt to read ias/gds from zip file
            if (filename.ToLowerInvariant().EndsWith(".zip")) {
                using var zip = ZipFile.Open(filename, ZipArchiveMode.Read);
                var candidates = zip.Entries.Where(fn => fn.Name.EndsWith(".ias") || fn.Name.EndsWith(".gds")).ToList();
                foreach (var candidate in candidates) {
                    using var ms = new MemoryStream();
                    using var x = candidate.Open();
                    x.CopyTo(ms);
                    return ms.GetBuffer();
                }
            }

            // Regular ias/gds file
            return File.ReadAllBytes(filename);
        }

        private void buttonImport_Click(object sender, RoutedEventArgs e) {
            if (buttonImport.IsEnabled && _filename != null) {
                FileExporter io;

                if (radioIAStash.IsChecked == true) {
                    io = new IAFileExporter(_filename);
                }
                else {
                    GDTransferFile? settings = cbItemSelection.SelectedItem as GDTransferFile;
                    io = new GDFileExporter(_filename, settings?.Mod ?? string.Empty);
                }

                var items = io.Read(Read(_filename));
                Logger.Debug($"Storing {items.Count} items to db");
                progressBar1.Maximum = items.Count;
                progressBar1.Value = 0;
                buttonImport.IsEnabled = false;

                Thread t = new Thread(() => {
                    ExceptionReporter.EnableLogUnhandledOnThread();
                    isLocked = true;

                    var batches = BatchUtil.ToBatches<PlayerItem>(items);
                    foreach (var batch in batches) {
                        _playerItemDao.Import(batch);
                        Dispatcher.UIThread.Post(() => progressBar1.Value += batch.Count);
                    }

                    isLocked = false;

                    Dispatcher.UIThread.Post(async () => {
                        var result = await MessageBox.Show(
                            RuntimeSettings.Language!.GetTag("iatag_ui_importexport_import_success_body"),
                            RuntimeSettings.Language.GetTag("iatag_ui_importexport_import_success"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        if (result == MessageBoxResult.OK) {
                            var processPath = Environment.ProcessPath;
                            if (!string.IsNullOrEmpty(processPath)) {
                                Process.Start(new ProcessStartInfo{FileName = processPath, UseShellExecute = true});
                            }
                            Environment.Exit(0);
                        }
                    });
                });

                t.Start();
            }
        }

        private void helpRestoreBackup_Click(object sender, RoutedEventArgs e) {
            new HelpService().ShowHelp(IHelpService.HelpType.RestoreBackup); // TODO: Move into UI?
        }
    }
}
