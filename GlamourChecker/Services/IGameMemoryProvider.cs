using System;
using System.Collections.Generic;

namespace GlamourChecker.Core;

public interface IGameMemoryProvider {
    unsafe Span<uint> GetMirageManagerPrismBoxItemIds();
    unsafe Span<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> GetInventoryContainer(FFXIVClientStructs.FFXIV.Client.Game.InventoryType type);
    HashSet<uint> GetGearsetItems();
    bool IsCabinetLoaded();
    bool IsItemInCabinet(uint itemId);
}
