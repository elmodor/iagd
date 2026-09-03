using Newtonsoft.Json;

namespace IAGrim.Overwrites.HookSettings;

internal sealed class HookSettings
{
    [JsonProperty("local")]
    public HookLocalSettings Local { get; set; } = new();

    [JsonProperty("persistent")]
    public HookPersistentSettings Persistent { get; set; } = new();
}

internal sealed class HookLocalSettings
{
    [JsonProperty("stashToLootFrom")]
    public int StashToLootFrom { get; set; }

    [JsonProperty("stashToDepositTo")]
    public int StashToDepositTo { get; set; }

    [JsonProperty("isGrimDawnParsed")]
    public bool IsGrimDawnParsed { get; set; }
}

internal sealed class HookPersistentSettings
{
    [JsonProperty("isRunningInWine")]
    public bool IsRunningInWine { get; set; }
}
