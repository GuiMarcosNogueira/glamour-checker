using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System.Diagnostics.CodeAnalysis;

namespace GlamourChecker.Core;

public class GlamourLogic {
    private readonly InventoryWatcher _inventoryWatcher;
    private readonly Configuration _config;
    private readonly IGameMemoryProvider _memoryProvider;

    public GlamourLogic(InventoryWatcher inventoryWatcher, Configuration config, IGameMemoryProvider memoryProvider) {
        _inventoryWatcher = inventoryWatcher;
        _config = config;
        _memoryProvider = memoryProvider;
    }

    public List<InventoryItemInfo> GetFilteredNewAppearances(int selectedCategoryIndex, string searchQuery, string selectedCategoryName, Func<uint, (string Name, uint Category)?> itemSheetLookup) {
        var unstored = _inventoryWatcher.GetUnstoredItemsInBags();
        
        HashSet<uint> gearsetItems = new();
        if (_config.HideGearsetItems) {
            gearsetItems = _memoryProvider.GetGearsetItems();
        }
        
        return unstored.Where(i => {
            if (_config.HideGearsetItems && gearsetItems.Contains(i.ItemId)) return false;
            
            var itemSheet = itemSheetLookup(i.ItemId);
            if (itemSheet.HasValue) {
                string itemName = itemSheet.Value.Name;
                if (!string.IsNullOrWhiteSpace(searchQuery) && !itemName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }
            
            if (selectedCategoryIndex == 0) return true;
            string categoryName = GetCategoryName(i.ContainerType);
            return categoryName == selectedCategoryName;
        }).ToList();
    }

    public List<DuplicateAppearance> GetFilteredDuplicates(int selectedCategoryIndex, string searchQuery, string selectedCategoryName, Func<uint, (string Name, uint Category)?> itemSheetLookup) {
        var duplicates = _inventoryWatcher.GetDuplicates();

        return duplicates.Where(group => {
            var firstItemSheet = itemSheetLookup(group.ItemIds.First());
            if (!firstItemSheet.HasValue) return false;

            string itemName = firstItemSheet.Value.Name;
            if (!string.IsNullOrWhiteSpace(searchQuery) && !itemName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            if (selectedCategoryIndex == 0) return true;
            if (selectedCategoryIndex == 1 || selectedCategoryIndex >= 7) return false;

            string categoryName = GetCategoryName(MapEquipSlotToInventoryType(firstItemSheet.Value.Category));
            return categoryName == selectedCategoryName;
        }).ToList();
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

    public string GetCategoryName(InventoryType type) {
        if (CategoryKeyMap.TryGetValue(type, out string? key) && key != null) {
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

    public InventoryType MapEquipSlotToInventoryType(uint equipSlot) {
        if (EquipSlotToInventoryMap.TryGetValue(equipSlot, out var invType)) {
            return invType;
        }
        return InventoryType.Inventory1;
    }
}
