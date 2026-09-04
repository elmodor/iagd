using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IAGrim.Settings.Dto;
using IAGrim.Utilities;

namespace IAGrim.Overwrites.HookSettings;

public static class HookSettingsWriter
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Culture = System.Globalization.CultureInfo.InvariantCulture,
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
    };

    public static void Persist(LocalSettings local, PersistentSettings persistent)
    {
        if (!GlobalPaths.HasGrimDawnWineUserProfilePath) {
            return;
        }
        var hookSettings = new HookSettings {
            Local = new HookLocalSettings {
                StashToLootFrom = local.StashToLootFrom,
                StashToDepositTo = local.StashToDepositTo,
                IsGrimDawnParsed = local.IsGrimDawnParsed
            },
            Persistent = new HookPersistentSettings {
                IsRunningInWine = persistent.IsRunningInWine
            }
        };

        var hookSettingsFile = Path.Combine(GlobalPaths.CoreFolder, "settings.json");
        var directory = Path.GetDirectoryName(hookSettingsFile)!;
        Directory.CreateDirectory(directory);
        string json = JsonConvert.SerializeObject(hookSettings, Formatting.Indented, Settings);
        File.WriteAllText(hookSettingsFile, json);
    }
}
