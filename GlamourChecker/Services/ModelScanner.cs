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
    
    public virtual ulong GetModelId(uint itemId) {
        if (modelCache.TryGetValue(itemId, out var cachedId)) {
            return cachedId;
        }

        var item = _itemSheetLookup(itemId);
        if (!item.HasValue) {
            modelCache[itemId] = 0;
            return 0;
        }

        // FFXIV Item Model is typically represented by ModelMain.
        // It's a ulong representing an array of 4 ushorts: [0] = Id, [1] = Var, [2] = Dye, [3] = ?
        // We only care about the visual model, which usually is just the Id and Var.
        // But for simplicity, we can just use the exact ModelMain value, maybe stripping out the dye if needed.
        // FFXIV ModelMain is a ulong: [0..15] Id, [16..31] Variant, [32..47] Dye, [48..63] ?
        ulong modelMain = item.Value.ModelMain;
        ulong visualSignature;
        
        if (item.Value.DyeCount > 0) {
            // If it's dyeable (DyeCount > 0), the base color doesn't matter because we can dye it.
            // We ignore the color bits (bits 32 and above).
            visualSignature = modelMain & 0xFFFFFFFF;
        } else {
            // If it's NOT dyeable, the factory base color matters a lot!
            // Two non-dyeable outfits with different factory colors are different outfits.
            // We keep the Id, Variant, and Dye (bits 0 to 47).
            visualSignature = modelMain & 0xFFFFFFFFFFFF;
        }

        if (item.Value.ModelSub != 0) {
            ulong subSignature = item.Value.DyeCount > 0 ? (item.Value.ModelSub & 0xFFFFFFFF) : (item.Value.ModelSub & 0xFFFFFFFFFFFF);
            visualSignature ^= (subSignature << 13) | (subSignature >> 51);
        }
        
        // We must include the EquipSlotCategory to distinguish them.
        var slotCategory = item.Value.EquipSlotCategory;
        
        // If it has no EquipSlot, it is not gear (e.g. food, potions, materials)
        if (slotCategory == 0) {
            modelCache[itemId] = 0;
            return 0;
        }

        // Shift EquipSlotCategory to bits 48-63 so it doesn't overlap with Dye (which is 32-47).
        visualSignature |= ((ulong)slotCategory << 48);
        
        modelCache[itemId] = visualSignature;
        return visualSignature;
    }
}
