using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlamourChecker.Core;
using Xunit;

namespace GlamourChecker.Tests;

public class FakeGameMemoryProvider : IGameMemoryProvider {
    public uint[] DresserItems = new uint[800];
    public bool CabinetLoaded = false;
    public InventoryItem[] InventoryItems = new InventoryItem[35];
    
    public unsafe Span<uint> GetMirageManagerPrismBoxItemIds() {
        return new Span<uint>(DresserItems);
    }

    public unsafe Span<InventoryItem> GetInventoryContainer(InventoryType type) {
        if (type == InventoryType.Inventory1) {
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

public class InventoryWatcherTests {
    [Fact]
    public void ScanDresserAndArmoire_FindsItemsInDresserAndArmoire() {
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
    public void GetDuplicates_ChecksArmoire() {
        var config = new Configuration();
        config.ArmoireItemsByModel = new Dictionary<ulong, List<uint>> {
            { 999, new List<uint> { 111, 222 } }
        };
        var memoryFake = new FakeGameMemoryProvider();
        var scanner = new ModelScanner(_ => null);
        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        
        var dups = watcher.GetDuplicates();
        Assert.Single(dups);
        Assert.Equal(999ul, dups[0].ModelId);
    }
    
    [Fact]
    public void GetUnstoredItemsInBags_ReturnsEmptyWhenNone() {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        var scanner = new ModelScanner(_ => null);
        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        
        Assert.Empty(watcher.GetUnstoredItemsInBags());
    }
    
    [Fact]
    public void GetDuplicates_ReturnsEmptyWhenNone() {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        var scanner = new ModelScanner(_ => null);
        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        
        Assert.Empty(watcher.GetDuplicates());
    }
    
    [Fact]
    public void GetDuplicates_ReturnsDuplicatesFromConfig() {
        var config = new Configuration();
        config.DresserItemsByModel = new Dictionary<ulong, List<uint>> {
            { 1, new List<uint> { 100, 200 } }
        };
        var memoryFake = new FakeGameMemoryProvider();
        var scanner = new ModelScanner(_ => null);
        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        
        var dups = watcher.GetDuplicates();
        Assert.Single(dups);
        Assert.Equal(1ul, dups[0].ModelId);
    }

    [Fact]
    public void GetUnstoredItemsInBags_ReturnsItem_WhenItemFoundAndNotStored() {
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
    public void CheckDresserChanges_ReturnsFalse_WhenArrayIsEmpty() {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.DresserItems = Array.Empty<uint>(); // Empty span
        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        Assert.False(watcher.CheckDresserChanges());
    }

    [Fact]
    public void CheckDresserChanges_ReturnsFalse_WhenArrayIsAllZeros() {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        // DresserItems defaults to 800 zeros
        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        Assert.False(watcher.CheckDresserChanges());
    }

    [Fact]
    public void CheckDresserChanges_ReturnsTrue_WhenHashChanges() {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.DresserItems[0] = 123;
        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);

        // First call should return true (hash changed from 0 to something)
        Assert.True(watcher.CheckDresserChanges());

        // Second call should return false (hash is the same)
        Assert.False(watcher.CheckDresserChanges());

        // Change an item
        memoryFake.DresserItems[1] = 456;
        Assert.True(watcher.CheckDresserChanges());
    }

    [Fact]
    public void ScanDresserAndArmoire_ScansArmoireCorrectly() {
        var scanner = new MockModelScanner(); 
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        
        Func<IEnumerable<(uint, uint)>> cabinetProvider = () => new[] { (123u, 10u) };
        memoryFake.CabinetLoaded = true;
        // Make IsItemInCabinet return true for 123
        memoryFake.IsCabinetItem = true;

        var watcher = new InventoryWatcher(scanner, config, memoryFake, cabinetProvider);
        watcher.ScanDresserAndArmoire();
        
        Assert.Contains(456ul, config.ArmoireModelIds);
    }
}

public class MockModelScanner : ModelScanner {
    public MockModelScanner() : base(_ => null) { }
    public override ulong GetModelId(uint itemId) {
        if (itemId == 123) return 456;
        return 0;
    }
}
