using Avalonia;
using Avalonia.Styling;
using System.Text;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;

using EvilsoftCommons;
using EvilsoftCommons.Exceptions;
using EvilsoftCommons.Cloud;
using IAGrim.Parsers.Arz;
using IAGrim.Parsers.TransferStash;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Services;
using IAGrim.Services.ItemReplica;
using IAGrim.Services.ItemStats;
using IAGrim.Database.Interfaces;
using IAGrim.UI.Misc.CEF;
using IAGrim.UI.Controller;
using IAGrim.UI.Tabs;
using IAGrim.UI;
using IAGrim.Services.MessageProcessor;
using IAGrim.Overwrites.RegisterWindowDataAndType;
using IAGrim.Backup.Cloud.Service;
using IAGrim.Backup.Cloud.CefSharp.Events;
using IAGrim.Backup.Cloud.Util;
using IAGrim.BuddyShare;
using IAGrim.Utilities.Cloud;

using System.IO;
using IAGrim.Settings;

using IAGrim.Utilities;
using IAGrim.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IAGrim.UI.Misc;

using log4net;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Overwrites;

namespace IAGrim.Linux;

internal sealed class JavaScriptMessage
{
    [JsonProperty("id")]
    public string? Id { get; init; }
    [JsonProperty("method")]
    public string? Method { get; init; }
    [JsonProperty("args")]
    public JToken[]? Args { get; init; }
}

public partial class MainWindow : Window
{
    private readonly JsonSerializerSettings _serializerSettings =
        new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

    private static readonly ILog Logger = LogManager.GetLogger(typeof(MainWindow));

    /// <summary>Users with fewer items than this are still getting set up, and don't need the numeric filter introduction.</summary>
    private const int NumericFilterBannerMinItems = 450;

    private readonly CefBrowserHandler _cefBrowserHandler;
    private readonly ISettingsReadController _settingsController;
    private readonly ServiceProvider _serviceProvider;
    private readonly ParsingService _parsingService;
    private readonly UserFeedbackService _userFeedbackService;
    private readonly SearchController _searchController;
    private readonly UsageStatisticsReporter _usageStatisticsReporter = new UsageStatisticsReporter();
    private readonly AutomaticUpdateChecker _automaticUpdateChecker;
    private readonly List<IMessageProcessor> _messageProcessors = new List<IMessageProcessor>();
    private readonly NativeWebView _webView;
    private CharacterBackupService? _charBackupService;
    private BackupServiceWorker? _backupServiceWorker;
    private WebSocketSyncService? _webSocketSyncService;
    private AuthService? _authService;
    private SplitSearchWindow? _searchWindow;
    private ModsDatabaseConfig? _modsDatabaseConfigTab;
    private ItemTransferController? _transferController;
    private ItemReplicaParser? _itemReplicaParser;
    private ItemStatPrecomputeService? _itemStatPrecomputeService;
    private CsvParsingService? _csvParsingService;
    private CsvFileMonitor? _csvFileMonitor = new CsvFileMonitor();
    private CsvFileMonitor? _replicaCsvFileMonitor = new CsvFileMonitor();
    private ItemReplicaRequesterService? _itemReplicaService;

    private BuddyItemsService? _buddyItemsService;
    private BackgroundTask? _backupBackgroundTask;
    private DispatcherTimer? _wineMessageTimer;

    public MainWindow(
        ServiceProvider serviceProvider,
        ParsingService parsingService
    )
    {
        _webView = new NativeWebView();
        _serviceProvider = serviceProvider;
        var settingsService = _serviceProvider.Get<SettingsService>();
        _cefBrowserHandler = new CefBrowserHandler(settingsService, () => MainTabStrip.SelectedIndex = 0);
        _cefBrowserHandler.Initialize(script => _webView.InvokeScript(script));
        _searchController = _serviceProvider.Get<SearchController>();
        InitializeComponent();
        Closing += MainWindow_Closing;

        // TODO
        // _minimizeToTrayHandler = new MinimizeToTrayHandler(this, notifyIcon1, serviceProvider.Get<SettingsService>());

        _automaticUpdateChecker = new AutomaticUpdateChecker(settingsService);
        _settingsController = new SettingsController(settingsService);
        _parsingService = parsingService;
        _userFeedbackService = new UserFeedbackService(_cefBrowserHandler);

        MainTabStrip.SelectionChanged += (_, _) =>
        {
            var index = MainTabStrip.SelectedIndex;

            splitSearchWindow.IsVisible = index == 0;
            onlineHost.IsVisible = index == 1;
            settingsWindow.IsVisible = index == 2;
            modsDatabaseConfig.IsVisible = index == 3;
        };

        if (settingsService.GetPersistent().DarkMode) {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        }
        AddHandler(InputElement.KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);

        MainWindow_Load();
    }


    private void Browser_NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e) {
        if (!e.IsSuccess) {
            Logger.Error("WebView navigation failed.");
            Logger.Error("Make sure webviewgtk is installed.");
            Logger.Error($"Error: {e.Request}");

            _ = MessageBox.Show($"A a fatal error occurred while attempting to navigate in webviewgtk\nError: {e.Request}", "Error - WebviewGTK", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Logger.Info("WebView navigation succeeded");
    }


    private void Browser_InitializationCompleted(object? sender, EventArgs e)
    {
        Logger.Info("WebUI signalled readiness");
        _cefBrowserHandler.SetReady();
        _searchWindow?.UpdateListViewDelayed();

        var isGdParsed = _serviceProvider.Get<IDatabaseItemDao>().GetRowCount() > 0;
        var settingsService = _serviceProvider.Get<SettingsService>();
        _cefBrowserHandler.SetDarkMode(settingsService.GetPersistent().DarkMode);
        _cefBrowserHandler.SetHideItemSkills(settingsService.GetPersistent().HideSkills);
        _cefBrowserHandler.SetIsGrimParsed(isGdParsed);


        _cefBrowserHandler.SetOnlineBackupsEnabled(!settingsService.GetLocal().OptOutOfBackups);

        var numItems = _serviceProvider.Get<IPlayerItemDao>().GetNumItems();
        _cefBrowserHandler.SetIsFirstRun(numItems == 0);
        if (numItems == 0) {
        } else if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) {
            if (settingsService.GetLocal().EasterPrank) {
                _cefBrowserHandler.SetEasterEggMode();
                settingsService.GetLocal().EasterPrank = false;
            }
        }
        else {
            settingsService.GetLocal().EasterPrank = true;
        }

        // Introduce the numeric stat filter to established users who haven't found it yet.
        var persistent = settingsService.GetPersistent();
        if (numItems >= NumericFilterBannerMinItems && !persistent.NumericFilterUsed && !persistent.NumericFilterBannerDismissed) {
            _cefBrowserHandler.SetShowNumericFilterBanner(true);
        }

    }

    // /// <summary>
    // /// Update the UI's language
    // /// </summary>
    public void UpdateLanguage() {
        if (RuntimeSettings.Language != null) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language);
        }
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e) {
        // No idea which of these are triggering on rare occasions, perhaps Deactivate, sizechanged or filterWindow.
        Closing -= MainWindow_Closing;
        // TODO
        // SizeChanged -= OnMinimizeWindow;

        _parsingService.OnParseComplete -= OnParseComplete;

        _authService?.Dispose();
        _authService = null;

        _csvFileMonitor?.Dispose();
        _csvFileMonitor = null;

        _replicaCsvFileMonitor?.Dispose();
        _replicaCsvFileMonitor = null;

        _csvParsingService?.Dispose();
        _csvParsingService = null;

        _itemReplicaService?.Dispose();
        _itemReplicaService = null;

        _itemStatPrecomputeService?.Dispose();
        _itemStatPrecomputeService = null;

        // TODO
        // _minimizeToTrayHandler?.Dispose();
        // _minimizeToTrayHandler = null;

        _backupBackgroundTask?.Dispose();
        _usageStatisticsReporter.Dispose();
        _automaticUpdateChecker.Dispose();

        _buddyItemsService?.Dispose();
        _buddyItemsService = null;

        // TODO
        // _injector?.Dispose();
        // _injector = null;

        _wineMessageTimer?.Stop();
        _wineMessageTimer = null;

        _backupServiceWorker?.Dispose();
        _backupServiceWorker = null;
        _webSocketSyncService?.Dispose();
        _webSocketSyncService = null;

        _itemReplicaParser?.Dispose();
        _itemReplicaParser = null;
    }

        /// <summary>
        /// Callback called when the Grim Dawn hook sends messages to IA
        /// </summary>
        /// <returns></returns>
        private void CustomWndProc(RegisterWindow.DataAndType bt) {
            // Most if not all actions may interact with SQL
            // SQL is done on the UI thread.
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => CustomWndProc(bt));
                return;
            }

            MessageType type = (MessageType) bt.Type;
            foreach (IMessageProcessor t in _messageProcessors) {
                t.Process(type, bt.Data, bt.StringData);
            }

            switch (type) {

                case MessageType.TYPE_REPORT_WORKER_THREAD_LAUNCHED:
                    Logger.Info("Grim Dawn hook reports successful launch.");
                    break;


                case MessageType.TYPE_GameInfo_IsHardcore:
                case MessageType.TYPE_GameInfo_IsHardcore_via_init:
                    Logger.Info($"TYPE_GameInfo_IsHardcore({bt.Data[0] > 0}, {type})");
                    if (_settingsController.AutoUpdateModSettings) {
                        _searchWindow?.ModSelectionHandler.UpdateModSelection(bt.Data[0] > 0);
                    }

                    break;

                case MessageType.TYPE_GameInfo_SetModName:
                    Logger.InfoFormat("TYPE_GameInfo_SetModName({0})", IOHelper.GetPrefixString(bt.Data, 0));
                    if (_settingsController.AutoUpdateModSettings) {
                        _searchWindow?.ModSelectionHandler.UpdateModSelection(IOHelper.GetPrefixString(bt.Data, 0));
                    }

                    break;
            }
        }

        // TODO
        // protected override void OnHandleCreated(EventArgs e) {
        //     base.OnHandleCreated(e);
        //     ShowExistingInstanceMessage.AllowReceiving(Handle);
        // }
        //
        // protected override void WndProc(ref Message m) {
        //     if (ShowExistingInstanceMessage.Id != 0 && m.Msg == ShowExistingInstanceMessage.Id) {
        //         Logger.Info("A second instance was started, showing the existing window.");
        //         ShowAndCenterWindow();
        //     }
        //
        //     base.WndProc(ref m);
        // }

        // TODO
        // /// <summary>
        // /// Brings IA back up wherever it happens to be: minimized, hidden in the tray, or on a monitor
        // /// that no longer exists. The window is centered on the screen the mouse is on, which is the screen
        // /// the user just started IA from.
        // /// </summary>
        // private void ShowAndCenterWindow() {
        //     try {
        //         // Restores from the tray, including the window state it had before it was minimized.
        //         _minimizeToTrayHandler?.notifyIcon_MouseDoubleClick(this, null);
        //
        //         Show();
        //         Visible = true;
        //
        //         if (WindowState == FormWindowState.Minimized) {
        //             WindowState = FormWindowState.Normal;
        //         }
        //
        //         if (WindowState != FormWindowState.Maximized) {
        //             var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
        //             Left = screen.Left + Math.Max(0, (screen.Width - Width) / 2);
        //             Top = screen.Top + Math.Max(0, (screen.Height - Height) / 2);
        //         }
        //
        //         Activate();
        //         BringToFront();
        //     }
        //     catch (Exception ex) {
        //         Logger.Warn("Error showing the window on request from a second instance", ex);
        //     }
        // }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _searchWindow?.ClearFilters();
                e.Handled = true;
            }
        }

        private void SetFeedback(string feedback) {
            try {
                if (!Dispatcher.UIThread.CheckAccess()) {
                    Dispatcher.UIThread.Post(() => SetFeedback(feedback));
                    return;
                }
                else {
                    statusLabel.Text = feedback.Replace("\\n", " - ");
                    _userFeedbackService.SetFeedback(feedback);
                }
            }
            catch (ObjectDisposedException) {
                Logger.Debug("Attempted to set feedback, but UI already disposed. (Probably shutting down)");
            }
        }

        private void SetInjectionAbortedStatus() {
            try {
                if (!Dispatcher.UIThread.CheckAccess())
                {
                    Dispatcher.UIThread.Post(SetInjectionAbortedStatus);
                    return;
                }
                else {
                    // TODO
                    // InjectorCallback(null, new ProgressChangedEventArgs(InjectionHelper.ABORTED, null));
                }
            }
            catch (ObjectDisposedException ex) {
                Logger.Warn(ex.ToString());
            }
        }


        private void TimerTickLookForGrimDawn(object? sender, EventArgs e) {
            var timer = sender as DispatcherTimer;
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "DetectGrimDawnTimer";
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }

            var grimDawnDetector = _serviceProvider.Get<GrimDawnDetector>();
            var grimLocations = grimDawnDetector.GetGrimLocations();
            if (grimLocations.Count > 0) {
                timer?.Stop();
                var gdPath = grimLocations.First();

                // Attempt to force a database update
                _modsDatabaseConfigTab?.ForceDatabaseUpdate(gdPath, string.Empty);

                Logger.InfoFormat("Found Grim Dawn at {0}", gdPath);
            }
        }

        /// <summary>
        /// We've looted some items, so make sure the listview is up to date!
        /// Otherwise people freak out.
        ///
        /// The first ~1700 users did not notice at all, but past that seems its the end of days if items don't appear immediately.
        /// </summary>
        private void ListviewUpdateTrigger() {
            _searchWindow?.UpdateListViewDelayed();
        }

        private void DatabaseLoadedTrigger() {
            _searchWindow?.UpdateInterface();
            _searchWindow?.UpdateListViewDelayed();
            _itemReplicaService?.Reset();
        }


    public void MainWindow_Load()
    {
        if (Thread.CurrentThread.Name == null) {
            Thread.CurrentThread.Name = "UI";
        }

        Logger.Debug("Starting UI initialization");

        // Set version number
        DateTime buildDate = ExceptionReporter.BuildDate;
        statusLabel.Text = statusLabel.Text + $" - {ExceptionReporter.VersionString} from {buildDate.ToString("dd/MM/yyyy")}";
        tsVersionNumber.Text = $"{ExceptionReporter.VersionString}";

        var settingsService = _serviceProvider.Get<SettingsService>();
        ExceptionReporter.EnableLogUnhandledOnThread();
        // TODO
        // SizeChanged += OnMinimizeWindow;

        // Chicken and the egg.. search controller needs browser, browser needs search controllers var.
        var databaseItemDao = _serviceProvider.Get<IDatabaseItemDao>();
        _searchController.JsIntegration.OnRequestSetItemAssociations += (s, evvv) => { (evvv as GetSetItemAssociationsEventArgs).Elements = databaseItemDao.GetItemSetAssociations(); };

        _searchController.Browser = _cefBrowserHandler;
        _searchController.JsIntegration.OnSignalReadiness += Browser_InitializationCompleted;
        _searchController.JsIntegration.OnClipboard += SetItemsClipboard;
        _searchController.JsIntegration.OnDismissNumericFilterBanner += (_, _) => settingsService.GetPersistent().NumericFilterBannerDismissed = true;

        var playerItemDao = _serviceProvider.Get<IPlayerItemDao>();
        var cacher = _serviceProvider.Get<TransferStashServiceCache>();
        _parsingService.OnParseComplete += OnParseComplete;


        var replicaItemDao = _serviceProvider.Get<IReplicaItemDao>();
        var computedItemStatDao = _serviceProvider.Get<IComputedItemStatDao>();
        var transferStashService = new TransferStashService();


        // Load the grim database
        var grimDawnDetector = _serviceProvider.Get<GrimDawnDetector>();
        if (grimDawnDetector.GetGrimLocations().Count == 0) {
            Logger.Warn("Could not find the Grim Dawn install location");
            statusLabel.Text = "Could not find the Grim Dawn install location";

            var timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(10000)
            };
            timer.Tick += TimerTickLookForGrimDawn;
            timer.Start();
        }

        
        var buddyItemDao = _serviceProvider.Get<IBuddyItemDao>();
        var buddySubscriptionDao = _serviceProvider.Get<IBuddySubscriptionDao>();


        _authService = new AuthService(new AuthenticationProvider(settingsService), playerItemDao);
        var onlineSettings = new OnlineSettings(playerItemDao, settingsService, _cefBrowserHandler, buddyItemDao, buddySubscriptionDao);
        onlineHost.Content = onlineSettings;
        _authService.OnAuthCompletion += (sender, args_) => {
            if (((args_ as AuthResultEvent)!).IsAuthorized) {
                onlineSettings.UpdateUi();
            }
            else {
            }
        };

        _modsDatabaseConfigTab = new ModsDatabaseConfig(DatabaseLoadedTrigger, playerItemDao, _parsingService, grimDawnDetector, settingsService, _cefBrowserHandler, databaseItemDao, replicaItemDao, computedItemStatDao);
        modsDatabaseConfig.Content = _modsDatabaseConfigTab;

        var itemTagDao = _serviceProvider.Get<IItemTagDao>();
        var backupService = new BackupService(_authService, playerItemDao, settingsService, _cefBrowserHandler);
        _charBackupService = new CharacterBackupService(settingsService, _authService);
        _backupServiceWorker = new BackupServiceWorker(backupService, _charBackupService);

        // Live sync for "multiple PCs" users: pushes new items/deletions and applies the same
        // events from the user's other machines instantly. The regular backup above remains the
        // source of truth; this only makes updates propagate faster.
        _webSocketSyncService = new WebSocketSyncService(new AuthenticationProvider(settingsService), settingsService, playerItemDao);
        _webSocketSyncService.Start();
        _searchController.JsIntegration.OnRequestBackedUpCharacterList += (_, args) => {
            RequestCharacterListEventArg a = args as RequestCharacterListEventArg;
            a.Characters = _charBackupService.ListBackedUpCharacters();
        };
        _searchController.JsIntegration.OnRequestCharacterDownloadUrl += (_, args) => {
            RequestCharacterDownloadUrlEventArg a = args as RequestCharacterDownloadUrlEventArg;
            if (a.Character != null) {
                a.Url = _charBackupService.GetDownloadUrl(a.Character);
            }
        };

        _searchController.OnSearch += (o, args) => backupService.OnSearch();

        _searchWindow = new SplitSearchWindow(_webView, SetFeedback, playerItemDao, _searchController, itemTagDao, settingsService);
        splitSearchWindow.Content = _searchWindow;

        // TODO browser setup here
        // var browser = _searchWindow.Browser;
        // browser.CoreWebView2InitializationCompleted += Browser_CoreWebView2InitializationCompleted;

        settingsWindow.Content = new SettingsWindow(
                _cefBrowserHandler,
                ListviewUpdateTrigger,
                () => MainTabStrip.SelectedIndex = 0,
                playerItemDao,
                _searchWindow.ModSelectionHandler.GetAvailableModSelection(),
                settingsService,
                itemTagDao,
                _parsingService,
                grimDawnDetector,
                _automaticUpdateChecker
            );

        
        _itemReplicaService = _serviceProvider.Get<ItemReplicaRequesterService>();
        if (GlobalPaths.HasGrimDawnWineUserProfilePath) {
            _itemReplicaService.Start();
        }

        _itemStatPrecomputeService = _serviceProvider.Get<ItemStatPrecomputeService>();
        _itemStatPrecomputeService.Start();

#if !DEBUG
        if (_automaticUpdateChecker.ShouldCheckForUpdates()) {
            _automaticUpdateChecker.CheckForUpdates();
        }
#endif

        if (GlobalPaths.HasGrimDawnWineUserProfilePath) {
            Opened += (_, _) => { StartInjector(); };
        }
        _buddyItemsService = new BuddyItemsService(
            buddyItemDao,
            3 * 60 * 1000,
            settingsService,
            _authService,
            buddySubscriptionDao
        );

        // Start the backup task
        _backupBackgroundTask = new BackgroundTask(new FileBackup(playerItemDao, settingsService));

        if (RuntimeSettings.Language != null) {
            LocalizationLoader.ApplyLanguage(this, RuntimeSettings.Language);
        }

        _messageProcessors.Add(new GenericErrorHandler());
        _messageProcessors.Add(new InjectionAbortedProcessor(SetInjectionAbortedStatus));


        _transferController = new ItemTransferController(
            _cefBrowserHandler,
            SetFeedback,
            playerItemDao,
            transferStashService,
            settingsService
        );
        // TODO
        // Application.AddMessageFilter(new MousewheelMessageFilter());

        if (_authService.CheckAuthentication() == AuthService.AccessStatus.Unauthorized && !settingsService.GetLocal().OptOutOfBackups && playerItemDao.GetNumItems() > 100) {
            var authService = new AuthService(new AuthenticationProvider(settingsService), _serviceProvider.Get<IPlayerItemDao>());
            // TODO
            // new BackupLoginNagScreen(authService, settingsService).Show();
        }

        _searchController.JsIntegration.ItemTransferEvent += TransferItem;

        // TODO
        // new WindowSizeManager(this, settingsService);


        if (settingsService.GetLocal().LanguageCode.Equals("EN", StringComparison.OrdinalIgnoreCase) && !settingsService.GetLocal().HasSuggestedLanguageChange) {
            if (LocalizationLoader.HasSupportedTranslations(grimDawnDetector.GetGrimLocations())) {
                Logger.Debug("A new language pack has been detected, informing end user..");
                var languagePackPicker = new LanguagePackPicker(itemTagDao, playerItemDao, _parsingService, settingsService);
                _ = languagePackPicker.Show(grimDawnDetector.GetGrimLocations(), this);

                settingsService.GetLocal().HasSuggestedLanguageChange = true;
            }
        }


        if (settingsService.GetPersistent().DarkMode) {
            _cefBrowserHandler.SetDarkMode(settingsService.GetPersistent().DarkMode);
        }

        settingsService.GetLocal().OnMutate += delegate(object? o, EventArgs args) { _cefBrowserHandler.SetOnlineBackupsEnabled(!settingsService.GetLocal().OptOutOfBackups); };

        _csvParsingService = new CsvParsingService(playerItemDao, _userFeedbackService, cacher, transferStashService, replicaItemDao);
        _csvFileMonitor!.OnModified += (_, arg) => {
            var csvEvent = arg as CsvFileMonitor.CsvEvent;
            Logger.Debug($"Incoming item file detected: {csvEvent.Filename}");
            _csvParsingService.Queue(csvEvent.Filename, csvEvent.Cooldown);
        };

        _itemReplicaParser = new ItemReplicaParser(replicaItemDao, playerItemDao, _cefBrowserHandler);
        _replicaCsvFileMonitor!.OnModified += (_, arg) => {
            _itemReplicaParser.Enqueue(arg);
            Logger.Debug($"Replica Parser Queue");
        };
        if (GlobalPaths.HasGrimDawnWineUserProfilePath) {
            _itemReplicaParser.Start();
        }


        _csvParsingService.OnItemLooted += async (_, arg) => {
            await _searchWindow.SelectModFilterIfNotSelected();

            var item = arg.Item;
             Logger.Debug($"Item looted: Name='{item.Name}' BaseRecord='{item.BaseRecord}' PrefixRecord='{item.PrefixRecord}' SuffixRecord='{item.SuffixRecord}' MateriaRecord='{item.MateriaRecord}' Seed={item.Seed} Rarity='{item.Rarity}'");
            _searchWindow.UpdateListView(item);

            // Push the freshly looted item to the user's other machines immediately.
            _webSocketSyncService?.SendItems(new List<PlayerItem> { item });
            _cefBrowserHandler.SetIsFirstRun(false);
        };

        // Push in-game transfers (deletions) live, so the item disappears from the user's other
        // machines before it can be transferred a second time and duplicated.
        _transferController.OnItemsTransferredToGame += (_, arg) => {
            _webSocketSyncService?.SendDeletions(arg.CloudIds);
        };

        if (GlobalPaths.HasGrimDawnWineUserProfilePath) {
            _csvFileMonitor.StartMonitoring(GlobalPaths.CsvLocationIngoing, "*.csv");
            _replicaCsvFileMonitor.StartMonitoring(GlobalPaths.CsvReplicaReadLocation, "*.json");
            _csvParsingService.Start();
            Logger.Debug($"Monitoring incoming items: {GlobalPaths.CsvLocationIngoing}");

            var preloadThread= new Thread(_itemReplicaParser.Preload);
            preloadThread.Start();
        }

        Logger.Debug("UI initialization complete");

        _webView.NavigationCompleted += Browser_NavigationCompleted;
        _webView.WebMessageReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Body))
                HandleJavaScriptMessageAsync(args.Body)
                    .ContinueWith(task =>
                    {
                        Logger.Error($"JavaScript message handler failed: {task.Exception}");
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        };

        _webView.EnvironmentRequested += (_, args) =>
        {
            if (args is LinuxWpeWebViewEnvironmentRequestedEventArgs wpe)
            {
                wpe.PreferWebKitGtkInstead = true;
                Logger.Info("Using WebKitGTK");
            }
        };
        _webView.NavigationStarted += (_, args) =>
        {
            if (args.Request is Uri uri)
            {
                Logger.Info($"Navigation requested: {uri}");
                if (!IsAllowedNavigation(uri))
                {
                    Logger.Warn($"BLOCKED navigation: {uri}");
                    args.Cancel = true;
                }
            }
        };
        _webView.NewWindowRequested += (_, args) =>
        {
            Logger.Info($"New window requested: {args.Request}");
            args.Handled = true;
        };
        _webView.AdapterCreated += (_, _) =>
        {
            Logger.Info("AdapterCreated");
            var handle = _webView.TryGetPlatformHandle();
            if (handle is not IGtkWebViewPlatformHandle gtkHandle)
            {
                Logger.Fatal($"Unexpected platform handle: {handle?.GetType().FullName ?? "null"}");
                return;
            }
            var _webViewHandle = gtkHandle.WebKitWebView;
            if (_webViewHandle == IntPtr.Zero)
            {
                Logger.Fatal("IGtkWebViewPlatformHandle returned a null WebKitWebView.");
                return;
            }
            Logger.Debug($"WebKitWebView*: 0x{_webViewHandle.ToInt64():X}");
            WebKitGtkInterop.Register(_webViewHandle, HandleWebResource);
            Logger.Info("IAGrim URI scheme registered.");
            _webView.Navigate(new Uri("iagrim://app/index.html"));
        };
    }

    private Task TransferItem(object? ignored, StashTransferEventArgs args) {
        if (_transferController == null) {
            return Task.CompletedTask;
        }

        return _transferController.TransferItem(args, this);
    }


    private void StartInjector() {
        // TODO ?
        // Start looking for GD processes!
        // _registerWindowDelegate = CustomWndProc;
        // _window = new RegisterWindow("GDIAWindowClass", _registerWindowDelegate);
        //
        // // This prevents a implicit cast to new ProgressChangedEventHandler(func), which would hit the GC and before being used from another thread
        // // Same happens when shutting down, fix unknown
        // _injectorCallbackDelegate = InjectorCallback;

        bool isWine = true;
        string? linuxHackPath = isWine ? GlobalPaths.LinuxHack : null;

        // string dllname = "ItemAssistantHook_x64.dll";
        // _injector = new InjectionHelper(_injectorCallbackDelegate, false, "Grim Dawn", string.Empty, dllname, linuxHackPath);

        // Under Wine, WM_COPYDATA messages don't work, so poll for .msg files instead
        if (isWine) {
            var settingsService = _serviceProvider.Get<SettingsService>();
            if (settingsService.GetLocal().UseDllHookFiles)
                HookFiles.UpdateHookFiles(settingsService.GetLocal().GrimDawnLocation);
            var messageMonitor = new CsvFileMonitor();
            messageMonitor!.OnModified += (_, arg) => {
                var csvEvent = arg as CsvFileMonitor.CsvEvent;
                if (!string.IsNullOrEmpty(csvEvent.Filename))
                    WineMessageHandler(csvEvent.Filename);
            };
            messageMonitor.StartMonitoring(linuxHackPath, "*.msg");
            // Logger.Info("Wine detected, starting file-based message polling");
            // _wineMessageTimer = new DispatcherTimer {
            //     Interval = TimeSpan.FromMilliseconds(500)
            // };
            //
            // _wineMessageTimer.Tick += WineMessagePollTick;
            // _wineMessageTimer.Start();
        }
    }

    /// <summary>
    /// Poll the LinuxHack folder for .msg files written by the injected DLL.
    /// File format (binary, matching COPYDATASTRUCT layout):
    ///   bytes 0-3:  cbData (int32, message type)
    ///   bytes 4-7:  dwData (int32, data length)
    ///   bytes 8+:   lpData (raw data bytes)
    /// Files older than 30 seconds are deleted without reading.
    /// Files younger than 2 seconds are skipped (may still be written).
    /// </summary>
    // private void WineMessagePollTick(object? sender, EventArgs e) {
    private void WineMessageHandler(string file) {
        try {
            // var linuxHackPath = GlobalPaths.LinuxHack;
            // if (!Directory.Exists(linuxHackPath)) return;
            //
            // foreach (var file in Directory.GetFiles(linuxHackPath, "*.msg")) {
                try {
                    var fileAge = DateTime.Now - File.GetLastWriteTime(file);

                    // Stale message, just delete
                    if (fileAge.TotalSeconds > 30) {
                        File.Delete(file);
                        return;
                    }

                    var bytes = File.ReadAllBytes(file);
                    File.Delete(file);

                    if (bytes.Length < 8) {
                        Logger.Warn($"Wine message file too small: {file} ({bytes.Length} bytes)");
                        return;
                    }

                    int type = BitConverter.ToInt32(bytes, 0);
                    int dataLength = BitConverter.ToInt32(bytes, 4);

                    byte[] data;
                    string stringData = string.Empty;

                    if (dataLength > 0 && bytes.Length >= 8 + dataLength) {
                        data = new byte[dataLength];
                        Array.Copy(bytes, 8, data, 0, dataLength);
                        // Try to read as unicode string
                        try {
                            stringData = System.Text.Encoding.Unicode.GetString(data).TrimEnd('\0');
                        }
                        catch {
                            // Not a valid string, that's fine
                        }
                    }
                    else {
                        data = Array.Empty<byte>();
                    }

                    var msg = new RegisterWindow.DataAndType(type, data, stringData);
                    CustomWndProc(msg);
                }
                catch (IOException ex) {
                    // File may be locked, skip and retry next poll
                    Logger.Warn($"Error processing wine message file: {ex.Message}");
                }
                // catch (Exception ex) {
                //     Logger.Warn($"Error processing wine message file: {ex.Message}");
                // }
            // }
        }
        catch (Exception ex) {
            Logger.Warn($"Error handling wine message files: {ex.Message}");
        }
    }

    private void OnParseComplete(object? sender, EventArgs args)
    {
        var cacher = _serviceProvider.Get<TransferStashServiceCache>();
        cacher.Refresh();
        var databaseItemDao = _serviceProvider.Get<IDatabaseItemDao>();
        var isGrimParsed = databaseItemDao.GetRowCount() > 0;
        Dispatcher.UIThread.Post(() => { _cefBrowserHandler.SetIsGrimParsed(isGrimParsed); });
    }

    // TODO
    // #region Tray and Menu
    //
    // /// <summary>
    // /// Minimize to tray
    // /// </summary>
    // /// <param name="sender"></param>
    // /// <param name="e"></param>
    // private void OnMinimizeWindow(object? sender, EventArgs e) {
    //     _usageStatisticsReporter.ResetLastMinimized();
    //     _automaticUpdateChecker.ResetLastMinimized();
    // }
    //
    //
    // private void trayContextMenuStrip_Opening(object sender, CancelEventArgs e) {
    //     e.Cancel = false;
    // }
    //
    // private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
    //     Close();
    // }
    //
    // #endregion Tray and Menu

    private async void SetItemsClipboard(object? ignored, EventArgs args)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetItemsClipboard(ignored, args));
            return;
        }

        if (args is ClipboardEventArg { Text: { } text })
        {
            if (Clipboard != null)
            {
                Logger.Info($"Copying {text.Length} characters to clipboard");
                var data = new DataTransfer();
                data.Add(DataTransferItem.CreateText(text));
                await Clipboard.SetDataAsync(data);
            }
        }

        TooltipHelper.ShowTooltipAtMouse(RuntimeSettings.Language!.GetTag("iatag_copied_clipboard"), _webView);
    }


    private async Task HandleJavaScriptMessageAsync(string json)
    {
        JavaScriptMessage? request;
        try {
            request = JsonConvert.DeserializeObject<JavaScriptMessage>(json, _serializerSettings);
        }
        catch (JsonException ex) {
            Logger.Error($"Invalid JavaScript message: {ex.Message}");
            return;
        }

        if (string.IsNullOrWhiteSpace(request?.Method)) {
            Logger.Error("JavaScript message has no method.");
            return;
        }

        try {
            var result = await _searchController.JsIntegration.HandleMessage(json);

            if (!string.IsNullOrWhiteSpace(request.Id)) {
                await SendJavaScriptResponseAsync(request.Id, result);
            }
        }
        catch (Exception ex) {
            Logger.Error($"Failed handling JavaScript request {request.Method}", ex);
            if (!string.IsNullOrWhiteSpace(request.Id)) {
                await SendJavaScriptResponseAsync(request.Id, null, ex.Message);
            }
        }
    }

    private async Task SendJavaScriptResponseAsync(string requestId, string? result, string? error = null)
    {
        var idJson = JsonConvert.SerializeObject(requestId, _serializerSettings);
        // result is already JSON from JavascriptIntegration for methods
        // such as TransferItem/GetTranslationStrings/etc.
        // var resultJson = result ?? "null";
        // result is a C# string containing JSON.
        // Serialize it as a JavaScript string literal.
        var resultJson = JsonConvert.SerializeObject(result, _serializerSettings);
        var errorJson = JsonConvert.SerializeObject(error, _serializerSettings);

        Logger.Debug($"Sending JavaScript response for {requestId}: {resultJson}");
        await _webView.InvokeScript($$"""window.__coreResponse({{idJson}}, {{resultJson}}, {{errorJson}});""");
    }

    private static bool IsAllowedNavigation(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            return false;

        if (!string.Equals(uri.Scheme, "iagrim", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!string.Equals(uri.Host, "app", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return true;
    }

    private static bool ContainsSymlink(string resourcesRoot, string file)
    {
        var root = Path.GetFullPath(resourcesRoot);
        var current = root;
        var relative = Path.GetRelativePath(root, Path.GetFullPath(file));
        foreach (var part in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (File.Exists(current))
            {
                var info = new FileInfo(current);
                if (info.LinkTarget != null)
                    return true;
            }
            else if (Directory.Exists(current))
            {
                var info = new DirectoryInfo(current);
                if (info.LinkTarget != null)
                    return true;
            }
        }
        return false;
    }

    private static bool IsInside(string root, string path)
    {
        root = Path.GetFullPath(root);

        path = Path.GetFullPath(path);

        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            root += Path.DirectorySeparatorChar;
        }

        return path.StartsWith(root, StringComparison.Ordinal);
    }

    private static void HandleWebResource(IntPtr request, IntPtr userData)
    {
        var uriString = WebKitGtkInterop.GetUri(request);
        Logger.Debug($"Resource request: {uriString}");
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Forbidden"), "text/plain; charset=utf-8");
            return;
        }

        if (!IsAllowedNavigation(uri))
        {
            WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Forbidden"), "text/plain; charset=utf-8");
            return;
        }

        var path = WebKitGtkInterop.GetPath(request);
        var resourcesRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources"));
        var relativePath = Uri.UnescapeDataString(path).TrimStart('/');
        if (relativePath.Contains('\0'))
        {
            WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Forbidden"), "text/plain; charset=utf-8");
            return;
        }

        var file = Path.GetFullPath(Path.Combine(resourcesRoot, relativePath));
        if (!IsInside(resourcesRoot, file))
        {
            WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Forbidden"), "text/plain; charset=utf-8");
            return;
        }

        if (ContainsSymlink(resourcesRoot, file))
        {
            WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Forbidden"), "text/plain; charset=utf-8");
            return;
        }

        if (!File.Exists(file))
        {
            var storageRoot = Path.GetFullPath(GlobalPaths.StorageFolder);
            var storageFile = Path.GetFullPath(Path.Combine(storageRoot, relativePath));
            if (!IsInside(storageRoot, storageFile))
            {
                WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Forbidden"), "text/plain; charset=utf-8");
                return;
            }
            file = storageFile;
        }

        if (!File.Exists(file))
        {
            WebKitGtkInterop.Finish(request, Encoding.UTF8.GetBytes("Not found"), "text/plain; charset=utf-8");
            return;
        }

        Logger.Debug($"Serving: {file}");
        var data = File.ReadAllBytes(file);
        WebKitGtkInterop.Finish(request, data, GetContentType(file));
    }

    private static string GetContentType(string file)
    {
        return Path.GetExtension(file).ToLowerInvariant()
            switch
            {
                ".html" => "text/html; charset=utf-8",
                ".js" => "text/javascript; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                ".map" => "application/json",
                _ => "application/octet-stream"
            };
    }
}
