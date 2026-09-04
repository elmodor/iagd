// using IAGrim.Database.Model;
using IAGrim.Services;
// using IAGrim.Services.ItemReplica;
// using IAGrim.Settings;
// using IAGrim.UI.Controller.dto;
// using IAGrim.UI.Misc.Protocol;
// using IAGrim.Utilities;
using log4net;
// using Newtonsoft.Json;
// using System.Collections.Concurrent;
// using System.Collections.ObjectModel;
// using System.Diagnostics;
// using System.Net;
// using System.Threading;
using System.Collections.Generic;
using IAGrim.Database.Model;
using IAGrim.Services.ItemReplica;
using IAGrim.Settings;
using IAGrim.UI.Controller.dto;

using Avalonia.Controls.Gtk;
using IAGrim.UI.Misc.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading.Tasks;
using IAGrim.Overwrites.IsProgramActive;

namespace IAGrim.UI.Misc.CEF {
    public class CefBrowserHandler(SettingsService settings, Action selectItemsTab) : IUserFeedbackHandler, IBrowserCallbacks, IHelpService {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CefBrowserHandler));
        private readonly ConcurrentQueue<IOMessage> _initializationQueue = new();

        private Func<string, Task>? _invokeScript;

        private volatile bool _isReady;
        private volatile bool _isReadyUi;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public void Initialize(Func<string, Task> invokeScript) {
            _invokeScript = invokeScript;
            Logger.Info("CefBrowserHandler initialized");
        }
        public void SetReady() {
            if (_isReady) {
                return;
            }
            _isReady = true;
            Logger.Info($"WebUI ready, processing {_initializationQueue.Count} queued messages");
            _ = FlushQueueAsync();
        }
        public bool IsReady() => _isReady;

        private async Task FlushQueueAsync() {
            while (_initializationQueue.TryDequeue(out var message)) {
                await SendMessageAsync(message);
            }
        }

        private void SendMessage(IOMessage message) {
            _ = SendMessageAsync(message);
        }

        private async Task SendMessageAsync(IOMessage message) {
            if (_invokeScript == null || !_isReady) {
                Logger.Info($"Queueing WebUI message: {message.Type}");
                _initializationQueue.Enqueue(message);
                return;
            }

            try {
                var json = JsonConvert.SerializeObject(
                    message,
                    _serializerSettings);

                await _invokeScript($"window.message({json});");
            }
            catch (Exception ex) {
                Logger.Error($"Failed to send {message.Type} to WebUI", ex);
            }
        }

        // TODO all
        // private TabControl? _tabControl; // TODO: UGh.. why?
        // private readonly ConcurrentQueue<IOMessage> _initializationQueue = new ConcurrentQueue<IOMessage>();

        // public WebView2? BrowserControl { get; private set; }

        // private volatile bool _isReady;
        // private volatile bool _isReadyUi;
        // public bool IsReady { 
        //     get => _isReady; 
        //     set { 
        //         if (value && !_isReady && BrowserControl != null) {
        //             BrowserControl.Source = new Uri(GetSiteUri());
        //
        //             if (_isReadyUi) {
        //                 Logger.Info("Both UI and browser are ready, processing queue..");
        //                 foreach (var item in _initializationQueue) {
        //                     Logger.Info($"Message: {JsonConvert.SerializeObject(item, _serializerSettings)}");
        //                     SendMessage(item);
        //                 }
        //                 _initializationQueue.Clear();
        //             }
        //             else {
        //                 Logger.Info("Browser signaled readiness, waiting for UI..");
        //                 _isReady = value;
        //                 /*var t = new Thread(new ThreadStart(() => {
        //                     Thread.Sleep(3500);
        //                     _isReady = value;
        //                     Logger.Warn("Did not receive any alert from the WebUI, sending messages anyways..");
        //                     foreach (var item in _initializationQueue) {
        //                         Logger.Info($"Message: {JsonConvert.SerializeObject(item, _serializerSettings)}");
        //                         SendMessage(item);
        //                     }
        //
        //                     _initializationQueue.Clear();
        //                 }));
        //                 t.Start();*/
        //             }
        //
        //             Logger.Info($"There are {_initializationQueue.Count} queued messages");
        //         }
        //     } 
        // }
//
//         private void SendMessage(IOMessage message) {
//             if (BrowserControl?.Parent == null || !_isReadyUi) {
//                 if (message.IsLogHeavy) {
//                     Logger.Warn("Attempted to communicate with the frontend, but browser not yet initialized, queued: " + message.Type.ToString());
//                 }
//                 else {
//                     Logger.Warn("Attempted to communicate with the frontend, but browser not yet initialized, queued: " + message.Type.ToString() + " " + JsonConvert.SerializeObject(message, _serializerSettings));
//                 }
//
//                 _initializationQueue.Enqueue(message);
//                 return;
//             }
//             // window.message({'type':5, 'data':{'items': [], 'replaceExistingItems': true, 'numItemsFound': 0}})
//
//
//
//             if (IsReady && _isReadyUi) {
//                 // Attempting to read the result or anything in a sync way will just stall.
//                 //Logger.Info("Exec: " + JsonConvert.SerializeObject(message, _serializerSettings));
//                 if (BrowserControl.Parent.InvokeRequired) {
//                     BrowserControl.Parent.Invoke((MethodInvoker)delegate {
//                         BrowserControl.ExecuteScriptAsync("window.message(" + JsonConvert.SerializeObject(message, _serializerSettings) + ")");
//                     });
//                 }
//                 else {
//                     BrowserControl.ExecuteScriptAsync("window.message(" + JsonConvert.SerializeObject(message, _serializerSettings) + ")");
//                 }
//
//             }
//             else {
//                 Logger.Warn("Attempting to interact with webview, but not yet ready.");
//                 _initializationQueue.Enqueue(message);
//             }
//
//         }
//
        public void ShowCharacterBackups() {
            SendMessage(new IOMessage { Type = IOMessageType.ShowCharacterBackups });
            selectItemsTab();
        }

        public void ShowHelp(IHelpService.HelpType type) {
            SendMessage(new IOMessage { Type = IOMessageType.ShowHelp, Data = type.ToString() });
            selectItemsTab();
        }

        public void SetCollectionAggregateData(IList<CollectionItemAggregateRow> rows) {
            SendMessage(new IOMessage { Type = IOMessageType.SetAggregateItemData, Data = rows });
        }

        public void ShowLoadingAnimation(bool visible) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.IsLoading, Value = visible } });
        }

//         bool IBrowserCallbacks.IsReady() {
//             return IsReady;
//         }
//
        /// <summary>
        /// Set the current batch of items
        /// </summary>
        /// <param name="items">The current batch</param>
        /// <param name="numItemsFound">The number of items found, total (eg 3000 found, but batch has 64)</param>
        public void SetItems(List<List<JsonItem>> items, int numItemsFound, bool hasMore, bool numItemsApproximate = false) {
            Logger.Debug($"Sending {items.Count} item groups / {numItemsFound} total results to WebUI");
            foreach (var itemGroup in items)
            {
                foreach (var item in itemGroup)
                {
                    Logger.Debug($"WebUI item data: {Newtonsoft.Json.JsonConvert.SerializeObject(item)}");
                }
            }
            SendMessage(new IOMessage {
                Type = IOMessageType.SetItems,
                Data = new IOMessageSetItems {
                    NumItemsFound = numItemsFound,
                    NumItemsApproximate = numItemsApproximate,
                    Items = items,
                    ReplaceExistingItems = true,
                    HasMore = hasMore,
                }
            });
        }

        public void SetCollectionItems(IList<CollectionItem> items, bool isHardcore) {
            SendMessage(new IOMessage {
                Type = IOMessageType.SetCollectionItems,
                Data = new { Items = items, IsHardcore = isHardcore }
            });
        }

        // numItemsFound < 0 means "no update to the displayed total" (the common append case). A non-negative
        // value updates it - used when the real total was deferred on the first page and later computed once
        // the user paginated past it.
        public void AddItems(List<List<JsonItem>> items, bool hasMore, int numItemsFound = -1) {
            Logger.Debug($"Sending {items.Count} item groups / {numItemsFound} total results to WebUI");
            foreach (var itemGroup in items)
            {
                foreach (var item in itemGroup)
                {
                    Logger.Debug($"WebUI item data: {Newtonsoft.Json.JsonConvert.SerializeObject(item)}");
                }
            }
            SendMessage(new IOMessage {
                Type = IOMessageType.SetItems,
                Data = new IOMessageSetItems {
                    Items = items,
                    ReplaceExistingItems = false,
                    HasMore = hasMore,
                    NumItemsFound = numItemsFound,
                }
            });
        }
//
//
//         private string GetSiteUri() {
// #if DEBUG
//             /*
//             var client = new WebClient();
//
//             try {
//                 Logger.Debug("Checking if NodeJS is running...");
//                 client.DownloadString("http://localhost:3000/");
//                 Logger.Debug("NodeJS running");
//                 return "http://localhost:3000/";
//             }
//             catch (System.Net.WebException) {
//                 Logger.Debug("NodeJS not running, defaulting to standard view");
//             }*/
// #endif
//             return "https://app/index.html";
//         }
//
//         public void InitializeChromium(Microsoft.Web.WebView2.WinForms.WebView2 browserControlView2, JavascriptIntegration bindable, TabControl tabControl) {
//             try {
//                 _tabControl = tabControl;
//                 this.BrowserControl = browserControlView2;
//
//                 BrowserControl.CoreWebView2.AddHostObjectToScript("core", bindable);
//                 bindable.OnSignalReadiness += (sender, args) => {
//                     _isReadyUi = true;
//                     Logger.Info("UI signalled readiness");
//
//                     if (_isReady) {
//                         Logger.Info("Both UI and browser are ready, processing queue..");
//                         foreach (var item in _initializationQueue) {
//                             Logger.Info($"Message: {item.Type.ToString() + (item.IsLogHeavy ? "" : " "+ JsonConvert.SerializeObject(item, _serializerSettings))}");
//                             SendMessage(item);
//                         }
//                         _initializationQueue.Clear();
//                     }
//                 };
//
//             }
//             catch (System.IO.FileNotFoundException ex) {
//                 MessageBox.Show("Error \"File Not Found\" loading Chromium, did you forget to install Visual C++ runtimes?\n\nvc_redist86 in the IA folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 Logger.Warn(ex.Message);
//                 Logger.Warn(ex.StackTrace);
//                 throw;
//             }
//             catch (IOException ex) {
//                 MessageBox.Show("Error loading Chromium, You may be lacking the proper Visual C++ runtimes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 Process.Start("https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022");
//                 Logger.Warn(ex.Message);
//                 Logger.Warn(ex.StackTrace);
//                 throw;
//             }
//             catch (Exception ex) {
//                 MessageBox.Show("Unknown error loading Chromium, please see log file for more information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                 Logger.Warn(ex.Message);
//                 Logger.Warn(ex.StackTrace);
//                 throw;
//             }
//         }
        public void SetDarkMode(bool enabled) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.DarkMode, Value = enabled } });
        }

        public void SetHideItemSkills(bool enabled) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.HideItemSkills, Value = enabled } });
        }

        public void SetIsGrimParsed(bool enabled) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.GrimDawnIsParsed, Value = enabled } });
        }

        public void SetIsFirstRun(bool val) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.FirstRun, Value = val } });
        }

        public void SetEasterEggMode() {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.EasterEggMode, Value = true } });
        }

        public void SetShowNumericFilterBanner(bool val) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.ShowNumericFilterBanner, Value = val } });
        }

        public void SetGdSeasonMode() {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.GdSeasonError, Value = true } });
        }

        public void ShowModFilterWarning(int numOtherItems) {
            SendMessage(new IOMessage { Type = IOMessageType.ShowModFilterWarning, Data = numOtherItems });
        }

        public void SignalCloudIconChange(IList<long> playerItemIds) {
            SendMessage(new IOMessage { Type = IOMessageType.UpdateCloudIconStatus, Data = new IOMessageCloudIconStateChange { Ids = playerItemIds } });
        }
        public void SignalReplicaStatChange(long playerItemId, IList<ItemStatInfo> stats) {
            SendMessage(new IOMessage {
                Type = IOMessageType.UpdateItemStats,
                Data = new IOMessageSetReplicaStats { Id = playerItemId, ReplicaStats = stats }
            }
            );
        }



        public void SetOnlineBackupsEnabled(bool enabled) {
            SendMessage(new IOMessage { Type = IOMessageType.SetState, Data = new IOMessageStateChange { Type = IOMessageStateChangeType.ShowCloudIcon, Value = enabled } });
        }


        public void ShowMessage(string message, UserFeedbackLevel level = UserFeedbackLevel.Info, string? helpUrl = null) {
            string levelLowercased = level.ToString().ToLowerInvariant();
            var m = message.Replace("\n", "\\n").Replace("'", "\\'");
            if (!string.IsNullOrEmpty(message)) {
                var autoDismissMessage = IsProgramActive.IsActive() || settings.GetPersistent().AutoDismissNotifications;
                var ret = new Dictionary<string, string> {
                    {"message", m},
                    {"type", levelLowercased},
                    {"fade", autoDismissMessage ? "true" : "false"},
                };

                SendMessage(new IOMessage { Type = IOMessageType.ShowMessage, Data = ret });
            }
        }
    }
}
