using Avalonia.Controls;
using IAGrim.Services.ItemStats;
using IAGrim.Theme;
using System.Collections.Generic;
using System.Linq;

namespace IAGrim.UI.Filters {
    public partial class DamageOverTimeFilter : UserControl {
        public DamageOverTimeFilter() {
            InitializeComponent();
            FirefoxCheckBox.EnableNumericFilters(this);
        }


        // Selected DoT checkboxes paired with their stat fields, built once so both the plain "stat exists"
        // filters and the per-checkbox numeric filters derive from the same mapping.
        private List<(FirefoxCheckBox cb, string[] fields)> SelectedStatGroups() {
            var groups = new List<(FirefoxCheckBox, string[])>();

            var dotTypes = new[] {
                (dmgBleeding, "Bleeding"),
                (dmgTrauma, "Physical"),
                (dmgBurn, "Fire"),
                (dmgElectrocute, "Lightning"),
                (dmgVitalityDecay, "Life"),
                (dmgFrost, "Cold"),
                (dmgPoison, "Poison"),
            };

            foreach (var (cb, dot) in dotTypes) {
                if (cb.IsChecked != true)
                    continue;

                groups.Add((cb, new[] {
                    $"offensiveSlow{dot}",
                    $"offensiveSlow{dot}Modifier",
                    $"offensiveSlow{dot}ModifierChance",
                    $"offensiveSlow{dot}DurationModifier",
                    $"retaliationSlow{dot}Min",
                    $"retaliationSlow{dot}Chance",
                    $"retaliationSlow{dot}Duration",
                    $"retaliationSlow{dot}DurationMin"
                }));
            }

            if (dmgLifeLeech.IsChecked == true)
                groups.Add((dmgLifeLeech, new[] {"offensiveLifeLeechMin", "offensiveSlowLifeLeachMin"}));

            return groups;
        }

        public List<string[]> Filters => SelectedStatGroups().Select(g => g.fields).ToList();

        public List<StatValueFilter> NumericFilters => FilterBuilder.From(SelectedStatGroups());
    }
}
