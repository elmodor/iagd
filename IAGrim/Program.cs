using Avalonia;
using System;
using System.Reflection;
using EvilsoftCommons.SingleInstance;
using EvilsoftCommons.Exceptions;
using IAGrim.Backup.Cloud;
using IAGrim.Services;
using IAGrim.Settings;
using IAGrim.UI;
using IAGrim.UI.Misc.CEF;
using IAGrim.Utilities;
using StatTranslator;
using log4net;
using log4net.Config;
using log4net.Appender;

using IAGrim.Database;
using IAGrim.Database.Interfaces;
using IAGrim.Database.Migrations;
using NHibernate.SqlCommand;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Overwrites.MessageBox;
using IAGrim.Overwrites.LinuxConfig;

namespace IAGrim.Linux;

class Program
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
    private static readonly StartupService StartupService = new StartupService();

    private static void LoadUuid(SettingsService settings) {
        var uuid = settings.GetPersistent().UUID;

        if (string.IsNullOrEmpty(uuid)) {
            uuid = Guid.NewGuid().ToString().Replace("-", "");
            settings.GetPersistent().UUID = uuid;
        }

        RuntimeSettings.Uuid = uuid;
        ExceptionReporter.Uuid = uuid;
    }

    /// <summary>
    /// Builds the session factory ahead of the first caller that needs it.
    ///
    /// Purely an overlap: a failure here is swallowed, because <see cref="SessionFactory"/> caches it on the
    /// shared Lazy and rethrows it to whoever asks next -- on their thread, with their error handling intact.
    /// </summary>
    private static void WarmUpDatabase() {
        var thread = new Thread(() => {
            ExceptionReporter.EnableLogUnhandledOnThread();

            try {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                SessionFactory.Warmup();
                Logger.Info($"[timing] Session factory warmup took {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex) {
                Logger.Debug($"Session factory warmup failed, deferring to the first real caller: {ex.Message}");
            }
        });

        // Never hold up process exit; the second-instance path bails out long before this finishes.
        thread.IsBackground = true;
        thread.Name = "DbWarmup";
        thread.Start();
    }


    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
        if (Thread.CurrentThread.Name == null) {
            Thread.CurrentThread.Name = "Main";
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
        }

        var logDirectory = Path.Combine(LinuxConfig.DataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        var configPath = Path.Combine(AppContext.BaseDirectory, "Log4net.config");
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo(configPath));
        foreach (var appender in logRepository.GetAppenders()) {
            if (appender is FileAppender fileAppender) {
                fileAppender.File = Path.Combine(logDirectory, "log.txt");
                fileAppender.ActivateOptions();
            }
        }

        Logger.Info("Starting IA:GD Linux..");
        ExceptionReporter.UrlStats = "https://webstats.evilsoft.net/report/iagd";
        SQLitePCL.Batteries.Init();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // ApplicationConfiguration.Initialize();

        // Compiling the NHibernate mappings is ~0.5s of work that nothing before Run() depends on, so it runs
        // alongside the version checks and the diagnostics dump instead of after them. The factory is a shared
        // Lazy, so Migrate() below either finds it built or blocks until it is.
        WarmUpDatabase();

        Logger.Info("Starting exception monitor for bug reports..");
        Logger.Debug("Anonymous usage statistics can be seen at https://webstats.evilsoft.net/iagd");
        ExceptionReporter.EnableLogUnhandledOnThread();

        Uris.Initialize(Uris.EnvCloud);
        StartupService.Init();

        if (DiagnosticsReport.IsRequested(args)) {
            var reportPath = DiagnosticsReport.WriteAndOpen();
            MessageBox.Show(
                reportPath != null
                    ? $"Diagnostics written to:\n{reportPath}\n\nAttach this file to your bug report."
                    : "The diagnostics report could not be written. See the log for details.",
                "Item Assistant diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LogManager.Shutdown();
            System.Environment.Exit(0);
        }

        // Into the ordinary log as well, so every log file a user sends already carries it.
        DiagnosticsReport.LogAtStartup();

#if DEBUG
            Uris.Initialize(Uris.EnvLocalDev);
#endif

            // Prevent running in RELEASE mode by accident
            // And thus risking the live database
#if !DEBUG
            if (System.Diagnostics.Debugger.IsAttached) {
                Logger.Fatal("Debugger attached, please run in DEBUG mode");
                return;
            }
#endif

        ItemHtmlWriter.CopyMissingFiles();

        Guid guid = new Guid("{F3693953-C090-4F93-86A2-B98AB96A9368}");
        var safeMode = StartupService.IsSafeMode(args);
        using (SingleInstance singleInstance = new SingleInstance(guid)) {
            if (singleInstance.IsFirstInstance) {
                Logger.Info("Calling run..");
                singleInstance.ListenForArgumentsFromSuccessiveInstances();
                // Application.EnableVisualStyles();
                // Application.SetCompatibleTextRenderingDefault(false);
                Logger.Info("Visual styles enabled..");
                Run(args);
            }
            else {
                // Nothing listens for arguments from successive instances, so a safe mode reset here would just be overwritten by the running instance when it stores its window position on exit.
                if (safeMode) {
                    Logger.Info("Safe mode requested, but IA is already running.");
                    MessageBox.Show(
                        "Item Assistant is already running, look for the icon in the system tray next to the clock.\n\n"
                        + "Close the running instance and then start safe mode again.",
                        "Item Assistant is already running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Ask the running instance to show itself, otherwise starting IA a second time looks like
                // nothing happened at all: the window may well be hidden away in the system tray.
                // TODO
                // ShowExistingInstanceMessage.Notify();
                MessageBox.Show(
                    "Item Assistant is already running, look for the icon in the system tray next to the clock.",
                    "Item Assistant is already running", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Logger.Info("Already has an instance of IA Running, exiting..");
            }
        }

        Logger.Info("IA Exited");
        LogManager.Shutdown();
        System.Environment.Exit(0);
    }

    private static void DumpTranslationTemplate() {
        try {
            var translationsDir = Path.Combine(AppContext.BaseDirectory, "Resources/translations");
            translationsDir = Path.GetFullPath(translationsDir);
            Logger.Debug($"Translations directory: {translationsDir}");
            if (!Directory.Exists(translationsDir)) {
                Logger.Debug($"Translations directory not found: {translationsDir}");
                return;
            }

            var english = new EnglishLanguage(new Dictionary<string, string>());
            var englishEntries = english.Stats;

            foreach (var filePath in Directory.GetFiles(translationsDir, "*.txt")) {
                var lines = File.ReadAllLines(filePath).ToList();
                var existingKeys = new HashSet<string>();

                foreach (var line in lines) {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    var eqIndex = trimmed.IndexOf('=');
                    if (eqIndex > 0) {
                        existingKeys.Add(trimmed.Substring(0, eqIndex));
                    }
                }

                var missingKeys = englishEntries.Keys
                    .Where(k => !existingKeys.Contains(k))
                    .OrderBy(k => k)
                    .ToList();

                if (missingKeys.Count > 0) {
                    lines.Add("");
                    lines.Add("# Missing translations (English defaults)");
                    foreach (var key in missingKeys) {
                        lines.Add($"{key}={englishEntries[key].Replace("\n", "\\n")}");
                    }

                    File.WriteAllLines(filePath, lines);
                    Logger.Debug($"Added {missingKeys.Count} missing keys to {Path.GetFileName(filePath)}");
                }
            }
        }
        catch (Exception ex) {
            Logger.Debug("Error syncing translation files", ex);
        }
    }

    private static void Run(string[] args) {
        var startupTimer = System.Diagnostics.Stopwatch.StartNew();
        void Timed(string step) {
            Logger.Info($"[timing] {step} took {startupTimer.ElapsedMilliseconds} ms");
            startupTimer.Restart();
        }

        var factory = new SessionFactory();
        Logger.Debug("Executing DB migrations..");
        new MigrationHandler(factory).Migrate();
        Timed("DB migrations");

        var serviceProvider = ServiceProvider.Initialize();
        Timed("ServiceProvider.Initialize");

        var settingsService = serviceProvider.Get<SettingsService>();

        // Must happen before the main window is created, it reads these settings on construction.
        if (StartupService.IsSafeMode(args)) {
            StartupService.ResetWindowSettings(settingsService);
            Timed("Safe mode reset");
        }

        var databaseItemDao = serviceProvider.Get<IDatabaseItemDao>();
        RuntimeSettings.InitializeLanguage(settingsService.GetLocal().LanguageCode, databaseItemDao.GetTagDictionary());
        Timed("InitializeLanguage");
#if DEBUG
        DumpTranslationTemplate();
        Timed("DumpTranslationTemplate");
#endif

        Logger.Debug("Loading UUID");
        LoadUuid(settingsService);
        Timed("LoadUuid");
        startupTimer.Stop();

        App.StartupContext = new AppStartupContext {
            ServiceProvider = serviceProvider
        };

        StartupService.PrintStartupInfo(factory, settingsService);

        // Self-heal a WAL that bloated from a previous unclean shutdown (e.g. a crash or the
        // debugger being stopped). Runs off the UI thread so it never delays the window.
        System.Threading.Tasks.Task.Run(() => factory.Checkpoint());

        // Application.Run(_mw);

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Truncate the WAL back into the main db on a clean exit so the next launch starts fast.
        factory.Checkpoint();

        Logger.Info("Application ended.");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
