using Avalonia.Controls;
using IAGrim.Services.ItemStats;
using IAGrim.Theme;
using System.Collections.Generic;
using System.Linq;

namespace IAGrim.UI.Filters {
    public partial class Misc : UserControl {
        public Misc() {
            InitializeComponent();
            health.SupportsNumericFilter = true;
            cbDefense.SupportsNumericFilter = true;
            cbOffensive.SupportsNumericFilter = true;
        }


        // The three Misc stats that expose a numeric filter button, paired with the same fields their
        // "stat exists" entries use in Filters below.
        public List<StatValueFilter> NumericFilters => FilterBuilder.From(new (FirefoxCheckBox, string[])[] {
            (health, new[] { "characterLifeModifier", "characterLife" }),
            (cbDefense, new[] { "characterDefensiveAbilityModifier", "characterDefensiveAbility" }),
            (cbOffensive, new[] { "characterOffensiveAbility", "characterOffensiveAbilityModifier" }),
        });

        public bool SocketedOnly => cbSocketed.IsChecked == true;
        public bool DuplicatesOnly => cbDuplicates.IsChecked == true;
        public bool PetBonuses => cbPetBonuses.IsChecked == true;
        public bool HasPetBonus => cbHasPetBonus.IsChecked == true;
        public bool RecentOnly => cbRecentOnly.IsChecked == true;
        public bool GrantsSkill => cbGrantsSkill.IsChecked == true;
        public bool WithSummonerSkillOnly => cbSummonerSkill.IsChecked == true;

        public List<string[]> Filters {
            get {
                var filters = new List<string[]>();

                if (setbonus.IsChecked == true) {
                    filters.Add(new[] { "setName", "itemSetName" });
                }

                if (shieldStuff.IsChecked == true) {
                    filters.Add(new[] {
                        "blockAbsorption", "defensiveBlock", "defensiveBlockChance", "defensiveBlockModifier",
                        "defensiveBlockAmountModifier"
                    });
                }

                if (cbAttackSpeed.IsChecked == true) {
                    filters.Add(new[]
                        {"characterAttackSpeedModifier", "characterAttackSpeed", "characterTotalSpeedModifier"});
                }

                if (cbCastspeed.IsChecked == true) {
                    filters.Add(new[] {"characterSpellCastSpeedModifier", "characterTotalSpeedModifier"});
                }

                if (cbIncreaseArmor.IsChecked == true) {
                    filters.Add(new[] { "defensiveProtectionModifier" });
                }

                if (cbRunspeed.IsChecked == true) {
                    filters.Add(new[] {"characterRunSpeedModifier", "characterTotalSpeedModifier"});
                }

                if (exp.IsChecked == true) {
                    filters.Add(new[] {"characterIncreasedExperience"});
                }

                if (cbReflect.IsChecked == true) {
                    filters.Add(new[] {"defensiveReflect"});
                }

                if (health.IsChecked == true) {
                    filters.Add(new[] {"characterLifeModifier", "characterLife"});
                }

                if (cbDefense.IsChecked == true) {
                    filters.Add(new[] {"characterDefensiveAbilityModifier", "characterDefensiveAbility"});
                }

                if (cbOffensive.IsChecked == true) {
                    filters.Add(new[] {"characterOffensiveAbility", "characterOffensiveAbilityModifier"});
                }

                if (cbMasterySkills.IsChecked == true) {
                    filters.Add(new[] {"augmentMastery1", "augmentMastery2"});
                }

                if (cbEnergyRegen.IsChecked == true) {
                    filters.Add(new[] {"characterManaRegen", "characterManaRegenModifier"});
                }

                if (cbWeaponLifeLeech.IsChecked == true) {
                    filters.Add(new[] { "offensiveLifeLeechMin" });
                }

                if (cbDamageConversion.IsChecked == true) {
                    filters.Add(new[] { "conversionPercentage" });
                }

                if (cbCooldownReduction.IsChecked == true) {
                    filters.Add(new[] { "skillCooldownReduction" });
                }

                if (cbPhysique.IsChecked == true) {
                    filters.Add(new[] { "characterStrength", "characterStrengthModifier" });
                }

                if (cbSpirit.IsChecked == true) {
                    filters.Add(new[] { "characterIntelligence", "characterIntelligenceModifier" });
                }

                if (cbCunning.IsChecked == true) {
                    filters.Add(new[] { "characterDexterity", "characterDexterityModifier" });
                }

                return filters;
            }
        }
    }
}
