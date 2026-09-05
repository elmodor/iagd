using EvilsoftCommons.Exceptions;
using IAGrim.Settings;
using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities;

namespace IAGrim.UI.Popups {
    public partial class UpdateModal : Window {
        private readonly string _version;
        private readonly SettingsService _settingsService;
        public UpdateModal(SettingsService settingsService, string version, bool forceUpdate) {
            InitializeComponent();
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
            _settingsService = settingsService;
            _version = version;
            if (forceUpdate) {
                // Practically warrants its own modal at this point..
                this.label1.Text = "Are you sure you wish to downgrade?";
                this.lnkRemindMeLater.IsVisible = false;
                this.lnkWhatHasChanged.IsVisible = false;
                this.btnUpdateNow.Content = "Downgrade now";
                this.Title = "Downgrade IAGD";
            }
        }

        private void lnkWhatHasChanged_LinkClicked(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo { FileName = $"https://github.com/elmodor/iagd/compare/{ExceptionReporter.VersionString}...{_version}", UseShellExecute = true });
        }

        private void lnkRemindMeLater_LinkClicked(object sender, RoutedEventArgs e) {
            _settingsService.GetPersistent().NextUpdateCheck = DateTime.UtcNow.AddDays(7);
            this.Close();
        }

        private void btnUpdateNow_Click(object sender, RoutedEventArgs e) {
            // And what if it's not a modal? eh?
            this.Close(true);
        }
    }
}
