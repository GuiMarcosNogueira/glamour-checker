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

    public string GetCategoryName(InventoryType type) {
        switch (type) {
            case InventoryType.Inventory1:
            case InventoryType.Inventory2:
            case InventoryType.Inventory3:
            case InventoryType.Inventory4:
                return Loc.Localize("Category_Inventory", "Inventory");
            case InventoryType.ArmoryMainHand:
            case InventoryType.ArmoryOffHand:
                return Loc.Localize("Category_ArmoryMainOff", "Armoury Chest (Main Hand/Off Hand)");
            case InventoryType.ArmoryHead:
            case InventoryType.ArmoryBody:
            case InventoryType.ArmoryHands:
                return Loc.Localize("Category_ArmoryHeadBodyHands", "Armoury Chest (Head/Body/Hands)");
            case InventoryType.ArmoryLegs:
            case InventoryType.ArmoryFeets:
                return Loc.Localize("Category_ArmoryLegsFeet", "Armoury Chest (Legs/Feet)");
            case InventoryType.ArmoryEar:
            case InventoryType.ArmoryNeck:
                return Loc.Localize("Category_ArmoryEarsNeck", "Armoury Chest (Ears/Neck)");
            case InventoryType.ArmoryWrist:
            case InventoryType.ArmoryRings:
                return Loc.Localize("Category_ArmoryWristsFingers", "Armoury Chest (Wrists/Fingers)");
            case InventoryType.SaddleBag1:
            case InventoryType.SaddleBag2:
            case InventoryType.PremiumSaddleBag1:
            case InventoryType.PremiumSaddleBag2:
                return Loc.Localize("Category_Saddlebag", "Chocobo Saddlebag");
            case InventoryType.RetainerPage1:
            case InventoryType.RetainerPage2:
            case InventoryType.RetainerPage3:
            case InventoryType.RetainerPage4:
            case InventoryType.RetainerPage5:
            case InventoryType.RetainerPage6:
            case InventoryType.RetainerPage7:
                return Loc.Localize("Category_Retainer", "Retainer Inventory");
            default:
                return Loc.Localize("Category_Other", "Other");
        }
    }

    public InventoryType MapEquipSlotToInventoryType(uint equipSlot) {
        switch (equipSlot) {
            case 1: case 2: case 13: case 14: return InventoryType.ArmoryMainHand;
            case 3: case 15: return InventoryType.ArmoryHead;
            case 4: case 16: case 19: case 20: case 21: return InventoryType.ArmoryBody;
            case 5: return InventoryType.ArmoryHands;
            case 7: case 18: return InventoryType.ArmoryLegs;
            case 8: return InventoryType.ArmoryFeets;
            case 9: return InventoryType.ArmoryEar;
            case 10: return InventoryType.ArmoryNeck;
            case 11: return InventoryType.ArmoryWrist;
            case 12: return InventoryType.ArmoryRings;
            default: return InventoryType.Inventory1;
        }
    }
}
