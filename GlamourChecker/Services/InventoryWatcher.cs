using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourChecker.Core;

public unsafe class InventoryWatcher {
    private readonly ModelScanner _modelScanner;
    private readonly Configuration _config;
    private readonly IGameMemoryProvider _memoryProvider;
    private readonly Func<IEnumerable<(uint ItemId, uint RowId)>> _cabinetProvider;

    public InventoryWatcher(ModelScanner modelScanner, Configuration config, IGameMemoryProvider memoryProvider, Func<IEnumerable<(uint ItemId, uint RowId)>>? cabinetProvider = null) {
        _modelScanner = modelScanner;
        _config = config;
        _memoryProvider = memoryProvider;
        _cabinetProvider = cabinetProvider ?? DefaultCabinetProvider;
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<(uint ItemId, uint RowId)> DefaultCabinetProvider() {
        var cabinetSheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>();
        if (cabinetSheet == null) return Array.Empty<(uint, uint)>();
        return cabinetSheet.Select(r => (r.Item.RowId, r.RowId));
    }

    private ulong _lastDresserHash = 0;

    public bool CheckDresserChanges() {
        var dresserItems = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        if (dresserItems.Length == 0) return false;

        ulong hash = 0;
        bool hasAnyItem = false;
        for(int i = 0; i < dresserItems.Length; i++) {
            var id = dresserItems[i];
            hash = unchecked(hash * 31 + id);
            if (id != 0) hasAnyItem = true;
        }

        // Se o array de memória estiver completamente zerado (ex: jogador saiu da pousada), ignoramos
        if (!hasAnyItem) return false;

        if (hash != _lastDresserHash) {
            _lastDresserHash = hash;
            return true;
        }
        return false;
    }

    public void ScanDresserAndArmoire() {
        bool updated = false;

        ScanArmoire(ref updated);
        ScanDresser(ref updated);

        if (updated) {
            _config.StoredModelIds.Clear();
            _config.Save();
        }
    }

    private void ScanArmoire(ref bool updated) {
        if (!_memoryProvider.IsCabinetLoaded()) return;

        var unlockedItems = _cabinetProvider()
            .Where(row => row.ItemId != 0 && (_memoryProvider.IsItemInCabinet((uint)row.ItemId) || _memoryProvider.IsItemInCabinet((uint)row.RowId)))
            .Select(row => new { ItemId = row.ItemId, ModelId = _modelScanner.GetModelId(row.ItemId) })
            .Where(x => x.ModelId != 0)
            .ToList();

        var newArmoireIds = new HashSet<ulong>(unlockedItems.Select(x => x.ModelId));

        if (!_config.ArmoireModelIds.SetEquals(newArmoireIds)) {
            _config.ArmoireModelIds = newArmoireIds;
            _config.ArmoireItemsByModel = unlockedItems
                .GroupBy(x => x.ModelId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
            updated = true;
        }
    }

    private void ScanDresser(ref bool updated) {
        var dresserItems = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        if (dresserItems.Length == 0) return;

        var validItems = new List<(uint ItemId, ulong ModelId)>();
        for (int i = 0; i < dresserItems.Length; i++) {
            var itemId = dresserItems[i];
            if (itemId == 0) continue;
            
            var actualItemId = itemId > 1000000 ? itemId - 1000000 : itemId;
            var modelId = _modelScanner.GetModelId(actualItemId);
            if (modelId != 0) {
                validItems.Add((actualItemId, modelId));
            }
        }

        _config.DresserModelIds = new HashSet<ulong>(validItems.Select(x => x.ModelId));
        _config.DresserItemsByModel = validItems
            .GroupBy(x => x.ModelId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
        updated = true;
    }

    private static readonly InventoryType[] TypesToCheck = {
        InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4,
        InventoryType.ArmoryMainHand, InventoryType.ArmoryHead, InventoryType.ArmoryBody, InventoryType.ArmoryHands,
        InventoryType.ArmoryLegs, InventoryType.ArmoryFeets, InventoryType.ArmoryEar, InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist, InventoryType.ArmoryRings, InventoryType.SaddleBag1, InventoryType.SaddleBag2,
        InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag2, InventoryType.RetainerPage1,
        InventoryType.RetainerPage2, InventoryType.RetainerPage3, InventoryType.RetainerPage4,
        InventoryType.RetainerPage5, InventoryType.RetainerPage6, InventoryType.RetainerPage7
    };

    public List<InventoryItemInfo> GetUnstoredItemsInBags() {
        ScanDresserAndArmoire();
        
        var result = new List<InventoryItemInfo>();

        foreach (var type in TypesToCheck) {
            var items = _memoryProvider.GetInventoryContainer(type);
            if (items.Length == 0) continue;

            for (int i = 0; i < items.Length; i++) {
                var item = items[i];
                if (item.ItemId != 0) {
                    var actualItemId = item.ItemId > 1000000 ? item.ItemId - 1000000 : item.ItemId;
                    var modelId = _modelScanner.GetModelId(actualItemId);
                    if (modelId != 0 && !_config.HasModel(modelId)) {
                        result.Add(new InventoryItemInfo {
                            ItemId = actualItemId,
                            ModelId = modelId,
                            ContainerType = type,
                            Slot = i
                        });
                    }
                }
            }
        }

        return result;
    }

    public List<DuplicateAppearance> GetDuplicates() {
        var dresserEntries = _config.DresserItemsByModel ?? new Dictionary<ulong, List<uint>>();
        var armoireEntries = _config.ArmoireItemsByModel ?? new Dictionary<ulong, List<uint>>();

        return dresserEntries.Concat(armoireEntries)
            .GroupBy(kvp => kvp.Key)
            .Select(g => new DuplicateAppearance {
                ModelId = g.Key,
                ItemIds = g.SelectMany(kvp => kvp.Value).ToList()
            })
            .Where(d => d.ItemIds.Count > 1)
            .ToList();
    }
}

[ExcludeFromCodeCoverage]
public class InventoryItemInfo {
    public uint ItemId { get; set; }
    public ulong ModelId { get; set; }
    public InventoryType ContainerType { get; set; }
    public int Slot { get; set; }
}

[ExcludeFromCodeCoverage]
public class DuplicateAppearance {
    public ulong ModelId { get; set; }
    public List<uint> ItemIds { get; set; } = new();
}
