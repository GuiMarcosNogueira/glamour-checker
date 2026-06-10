using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Lumina.Excel.Sheets;

namespace GlamourChecker.Core;

public struct ItemModelData
{
    public ulong ModelMain;
    public ulong ModelSub;
    public byte DyeCount;
    public uint EquipSlotCategory;
}

public class ModelScanner
{
    private readonly Dictionary<uint, ulong> modelCache = new();
    private readonly Func<uint, ItemModelData?> _itemSheetLookup;
    private readonly VisualDictionary _visualDictionary = new();

    public ModelScanner(Func<uint, ItemModelData?>? itemSheetLookup = null)
    {
        _itemSheetLookup = itemSheetLookup ?? DefaultItemLookup;
    }

    [ExcludeFromCodeCoverage]
    private static ItemModelData? DefaultItemLookup(uint itemId)
    {
        var item = Services.DataManager?.GetExcelSheet<Item>()?.GetRowOrDefault(itemId);
        if (item == null) return null;
        return new ItemModelData
        {
            ModelMain = item.Value.ModelMain,
            ModelSub = item.Value.ModelSub,
            DyeCount = item.Value.DyeCount,
            EquipSlotCategory = item.Value.EquipSlotCategory.RowId
        };
    }

    public virtual bool IsDyeable(uint itemId)
    {
        var item = _itemSheetLookup(itemId);
        return item.HasValue && item.Value.DyeCount > 0;
    }

    public virtual ulong GetSharedModelId(uint itemId)
    {

        // A Shared Model ID strips the factory color bits (32-47) for ALL items.
        // This allows us to group dyeable and non-dyeable items together.
        var item = _itemSheetLookup(itemId);
        if (!item.HasValue || item.Value.EquipSlotCategory == 0) return 0;

        ulong visualSignature = item.Value.ModelMain & 0xFFFFFFFF; // Keep only Base and Variant
        if (item.Value.ModelSub != 0)
        {
            ulong subSignature = item.Value.ModelSub & 0xFFFFFFFF;
            visualSignature ^= (subSignature << 13) | (subSignature >> 51);
        }
        visualSignature |= ((ulong)item.Value.EquipSlotCategory << 48);
        return visualSignature;
    }

    public virtual ulong GetVisualGroupId(uint itemId)
    {
        if (!FeatureFlags.EnableVisualDictionary) return 0;

        var item = _itemSheetLookup(itemId);
        if (!item.HasValue || item.Value.EquipSlotCategory == 0) return 0;

        // Testing mode: allow Visual Dictionary for all categories
        // int cat = (int)item.Value.EquipSlotCategory;
        // if (cat != 4 && cat != 5 && cat != 7 && cat != 8) return 0;

        if (_visualDictionary.TryGetVisualGroup(itemId, out var groupId))
        {
            return groupId;
        }

        return 0;
    }

    public virtual ulong GetModelId(uint itemId)
    {
        if (modelCache.TryGetValue(itemId, out var cachedId))
        {
            return cachedId;
        }

        var item = _itemSheetLookup(itemId);
        if (!item.HasValue)
        {
            modelCache[itemId] = 0;
            return 0;
        }

        var slotCategory = item.Value.EquipSlotCategory;
        if (slotCategory == 0)
        {
            modelCache[itemId] = 0;
            return 0;
        }

        ulong modelMain = item.Value.ModelMain;
        ulong visualSignature;

        // Base and Variant always matter. Color only matters if it's NOT a dyeable item variant.
        // For dyeable items (DyeCount > 0), the game typically assigns a specific variant or color.
        // But to ensure exact matches work, we must ALWAYS extract Base (0-15), Variant (16-31),
        // and Color (32-47). Wait, GetSharedModelId strips Color.
        // GetModelId should KEEP Color, but mask it EXACTLY the same for dyeable vs non-dyeable 
        // to group them strictly by their exact model definition.
        visualSignature = modelMain & 0xFFFFFFFFFFFF;

        if (item.Value.ModelSub != 0)
        {
            ulong subSignature = item.Value.ModelSub & 0xFFFFFFFFFFFF;
            visualSignature ^= (subSignature << 13) | (subSignature >> 51);
        }

        visualSignature |= ((ulong)slotCategory << 48);

        modelCache[itemId] = visualSignature;
        return visualSignature;
    }
}
