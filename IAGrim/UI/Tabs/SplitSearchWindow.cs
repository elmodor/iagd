using IAGrim.Database;
using IAGrim.Database.Dto;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Services.ItemStats;
using IAGrim.Settings;
using IAGrim.Theme;
using IAGrim.UI.Controller;
using IAGrim.UI.Misc.CEF;
using IAGrim.UI.Tabs.Util;
using IAGrim.Utilities;
using log4net;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.LogicalTree;

namespace IAGrim.UI.Tabs {
    internal sealed partial class SplitSearchWindow : UserControl {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SplitSearchWindow));
        private readonly Action<string> _setStatus;
        private readonly SearchController _searchController;
        private readonly IItemTagDao _itemTagDao;
        private DispatcherTimer? _delayedTextChangedTimer;
        private DesiredSkills? _filterWindow;
        private bool _clearingFilters;
        private readonly SettingsService _settings;
        // private ToolTip? toolTip1;
        // private System.ComponentModel.IContainer? components;
        private bool _hasCheckedModFilterNotEmpty = false;
        private bool _isAdjustingSplitter;

        /// <summary>
        /// ModSelectionHandler
        /// </summary>
        public ModSelectionHandler ModSelectionHandler { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="setStatus"></param>
        /// <param name="playerItemDao"></param>
        /// <param name="searchController"></param>
        /// <param name="itemTagDao"></param>
        public SplitSearchWindow(
            NativeWebView browser,
            Action<string> setStatus,
            IPlayerItemDao playerItemDao,
            SearchController searchController,
            IItemTagDao itemTagDao,
            SettingsService settings) {
            _setStatus = setStatus;
            _searchController = searchController;
            _itemTagDao = itemTagDao;
            _settings = settings;
            InitializeComponent();

            WebViewHost.Content = browser;
            //
            // Dock = DockStyle.Fill;
            //
            // FilterPanelMinSize = new DpiHelper(CreateGraphics()).ScaleX(250);
            //
            // // Weird hack to not have the searcbox be height=4 on windows 11. 
            // _searchBox!.MaximumSize = new Size(_searchBox.MaximumSize.Width, 0);
            // TODO move to load I guess?
            SearchBox.TextChanged += SearchBox_TextChanged;
            OrderByLevel.IsCheckedChanged += (_, _) => UpdateListViewDelayed();
            MinLevel.LostFocus += (_, _) => ValidateMinLevel();
            MaxLevel.LostFocus += (_, _) => ValidateMaxLevel();

            ItemQuality.ItemsSource = UIHelper.QualityFilter;
            ItemQuality.SelectedIndex = 0;
            ItemQuality.SelectionChanged += (_, _) => { UpdateListViewDelayed(); };

            SlotFilter.ItemsSource = UIHelper.SlotFilter;
            SlotFilter.SelectedIndex = 0;
            SlotFilter.SelectionChanged += (_, _) => { UpdateListViewDelayed(); };

            _filterWindow = new DesiredSkills(_itemTagDao);
            FilterPanel.Content = _filterWindow;
            _filterWindow.OnChanged += (_, filters) => { UpdateListViewDelayed(); };
            //TODO
            //
            // _mainSplitter!.SplitterDistance = FilterPanelMinSize;
            // _mainSplitter.SplitterWidth = 5;
            // _mainSplitter.BorderStyle = BorderStyle.None;
            // _mainSplitter.SplitterMoved += MainSplitterOnSplitterMoved;
            //
            ModSelectionHandler = new ModSelectionHandler(ModFilter, playerItemDao, UpdateListViewDelayed, setStatus, _settings);
            //
            // _toolStripContainer!.ContentPanel.Controls.Add(browser);
            //
            // Activated += SplitSearchWindow_Activated;
            // Deactivate += SplitSearchWindow_Deactivate;
            //
            // // Painted by the webview until the page itself paints; must match the WebUI body/App background
            // // or the user gets a white flash on startup.
            // webView21!.DefaultBackgroundColor = _settings.GetPersistent().DarkMode
            //     ? Color.Black
            //     : Color.FromArgb(65, 60, 53); // #413c35
            //
            // // WINE PATCH: the Chromium sandbox and GPU/renderer subprocesses fail to spawn reliably under
            // // Wine (especially while Grim Dawn is running), so WebView2 navigation never completes and the
            // // item grid stays blank. --no-sandbox + --disable-gpu + --single-process avoids the failing
            // // subprocess/sandbox path so the page renders regardless of Grim Dawn. Applied ONLY under Wine
            // // so Windows keeps its default (sandboxed, GPU-accelerated, multi-process) behaviour untouched.
            // CoreWebView2Environment conf;
            // if (IAGrim.Services.WineDetector.IsRunningInWine()) {
            //     var envOptions = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--no-sandbox --disable-gpu --disable-gpu-compositing --single-process" };
            //     conf = CoreWebView2Environment.CreateAsync(null, GlobalPaths.EdgeCacheLocation, envOptions).Result;
            // }
            // else {
            //     conf = CoreWebView2Environment.CreateAsync(null, GlobalPaths.EdgeCacheLocation).Result;
            // }
            // webView21!.EnsureCoreWebView2Async(conf);
            //
            // InitializeFilterPanel();
            AttachedToVisualTree += SplitSearchWindow_Load;
        }

        public async Task SelectModFilterIfNotSelected() {
            await Dispatcher.UIThread.InvokeAsync(() => {
                if (!_hasCheckedModFilterNotEmpty) {
                    // Should only happen for the very first item ever looted, but it's a fairly poor user experience when it does happen.
                    if (ModFilter.SelectedItem == null) {
                        ModSelectionHandler.ConfigureModFilter();
                    }

                    if (ModFilter.SelectedItem == null && ModFilter.ItemCount > 0) {
                        ModFilter.SelectedIndex = 0;
                        _hasCheckedModFilterNotEmpty = true;
                    }
                }
            });
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearFilters() {
            _clearingFilters = true;
            ClearAllFirefoxCheckBoxes(FilterPanel);
            SearchBox.Text = string.Empty;
            OrderByLevel.IsChecked = true;
            ItemQuality.SelectedIndex = 0;
            SlotFilter.SelectedIndex = 0;
            MinLevel.Text = "0";
            MaxLevel.Text = "110";
            _clearingFilters = false;

            UpdateListViewDelayed();
        }

        /// <summary>
        /// Update interface
        /// </summary>
        public void UpdateInterface() {
            // TODO
            // InitializeFilterPanel();
        }

        /// <summary>
        /// Update view
        /// </summary>
        public void UpdateListView(PlayerItem? item = null) {
            if (!Dispatcher.UIThread.CheckAccess()) {
                Dispatcher.UIThread.Post(() => UpdateListView(_filterWindow!.Filters, item));
            }
            else {
                UpdateListView(_filterWindow!.Filters, item);
            }
        }

        /// <summary>
        /// Update view
        /// </summary>
        private void UpdateListView(FilterEventArgs filters, PlayerItem? item = null) {

            var transferFile = ModSelectionHandler.SelectedMod;
            if (transferFile == null) {
                Logger.Warn("Attempting to update item view, but no mod selection has been made");
                ModSelectionHandler.SetDefaultModIfAvailable();
                return;
            }
            var rarity  = ItemQuality.SelectedItem as IAGrim.Theme.ComboBoxItemQuality;
            var slot = SlotFilter.SelectedItem as IAGrim.Theme.ComboBoxItem;

            var query = new ItemSearchRequest
            {
                Wildcard = SearchBox.Text ?? string.Empty,
                StatValueFilters = filters.NumericFilters ?? new List<StatValueFilter>(),
                Filters = filters.Filters ?? new List<string[]>(),
                MinimumLevel = ParseNumeric(MinLevel, 0),
                MaximumLevel = ParseNumeric(MaxLevel, 110),
                Rarity = rarity?.Rarity,
                PrefixRarity = rarity?.PrefixRarity ?? 0,
                Slot = slot?.Filter,
                SlotInverse = slot?.Inverse ?? false,
                PetBonuses = filters.PetBonuses,
                HasPetBonus = filters.HasPetBonus,
                IsRetaliation = filters.IsRetaliation,
                DuplicatesOnly = filters.DuplicatesOnly,
                Mod = transferFile.Mod,
                IsHardcore = transferFile.IsHardcore,
                Classes = filters.DesiredClass ?? new List<string>(),
                SocketedOnly = filters.SocketedOnly,
                RecentOnly = filters.RecentOnly,
                WithGrantSkillsOnly = filters.GrantsSkill,
                WithSummonerSkillOnly = filters.WithSummonerSkillOnly
            };

            if (item != null)
            {
                _searchController.Search(query, item);
            }
            else {
                bool includeBuddyItems = !filters.DuplicatesOnly; // If we're looking for duplicates, we're probably doing a cleanup, not caring about buddyitems
                var message = _searchController.Search(query, includeBuddyItems, OrderByLevel.IsChecked == true);

                Logger.Info("Updating UI...");

                if (!string.IsNullOrEmpty(message)) {
                    _setStatus(message);
                }

                Logger.Info("Done");
            }
        }

        /// <summary>
        /// Update view with delay
        /// </summary>
        public void UpdateListViewDelayed() {
            UpdateListViewDelayed(_settings.GetLocal().PreferDelayedSearch ? 200 : 0);
        }

        // TODO all of it
        // private void BeginSearchOnAutoSearch(object? sender, EventArgs e) {
        //     // Once the user finds the numeric stat filter on their own, the introduction banner is no longer relevant.
        //     var persistent = _settings.GetPersistent();
        //     if (!persistent.NumericFilterUsed && e is FilterEventArgs { NumericFilters.Count: > 0 }) {
        //         persistent.NumericFilterUsed = true;
        //     }
        //
        //     UpdateListViewDelayed();
        // }
        //
        private void HandleDelayedTextChangedTimerTick(object? sender, EventArgs e) {
            if (_delayedTextChangedTimer != null) {
                _delayedTextChangedTimer.Stop();
                _delayedTextChangedTimer = null;
            }

            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => UpdateListView(_filterWindow!.Filters));
            }
            else
            {
                UpdateListView(_filterWindow!.Filters);
            }
        }
        //
        // TODO
        // private void InitializeFilterPanel() {
        //     if (_filterWindow != null) {
        //         _filterWindow.OnChanged -= BeginSearchOnAutoSearch;
        //         _filterWindow.Close();
        //         _mainSplitter.Panel1.Controls.Remove(_filterWindow);
        //     }
        //
        //     _filterWindow = new DesiredSkills(_itemTagDao) {
        //         TopLevel = false
        //     };
        //     _filterWindow.OnChanged += BeginSearchOnAutoSearch;
        //     _mainSplitter.Panel1.Controls.Add(_filterWindow);
        //     _filterWindow.Show();
        // }
        //
        // /// <summary>
        // /// Keeps the filter panel from being dragged below its minimum width.
        // /// Never set SplitterDistance to a value the SplitContainer cannot honour: the setter silently clamps
        // /// to (Width - Panel2MinSize - SplitterWidth) and then raises SplitterMoved again, so an unreachable
        // /// value makes this handler re-enter itself endlessly and hangs the UI thread (blank window /
        // /// "not responding") whenever the control is laid out narrower than FilterPanelMinSize.
        // /// </summary>
        // private void MainSplitterOnSplitterMoved(object? sender, SplitterEventArgs e) => EnforceFilterPanelMinSize();
        //
        // private void MainSplitterOnResize(object? sender, EventArgs e) => EnforceFilterPanelMinSize();
        //
        // private void EnforceFilterPanelMinSize() {
        //     if (_isAdjustingSplitter || _mainSplitter.SplitterDistance >= FilterPanelMinSize) {
        //         return;
        //     }
        //
        //     var maxDistance = _mainSplitter.Width - _mainSplitter.Panel2MinSize - _mainSplitter.SplitterWidth;
        //     var desired = Math.Min(FilterPanelMinSize, maxDistance);
        //     if (desired < _mainSplitter.Panel1MinSize || desired == _mainSplitter.SplitterDistance) {
        //         return;
        //     }
        //
        //     _isAdjustingSplitter = true;
        //     try {
        //         _mainSplitter.SplitterDistance = desired;
        //     }
        //     finally {
        //         _isAdjustingSplitter = false;
        //     }
        // }
        //
        // private void MaxLevel_MouseWheel(object? sender, MouseEventArgs e) {
        //     if (sender is Control c) {
        //         if (e.Delta > 0) {
        //             c.Text = Math.Min(ParseNumeric(c) + 1, 110).ToString();
        //         }
        //         else if (e.Delta < 0) {
        //             var newValue = Math.Min(Math.Max(0, ParseNumeric(c) - 1), 110);
        //             if (ParseNumeric(_minLevel!) > newValue) {
        //                 _minLevel!.Text = newValue.ToString();
        //             }
        //
        //             c.Text = Math.Min(Math.Max(0, ParseNumeric(c) - 1), 110).ToString();
        //         }
        //     }
        //
        //     UpdateListViewDelayed(1200);
        // }
        //
        // private void MinLevel_MouseWheel(object? sender, MouseEventArgs e) {
        //     if (sender is Control c) {
        //         if (e.Delta > 0) {
        //             var newValue = Math.Min(ParseNumeric(c) + 1, 110);
        //             if (ParseNumeric(_maxLevel!) < newValue) {
        //                 _maxLevel!.Text = newValue.ToString();
        //             }
        //
        //             c.Text = newValue.ToString();
        //         }
        //         else if (e.Delta < 0) {
        //             c.Text = Math.Min(Math.Max(0, ParseNumeric(c) - 1), 110).ToString();
        //         }
        //     }
        //
        //     UpdateListViewDelayed(1200);
        // }
        //
        // private void MinLevel_KeyPress(object? sender, KeyPressEventArgs e) {
        //     e.Handled = !(char.IsDigit(e.KeyChar) || ParseNumeric(_minLevel!) > 105 || e.KeyChar == '\b');
        //     UpdateListViewDelayed(1200);
        // }
        //
        private void ValidateMinLevel()
        {
            if (!int.TryParse(MinLevel.Text, out _))
            {
                MinLevel.Text = "0";
            }
        }

        private void ValidateMaxLevel()
        {
            if (!int.TryParse(MaxLevel.Text, out _))
            {
                MaxLevel.Text = "110";
            }
        }

        private int ParseNumeric(TextBox tb, int defaultValue) {
            return int.TryParse(tb.Text, out var val) ? val : defaultValue;
        }

        private void SplitSearchWindow_Load(object? sender, Avalonia.VisualTreeAttachmentEventArgs e) {
            AttachedToVisualTree -= SplitSearchWindow_Load;
            ModSelectionHandler.ConfigureModFilter();
            LocalizationLoader.ApplyTooltipLanguage(this, RuntimeSettings.Language!);
        }
        // TODO
        // private void SplitSearchWindow_Load(object? sender, EventArgs e) {
        //     _minLevel!.KeyPress += MinLevel_KeyPress;
        //     _minLevel.Leave += (s, ev) => { if (!int.TryParse(_minLevel.Text, out _)) _minLevel.Text = "0"; };
        //     _minLevel.MouseWheel += MinLevel_MouseWheel;
        //
        //     _maxLevel!.KeyPress += MinLevel_KeyPress;
        //     _maxLevel.Leave += (s, ev) => { if (!int.TryParse(_maxLevel.Text, out _)) _maxLevel.Text = "200"; };
        //     _maxLevel.MouseWheel += MaxLevel_MouseWheel;
        //
        //     _itemQuality!.Items.AddRange(UIHelper.QualityFilter.ToArray<object>());
        //     _itemQuality.SelectedIndex = 0;
        //     _selectedItemQuality = _itemQuality.SelectedItem as ComboBoxItemQuality;
        //     _itemQuality.SelectedIndexChanged += (s, ev) => { _selectedItemQuality = _itemQuality.SelectedItem as ComboBoxItemQuality; };
        //     _itemQuality.SelectedIndexChanged += BeginSearchOnAutoSearch;
        //
        //     _slotFilter!.Items.AddRange(UIHelper.SlotFilter.ToArray<object>());
        //     _slotFilter.SelectedIndex = 0;
        //     _selectedSlot = _slotFilter.SelectedItem as ComboBoxItem;
        //     _slotFilter.SelectedIndexChanged += (s, ev) => { _selectedSlot = _slotFilter.SelectedItem as ComboBoxItem; };
        //     _slotFilter.SelectedIndexChanged += BeginSearchOnAutoSearch;
        //
        //     FormClosing += SplitSearchWindow_FormClosing;
        //
        //     _searchBox.TextChanged += SearchBox_TextChanged;
        //
        //     _orderByLevel!.CheckStateChanged += delegate { UpdateListViewDelayed(); };
        //
        //     _flowPanelFilter!.SizeChanged += FlowPanelFilter_Resize;
        //     _mainSplitter.SizeChanged += FlowPanelFilter_Resize;
        // }
        //
        private void SearchBox_TextChanged(object? sender, EventArgs e) {
            UpdateListViewDelayed(600);
        }
        //
        // private void SplitSearchWindow_Activated(object? sender, EventArgs e) {
        //     _scrollableFilterView = new ScrollPanelMessageFilter(_filterWindow!);
        //     Application.AddMessageFilter(_scrollableFilterView);
        // }
        //
        // private void SplitSearchWindow_Deactivate(object? sender, EventArgs e) {
        //     if (_scrollableFilterView != null) {
        //         Application.RemoveMessageFilter(_scrollableFilterView);
        //     }
        // }
        //
        // private void SplitSearchWindow_FormClosing(object? sender, FormClosingEventArgs e) {
        //     if (InvokeRequired) {
        //         Invoke((System.Windows.Forms.MethodInvoker)delegate { SplitSearchWindow_FormClosing(sender, e); });
        //     }
        //     else {
        //         _filterWindow!.OnChanged -= BeginSearchOnAutoSearch;
        //         Activated -= SplitSearchWindow_Activated;
        //         Deactivate -= SplitSearchWindow_Deactivate;
        //
        //         _toolStripContainer.ContentPanel.Controls.Clear();
        //     }
        // }
        //
        private void UpdateListViewDelayed(int delay) {
            if (_clearingFilters)
                return;

            _delayedTextChangedTimer?.Stop();

            if (delay > 0) {
                _delayedTextChangedTimer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(delay)
                };
                _delayedTextChangedTimer.Tick += HandleDelayedTextChangedTimerTick;
                _delayedTextChangedTimer.Start();
            }
            else {
                HandleDelayedTextChangedTimerTick(this, EventArgs.Empty);
            }
        }

        // private void FlowPanelFilter_Resize(object? sender, EventArgs e) {
        //     _searchBox.Width = Math.Max(300, _flowPanelFilter!.Width - 500);
        //     _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        // }
        private void ClearFiltersButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ClearFilters();
        }

        private void ClearAllFirefoxCheckBoxes(Control root)
        {
            foreach (var c in root.GetLogicalChildren())
            {
                if (c is FirefoxCheckBox checkBox)
                {
                    checkBox.IsChecked = false;
                    checkBox.ClearFilter();
                }

                if (c is Control control)
                {
                    ClearAllFirefoxCheckBoxes(control);
                }
            }
        }
    }
}
