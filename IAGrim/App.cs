using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Services;
using IAGrim.Database.Interfaces;
using IAGrim.Settings;
using IAGrim.Utilities;
using StatTranslator;

using log4net;

namespace IAGrim.Linux;

public sealed class AppStartupContext
{
    public required ServiceProvider ServiceProvider { get; init; }
}

public partial class App : Application
{
    public static AppStartupContext? StartupContext { get; set; }
    private static readonly ILog Logger = LogManager.GetLogger(typeof(App));

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startup = StartupContext ?? throw new InvalidOperationException("Startup context not initialized.");

            var startupTimer = System.Diagnostics.Stopwatch.StartNew();
            void Timed(string step) {
                Logger.Info($"[timing] {step} took {startupTimer.ElapsedMilliseconds} ms");
                startupTimer.Restart();
            }
            var serviceProvider = startup.ServiceProvider;
            var itemTagDao = serviceProvider.Get<IItemTagDao>();
            var databaseItemStatDao = serviceProvider.Get<IDatabaseItemStatDao>();
            var itemSkillDao = serviceProvider.Get<IItemSkillDao>();
            var databaseItemDao = serviceProvider.Get<IDatabaseItemDao>();
            var settingsService = serviceProvider.Get<SettingsService>();
            ParsingService parsingService = new ParsingService(itemTagDao, string.Empty, databaseItemDao, databaseItemStatDao, itemSkillDao, settingsService.GetLocal().LanguageCode);

            // Before the main window exists: this is modal, and it may reload the language.
            var grimDawnDetector = serviceProvider.Get<GrimDawnDetector>();
            StartupService.PerformGrimWineUserProfilePathCheck(grimDawnDetector, settingsService);
            var autoParsed = StartupService.PerformMissingExpansionDataCheck(
                parsingService,
                databaseItemDao,
                serviceProvider.Get<IPlayerItemDao>(),
                grimDawnDetector,
                settingsService
            );
            Timed("PerformMissingExpansionDataCheck");

            // Only if the parse above didn't already run, it parses in the current language anyway.
            if (!autoParsed) {
                autoParsed = StartupService.PerformLanguageChangeCheck(
                    parsingService,
                    databaseItemDao,
                    serviceProvider.Get<IPlayerItemDao>(),
                    grimDawnDetector,
                    settingsService
                );
                Timed("PerformLanguageChangeCheck");
            }

            // An automatic parse already queued a full icon extraction, no need to scan the arc files twice.
            if (!autoParsed) {
                StartupService.PerformIconCheck(grimDawnDetector, settingsService);
            }
            startupTimer.Stop();

            // TODO: Offload to the new language loader
            if (RuntimeSettings.Language is EnglishLanguage language) {
                foreach (var tag in itemTagDao.GetClassItemTags()) {
                    if (tag.Tag == null || tag.Name == null) {
                        continue;
                    }
                    language.SetTagIfMissing(tag.Tag, tag.Name);
                }
            }

            Logger.Info("Checking for database updates..");



            // _mw.Visible = false;
            // TODO ?
            // if (new DonateNagScreen(settingsService).CanNag)
            //     Application.Run(new DonateNagScreen(settingsService));

            Logger.Info("Running the main application..");

            StartupService.PerformGrimUpdateCheck(settingsService);

            desktop.MainWindow = new MainWindow(startup.ServiceProvider, parsingService);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
