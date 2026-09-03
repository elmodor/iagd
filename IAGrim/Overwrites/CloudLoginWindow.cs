using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Platform;
using IAGrim.Backup.Cloud;
using IAGrim.Backup.Cloud.Service;
using IAGrim.Settings;
using IAGrim.Utilities;
using IAGrim.Backup.Cloud.CefSharp.Events;

namespace IAGrim.Overwrites.CloudLogin {
    public partial class CloudLoginWindow : Window {
        private readonly AuthService _authService;
        private readonly SettingsService _settings;
        private readonly NativeWebView _webView;

        public CloudLoginWindow(AuthService authService, SettingsService settings) {
            InitializeComponent();
            _authService = authService;
            _settings = settings;
            _authService.OnAuthCompletion += AuthService_OnAuthCompletion;
            _webView = new NativeWebView();
            _webView.EnvironmentRequested += (_, args) => {
                if (args is LinuxWpeWebViewEnvironmentRequestedEventArgs wpe) {
                    wpe.PreferWebKitGtkInstead = true;
                }
            };
            _webView.NavigationCompleted += (_, args) => {
                if (!args.IsSuccess)
                    Close();
            };
            WebViewHost.Content = _webView;
            Opened += CloudLoginWindow_Opened;
            Closing += CloudLoginWindow_Closing;
        }

        private void CloudLoginWindow_Opened(object? sender, EventArgs e) {
            var pollingId = _authService.Authenticate(true);
            _webView.Navigate(new Uri($"{Uris.LoginPageUrl}?token={pollingId}&embedded=1"));
        }

        private void AuthService_OnAuthCompletion(object? sender, EventArgs e) {
            if (e is not AuthResultEvent args || !args.IsAuthorized) {
                return;
            }

            Dispatcher.UIThread.Post(() => {
                if (!IsVisible) {
                    return;
                }
                Close(true);
            });
        }

        private void CloudLoginWindow_Closing(object? sender, WindowClosingEventArgs e) {
            _authService.OnAuthCompletion -= AuthService_OnAuthCompletion;
            _authService.Dispose();
        }
    }
}


