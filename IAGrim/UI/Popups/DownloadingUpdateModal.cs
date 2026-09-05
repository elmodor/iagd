using System;
using Avalonia.Controls;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities;

namespace IAGrim.UI.Popups {
    public partial class DownloadingUpdateModal : Window {
        public ProgressBar ProgressBar => progressBar1;
        public DownloadingUpdateModal() {
            InitializeComponent();
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
        }
    }
}
