using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IAGrim.Database;
using IAGrim.Theme;

namespace IAGrim.UI.Filters {
    public partial class Classes : UserControl {
        private readonly Dictionary<string, FirefoxCheckBox> _classes;
        private readonly IList<ItemTag> _classTags;

        public Classes(IList<ItemTag> classTags) {
            InitializeComponent();
            _classes = new Dictionary<string, FirefoxCheckBox>();
            _classTags = classTags;
            Classes_Load();
        }

        public List<string> DesiredClasses {
            get {
                return _classes.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToList();
            }
        }

        private void Classes_Load()
        {
            // TODO: Localize hardcoded classes and skip adding them if they exist
            _classes["class01"] = cbSoldier;
            _classes["class02"] = cbDemolitionist;
            _classes["class03"] = cbOccultist;
            _classes["class04"] = cbNightblade;
            _classes["class05"] = cbArcanist;
            _classes["class06"] = cbShaman;
            _classes["class07"] = cbInquisitor;
            _classes["class08"] = cbNecromancer;
            _classes["class09"] = cbOathkeeper;
            _classes["class10"] = cbBerserker;

            // Hardcoded classes from the base game -- Helps a bit with 4k scaling to not create these dynamically.
            var prefilled = new[] {
                "class01", "class02", "class03", "class04", "class05",
                "class06", "class07", "class08", "class09", "class10"
            };

            foreach (var tag in _classTags) {
                var translationTag = $"{(tag.Tag ?? string.Empty).ToLowerInvariant()}";

                if (!prefilled.Contains(translationTag)) {
                    var cb = new FirefoxCheckBox {
                        Content = tag.Name
                    };

                    _classes[translationTag] = cb;
                    classesPanelBox.Children.Add(cb);
                }
                else if (_classes.ContainsKey(translationTag)) {
                    _classes[translationTag].Content = tag.Name;
                }
            }
        }
    }
}
