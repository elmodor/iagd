using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using IAGrim.Services;
using IAGrim.Settings;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities;

namespace IAGrim.UI.Popups {
    public partial class StashTabPicker : Window {
        private readonly SettingsService _settings;
        private readonly int _numStashTabs = 6;
        private readonly IHelpService _helpService;

        public StashTabPicker(SettingsService settings, IHelpService helpService) {
            InitializeComponent();
            _settings = settings;
            _helpService = helpService;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e) {
            if (_settings.GetLocal().StashToLootFrom == _settings.GetLocal().StashToDepositTo &&
                _settings.GetLocal().StashToLootFrom != 0) {
                MessageBox.Show(
                    "I cannot overstate what an incredibly bad experience it would be to use only one tab.",
                    "Yeah.. Nope!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            else {
                this.Close();
            }
        }

        private RadioButton CreateCheckbox(string name, string label, string text, EventHandler<RoutedEventArgs> callback) {
            RadioButton checkbox = new RadioButton();
            checkbox.FontSize = 10;
            checkbox.Foreground = new SolidColorBrush(Color.FromRgb(66, 78, 90));
            checkbox.Name = name;
            checkbox.Tag = label;
            checkbox.Content = text;
            checkbox.Width = 188;
            checkbox.Height = 27;
            checkbox.PropertyChanged += (sender, args) => {
                if (args.Property == RadioButton.IsCheckedProperty && checkbox.IsChecked == true) {
                    callback(sender, new RoutedEventArgs());
                }
            };
            return checkbox;
        }

        private void StashTabPicker_Load(object sender, EventArgs e) {
            // Calculate the height dynamically depending on how many stashes the user has
            Height = Math.Min(800, Math.Max(357, 202 + 31 * _numStashTabs));
            gbMoveTo.Height = Math.Max(248, 83 + 33 * _numStashTabs);
            gbLootFrom.Height = Math.Max(248, 83 + 33 * _numStashTabs);


            for (int i = 1; i <= Math.Max(5, _numStashTabs); i++) {
                int p = i; // Don't reference out scope (mutated)
                EventHandler<RoutedEventArgs> callback = (o, args) => {
                    if (p <= _numStashTabs) {
                        // Don't trust the "Firefox framework" to not trigger clicks on disabled buttons.
                        _settings.GetLocal().StashToDepositTo = p;
                    }
                };

                var cb = CreateCheckbox($"moveto_tab_{i}", $"iatag_ui_tab_{i}", $"Tab {i}", callback);
                
                cb.IsChecked = _settings.GetLocal().StashToDepositTo == i;
                cb.IsEnabled = i <= _numStashTabs;
                moveToPanel.Children.Add(cb);
                helpWhyAreTheseDisabled.IsVisible = _numStashTabs <= 4;
            }


            for (int i = 1; i <= Math.Max(5, _numStashTabs); i++) {
                int p = i; // Don't reference out scope (mutated)
                EventHandler<RoutedEventArgs> callback = (o, args) => {
                    if (p <= _numStashTabs) {
                        // Don't trust the "Firefox framework" to not trigger clicks on disabled buttons.
                        _settings.GetLocal().StashToLootFrom = p;
                    }
                };

                var cb = CreateCheckbox($"lootfrom_tab_{i}", $"iatag_ui_tab_{i}", $"Tab {i}", callback);
                cb.IsChecked = _settings.GetLocal().StashToLootFrom == i;
                cb.IsEnabled = i <= _numStashTabs;
                lootFromPanel.Children.Add(cb);
            }
            


            radioOutputSecondToLast.IsChecked = _settings.GetLocal().StashToDepositTo == 0;
            radioInputLast.IsChecked = _settings.GetLocal().StashToLootFrom == 0;

            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language!);
        }

        private void radioOutputSecondToLast_CheckedChanged(object sender, RoutedEventArgs e) {
            _settings.GetLocal().StashToDepositTo = 0;
        }

        private void radioInputLast_CheckedChanged(object sender, RoutedEventArgs e) {
            _settings.GetLocal().StashToLootFrom = 0;
        }

        private void helpWhyAreTheseDisabled_LinkClicked(object sender, RoutedEventArgs e) {
            _helpService.ShowHelp(IHelpService.HelpType.NotEnoughStashTabs);
        }
    }
}
