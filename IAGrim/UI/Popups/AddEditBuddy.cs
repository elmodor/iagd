using System;
using System.Net;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using IAGrim.Backup.Cloud;
using IAGrim.Parsers.Arz;
using IAGrim.Services;
using IAGrim.Utilities;
using IAGrim.Utilities.HelperClasses;

namespace IAGrim.UI.Popups {
    public partial class AddEditBuddy : Window {
        private readonly IHelpService _helpService;
        private readonly RestService _restService;

        public long BuddyId {
            get {
                var t = tbBuddyId.Text;
                if (t.Length == 6) {
                    if (long.TryParse(t, out var r)) {
                        return r;
                    }
                }

                return -1;
            }
            set => tbBuddyId.Text = value.ToString();
        }

        public string Nickname => tbBuddyNickname.Text;

        public AddEditBuddy(IHelpService helpService, RestService restService) {
            _helpService = helpService;
            _restService = restService;
            InitializeComponent();

            Opened += AddEditBuddy_Load;
        }

        private void AddEditBuddy_Load(object? sender, EventArgs e) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
            tbBuddyId.KeyDown += buddyId_KeyPress;
            tbBuddyNickname.KeyDown += nickname_KeyPress;

            if (!string.IsNullOrEmpty(tbBuddyId.Text)) {
                tbBuddyId.IsEnabled = false;
                tbBuddyNickname.Focus();
            }
            else {
                tbBuddyId.Focus();
            }
        }

        private void lbHelpWhatisBuddyId_LinkClicked(object? sender, RoutedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.WhatIsBuddyId);
        }

        private void lbHelpWhatisBuddyNickname_LinkClicked(object? sender, RoutedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.WhatIsBuddyNickname);
        }

        private void buttonAdd_Click(object? sender, RoutedEventArgs e) {
            if ((tbBuddyId.Text?.Length ?? 0) != 6) {
                SetBuddyIdError(RuntimeSettings.Language!.GetTag("iatag_ui_buddy_userid_numeric_error_message"));
            }
            else if (string.IsNullOrEmpty(tbBuddyNickname.Text)) {
                SetBuddyNicknameError(RuntimeSettings.Language!.GetTag("iatag_ui_buddy_nickname_error_message"));
            }

            else {
                if (Verify(tbBuddyId.Text!)) {
                    Close(true);
                } else {
                    SetBuddyIdError(RuntimeSettings.Language!.GetTag("iatag_ui_buddy_userid_doesnotexist_error_message"));
                }
            }
        }

        void buddyId_KeyPress(object? sender, KeyEventArgs e) {
            // Avalonia textbox already enforces max length
            if (e.Key == Key.Enter) {
                if ((tbBuddyId.Text?.Length ?? 0) != 6) {
                    SetBuddyIdError(RuntimeSettings.Language!.GetTag("iatag_ui_buddy_userid_numeric_error_message"));
                }
                else {
                    // Verify if ID exists
                    if (Verify(tbBuddyId.Text!)) {
                        tbBuddyNickname.Focus();
                    }
                    else {
                        SetBuddyIdError(RuntimeSettings.Language!.GetTag("iatag_ui_buddy_userid_doesnotexist_error_message"));
                    }
                }
                e.Handled = true;
            }
        }

        void nickname_KeyPress(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if ((tbBuddyNickname.Text?.Length ?? 0) >= 1) {
                    buttonAdd_Click(sender, e);
                }
                else {
                    SetBuddyNicknameError(RuntimeSettings.Language!.GetTag("iatag_ui_buddy_nickname_error_message"));
                }
                e.Handled = true;
            }
        }

        bool Verify(string buddyId) {
            var status = _restService.VerifyGet($"{Uris.BuddyItemsUrl}?id={buddyId}&ts=900000000000");
            return status == HttpStatusCode.OK;
        }

        private void SetBuddyIdError(string message) {
            BuddyIdError.Text = message;
            BuddyIdError.IsVisible = true;
            tbBuddyId.Focus();
        }

        private void SetBuddyNicknameError(string message) {
            BuddyNicknameError.Text = message;
            BuddyNicknameError.IsVisible = true;
            tbBuddyNickname.Focus();
        }

        private void ClearErrors() {
            BuddyIdError.IsVisible = false;
            BuddyNicknameError.IsVisible = false;
        }
    }
}
