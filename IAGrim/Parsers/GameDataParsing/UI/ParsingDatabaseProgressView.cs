using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using IAGrim.Utilities;
using IAGrim.Parsers.Arz;

namespace IAGrim.Parsers.GameDataParsing.UI {
    public partial class ParsingDatabaseProgressView : Window {
        public ProgressBar LoadingTags => progressLoadingTags;
        public ProgressBar SavingTags => progressSavingTags;
        public ProgressBar LoadingItems => progressLoadingItems;
        public ProgressBar MappingItemNames => progressMappingItemNames;
        public ProgressBar MappingPetStats => progressMappingPetStats;
        public ProgressBar SavingItems => progressSaveItems;
        public ProgressBar IndexingItems => progressIndexItems;
        public ProgressBar GeneratingSpecialStats => progressGeneratingSpecialStats;
        public ProgressBar SavingSpecialStats => progressSavingSpecialStats;

        public ProgressBar GeneratingSkills => progressGeneratingSkills;
        public ProgressBar SkillCorrectnessCheck => progressSkillCorrectnessCheck;

        private bool _closePermitted;

        public ParsingDatabaseProgressView() {
            InitializeComponent();
            Closing += OnClosing;
            Opened += OnOpened;
        }

        public void OverrideClose() {
            _closePermitted = true;
            Close();
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e) {
            if (!_closePermitted) {
                e.Cancel = true;
            }
        }

        private void OnOpened(object? sender, EventArgs e) {
            if (RuntimeSettings.Language != null) {
                LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language);
            }
        }
    }
}
