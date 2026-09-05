using EvilsoftCommons.Exceptions;
using IAGrim.Settings;
using IAGrim.UI.Popups;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Timer = System.Timers.Timer;
using IAGrim.Overwrites.MessageBox;
using Avalonia.Controls;

namespace IAGrim.Utilities {
    /// <summary>
    /// Flows:
    /// Auto => You have an update [only if IA has focus? check on get focus?]. Not modal/blocking
    /// Manual => You have an update[modal/blocking]
    /// Manual => No update available (popup)
    ///
    /// Download progress bar view
    /// </summary>
    class AutomaticUpdateChecker {
        private Timer? _timer;
        private DateTime _lastTimeNotMinimized = DateTime.UtcNow;
        private string _downloadUri = string.Empty;
        private string _installerPath = string.Empty;
        private readonly SettingsService _settings;
        private DownloadingUpdateModal? _progressModal = null;
        private readonly Window _owner;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AutomaticUpdateChecker));

        private readonly DateTime _startupTime = DateTime.UtcNow;

        private const string Url = "https://api.github.com/repos/elmodor/iagd/releases/latest";

        private class GitHubAsset {
            [JsonProperty("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }
        }


        private class GitHubRelease {
            [JsonProperty("tag_name")]
            public string? TagName { get; set; }

            [JsonProperty("assets")]
            public List<GitHubAsset>? Assets { get; set; }
        }

        public AutomaticUpdateChecker(SettingsService settings, Window owner) {
            _settings = settings;
            _owner = owner;
            int min = 1000 * 60;
            int hour = 60 * min;
            _timer = new Timer();
            _timer.Start();
            _timer.Elapsed += (a1, a2) => {
                if (Thread.CurrentThread.Name == null) {
                    Thread.CurrentThread.Name = "CheckUpdatesThread";
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                }

                bool hasBeenMinimizedRecently = (DateTime.UtcNow - _lastTimeNotMinimized).TotalHours < 38;
                if ((DateTime.UtcNow - _startupTime).TotalMinutes > 5 && hasBeenMinimizedRecently && ShouldCheckForUpdates()) {
                    CheckForUpdates();
                    int checkIntervalDays = _settings.GetPersistent().CheckUpdatesDaily ? 1 : 7;
                    _settings.GetPersistent().NextUpdateCheck = DateTime.UtcNow.AddDays(checkIntervalDays);
                }
            };
            _timer.Interval = 12 * hour;
            _timer.AutoReset = true;
            _timer.Start();
        }

        public void ResetLastMinimized() {
            _lastTimeNotMinimized = DateTime.UtcNow;
        }

        public bool ShouldCheckForUpdates() {
            return _settings.GetPersistent().NextUpdateCheck < DateTime.UtcNow;
        }

        public void CheckForUpdates(bool manualUpdate = false) {
            CheckForUpdates(Url, false, manualUpdate);
        }

        private async void CheckForUpdates(string uri, bool forceUpdate, bool userInitiated) {
            try {
                using WebClient client = new WebClient();
                client.Headers.Add("User-Agent", "IAGrim");
                var jsonContent = client.DownloadString(uri);

                var release = JsonConvert.DeserializeObject<GitHubRelease>(jsonContent);
                if (release == null) {
                    if (userInitiated) {
                        _ = MessageBox.Show("Something went wrong checking for updates", "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    Logger.Warn("Could not parse JSON in version check");
                    return;
                }

                string? version = release.TagName;
                _downloadUri = release.Assets?.Count > 0 ? release.Assets[0].BrowserDownloadUrl ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(_downloadUri)) {
                    if (userInitiated) {
                        _ = MessageBox.Show("Something went wrong checking for updates", "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    Logger.Warn("Could not check version");
                    return;
                }

                // Numeric compare: the tag may be padded or unpadded ("1.5.9707.09210" / "1.5.9707.9210"), and a
                // string compare on a variable-width revision would rank "9500" above "11000" -- which offered
                // users a downgrade as an update.
                if (VersionUtility.IsNewerThan(version, ExceptionReporter.VersionString) || forceUpdate) {
                    Logger.Info($"Latest version is {version}, local version is {ExceptionReporter.VersionString}, update available");
                    if (await new UpdateModal(_settings, version, forceUpdate).ShowDialog<bool?>(_owner) == true) {
                        _progressModal = new DownloadingUpdateModal();
                        _progressModal.Show(_owner);
                        await Download(version);
                    } else {
                        Logger.Info("User was made aware of a new update, chose not to update.");
                    }
                } else if(userInitiated) {
                    _ = MessageBox.Show("You are on the latest version", "No new updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logger.Info("Is latest version.");
                }
            }
            catch (Exception ex) {
                Logger.Warn(ex);
                if (userInitiated) {
                    _ = MessageBox.Show("Something went wrong checking for updates", "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logger.Warn("Could not check version");
                }
            }
        }

        private async Task Download(string version) {
            var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
            if (string.IsNullOrEmpty(appImagePath)) {
                _progressModal?.Close();
                _progressModal = null;
                var result = await MessageBox.Show("The application was not started as an AppImage. Please download the update manually at\nhttps://github.com/elmodor/iagd/releases\n\nWould you like to open the download page?", "Manual Update",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information );
                if (result == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo{FileName = "https://github.com/elmodor/iagd/releases", UseShellExecute = true});
                return;
            }

            var downloadPath = Path.Combine(Path.GetTempPath(), $"IAGrim-{version}.AppImage");
            var checksumPath = Path.Combine(Path.GetTempPath(), $"IAGrim-{version}.AppImage.sha256");
            var appImageUrl = $"https://github.com/elmodor/iagd/releases/download/{version}/IAGrim-x86_64.AppImage";
            var checksumUrl = $"https://github.com/elmodor/iagd/releases/download/{version}/IAGrim-x86_64.AppImage.sha256";

            Logger.Info($"Downloading new update to {downloadPath}");
            WebClient client = new WebClient();
            _progressModal.Closing += (_, __) => {
                client?.CancelAsync();
            };
            try {
                await client.DownloadFileTaskAsync(new Uri(checksumUrl), checksumPath);
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                await client.DownloadFileTaskAsync(new Uri(appImageUrl), downloadPath);
                Client_DownloadFileCompleted(downloadPath, checksumPath);
            }
            catch (Exception ex) {
                Logger.Error("Failed to download update.", ex);
                if (File.Exists(downloadPath))
                    File.Delete(downloadPath);
                if (File.Exists(checksumPath))
                    File.Delete(checksumPath);
                var result = await MessageBox.Show("Automatic Update failed. Please download the update manually at\nhttps://github.com/elmodor/iagd/releases\n\nWould you like to open the download page?", "Manual Update",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error );
                if (result == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo{FileName = "https://github.com/elmodor/iagd/releases", UseShellExecute = true});
            }
            finally {
                client.Dispose();
                _progressModal?.Close();
                _progressModal = null;
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            if (_progressModal == null) {
                return;
            }

            _progressModal.ProgressBar.Value = e.ProgressPercentage;
        }

        private void Client_DownloadFileCompleted(string downloadPath, string checksumPath) {
            var expectedHash = File.ReadAllText(checksumPath).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            using var stream = File.OpenRead(downloadPath);
            using var sha256 = SHA256.Create();
            var actualHash = Convert.ToHexString(sha256.ComputeHash(stream));
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase)) {
                throw new Exception($"SHA256 verification failed. Expected {expectedHash}, got {actualHash}.");
            }

            Logger.Info("Downloaded update successfully verified.");
            var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
            if (string.IsNullOrEmpty(appImagePath) || !File.Exists(appImagePath)) {
                throw new Exception("Not an appimage anymore?");
            }

            var newAppImagePath = appImagePath + ".new";
            File.Copy(downloadPath, newAppImagePath, true);
            var mode = File.GetUnixFileMode(appImagePath);
            File.SetUnixFileMode(newAppImagePath, mode);
            File.Move(newAppImagePath, appImagePath, true);
            File.Delete(checksumPath);
            File.Delete(downloadPath);
            Logger.Info("Update download complete, initating restart");
            Process.Start(new ProcessStartInfo{FileName = appImagePath, UseShellExecute = true});
            Environment.Exit(0);
        }

        public void Dispose() {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
