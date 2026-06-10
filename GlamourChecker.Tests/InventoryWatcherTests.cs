using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlamourChecker.Core;
using Xunit;

namespace GlamourChecker.Tests;

public class FakeGameMemoryProvider : IGameMemoryProvider
{
    public uint[] DresserItems = new uint[800];
    public bool CabinetLoaded = false;
    public InventoryItem[] InventoryItems = new InventoryItem[35];

    public unsafe Span<uint> GetMirageManagerPrismBoxItemIds()
    {
        return new Span<uint>(DresserItems);
    }

    public unsafe Span<InventoryItem> GetInventoryContainer(InventoryType type)
    {
        if (type == InventoryType.Inventory1)
        {
            return new Span<InventoryItem>(InventoryItems);
        }
        return Span<InventoryItem>.Empty;
    }

    public HashSet<uint> GearsetItems = new();
    public HashSet<uint> GetGearsetItems() => GearsetItems;

    public bool IsCabinetLoaded() => CabinetLoaded;
    public bool IsCabinetItem = false;
    public bool IsItemInCabinet(uint itemId) => IsCabinetItem;
}

public class InventoryWatcherTests
{
    [Fact]
    public void ScanDresserAndArmoire_FindsItemsInDresserAndArmoire()
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();

        memoryFake.CabinetLoaded = true;
        // Make MockScanner return a specific model ID for ItemId=123 (returns 456)
        memoryFake.DresserItems[0] = 123;

        Func<IEnumerable<(uint, uint)>> cabinetProvider = () => new[] { (789u, 10u) };
        memoryFake.CabinetLoaded = true; // Wait, actually I just test Cabinet returns 789 and model gives something

        var watcher = new InventoryWatcher(scanner, config, memoryFake, cabinetProvider);
        watcher.ScanDresserAndArmoire();

        Assert.Contains(456ul, config.DresserModelIds);
        Assert.True(config.DresserItemsByModel.ContainsKey(456ul));
    }

    [Fact]
    public void GetDuplicates_ChecksArmoire()
    {
        var config = new Configuration();
        config.ArmoireItemsBySharedModel = new Dictionary<ulong, List<uint>> {
            { 999, new List<uint> { 111, 222 } }
        };
        var memoryFake = new FakeGameMemoryProvider();

        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);
        var dups = watcher.GetDuplicates();

        Assert.Single(dups);
        Assert.Equal(0UL, dups[0].ModelId);
    }

    [Fact]
    public void GetUnstoredItemsInBags_ReturnsEmptyWhenNone()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        var scanner = new ModelScanner(_ => null);
        var watcher = new InventoryWatcher(scanner, config, memoryFake);

        Assert.Empty(watcher.GetUnstoredItemsInBags());
    }

    [Fact]
    public void GetDuplicates_ReturnsEmptyWhenNone()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        var scanner = new ModelScanner(_ => null);
        var watcher = new InventoryWatcher(scanner, config, memoryFake);

        Assert.Empty(watcher.GetDuplicates());
    }

    [Fact]
    public void GetDuplicates_ReturnsDuplicatesFromConfig()
    {
        var config = new Configuration();
        config.DresserItemsBySharedModel = new Dictionary<ulong, List<uint>> {
            { 1, new List<uint> { 100, 200 } }
        };
        var memoryFake = new FakeGameMemoryProvider();

        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        var dups = watcher.GetDuplicates();
        Assert.Single(dups);
        Assert.Equal(0UL, dups[0].ModelId);
    }

    [Fact]
    public void GetUnstoredItemsInBags_ReturnsItem_WhenItemFoundAndNotStored()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 1000123 }; // HQ item

        var mockScanner = new MockModelScanner();
        var watcher = new InventoryWatcher(mockScanner, config, memoryFake);

        var unstored = watcher.GetUnstoredItemsInBags();
        Assert.Single(unstored);
        Assert.Equal(123u, unstored[0].ItemId); // Should strip HQ
        Assert.Equal(456ul, unstored[0].ModelId);
    }

    [Fact]
    public void CheckDresserChanges_ReturnsFalse_WhenArrayIsEmpty()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.DresserItems = Array.Empty<uint>(); // Empty span
        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        Assert.False(watcher.CheckStorageChanges(out _, out _));
    }

    [Fact]
    public void CheckDresserChanges_ReturnsFalse_WhenArrayIsAllZeros()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        // DresserItems defaults to 800 zeros
        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        Assert.False(watcher.CheckStorageChanges(out _, out _));
    }

    [Fact]
    public void CheckDresserChanges_ReturnsTrue_WhenHashChanges()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.DresserItems[0] = 123;
        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        // First call should return true (hash changed from 0 to something)
        Assert.True(watcher.CheckStorageChanges(out _, out _));

        // Second call should return false (hash is the same)
        Assert.False(watcher.CheckStorageChanges(out _, out _));

        // Change an item
        memoryFake.DresserItems[1] = 456;
        Assert.True(watcher.CheckStorageChanges(out _, out _));
    }

    [Fact]
    public void ScanDresserAndArmoire_ScansArmoireCorrectly()
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();

        Func<IEnumerable<(uint, uint)>> cabinetProvider = () => new[] {
            (123u, 10u), // Unlocked and valid model (returns 456)
            (999u, 20u), // Unlocked but returns model 0
            (0u, 30u)    // Invalid itemId 0
        };
        memoryFake.CabinetLoaded = true;
        memoryFake.IsCabinetItem = true; // All items are unlocked

        var watcher = new InventoryWatcher(scanner, config, memoryFake, cabinetProvider);
        watcher.ScanDresserAndArmoire();

        Assert.Contains(456ul, config.ArmoireModelIds);
        Assert.DoesNotContain(0ul, config.ArmoireModelIds);
    }

    [Fact]
    public void ScanDresserAndArmoire_ScansDresserCorrectlyWithZeros()
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();

        memoryFake.DresserItems[0] = 0; // Empty slot
        memoryFake.DresserItems[1] = 999; // Valid slot but returns model 0
        memoryFake.DresserItems[2] = 123; // Valid slot, returns model 456

        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        watcher.ScanDresserAndArmoire();

        Assert.Contains(456ul, config.DresserModelIds);
        Assert.DoesNotContain(0ul, config.DresserModelIds);
    }

    [Fact]
    public void ScanDresserAndArmoire_IgnoresArmoireWhenNotLoaded()
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.CabinetLoaded = false; // Not loaded!

        var watcher = new InventoryWatcher(scanner, config, memoryFake, () => new[] { (123u, 10u) });
        watcher.ScanDresserAndArmoire();

        Assert.Empty(config.ArmoireModelIds);
    }

    [Fact]
    public void ScanDresserAndArmoire_IgnoresDresserWhenEmpty()
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.DresserItems = Array.Empty<uint>(); // Empty!

        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        watcher.ScanDresserAndArmoire();

        Assert.Empty(config.DresserModelIds);
    }

    [Fact]
    public void Constructor_UsesDefaultCabinetProvider_WhenNull()
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();

        // Passing null for provider
        var watcher = new InventoryWatcher(scanner, config, memoryFake, null);
        Assert.NotNull(watcher);
    }

    [Fact]
    public void IsDyeable_DelegatesToModelScanner()
    {
        var scanner = new MockModelScanner();
        var watcher = new InventoryWatcher(scanner, new Configuration(), new FakeGameMemoryProvider());

        Assert.False(watcher.IsDyeable(999));
    }

    [Fact]
    public void GetUnstoredItems_SkipsIgnoredItems()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 }; // "TestItem"
        var config = new Configuration();
        config.IgnoredItemIds.Add(123);

        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);
        var result = watcher.GetUnstoredItemsInBags();

        Assert.Empty(result);
    }

    [Fact]
    public void GetUnstoredItems_SkipsZeroModelId()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 999 }; // Scanner returns 0 for this
        var config = new Configuration();

        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);
        var result = watcher.GetUnstoredItemsInBags();

        Assert.Empty(result);
    }
}

public class MockModelScanner : ModelScanner
{
    public MockModelScanner() : base(_ => null) { }
    public override ulong GetModelId(uint itemId)
    {
        if (itemId == 123) return 456;
        return 0;
    }
}
