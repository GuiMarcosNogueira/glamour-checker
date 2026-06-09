using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace GlamourChecker.Core;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class GameMemoryProvider : IGameMemoryProvider {
    public unsafe Span<uint> GetMirageManagerPrismBoxItemIds() {
        var mirageManager = MirageManager.Instance();
        if (mirageManager == null) return Span<uint>.Empty;
        return mirageManager->PrismBoxItemIds;
    }

    public unsafe Span<InventoryItem> GetInventoryContainer(InventoryType type) {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return Span<InventoryItem>.Empty;
        var container = inventoryManager->GetInventoryContainer(type);
        if (container == null) return Span<InventoryItem>.Empty;
        
        return new Span<InventoryItem>(container->Items, (int)container->Size);
    }

    public HashSet<uint> GetGearsetItems() {
        HashSet<uint> gearsetItems = new();
        unsafe {
            var gearsetMod = RaptureGearsetModule.Instance();
            if (gearsetMod != null) {
                var entries = gearsetMod->Entries;
                for (int i = 0; i < 100; i++) {
                    var entry = entries[i];
                    if (!entry.Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;
                    for (int j = 0; j < 14; j++) {
                        var itemId = entry.Items[j].ItemId;
                        if (itemId != 0) {
                            gearsetItems.Add(itemId > 1000000 ? itemId - 1000000 : itemId);
                        }
                    }
                }
            }
        }
        return gearsetItems;
    }

    public bool IsCabinetLoaded() {
        unsafe {
            var uiState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
            if (uiState != null) {
                return uiState->Cabinet.IsCabinetLoaded();
            }
            return false;
        }
    }

    public bool IsItemInCabinet(uint itemId) {
        unsafe {
            var uiState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
            if (uiState != null) {
                return uiState->Cabinet.IsItemInCabinet(itemId);
            }
            return false;
        }
    }
}
