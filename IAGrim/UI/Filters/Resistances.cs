using Avalonia.Controls;
using IAGrim.Services.ItemStats;
using IAGrim.Theme;
using System.Collections.Generic;
using System.Linq;

namespace IAGrim.UI.Filters {
    public partial class Resistances : UserControl {
        public Resistances() {
            InitializeComponent();
            FirefoxCheckBox.EnableNumericFilters(this);
        }

        // Selected resist checkboxes paired with their stat fields, built once so both the plain "stat
        // exists" filters and the per-checkbox numeric filters derive from the same mapping.
        private List<(FirefoxCheckBox cb, string[] fields)> SelectedStatGroups() {
            var groups = new List<(FirefoxCheckBox, string[])>();

            if (resistElemental.IsChecked == true) {
                groups.Add((resistElemental, new[] { "defensiveElementalResistance" }));
            }

            var resistTypes = new[] {
                (resistPhysical, "Physical"),
                (resistPiercing, "Pierce"),
                (resistFire, "Fire"),
                (resistCold, "Cold"),
                (resistLightning, "Lightning"),
                (resistAether, "Aether"),
                (resistVitality, "Life"),
                (resistChaos, "Chaos"),
                (resistPoison, "Poison"),
                (resistBleeding, "Bleeding"),
                // TODO: Add freeze
                (resistStun, "Stun"),
            };

            foreach (var (cb, damageType) in resistTypes) {
                if (cb.IsChecked != true)
                    continue;

                groups.Add((cb, new[] {
                    $"defensive{damageType}",
                    $"defensive{damageType}Modifier",
                    $"defensiveSlow{damageType}",
                    $"defensiveSlow{damageType}Modifier"
                }));
            }

            if (resistSlow.IsChecked == true) {
                groups.Add((resistSlow, new[] { "defensiveTotalSpeedResistance" }));
            }

            return groups;
        }

        public List<string[]> Filters => SelectedStatGroups().Select(g => g.fields).ToList();

        public List<StatValueFilter> NumericFilters => FilterBuilder.From(SelectedStatGroups());
    }
}
