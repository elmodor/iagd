using DataAccess;
using StatTranslator;
using Xunit;

namespace StatTranslator.Tests;

public class StatManagerTests {
    [Fact]
    public void ProcessStats_TranslatesStatsCorrectly() {
        var language = new EnglishLanguage(new Dictionary<string, string>());
        var statManager = new StatManager(language);
        var stats = new HashSet<IItemStat> {
            new ItemStat { Record = "records/items/test_item.dbr", Stat = "characterLife", Value = 125 }, new ItemStat { Record = "records/items/test_item.dbr", Stat = "characterOffensiveAbility", Value = 42 },
            new ItemStat { Record = "records/items/test_item.dbr", Stat = "offensiveFireMin", Value = 10 }, new ItemStat { Record = "records/items/test_item.dbr", Stat = "offensiveFireMax", Value = 20 }
        };
        var translated = statManager.ProcessStats(stats, TranslatedStatType.BODY);
        Assert.Equal(3, translated.Count);

        var output = translated.Select(stat => stat.ToString()).ToHashSet();
        Assert.Contains("10-20 Fire Damage", output);
        Assert.Contains("+125 Health", output);
        Assert.Contains("+42 Offensive Ability", output);
    }

    [Fact]
    public void ProcessStats_WithNoStats_ReturnsNoStats() {
        var language = new EnglishLanguage(new Dictionary<string, string>());
        var statManager = new StatManager(language);
        var stats = new HashSet<IItemStat>();
        var translated = statManager.ProcessStats(stats, TranslatedStatType.BODY);
        Assert.Empty(translated);
    }

    [Fact]
    public void ProcessStats_IgnoresUnknownStats() {
        var language = new EnglishLanguage(new Dictionary<string, string>());
        var statManager = new StatManager(language);
        var stats = new HashSet<IItemStat> { new ItemStat { Record = "records/items/test_item.dbr", Stat = "thisStatDoesNotExist", Value = 123 } };
        var translated = statManager.ProcessStats(stats, TranslatedStatType.BODY);

        Assert.Empty(translated);
    }
}
