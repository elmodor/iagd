using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities.HelperClasses;
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using IAGrim.Parsers.TransferStash;
using IAGrim.Utilities;

namespace IAGrim.UI.Popups.ImportExport.Panels {

    partial class ImportExportModePicker : UserControl {
        private readonly ContentControl parentContainer;
        private readonly IPlayerItemDao playerItemDao;
        private readonly GDTransferFile[] modFilter;
        private readonly Action onClose;

        public ImportExportModePicker(
            GDTransferFile[] modFilter,
            IPlayerItemDao playerItemDao,
            ContentControl parentContainer,
            Action onClose
            ) {
            InitializeComponent();
            this.modFilter = modFilter;
            this.playerItemDao = playerItemDao;
            this.parentContainer = parentContainer;
            this.onClose = onClose;
        }

        private void buttonImport_Click(object sender, RoutedEventArgs e) {
            var form = new ImportMode(modFilter, playerItemDao);
            parentContainer.Content = form;
        }

        private void buttonExport_Click(object sender, RoutedEventArgs e) {
            var form = new ExportMode(modFilter, playerItemDao, onClose);
            parentContainer.Content = form;
        }

        private void ImportExportModePicker_Loaded(object sender, RoutedEventArgs e) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
        }
    }
}
