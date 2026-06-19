using System.Collections.Generic;
using System.Linq;
using System;

namespace GlamourChecker.Core;

public static class ItemCategoryHelper
{
    private static readonly Dictionary<uint, (string Key, string DefaultValue)> SlotGroups = new()
    {
        { 1, ("SlotGroup_MainHand", "Main Hand") },
        { 13, ("SlotGroup_MainHand", "Main Hand") },
        { 14, ("SlotGroup_MainHand", "Main Hand") },
        { 19, ("SlotGroup_MainHand", "Main Hand") },
        { 33, ("SlotGroup_MainHand", "Main Hand") },
        { 2, ("SlotGroup_OffHand", "Off Hand") },
        { 3, ("SlotGroup_Head", "Head") },
        { 4, ("SlotGroup_Body", "Body") },
        { 15, ("SlotGroup_Body", "Body") },
        { 16, ("SlotGroup_Body", "Body") },
        { 20, ("SlotGroup_Body", "Body") },
        { 21, ("SlotGroup_Body", "Body") },
        { 5, ("SlotGroup_Hands", "Hands") },
        { 7, ("SlotGroup_Legs", "Legs") },
        { 18, ("SlotGroup_Legs", "Legs") },
        { 8, ("SlotGroup_Feet", "Feet") },
        { 9, ("SlotGroup_Ears", "Ears") },
        { 10, ("SlotGroup_Neck", "Neck") },
        { 11, ("SlotGroup_Wrists", "Wrists") },
        { 12, ("SlotGroup_Fingers", "Fingers") }
    };

    private static readonly (string Key, string DefaultValue)[] SlotOrder = new[]
    {
        ("SlotGroup_MainHand", "Main Hand"),
        ("SlotGroup_OffHand", "Off Hand"),
        ("SlotGroup_Head", "Head"),
        ("SlotGroup_Body", "Body"),
        ("SlotGroup_Hands", "Hands"),
        ("SlotGroup_Legs", "Legs"),
        ("SlotGroup_Feet", "Feet"),
        ("SlotGroup_Ears", "Ears"),
        ("SlotGroup_Neck", "Neck"),
        ("SlotGroup_Wrists", "Wrists"),
        ("SlotGroup_Fingers", "Fingers")
    };

    public static string GetEquipSlotGroup(uint rowId)
    {
        if (SlotGroups.TryGetValue(rowId, out var group))
        {
            return Loc.Localize(group.Key, group.DefaultValue);
        }
        return Loc.Localize("SlotGroup_Other", "Other");
    }

    public static List<GlamourChecker.ViewModels.SlotGroup<IGrouping<ulong, InventoryItemInfo>>> GroupInventoryItems(IEnumerable<InventoryItemInfo> items, Func<uint, (string Name, uint Category, uint LevelItem)?> itemSheetLookup)
    {
        return items
            .GroupBy(x =>
            {
                var sheet = itemSheetLookup(x.ItemId);
                return sheet.HasValue ? GetEquipSlotGroup(sheet.Value.Category) : Loc.Localize("SlotGroup_Other", "Other");
            })
            .OrderBy(g => GetEquipSlotSortOrder(g.Key))
            .Select(g => new GlamourChecker.ViewModels.SlotGroup<IGrouping<ulong, InventoryItemInfo>>
            {
                Name = g.Key,
                Items = g.GroupBy(x => x.ModelId == 0 ? (ulong.MaxValue - x.ItemId) : x.ModelId)
            })
            .ToList();
    }

    public static List<GlamourChecker.ViewModels.SlotGroup<DuplicateAppearance>> GroupDuplicates(IEnumerable<DuplicateAppearance> items, Func<uint, (string Name, uint Category, uint LevelItem)?> itemSheetLookup)
    {
        return items
            .GroupBy(x =>
            {
                var firstId = x.ItemIds.FirstOrDefault();
                var sheet = itemSheetLookup(firstId);
                return sheet.HasValue ? GetEquipSlotGroup(sheet.Value.Category) : Loc.Localize("SlotGroup_Other", "Other");
            })
            .OrderBy(g => GetEquipSlotSortOrder(g.Key))
            .Select(g => new GlamourChecker.ViewModels.SlotGroup<DuplicateAppearance>
            {
                Name = g.Key,
                Items = g
            })
            .ToList();
    }

    public static int GetEquipSlotSortOrder(string slotGroup)
    {
        for (int i = 0; i < SlotOrder.Length; i++)
        {
            if (slotGroup == Loc.Localize(SlotOrder[i].Key, SlotOrder[i].DefaultValue))
                return i + 1;
        }
        return 99;
    }
}
