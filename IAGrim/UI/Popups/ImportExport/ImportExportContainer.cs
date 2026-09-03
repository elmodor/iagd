using System;
using Avalonia.Controls;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.TransferStash;
using IAGrim.UI.Popups.ImportExport.Panels;
using IAGrim.Utilities.HelperClasses;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities;

namespace IAGrim.UI.Popups.ImportExport {
    partial class ImportExportContainer : Window {
        private readonly GDTransferFile[] _modFilter;
        private readonly IPlayerItemDao _playerItemDao;

        public ImportExportContainer(GDTransferFile[] modFilter, IPlayerItemDao playerItemDao) {
            InitializeComponent();
            this._modFilter = modFilter;
            this._playerItemDao = playerItemDao;
        }

        private void ImportExportContainer_Load(object? sender, EventArgs e) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
            contentPanel.Content = new ImportExportModePicker(
                _modFilter,
                _playerItemDao,
                contentPanel,
                () => this.Close()
            );
        }
    }
}
