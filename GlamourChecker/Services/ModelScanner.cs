using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Lumina.Excel.Sheets;

namespace GlamourChecker.Core;

public struct ItemModelData {
    public ulong ModelMain;
    public ulong ModelSub;
    public byte DyeCount;
    public uint EquipSlotCategory;
}

public class ModelScanner {
    private readonly Dictionary<uint, ulong> modelCache = new();
    private readonly Func<uint, ItemModelData?> _itemSheetLookup;

    public ModelScanner(Func<uint, ItemModelData?>? itemSheetLookup = null) {
        _itemSheetLookup = itemSheetLookup ?? DefaultItemLookup;
    }

    [ExcludeFromCodeCoverage]
    private static ItemModelData? DefaultItemLookup(uint itemId) {
        var item = Services.DataManager?.GetExcelSheet<Item>()?.GetRowOrDefault(itemId);
        if (item == null) return null;
        return new ItemModelData {
            ModelMain = item.Value.ModelMain,
            ModelSub = item.Value.ModelSub,
            DyeCount = item.Value.DyeCount,
            EquipSlotCategory = item.Value.EquipSlotCategory.RowId
        };
    }

    public virtual bool IsDyeable(uint itemId) {
        var item = _itemSheetLookup(itemId);
        return item.HasValue && item.Value.DyeCount > 0;
    }

    public virtual ulong GetSharedModelId(uint itemId) {
        // A Shared Model ID strips the factory color bits (32-47) for ALL items.
        // This allows us to group dyeable and non-dyeable items together.
        var item = _itemSheetLookup(itemId);
        if (!item.HasValue || item.Value.EquipSlotCategory == 0) return 0;

        ulong visualSignature = item.Value.ModelMain & 0xFFFFFFFF; // Keep only Base and Variant
        if (item.Value.ModelSub != 0) {
            ulong subSignature = item.Value.ModelSub & 0xFFFFFFFF;
            visualSignature ^= (subSignature << 13) | (subSignature >> 51);
        }
        visualSignature |= ((ulong)item.Value.EquipSlotCategory << 48);
        return visualSignature;
    }

    public virtual ulong GetModelId(uint itemId) {
        if (modelCache.TryGetValue(itemId, out var cachedId)) {
            return cachedId;
        }

        var item = _itemSheetLookup(itemId);
        if (!item.HasValue) {
            modelCache[itemId] = 0;
            return 0;
        }

        ulong modelMain = item.Value.ModelMain;
        ulong visualSignature;
        
        if (item.Value.DyeCount > 0) {
            visualSignature = modelMain & 0xFFFFFFFF;
        } else {
            visualSignature = modelMain & 0xFFFFFFFFFFFF;
        }

        if (item.Value.ModelSub != 0) {
            ulong subSignature = item.Value.DyeCount > 0 ? (item.Value.ModelSub & 0xFFFFFFFF) : (item.Value.ModelSub & 0xFFFFFFFFFFFF);
            visualSignature ^= (subSignature << 13) | (subSignature >> 51);
        }
        
        var slotCategory = item.Value.EquipSlotCategory;
        if (slotCategory == 0) {
            modelCache[itemId] = 0;
            return 0;
        }

        visualSignature |= ((ulong)slotCategory << 48);
        
        modelCache[itemId] = visualSignature;
        return visualSignature;
    }
}
