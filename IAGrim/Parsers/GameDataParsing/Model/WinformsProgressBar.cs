using Avalonia.Controls;
using Avalonia.Threading;
using IAGrim.Utilities;

namespace IAGrim.Parsers.GameDataParsing.Model {
    public class AvaloniaProgressBar {
        private readonly ProgressBar _progressBar;
        public ProgressTracker Tracker { get; } = new ProgressTracker();

        public AvaloniaProgressBar(ProgressBar progressBar) {
            _progressBar = progressBar;
            Dispatcher.UIThread.Post(() => {
                _progressBar.Maximum = 100;
                _progressBar.Value = Tracker.Progress;
            });

            Tracker.OnProgressChanged += (_, _) => {
                int progress = Tracker.Progress;
                Dispatcher.UIThread.Post(() => {
                    _progressBar.Value = progress;
                });
            };
        }
    }
}
