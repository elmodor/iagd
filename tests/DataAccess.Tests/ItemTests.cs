using DataAccess;
using System.Linq;

namespace DataAccess.Tests;

public class ItemTests {
    [Fact]
    public void Item_CanStoreStats() {
        var stat1 = new ItemStat { Id = 123, Record = "records/items/test_item.dbr", Stat = "itemCost", Value = 150.0f, TextValue = null };
        var stat2 = new ItemStat { Id = 124, Record = "records/items/test_item.dbr", Stat = "itemName", Value = 0, TextValue = "Test Item" };
        var item = new Item { Record = "records/items/test_item.dbr", Stats = new List<IItemStat> { stat1, stat2 } };
        Assert.Equal("records/items/test_item.dbr", item.Record);
        Assert.NotNull(item.Stats);
        Assert.Equal(2, item.Stats.Count);
        var stats = item.Stats.ToList();
        Assert.Equal("itemCost", stats[0].Stat);
        Assert.Equal(150.0f, stats[0].Value);
        Assert.Equal("itemName", stats[1].Stat);
        Assert.Equal("Test Item", stats[1].TextValue);
    }

    [Fact]
    public void Item_CanStoreRecord() {
        var item = new Item { Record = "records/items/test_item.dbr" };
        Assert.Equal("records/items/test_item.dbr", item.Record);
    }

    [Fact]
    public void Item_CanStoreMultipleStats() {
        var item = new Item { Stats = new List<IItemStat> { new ItemStat { Id = 1, Stat = "stat1" }, new ItemStat { Id = 2, Stat = "stat2" } } };
        Assert.NotNull(item.Stats);
        Assert.Equal(2, item.Stats.Count);
    }

    [Fact]
    public void ItemStat_CanStoreTextValue() {
        var stat = new ItemStat { TextValue = "Test Item" };
        Assert.Equal("Test Item", stat.TextValue);
    }
}
