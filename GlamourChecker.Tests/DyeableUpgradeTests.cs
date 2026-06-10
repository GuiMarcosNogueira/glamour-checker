using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlamourChecker.Core;
using Xunit;

namespace GlamourChecker.Tests;

public class DyeableUpgradeTests
{
    private class FakeModelScannerDyeable : MockModelScanner
    {
        public override ulong GetModelId(uint itemId)
        {
            if (itemId == 100) return 10001; // Non-dyeable version
            if (itemId == 200) return 10002; // Dyeable version
            return 0;
        }

        public override ulong GetSharedModelId(uint itemId)
        {
            if (itemId == 100 || itemId == 200) return 9999; // Shared base model
            return 0;
        }

        public override bool IsDyeable(uint itemId)
        {
            if (itemId == 100) return false;
            if (itemId == 200) return true;
            return false;
        }
    }

    [Fact]
    public void GetUnstoredItems_FlagsUpgrade_WhenDresserHasNonDyeableAndBagHasDyeable()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 200 }; // Dyeable in bag
        memoryFake.DresserItems[0] = 100; // Non-dyeable in dresser

        var config = new Configuration();
        var scanner = new FakeModelScannerDyeable();
        var watcher = new InventoryWatcher(scanner, config, memoryFake);

        var result = watcher.GetUnstoredItemsInBags();

        Assert.Single(result);
        Assert.True(result[0].IsDyeableUpgrade);
        Assert.Equal(200u, result[0].ItemId);
    }

    [Fact]
    public void GetUnstoredItems_HidesItem_WhenDresserHasDyeableAndBagHasNonDyeable()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 100 }; // Non-Dyeable in bag
        memoryFake.DresserItems[0] = 200; // Dyeable in dresser

        var config = new Configuration();
        var scanner = new FakeModelScannerDyeable();
        var watcher = new InventoryWatcher(scanner, config, memoryFake);

        var result = watcher.GetUnstoredItemsInBags();

        Assert.Empty(result); // Should be hidden completely
    }

    [Fact]
    public void ScanDresser_PopulatesDresserSharedModels()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.DresserItems[0] = 100;
        memoryFake.DresserItems[1] = 200; // Both non-dyeable and dyeable

        var config = new Configuration();
        var scanner = new FakeModelScannerDyeable();
        var watcher = new InventoryWatcher(scanner, config, memoryFake);

        watcher.ScanDresserAndArmoire();

        // It should take the true value because 200 is dyeable
        Assert.True(config.DresserSharedModels.ContainsKey(9999));
        Assert.True(config.DresserSharedModels[9999]);
    }
}
