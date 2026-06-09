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
    private readonly Func<uint, IEnumerable<uint>> _outfitProvider;

    public InventoryWatcher(ModelScanner modelScanner, Configuration config, IGameMemoryProvider memoryProvider, Func<IEnumerable<(uint ItemId, uint RowId)>>? cabinetProvider = null, Func<uint, IEnumerable<uint>>? outfitProvider = null) {
        _modelScanner = modelScanner;
        _config = config;
        _memoryProvider = memoryProvider;
        _cabinetProvider = cabinetProvider ?? DefaultCabinetProvider;
        _outfitProvider = outfitProvider ?? DefaultOutfitProvider;
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<(uint ItemId, uint RowId)> DefaultCabinetProvider() {
        var cabinetSheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>();
        if (cabinetSheet == null) return Array.Empty<(uint, uint)>();
        return cabinetSheet.Select(r => (r.Item.RowId, r.RowId));
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<uint> DefaultOutfitProvider(uint itemId) {
        var sheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.MirageStoreSetItem>();
        if (sheet == null) yield break;

        var row = sheet.GetRowOrDefault(itemId);
        if (row == null) yield break;

        if (row.Value.MainHand.RowId != 0) yield return row.Value.MainHand.RowId;
        if (row.Value.OffHand.RowId != 0) yield return row.Value.OffHand.RowId;
        if (row.Value.Head.RowId != 0) yield return row.Value.Head.RowId;
        if (row.Value.Body.RowId != 0) yield return row.Value.Body.RowId;
        if (row.Value.Hands.RowId != 0) yield return row.Value.Hands.RowId;
        if (row.Value.Legs.RowId != 0) yield return row.Value.Legs.RowId;
        if (row.Value.Feet.RowId != 0) yield return row.Value.Feet.RowId;
        if (row.Value.Earrings.RowId != 0) yield return row.Value.Earrings.RowId;
        if (row.Value.Necklace.RowId != 0) yield return row.Value.Necklace.RowId;
        if (row.Value.Bracelets.RowId != 0) yield return row.Value.Bracelets.RowId;
        if (row.Value.Ring.RowId != 0) yield return row.Value.Ring.RowId;
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
            .Select(row => new { ItemId = row.ItemId, ModelId = _modelScanner.GetModelId(row.ItemId), SharedModelId = _modelScanner.GetSharedModelId(row.ItemId), IsDyeable = _modelScanner.IsDyeable(row.ItemId) })
            .Where(x => x.ModelId != 0)
            .ToList();

        var newArmoireIds = new HashSet<ulong>(unlockedItems.Select(x => x.ModelId));

        if (!_config.ArmoireModelIds.SetEquals(newArmoireIds)) {
            _config.ArmoireModelIds = newArmoireIds;
            _config.ArmoireItemsByModel = unlockedItems
                .GroupBy(x => x.ModelId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
            
            _config.ArmoireItemsBySharedModel = unlockedItems
                .GroupBy(x => x.SharedModelId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
            
            // Note: Armoire doesn't typically have dyeable items, but we process them for completeness.
            foreach (var item in unlockedItems) {
                if (!_config.DresserSharedModels.TryGetValue(item.SharedModelId, out bool isDyeable) || (!isDyeable && item.IsDyeable)) {
                    _config.DresserSharedModels[item.SharedModelId] = item.IsDyeable;
                }
            }
            updated = true;
        }
    }

    private void ScanDresser(ref bool updated) {
        var dresserItems = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        if (dresserItems.Length == 0) return;

        var validItems = new List<(uint ItemId, ulong ModelId, ulong SharedModelId, bool IsDyeable)>();
        for (int i = 0; i < dresserItems.Length; i++) {
            var itemId = dresserItems[i];
            if (itemId == 0) continue;
            
            uint noFlags = itemId & 0x00FFFFFF;
            uint actualItemId = noFlags % 100000;
            
            var modelId = _modelScanner.GetModelId(actualItemId);
            if (modelId != 0) {
                validItems.Add((actualItemId, modelId, _modelScanner.GetSharedModelId(actualItemId), _modelScanner.IsDyeable(actualItemId)));
            } else {
                foreach (var innerItem in _outfitProvider(actualItemId)) {
                    var innerModelId = _modelScanner.GetModelId(innerItem);
                    if (innerModelId != 0) {
                        validItems.Add((actualItemId, innerModelId, _modelScanner.GetSharedModelId(innerItem), _modelScanner.IsDyeable(innerItem)));
                    }
                }
            }
        }

        var newDresserIds = new HashSet<ulong>(validItems.Select(x => x.ModelId));
        if (!_config.DresserModelIds.SetEquals(newDresserIds)) {
            _config.DresserModelIds = newDresserIds;
            updated = true;
        }

        _config.DresserItemsByModel = validItems
            .GroupBy(x => x.ModelId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());

        _config.DresserItemsBySharedModel = validItems
            .GroupBy(x => x.SharedModelId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());

        _config.DresserSharedModels.Clear();
        foreach (var item in validItems) {
            if (!_config.DresserSharedModels.TryGetValue(item.SharedModelId, out bool isDyeable) || (!isDyeable && item.IsDyeable)) {
                _config.DresserSharedModels[item.SharedModelId] = item.IsDyeable;
            }
        }
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
                    if (modelId == 0) continue;

                    var sharedModelId = _modelScanner.GetSharedModelId(actualItemId);
                    var isDyeable = _modelScanner.IsDyeable(actualItemId);

                    if (_config.DresserSharedModels.TryGetValue(sharedModelId, out bool dresserHasDyeable)) {
                        if (dresserHasDyeable) {
                            // The dresser already has the dyeable version. This item is fully superseded.
                            continue;
                        }

                        if (!dresserHasDyeable && isDyeable) {
                            // Dresser has non-dyeable, but inventory has dyeable. This is an upgrade!
                            if (!_config.HasModel(modelId)) {
                                result.Add(new InventoryItemInfo {
                                    ItemId = actualItemId,
                                    ModelId = modelId,
                                    ContainerType = type,
                                    Slot = i,
                                    IsDyeableUpgrade = true
                                });
                            }
                            continue;
                        }
                        
                        // If dresser has non-dyeable, and inv has non-dyeable, we fall back to strict GetModelId check
                    }

                    if (!_config.HasModel(modelId)) {
                        result.Add(new InventoryItemInfo {
                            ItemId = actualItemId,
                            ModelId = modelId,
                            ContainerType = type,
                            Slot = i,
                            IsDyeableUpgrade = false
                        });
                    }
                }
            }
        }

        return result;
    }

    public List<DuplicateAppearance> GetDuplicates() {
        var dresserEntries = _config.DresserItemsBySharedModel ?? new Dictionary<ulong, List<uint>>();
        var armoireEntries = _config.ArmoireItemsBySharedModel ?? new Dictionary<ulong, List<uint>>();

        var rawDuplicates = dresserEntries.Concat(armoireEntries)
            .GroupBy(kvp => kvp.Key)
            .Select(g => new DuplicateAppearance {
                ModelId = g.Key,
                ItemIds = g.SelectMany(kvp => kvp.Value).Distinct().ToList()
            })
            .Where(d => d.ItemIds.Count > 1)
            .ToList();

        foreach (var dup in rawDuplicates) {
            dup.ItemIds = dup.ItemIds.OrderByDescending(id => GetItemScore(id)).ToList();
        }

        return rawDuplicates;
    }

    private int GetItemScore(uint itemId) {
        int score = 0;
        var itemSheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRowOrDefault(itemId);
        if (itemSheet == null) return 0;
        
        if (itemSheet.Value.DyeCount > 0) score += 1000;
        
        var catId = itemSheet.Value.ClassJobCategory.RowId;
        if (catId == 1) score += 500;
        else if (catId == 30 || catId == 31 || catId == 32 || catId == 33) score += 250;
        
        score += (int)itemSheet.Value.LevelItem.RowId;

        return score;
    }
}

[ExcludeFromCodeCoverage]
public class InventoryItemInfo {
    public uint ItemId { get; set; }
    public ulong ModelId { get; set; }
    public InventoryType ContainerType { get; set; }
    public int Slot { get; set; }
    public bool IsDyeableUpgrade { get; set; }
}

[ExcludeFromCodeCoverage]
public class DuplicateAppearance {
    public ulong ModelId { get; set; }
    public List<uint> ItemIds { get; set; } = new();
}
