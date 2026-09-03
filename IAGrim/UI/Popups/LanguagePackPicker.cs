using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Settings;
using IAGrim.Utilities;
using log4net;
using StatTranslator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Parsers.Arz;

namespace IAGrim.UI {
    public partial class LanguagePackPicker : Window {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LanguagePackPicker));

        private IEnumerable<string>? _paths;
        private readonly List<RadioButton> _checkboxes = new();

        private readonly IItemTagDao _itemTagDao;
        private readonly IPlayerItemDao _playerItemDao;
        private readonly ParsingService _parsingService;
        private readonly SettingsService _settings;

        public LanguagePackPicker(
            IItemTagDao itemTagDao,
            IPlayerItemDao playerItemDao,
            ParsingService parsingService,
            SettingsService settings
        ) {
            InitializeComponent();

            _parsingService = parsingService;
            _settings = settings;
            _itemTagDao = itemTagDao;
            _playerItemDao = playerItemDao;
        }

        public async Task Show(IEnumerable<string> paths, Window owner) {
            _paths = paths;
            await ShowDialog(owner);
        }

        private void LanguagePackPicker_Loaded(object? sender, RoutedEventArgs e) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);

            var currentCode = _settings.GetLocal().LanguageCode;

            var availableCodes = LanguageMapping
                .GetAvailableLanguages(_paths ?? Enumerable.Empty<string>())
                .ToList();

            // Always show English first
            if (!availableCodes.Contains("EN")) {
                availableCodes.Insert(0, "EN");
            }

            _checkboxes.Clear();

            foreach (var code in availableCodes) {
                var displayName = LanguageMapping.GetDisplayName(code);

#if DEBUG
                displayName += $" ({code})";
#endif

                var isFullySupported =
                    code.Equals("EN", StringComparison.OrdinalIgnoreCase) ||
                    LanguageMapping.IsFullySupported(code);

                var prefix = isFullySupported ? "" : "[Partial] ";

                var cb = new RadioButton {
                    Content = prefix + displayName,
                    Tag = code,
                    IsChecked = code.Equals(
                        currentCode,
                        StringComparison.OrdinalIgnoreCase)
                };

                languagePanel.Children.Add(cb);
                _checkboxes.Add(cb);
            }
        }

        private void buttonSelect_Click(object? sender, RoutedEventArgs e) {
            var cb = _checkboxes.FirstOrDefault(m => m.IsChecked == true);

            if (cb != null) {
                var selectedCode = cb.Tag?.ToString() ?? string.Empty;

                if (selectedCode != _settings.GetLocal().LanguageCode) {
                    Logger.Info($"Switching language to {selectedCode}");

                    _settings.GetLocal().LanguageCode = selectedCode;

                    MessageBox.Show("IAGD is restarting to apply language change", "Restarting");

                    var processPath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(processPath)) {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {FileName = processPath, UseShellExecute = true});
                    }
                    Environment.Exit(0);
                }
            }

            Close();
        }

        private void LanguagePackPicker_KeyDown(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                buttonSelect_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape) {
                Close();
                e.Handled = true;
            }
        }
    }
}
