using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using System;
using System.ComponentModel;

namespace IAGrim.UI {
    /// <summary>
    /// Loading screen while parsing player item stats
    /// </summary>
    public partial class UpdatingPlayerItemsScreen : Window {
        private StatUpdateUIBackgroundWorker _worker;
        public bool CanClose { get; set; }


        public UpdatingPlayerItemsScreen(IPlayerItemDao playerItemDao) {
            InitializeComponent();
            CanClose = false;

            _worker = new StatUpdateUIBackgroundWorker(playerItemDao, bw_RunWorkerCompleted, bw_ProgressChanged);

            Closing += UpdatingPlayerItemsScreen_FormClosing;
        }

        /// <summary>
        /// Update progress bar safely in UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bw_ProgressChanged(object? sender, ProgressChangedEventArgs e) {
            Dispatcher.UIThread.Post(() => {
                if ((int)e.UserState! == 1)
                    this.progressBar2.Maximum = e.ProgressPercentage;
                else
                    this.progressBar2.Value = e.ProgressPercentage;
            });
        }


        void bw_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null)
                throw e.Error;

            this.CanClose = true;
            this.Close();
        }


        void UpdatingPlayerItemsScreen_FormClosing(object? sender, WindowClosingEventArgs e) {
            e.Cancel = !CanClose;
        }

    }
}
