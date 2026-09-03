using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Platform;
using EvilsoftCommons;
using IAGrim.Backup.Cloud.Service;
using IAGrim.Database;
using IAGrim.Database.Interfaces;
using IAGrim.Services;
using IAGrim.Settings;
using IAGrim.UI.Popups;
using IAGrim.Utilities;
using log4net;
using System;
using System.Collections.ObjectModel;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Overwrites.CloudLogin;

namespace IAGrim.UI.Tabs {
    public sealed class BuddyListItem {
        public long Id { get; init; }
        public string Buddy { get; init; } = "";
        public string Items { get; init; } = "";
        public string Visible { get; init; } = "";
    }

    public partial class OnlineSettings : UserControl {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OnlineSettings));
        private readonly IPlayerItemDao _playerItemDao;
        private readonly SettingsService _settings;
        private readonly IHelpService _helpService;
        private readonly IBuddyItemDao _buddyItemDao;
        private readonly IBuddySubscriptionDao _buddySubscriptionDao;
        private readonly ObservableCollection<BuddyListItem> _buddyItems = new();

        public OnlineSettings(IPlayerItemDao playerItemDao, SettingsService settings, IHelpService helpService, IBuddyItemDao buddyItemDao, IBuddySubscriptionDao buddySubscriptionDao) {
            InitializeComponent();
            _playerItemDao = playerItemDao;
            _settings = settings;
            _helpService = helpService;
            _buddyItemDao = buddyItemDao;
            _buddySubscriptionDao = buddySubscriptionDao;
            buddyList.ItemsSource = _buddyItems;
            Loaded += BackupSettings_Load;
        }

        public void UpdateUi() {
            if (!Dispatcher.UIThread.CheckAccess()) {
                Dispatcher.UIThread.Post(UpdateUi);
                return;
            }
            var authService = new AuthService(new AuthenticationProvider(_settings), _playerItemDao);

            var status = authService?.CheckAuthentication();
            if (status == AuthService.AccessStatus.Authorized) {
                labelStatus.Text = RuntimeSettings.Language!.GetTag("iatag_ui_backup_loggedinas", _settings.GetPersistent().CloudUser ?? string.Empty);
                buttonLogin.IsEnabled = false;
                _settings.GetLocal().OptOutOfBackups = false;
            }
            else if (status == AuthService.AccessStatus.Unknown) {
                labelStatus.Text = RuntimeSettings.Language!.GetTag("iatag_ui_backup_statusunknown");
                buttonLogin.IsEnabled = false;
            }
            else {
                labelStatus.Text = RuntimeSettings.Language!.GetTag("iatag_ui_backup_notloggedin");
                buttonLogin.IsEnabled = true;
            }

            linkLogout.IsEnabled = !buttonLogin.IsEnabled;
            linkDeleteBackup.IsEnabled = !buttonLogin.IsEnabled;
            cbDontWantBackups.IsVisible = buttonLogin.IsEnabled; // Hide "I don't want backups" if already logged in.
            groupBoxBackupDetails.IsVisible = cbDontWantBackups.IsChecked != true; // No point displaying info if user has opted for zero features
            pbBuddyItems.IsVisible = cbDontWantBackups.IsChecked != true;
            btnAddBuddy.IsEnabled = !buttonLogin.IsEnabled;
            btnModifyBuddy.IsEnabled = !buttonLogin.IsEnabled;
            linkViewCharacters.IsEnabled = !buttonLogin.IsEnabled;
            if (buddyList.IsEnabled) UpdateBuddyList();

            var buddyId = _settings.GetPersistent().BuddySyncUserIdV3;
            if (buddyId.HasValue && buddyId > 0)
                labelBuddyId.Content = buddyId.ToString();
            else
                labelBuddyId.Content = "-";
        }

        private void BackupSettings_Load(object? sender, RoutedEventArgs e) {
            cbDontWantBackups.IsChecked = _settings.GetLocal().OptOutOfBackups;
            buttonLogin.IsEnabled = !_settings.GetLocal().OptOutOfBackups;

            UpdateUi();
        }

        private async void firefoxButton1_Click(object? sender, RoutedEventArgs e) {
            // Don't want backups? Don't try to login.
            if (cbDontWantBackups.IsChecked == true) {
                return;
            }

            var authService = new AuthService(new AuthenticationProvider(_settings), _playerItemDao);
            if (buttonLogin.IsEnabled) {
                var access = authService.CheckAuthentication();

                switch (access) {
                    case AuthService.AccessStatus.Unauthorized:
                        Logger.Debug($"Login, state {access}, authenticating..");
                        var owner = TopLevel.GetTopLevel(this) as Window;
                        if (owner != null) {
                            var loginWindow = new CloudLoginWindow(authService, _settings);
                            await loginWindow.ShowDialog(owner);
                            UpdateUi();
                        }

                        break;
                    case AuthService.AccessStatus.Unknown:
                        Logger.Debug($"Login, state {access}, displaying error..");
                        await MessageBox.Show(RuntimeSettings.Language!.GetTag("iatag_ui_backup_service_error"));
                        break;
                    default: {
                        Logger.Debug($"Login, state {access}, displaying already logged in..");
                        var alreadyLoggedIn = RuntimeSettings.Language!.GetTag("iatag_feedback_already_logged_in");

                        await MessageBox.Show(
                            alreadyLoggedIn,
                            alreadyLoggedIn,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        break;
                    }
                }
            }
            Logger.Debug("Cancelling login operation, button is cancelled.");

            UpdateUi();
        }

        private void cbDontWantBackups_CheckedChanged(object? sender, RoutedEventArgs e) {
            var dontWantBackups = cbDontWantBackups.IsChecked == true;
            buttonLogin.IsEnabled = !dontWantBackups;
            _settings.GetLocal().OptOutOfBackups = dontWantBackups;
            UpdateUi();
        }

        private async void linkDeleteBackup_LinkClicked(object? sender, RoutedEventArgs e) {
            var authService = new AuthService(new AuthenticationProvider(_settings), _playerItemDao);
            var caption = RuntimeSettings.Language!.GetTag("iatag_ui_backup_deleteaccount_header");
            var content = RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_body");
            if (await MessageBox.Show(
                content,
                caption,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
                ) == MessageBoxResult.Yes) {
                try {
                    var restService = authService.GetRestService();
                    var cloudSyncService = new CloudSyncService(authService.GetRestService()!);
                    if (restService != null && cloudSyncService.DeleteAccount()) {
                        await MessageBox.Show(
                            RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_success_body"),
                            RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_success_header"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        _settings.GetPersistent().CloudUploadTimestamp = 0;
                        authService.UnAuthenticate();
                        _playerItemDao.ResetOnlineSyncState();
                        _settings.GetPersistent().BuddySyncUserIdV3 = null;
                    }
                    else {
                        await MessageBox.Show(
                            RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_failure_body"),
                            RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_failure_header"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
                catch (Exception ex) {
                    Logger.Warn("Error deleting account", ex);

                    await MessageBox.Show(
                        RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_failure_body"),
                        RuntimeSettings.Language.GetTag("iatag_ui_backup_deleteaccount_failure_header"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }

            UpdateUi();
        }

        private async void linkLabel1_LinkClicked(object? sender, RoutedEventArgs e) {
            var authService = new AuthService(new AuthenticationProvider(_settings), _playerItemDao);
            Logger.Info("Logging out of online backups.");
            _settings.GetPersistent().BuddySyncUserIdV3 = null;
            authService.Logout();
            _settings.GetPersistent().CloudUploadTimestamp = 0;
            _playerItemDao.ResetOnlineSyncState();
            _buddyItemDao.Delete();
            await MessageBox.Show(RuntimeSettings.Language!.GetTag("iatag_ui_backup_logout_successful_body"), RuntimeSettings.Language.GetTag("iatag_ui_backup_logout_successful_header"));

            UpdateUi();
        }

        private void btnRefreshBackupDetails_Click(object? sender, RoutedEventArgs e) {
            UpdateUi();
        }

        /// <summary>
        /// Update the list of buddies
        /// </summary>
        public void UpdateBuddyList() {
            if (!Dispatcher.UIThread.CheckAccess()) {
                Dispatcher.UIThread.Post(UpdateBuddyList);
                return;
            }

            _buddyItems.Clear();

            var visible = RuntimeSettings.Language!.GetTag("iatag_ui_buddy_column_visible");
            var hidden = RuntimeSettings.Language.GetTag("iatag_ui_buddy_column_hidden");
            var subscriptions = _buddySubscriptionDao.ListAll();
            foreach (var subscription in subscriptions) {
                var label = subscription.Id.ToString();
                var stash = subscription.Nickname;

                if (stash != null) {
                    label = $"[{label}] {stash}";
                }

                var numItems = _buddyItemDao.GetNumItems(subscription.Id);

                _buddyItems.Add(new BuddyListItem {
                        Id = subscription.Id,
                        Buddy = label,
                        Items = numItems.ToString(),
                        Visible = subscription.IsHidden ? hidden : visible
                });
            }
        }

        private void helpWhatIsThis_LinkClicked(object? sender, RoutedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.BuddyItems);
        }

        private void linkLabel1_LinkClicked_1(object? sender, RoutedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.OnlineBackups);
        }

        private async void btnAddBuddy_Click(object? sender, RoutedEventArgs e) {
            var authService = new AuthService(new AuthenticationProvider(_settings), _playerItemDao);
            if (btnAddBuddy.IsEnabled) {
                var diag = new AddEditBuddy(_helpService, authService.GetRestService()!);
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner != null && await diag.ShowDialog<bool>(owner)) {
                    bool isMyself = diag.BuddyId == _settings.GetPersistent().BuddySyncUserIdV3;
                    if (diag.BuddyId > 0 && !isMyself) {
                        _buddySubscriptionDao.SaveOrUpdate(new BuddySubscription {Id = diag.BuddyId, Nickname = diag.Nickname});
                    }

                    UpdateBuddyList();
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object? sender, RoutedEventArgs e) {
            if (buddyList.SelectedItem is BuddyListItem item) {
                _buddyItemDao.RemoveBuddy(item.Id);
                UpdateBuddyList();
            }
        }

        private async void editToolStripMenuItem_Click(object? sender, RoutedEventArgs e) {
            var authService = new AuthService(new AuthenticationProvider(_settings), _playerItemDao);
            if (buddyList.SelectedItem is BuddyListItem item) {
                var diag = new AddEditBuddy(_helpService, authService.GetRestService()!) {BuddyId = item.Id};
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner != null && await diag.ShowDialog<bool>(owner)) {
                    var entry = _buddySubscriptionDao.GetById(diag.BuddyId);
                    entry.Nickname = diag.Nickname;
                    _buddySubscriptionDao.Update(entry);
                }

                UpdateBuddyList();
            }
        }

        private async void labelBuddyId_Click(object? sender, RoutedEventArgs e) {
            var buddySyncUserId = _settings.GetPersistent().BuddySyncUserIdV3;
            if (buddySyncUserId.HasValue && buddySyncUserId > 0) {
                var topLevel = TopLevel.GetTopLevel(this);
                var clipboard = topLevel?.Clipboard;
                if (clipboard != null) {
                    var text = buddySyncUserId.Value.ToString();
                    var data = new DataTransfer();
                    data.Add(DataTransferItem.CreateText(text));
                    await clipboard.SetDataAsync(data);
                    TooltipHelper.ShowTooltipForControl(RuntimeSettings.Language!.GetTag("iatag_ui_copiedclipboard"), labelBuddyId);
                }
            }
        }

        private void btnModifyBuddy_Click(object? sender, RoutedEventArgs e) {
            if (btnModifyBuddy.IsEnabled) {
                editToolStripMenuItem_Click(sender, e);
            }
        }

        private void btnDeleteBuddy_Click(object? sender, RoutedEventArgs e) {
            deleteToolStripMenuItem_Click(sender, e);
        }

        private void linkViewCharacters_LinkClicked(object? sender, RoutedEventArgs e) {
            _helpService.ShowCharacterBackups();
        }

        private void btnToggleBuddyVisibility_Click(object? sender, RoutedEventArgs e) {
            if (buddyList.SelectedItem is BuddyListItem item) {
                var entry = _buddySubscriptionDao.GetById(item.Id);
                entry.IsHidden = !entry.IsHidden;
                _buddySubscriptionDao.Update(entry);
            }
            UpdateBuddyList();
        }

        private void showToolStripMenuItem_Click(object? sender, RoutedEventArgs e) {
            if (buddyList.SelectedItem is BuddyListItem item) {
                var entry = _buddySubscriptionDao.GetById(item.Id);
                entry.IsHidden = false;
                _buddySubscriptionDao.Update(entry);
            }
            UpdateBuddyList();
        }

        private void hideToolStripMenuItem_Click(object? sender, RoutedEventArgs e) {
            if (buddyList.SelectedItem is BuddyListItem item) {
                var entry = _buddySubscriptionDao.GetById(item.Id);
                entry.IsHidden = true;
                _buddySubscriptionDao.Update(entry);
            }
            UpdateBuddyList();
        }
    }
}
