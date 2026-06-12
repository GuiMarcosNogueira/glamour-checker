using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System.Diagnostics.CodeAnalysis;

namespace GlamourChecker.Core;

public class GlamourLogic
{
    private readonly InventoryWatcher _inventoryWatcher;
    private readonly Configuration _config;
    private readonly IGameMemoryProvider _memoryProvider;

    public GlamourLogic(InventoryWatcher inventoryWatcher, Configuration config, IGameMemoryProvider memoryProvider)
    {
        _inventoryWatcher = inventoryWatcher;
        _config = config;
        _memoryProvider = memoryProvider;
    }

    public List<InventoryItemInfo> GetFilteredNewAppearances(int selectedCategoryIndex, string searchQuery, string selectedCategoryName, Func<uint, (string Name, uint Category, uint LevelItem)?> itemSheetLookup)
    {
        var unstored = _inventoryWatcher.GetUnstoredItemsInBags();

        HashSet<uint> gearsetItems = new();
        if (_config.HideGearsetItems)
        {
            gearsetItems = _memoryProvider.GetGearsetItems();
        }

        var result = new List<InventoryItemInfo>();
        foreach (var i in unstored)
        {
            if (IsNewAppearanceMatch(i, selectedCategoryIndex, searchQuery, selectedCategoryName, itemSheetLookup, gearsetItems))
            {
                result.Add(i);
            }
        }

        return result
            .OrderBy(x => GetSortOrderForItemId(x.ItemId, itemSheetLookup))
            .ThenByDescending(x => GetItemLevel(x.ItemId, itemSheetLookup))
            .ThenBy(x => x.ItemId)
            .ToList();
    }

    private bool IsNewAppearanceMatch(InventoryItemInfo i, int categoryIndex, string query, string categoryName, Func<uint, (string Name, uint Category, uint LevelItem)?> lookup, HashSet<uint> gearsetItems)
    {
        if (_config.HideGearsetItems && gearsetItems.Contains(i.ItemId)) return false;

        var sheet = lookup(i.ItemId);
        if (sheet.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(query) && !sheet.Value.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) return false;
        }

        if (categoryIndex == 0) return true;
        return GetCategoryName(i.ContainerType) == categoryName;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public List<DuplicateAppearance> GetFilteredDuplicates(int selectedCategoryIndex, string searchQuery, string selectedCategoryName, Func<uint, (string Name, uint Category, uint LevelItem)?> itemSheetLookup)
    {
        var duplicates = _inventoryWatcher.GetDuplicates();
        var result = new List<DuplicateAppearance>();

        foreach (var group in duplicates)
        {
            if (IsDuplicateMatch(group, selectedCategoryIndex, searchQuery, selectedCategoryName, itemSheetLookup))
            {
                result.Add(group);
            }
        }

        return result
            .OrderBy(x => GetSortOrderForItemId(x.ItemIds.FirstOrDefault(), itemSheetLookup))
            .ThenByDescending(x => GetItemLevel(x.ItemIds.FirstOrDefault(), itemSheetLookup))
            .ThenBy(x => x.ItemIds.FirstOrDefault())
            .ToList();
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private bool IsDuplicateMatch(DuplicateAppearance group, int categoryIndex, string query, string categoryName, Func<uint, (string Name, uint Category, uint LevelItem)?> lookup)
    {
        var sheet = lookup(group.ItemIds.First());
        if (!sheet.HasValue) return false;

        if (!string.IsNullOrWhiteSpace(query) && !sheet.Value.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) return false;
        if (categoryIndex == 0) return true;
        if (categoryIndex == 1 || categoryIndex >= 7) return false;

        return GetCategoryName(MapEquipSlotToInventoryType(sheet.Value.Category)) == categoryName;
    }

    private static readonly Dictionary<uint, int> EquipSlotToSortOrderMap = new() {
        { 1, 1 },   // 1H Weapon
        { 13, 2 },  // 2H Weapon
        { 14, 3 },  // 1H
        { 19, 4 },  // 2H
        { 2, 5 },   // Offhand
        { 3, 10 },  // Head
        { 15, 11 }, // Body/Head
        { 4, 12 },  // Body
        { 5, 13 },  // Hands
        { 7, 14 },  // Legs
        { 18, 14 }, // Legs/Feet
        { 8, 15 },  // Feet
        { 9, 20 },  // Earrings
        { 10, 21 }, // Necklace
        { 11, 22 }, // Bracelets
        { 12, 23 }  // Rings
    };

    private int GetSortOrderForItemId(uint itemId, Func<uint, (string Name, uint Category, uint LevelItem)?> lookup)
    {
        if (itemId == 0) return 99;
        var sheet = lookup(itemId);
        if (!sheet.HasValue) return 99;

        return EquipSlotToSortOrderMap.TryGetValue(sheet.Value.Category, out var order) ? order : 99;
    }

    private uint GetItemLevel(uint itemId, Func<uint, (string Name, uint Category, uint LevelItem)?> lookup)
    {
        if (itemId == 0) return 0;
        var sheet = lookup(itemId);
        return sheet?.LevelItem ?? 0;
    }

    private static readonly Dictionary<InventoryType, string> CategoryKeyMap = new() {
        { InventoryType.Inventory1, "Category_Inventory" },
        { InventoryType.Inventory2, "Category_Inventory" },
        { InventoryType.Inventory3, "Category_Inventory" },
        { InventoryType.Inventory4, "Category_Inventory" },
        { InventoryType.ArmoryMainHand, "Category_ArmoryMainOff" },
        { InventoryType.ArmoryOffHand, "Category_ArmoryMainOff" },
        { InventoryType.ArmoryHead, "Category_ArmoryHeadBodyHands" },
        { InventoryType.ArmoryBody, "Category_ArmoryHeadBodyHands" },
        { InventoryType.ArmoryHands, "Category_ArmoryHeadBodyHands" },
        { InventoryType.ArmoryLegs, "Category_ArmoryLegsFeet" },
        { InventoryType.ArmoryFeets, "Category_ArmoryLegsFeet" },
        { InventoryType.ArmoryEar, "Category_ArmoryEarsNeck" },
        { InventoryType.ArmoryNeck, "Category_ArmoryEarsNeck" },
        { InventoryType.ArmoryWrist, "Category_ArmoryWristsFingers" },
        { InventoryType.ArmoryRings, "Category_ArmoryWristsFingers" },
        { InventoryType.SaddleBag1, "Category_Saddlebag" },
        { InventoryType.SaddleBag2, "Category_Saddlebag" },
        { InventoryType.PremiumSaddleBag1, "Category_Saddlebag" },
        { InventoryType.PremiumSaddleBag2, "Category_Saddlebag" },
        { InventoryType.RetainerPage1, "Category_Retainer" },
        { InventoryType.RetainerPage2, "Category_Retainer" },
        { InventoryType.RetainerPage3, "Category_Retainer" },
        { InventoryType.RetainerPage4, "Category_Retainer" },
        { InventoryType.RetainerPage5, "Category_Retainer" },
        { InventoryType.RetainerPage6, "Category_Retainer" },
        { InventoryType.RetainerPage7, "Category_Retainer" }
    };

    private static readonly Dictionary<string, string> CategoryFallbackMap = new() {
        { "Category_Inventory", "Inventory" },
        { "Category_ArmoryMainOff", "Armoury Chest (Main Hand/Off Hand)" },
        { "Category_ArmoryHeadBodyHands", "Armoury Chest (Head/Body/Hands)" },
        { "Category_ArmoryLegsFeet", "Armoury Chest (Legs/Feet)" },
        { "Category_ArmoryEarsNeck", "Armoury Chest (Ears/Neck)" },
        { "Category_ArmoryWristsFingers", "Armoury Chest (Wrists/Fingers)" },
        { "Category_Saddlebag", "Chocobo Saddlebag" },
        { "Category_Retainer", "Retainer Inventory" }
    };

    public string GetCategoryName(InventoryType type)
    {
        if (CategoryKeyMap.TryGetValue(type, out string? key) && key != null)
        {
            return Loc.Localize(key, CategoryFallbackMap[key]);
        }
        return Loc.Localize("Category_Other", "Other");
    }

    private static readonly Dictionary<uint, InventoryType> EquipSlotToInventoryMap = new() {
        { 1, InventoryType.ArmoryMainHand },
        { 2, InventoryType.ArmoryMainHand },
        { 13, InventoryType.ArmoryMainHand },
        { 14, InventoryType.ArmoryMainHand },
        { 3, InventoryType.ArmoryHead },
        { 15, InventoryType.ArmoryHead },
        { 4, InventoryType.ArmoryBody },
        { 16, InventoryType.ArmoryBody },
        { 19, InventoryType.ArmoryBody },
        { 20, InventoryType.ArmoryBody },
        { 21, InventoryType.ArmoryBody },
        { 5, InventoryType.ArmoryHands },
        { 7, InventoryType.ArmoryLegs },
        { 18, InventoryType.ArmoryLegs },
        { 8, InventoryType.ArmoryFeets },
        { 9, InventoryType.ArmoryEar },
        { 10, InventoryType.ArmoryNeck },
        { 11, InventoryType.ArmoryWrist },
        { 12, InventoryType.ArmoryRings }
    };

    public InventoryType MapEquipSlotToInventoryType(uint equipSlot)
    {
        if (EquipSlotToInventoryMap.TryGetValue(equipSlot, out var invType))
        {
            return invType;
        }
        return InventoryType.Inventory1;
    }
}
