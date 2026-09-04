using Avalonia.Controls;
using Avalonia.LogicalTree;
using IAGrim.Database.Interfaces;
using IAGrim.Services.ItemStats;
using IAGrim.Theme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IAGrim.UI

{
    internal partial class DesiredSkills : UserControl {
        private readonly Filters.Misc _miscFilter = new Filters.Misc();
        private readonly Filters.Damage _damageFilter = new Filters.Damage();
        private readonly Filters.DamageOverTimeFilter _dotFilter = new Filters.DamageOverTimeFilter();
        private readonly Filters.Resistances _resistanceFilters = new Filters.Resistances();
        private readonly Filters.Classes _classesFilters;

        public DesiredSkills(IItemTagDao itemTagDao) {
            InitializeComponent();

            DamageFilter.Content = _damageFilter;
            DamageOverTimeFilter.Content = _dotFilter;
            MiscFilter.Content = _miscFilter;
            ResistanceFilter.Content = _resistanceFilters;

            // Classes
            var classTags = itemTagDao.GetValidClassItemTags()
                .Where(entry => Regex.Replace(entry.Tag ?? string.Empty, @"[^\d]", "").Length <= 3) // Filter out 4 digit classes (combo classes)
                .ToList();

            _classesFilters = new Filters.Classes(classTags);
            ClassesFilter.Content = _classesFilters;

            InitControlsRecursive(this);
        }

        public FilterEventArgs Filters =>
            new FilterEventArgs
            {
                Filters = OrFilters,
                NumericFilters = NumericFilters,
                PetBonuses = _miscFilter.PetBonuses,
                HasPetBonus = _miscFilter.HasPetBonus,
                IsRetaliation = _damageFilter.RetaliationDamage,
                DuplicatesOnly = _miscFilter.DuplicatesOnly,
                SocketedOnly = _miscFilter.SocketedOnly,
                RecentOnly = _miscFilter.RecentOnly,
                DesiredClass = _classesFilters.DesiredClasses,
                GrantsSkill = _miscFilter.GrantsSkill,
                WithSummonerSkillOnly = _miscFilter.WithSummonerSkillOnly,
            };

        public event EventHandler<FilterEventArgs>? OnChanged;

        /// <summary>
        /// Get the desired skills to filter by
        /// Where there is more than one skill, treat it as "OR"
        /// </summary>
        private List<string[]> OrFilters
        {
            get
            {
                var filters = new List<string[]>();

                filters.AddRange(_damageFilter.Filters);
                filters.AddRange(_dotFilter.Filters);
                filters.AddRange(_resistanceFilters.Filters);
                filters.AddRange(_miscFilter.Filters);

                return filters;
            }
        }

        /// <summary>
        /// The per-checkbox numeric stat filters across all panels (only checkboxes with a filter set).
        /// </summary>
        private List<Services.ItemStats.StatValueFilter> NumericFilters
        {
            get
            {
                var filters = new List<Services.ItemStats.StatValueFilter>();

                filters.AddRange(_damageFilter.NumericFilters);
                filters.AddRange(_dotFilter.NumericFilters);
                filters.AddRange(_resistanceFilters.NumericFilters);
                filters.AddRange(_miscFilter.NumericFilters);

                return filters;
            }
        }

        /// <summary>
        /// Set all the filters to false
        /// </summary>
        public void ClearFilters()
        {
            ClearFiltersRecursive(this);
        }

        private void InitControlsRecursive(ILogical root)
        {
            foreach (var c in root.GetLogicalChildren())
            {
                if (c is FirefoxCheckBox cb)
                {
                    cb.PropertyChanged += (sender, e) =>
                    {
                        if (e.Property == CheckBox.IsCheckedProperty)
                        {
                           // Only search if the user desires auto search (probably 99%)
                            OnChanged?.Invoke(this, Filters);
                        }
                    };

                    // Setting/removing a numeric stat filter must also re-run the search.
                    cb.FilterChanged += (sender, e) => OnChanged?.Invoke(this, Filters);
                }

                if (c is ILogical logical)
                    InitControlsRecursive(logical);
            }
        }

        private void ClearFiltersRecursive(ILogical root)
        {
            foreach (var c in root.GetLogicalChildren())
            {
                if (c is FirefoxCheckBox cb)
                {
                    cb.IsChecked = false;
                }

                if (c is ILogical logical)
                    ClearFiltersRecursive(logical);
            }
        }
    }
}
