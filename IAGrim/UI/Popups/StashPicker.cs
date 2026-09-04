using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using System.IO;
using IAGrim.Database.Dto;
using IAGrim.Database.Interfaces;
using IAGrim.Services;
using IAGrim.Settings;
using IAGrim.Utilities;
using IAGrim.Utilities.HelperClasses;
using IAGrim.Parsers.Arz;

namespace IAGrim.UI {
    public partial class StashPicker : Window {
        private readonly IHelpService _helpService;
        private readonly IPlayerItemDao _playerItemDao;
        private readonly SettingsService _settings;
        public StashPicker(IHelpService helpService, IPlayerItemDao playerItemDao, SettingsService settings) {
            _helpService = helpService;
            _playerItemDao = playerItemDao;
            _settings = settings;
            InitializeComponent();
            Opened += StashPicker_Load;
        }

        private void StashPicker_Load(object? sender, EventArgs e) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "static", "chest.png");
            ChestImage.Source = new Bitmap(path);

            var target = _settings.GetLocal().LastSelectedTargetMod;
            var isHardcore = _settings.GetLocal().LastSelectedTargetModIsHc;
            foreach (var mod in _playerItemDao.GetModSelection()) {
                var cb = new RadioButton {
                    Content = mod.Mod + " (" + (mod.IsHardcore ? "hc" : "sc") + ")",
                    Tag = mod,
                    IsChecked = mod.Mod == target && mod.IsHardcore == isHardcore,
                    GroupName = "ModSelection"
                };

                cb.TabIndex = ModSelectionPanel.Children.Count;
                cb.IsTabStop = true;

                ModSelectionPanel.Children.Add(cb);
            }
        }

        public StashPickerResult? Result { get; private set; }

        private void StashPicker_KeyDown(object? sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                buttonTransfer_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private async void buttonTransfer_Click(object? sender, RoutedEventArgs e) {
            foreach (var c in ModSelectionPanel.Children) {
                var cb = c as RadioButton;
                if (cb is { IsChecked: true }) {
                    ModSelection? mod = c.Tag as ModSelection;
                    if (mod != null) {
                        Result = new StashPickerResult {
                            Mod = mod.Mod,
                            IsHardcore = mod.IsHardcore
                        };
                        _settings.GetLocal().LastSelectedTargetMod = mod.Mod ?? string.Empty;
                        _settings.GetLocal().LastSelectedTargetModIsHc = mod.IsHardcore;
                        Close(Result);
                        return;
                    }
                }
            }

            Close(null);
        }

        private void helpLink_LinkClicked(object? sender, PointerPressedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.TransferToAnyMod);
            Close(null);
        }

        public class StashPickerResult {
            public string? Mod {
                get;
                set;
            }

            public bool IsHardcore {
                get;
                set;
            }
        }
    }
}
