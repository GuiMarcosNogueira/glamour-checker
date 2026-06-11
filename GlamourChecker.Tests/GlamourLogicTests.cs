using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlamourChecker.Core;
using Lumina.Excel.Sheets;
using Xunit;
using System.Reflection;

namespace GlamourChecker.Tests;

public class GlamourLogicTests
{
    [Obsolete]
    private Item CreateMockItem(string name, uint equipSlot)
    {
        var item = new Item();
        var type = typeof(Item);

        // Mock Name
        var seString = new Lumina.Text.SeString(System.Text.Encoding.UTF8.GetBytes(name));
        type.GetProperty("Name")?.SetValue(item, seString);

        // Mock EquipSlotCategory
        var equipSlotType = type.GetProperty("EquipSlotCategory")?.PropertyType;
        if (equipSlotType != null)
        {
            // Lumina 6+ RowRef or LazyRow
            var constructors = equipSlotType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (constructors.Length > 0)
            {
                // Try to create uninitialized and set RowId if possible, or just default it
                var lazyRow = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(equipSlotType);
                var rowIdProp = equipSlotType.GetProperty("RowId") ?? equipSlotType.GetProperty("Row");
                rowIdProp?.SetValue(lazyRow, equipSlot);
                type.GetProperty("EquipSlotCategory")?.SetValue(item, lazyRow);
            }
        }

        return item;
    }

    [Fact]
    public void GetCategoryName_ReturnsCorrectString_ForRetainer()
    {
        var watcher = new InventoryWatcher(new ModelScanner(_ => null), new Configuration(), new FakeGameMemoryProvider());
        var logic = new GlamourLogic(watcher, new Configuration(), new FakeGameMemoryProvider());

        var name = logic.GetCategoryName(InventoryType.RetainerPage1);
        Assert.Equal("Retainer Inventory", name);
    }

    [Fact]
    public void MapEquipSlotToInventoryType_ReturnsCorrectType()
    {
        var watcher = new InventoryWatcher(new ModelScanner(_ => null), new Configuration(), new FakeGameMemoryProvider());
        var logic = new GlamourLogic(watcher, new Configuration(), new FakeGameMemoryProvider());

        var type = logic.MapEquipSlotToInventoryType(3);
        Assert.Equal(InventoryType.ArmoryHead, type);
    }

    [Fact]
    public void GetFilteredNewAppearances_IgnoresWhenItemSheetNotFound()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 }; // Exists in bags

        var watcher = new InventoryWatcher(new MockModelScanner(), new Configuration(), memoryFake);
        var logic = new GlamourLogic(watcher, new Configuration(), memoryFake);

        // Lookup returns null (HasValue == false)
        var newApp = logic.GetFilteredNewAppearances(0, "", "", id => null);

        Assert.Single(newApp); // Still returned but with Other category
    }

    [Fact]
    public void GetFilteredDuplicates_IgnoresWhenItemSheetNotFound()
    {
        var config = new Configuration();
        config.DresserItemsBySharedModel = new Dictionary<ulong, List<uint>> {
            { 1, new List<uint> { 123, 456 } } // Duplicate
        };
        var memoryFake = new FakeGameMemoryProvider();

        var watcher = new InventoryWatcher(new MockModelScanner(), config, memoryFake);
        var logic = new GlamourLogic(watcher, config, memoryFake);

        // Lookup returns null (HasValue == false)
        var duplicates = logic.GetFilteredDuplicates(0, "", "", id => null);

        Assert.Empty(duplicates); // If HasValue == false, it returns false in LINQ where
    }

    [Fact]
    public void GetCategoryName_MapsVariousCategories()
    {
        var watcher = new InventoryWatcher(new ModelScanner(_ => null), new Configuration(), new FakeGameMemoryProvider());
        var logic = new GlamourLogic(watcher, new Configuration(), new FakeGameMemoryProvider());

        Assert.Equal("Inventory", logic.GetCategoryName(InventoryType.Inventory1));
        Assert.Equal("Armoury Chest (Main Hand/Off Hand)", logic.GetCategoryName(InventoryType.ArmoryMainHand));
        Assert.Equal("Armoury Chest (Head/Body/Hands)", logic.GetCategoryName(InventoryType.ArmoryHead));
        Assert.Equal("Armoury Chest (Legs/Feet)", logic.GetCategoryName(InventoryType.ArmoryLegs));
        Assert.Equal("Armoury Chest (Ears/Neck)", logic.GetCategoryName(InventoryType.ArmoryEar));
        Assert.Equal("Armoury Chest (Wrists/Fingers)", logic.GetCategoryName(InventoryType.ArmoryWrist));
        Assert.Equal("Chocobo Saddlebag", logic.GetCategoryName(InventoryType.SaddleBag1));
        Assert.Equal("Other", logic.GetCategoryName(InventoryType.Mail));
    }

    [Fact]
    public void MapEquipSlotToInventoryType_MapsAllSlots()
    {
        var logic = new GlamourLogic(new InventoryWatcher(new ModelScanner(_ => null), new Configuration(), new FakeGameMemoryProvider()), new Configuration(), new FakeGameMemoryProvider());

        Assert.Equal(InventoryType.ArmoryMainHand, logic.MapEquipSlotToInventoryType(1));
        Assert.Equal(InventoryType.ArmoryMainHand, logic.MapEquipSlotToInventoryType(2));
        Assert.Equal(InventoryType.ArmoryHead, logic.MapEquipSlotToInventoryType(3));
        Assert.Equal(InventoryType.ArmoryBody, logic.MapEquipSlotToInventoryType(4));
        Assert.Equal(InventoryType.ArmoryHands, logic.MapEquipSlotToInventoryType(5));
        Assert.Equal(InventoryType.ArmoryLegs, logic.MapEquipSlotToInventoryType(7));
        Assert.Equal(InventoryType.ArmoryFeets, logic.MapEquipSlotToInventoryType(8));
        Assert.Equal(InventoryType.ArmoryEar, logic.MapEquipSlotToInventoryType(9));
        Assert.Equal(InventoryType.ArmoryNeck, logic.MapEquipSlotToInventoryType(10));
        Assert.Equal(InventoryType.ArmoryWrist, logic.MapEquipSlotToInventoryType(11));
        Assert.Equal(InventoryType.ArmoryRings, logic.MapEquipSlotToInventoryType(12));
        Assert.Equal(InventoryType.ArmoryMainHand, logic.MapEquipSlotToInventoryType(13));
        Assert.Equal(InventoryType.ArmoryMainHand, logic.MapEquipSlotToInventoryType(14));
        Assert.Equal(InventoryType.ArmoryHead, logic.MapEquipSlotToInventoryType(15));
        Assert.Equal(InventoryType.ArmoryBody, logic.MapEquipSlotToInventoryType(16));
        Assert.Equal(InventoryType.ArmoryLegs, logic.MapEquipSlotToInventoryType(18));
        Assert.Equal(InventoryType.ArmoryBody, logic.MapEquipSlotToInventoryType(19));
        Assert.Equal(InventoryType.ArmoryBody, logic.MapEquipSlotToInventoryType(20));
        Assert.Equal(InventoryType.ArmoryBody, logic.MapEquipSlotToInventoryType(21));
        Assert.Equal(InventoryType.Inventory1, logic.MapEquipSlotToInventoryType(99));
    }

    [Fact]
    public void GetFilteredNewAppearances_ReturnsAll_WhenNoFilters()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 };

        var mockScanner = new MockModelScanner();
        var watcher = new InventoryWatcher(mockScanner, config, memoryFake);
        var logic = new GlamourLogic(watcher, config, memoryFake);

        var result = logic.GetFilteredNewAppearances(0, "", "", id => ("TestItem", 1u, 50u));

        Assert.Single(result);
    }



    [Fact]
    public void GetFilteredNewAppearances_FiltersBySearchQuery()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 };

        var mockScanner = new MockModelScanner();
        var watcher = new InventoryWatcher(mockScanner, config, memoryFake);
        var logic = new GlamourLogic(watcher, config, memoryFake);

        var resultEmpty = logic.GetFilteredNewAppearances(0, "WrongName", "", id => ("TestItem", 1u, 50u));
        var resultMatch = logic.GetFilteredNewAppearances(0, "Test", "", id => ("TestItem", 1u, 50u));

        Assert.Empty(resultEmpty);
        Assert.Single(resultMatch);
    }

    [Fact]
    public void GetFilteredDuplicates_FiltersByCategory()
    {
        var config = new Configuration();
        config.DresserItemsBySharedModel = new Dictionary<ulong, List<uint>> {
            { 1, new List<uint> { 100, 200 } }
        };
        var memoryFake = new FakeGameMemoryProvider();
        var mockScanner = new MockModelScanner();
        var watcher = new InventoryWatcher(mockScanner, config, memoryFake);
        var logic = new GlamourLogic(watcher, config, memoryFake);

        var result = logic.GetFilteredDuplicates(2, "", "Armoury Chest (Main Hand/Off Hand)", id => ("Weapon", 1u, 50u)); // Category 2: MainHand/OffHand matches InventoryType.ArmoryMainHand

        Assert.Single(result);

        var resultNoMatch = logic.GetFilteredDuplicates(3, "", "Armoury Chest (Head/Body/Hands)", id => ("Weapon", 1u, 50u));
        Assert.Empty(resultNoMatch);
    }

    [Fact]
    public void GetFilteredNewAppearances_SortsItemsCorrectly()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 100 }; // Category 15 (Feet), iLvl 10
        memoryFake.InventoryItems[1] = new InventoryItem { ItemId = 101 }; // Category 14 (Legs), iLvl 20
        memoryFake.InventoryItems[2] = new InventoryItem { ItemId = 102 }; // Category 14 (Legs), iLvl 50
        memoryFake.InventoryItems[3] = new InventoryItem { ItemId = 103 }; // Category 1 (1H Weapon), iLvl 50
        memoryFake.InventoryItems[4] = new InventoryItem { ItemId = 0 };   // Invalid itemId

        var mockScanner = new MockModelScannerAllValid();
        var watcher = new InventoryWatcher(mockScanner, config, memoryFake);
        var logic = new GlamourLogic(watcher, config, memoryFake);

        var result = logic.GetFilteredNewAppearances(0, "", "", id =>
        {
            if (id == 100) return ("FeetItem", 8u, 10u);
            if (id == 101) return ("LegsLow", 7u, 20u);
            if (id == 102) return ("LegsHigh", 7u, 50u);
            if (id == 103) return ("Weapon", 1u, 50u);
            return null;
        });

        // Expected order:
        // 1. Weapon (Cat 1) -> id 103
        // 2. LegsHigh (Cat 7/14), iLvl 50 -> id 102
        // 3. LegsLow (Cat 7/14), iLvl 20 -> id 101
        // 4. Feet (Cat 8/15), iLvl 10 -> id 100

        Assert.Equal(4, result.Count);
        Assert.Equal(103u, result[0].ItemId);
        Assert.Equal(102u, result[1].ItemId);
        Assert.Equal(101u, result[2].ItemId);
        Assert.Equal(100u, result[3].ItemId);
    }

    [Fact]
    public void GetFilteredNewAppearances_SortsAllCategories()
    {
        var config = new Configuration();
        var memoryFake = new FakeGameMemoryProvider();
        for (uint i = 1; i <= 17; i++)
        {
            memoryFake.InventoryItems[(int)i - 1] = new InventoryItem { ItemId = i };
        }

        var logic = new GlamourLogic(new InventoryWatcher(new MockModelScannerAllValid(), config, memoryFake), config, memoryFake);

        var result = logic.GetFilteredNewAppearances(0, "", "", id =>
        {
            // Map item ID to its respective EquipSlotCategory to hit every branch in GetSortOrderForItemId
            uint cat = id switch
            {
                1 => 1,
                2 => 13,
                3 => 14,
                4 => 19,
                5 => 2,
                6 => 3,
                7 => 15,
                8 => 4,
                9 => 5,
                10 => 7,
                11 => 18,
                12 => 8,
                13 => 9,
                14 => 10,
                15 => 11,
                16 => 12,
                _ => 99
            };
            return ("Test", cat, 100u);
        });

        Assert.Equal(17, result.Count);
    }

    private class MockModelScannerAllValid : MockModelScanner
    {
        public override ulong GetModelId(uint itemId)
        {
            return itemId; // valid model id
        }
    }
}
