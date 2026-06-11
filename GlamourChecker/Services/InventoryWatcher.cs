using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourChecker.Core;

public unsafe class InventoryWatcher
{
    private readonly ModelScanner _modelScanner;
    private readonly Configuration _config;
    private readonly IGameMemoryProvider _memoryProvider;
    private readonly Func<IEnumerable<(uint ItemId, uint RowId)>> _cabinetProvider;
    private readonly Func<uint, IEnumerable<uint>> _outfitProvider;
    public InventoryWatcher(ModelScanner modelScanner, Configuration config, IGameMemoryProvider memoryProvider, Func<IEnumerable<(uint ItemId, uint RowId)>>? cabinetProvider = null, Func<uint, IEnumerable<uint>>? outfitProvider = null)
    {
        _modelScanner = modelScanner;
        _config = config;
        _memoryProvider = memoryProvider;
        _cabinetProvider = cabinetProvider ?? DefaultCabinetProvider;
        _outfitProvider = outfitProvider ?? DefaultOutfitProvider;
    }

    public bool IsDyeable(uint itemId)
    {
        return _modelScanner.IsDyeable(itemId);
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<(uint ItemId, uint RowId)> DefaultCabinetProvider()
    {
        var cabinetSheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>();
        if (cabinetSheet == null) return Array.Empty<(uint, uint)>();
        return cabinetSheet.Select(r => (r.Item.RowId, r.RowId));
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<uint> DefaultOutfitProvider(uint itemId)
    {
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
    private bool _wasDresserOpen = false;
    private bool _wasCabinetOpen = false;
    private bool _wasCabinetLoaded = false;

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private bool IsAddonVisible(string name)
    {
        if (Services.GameGui == null) return false;
        return IsAddonVisibleInternal(name);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private bool IsAddonVisibleInternal(string name)
    {
        nint addonPtr = Services.GameGui.GetAddonByName(name, 1);
        if (addonPtr == nint.Zero) return false;
        return ((FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)addonPtr)->IsVisible;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public bool CheckStorageChanges(out bool justOpened, out bool justClosed)
    {
        justOpened = false;
        justClosed = false;
        bool changed = false;

        bool isDresserOpen = IsAddonVisible("MiragePrismPrismBox") || IsAddonVisible("MiragePrismBox");
        if (isDresserOpen && !_wasDresserOpen)
        {
            justOpened = true;
            changed = true;
        }
        else if (!isDresserOpen && _wasDresserOpen)
        {
            justClosed = true;
        }
        _wasDresserOpen = isDresserOpen;

        var dresserItems = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        if (dresserItems.Length > 0)
        {
            ulong hash = 0;
            for (int i = 0; i < dresserItems.Length; i++)
            {
                var id = dresserItems[i];
                hash = unchecked(hash * 31 + id);
            }

            if (hash != _lastDresserHash)
            {
                _lastDresserHash = hash;
                changed = true;
            }
        }

        bool isCabinetOpen = IsAddonVisible("Cabinet");
        if (isCabinetOpen && !_wasCabinetOpen)
        {
            justOpened = true;
            changed = true;
        }
        else if (!isCabinetOpen && _wasCabinetOpen)
        {
            justClosed = true;
        }
        _wasCabinetOpen = isCabinetOpen;

        bool isCabinetLoaded = _memoryProvider.IsCabinetLoaded();
        if (isCabinetLoaded)
        {
            if (!_wasCabinetLoaded)
            {
                _wasCabinetLoaded = true;
                changed = true; // Force a scan when it finishes loading
            }
        }
        else
        {
            _wasCabinetLoaded = false;
        }

        return changed || justOpened || justClosed;
    }

    public void ScanDresserAndArmoire()
    {
        bool updated = false;

        ScanArmoire(ref updated);
        ScanDresser(ref updated);

        if (updated)
        {
            _config.StoredModelIds.Clear();
            _config.Save();
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private void ScanArmoire(ref bool updated)
    {
        if (!_memoryProvider.IsCabinetLoaded()) return;

        var unlockedItems = _cabinetProvider()
            .Where(row => row.ItemId != 0 && (_memoryProvider.IsItemInCabinet((uint)row.ItemId) || _memoryProvider.IsItemInCabinet((uint)row.RowId)))
            .Select(row => new { ItemId = row.ItemId, ModelId = _modelScanner.GetModelId(row.ItemId), SharedModelId = _modelScanner.GetSharedModelId(row.ItemId), IsDyeable = _modelScanner.IsDyeable(row.ItemId) })
            .Where(x => x.ModelId != 0)
            .ToList();

        var newArmoireIds = new HashSet<ulong>(unlockedItems.Select(x => x.ModelId));

        if (!_config.ArmoireModelIds.SetEquals(newArmoireIds))
        {
            _config.ArmoireModelIds = newArmoireIds;
            _config.ArmoireItemsByModel = unlockedItems
                .GroupBy(x => x.ModelId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());

            _config.ArmoireItemsBySharedModel = unlockedItems
                .GroupBy(x => x.SharedModelId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());

            // Note: Armoire doesn't typically have dyeable items, but we process them for completeness.
            foreach (var item in unlockedItems)
            {
                if (!_config.DresserSharedModels.TryGetValue(item.SharedModelId, out bool isDyeable) || (!isDyeable && item.IsDyeable))
                {
                    _config.DresserSharedModels[item.SharedModelId] = item.IsDyeable;
                }
            }
            updated = true;
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private void ScanDresser(ref bool updated)
    {
        var dresserItems = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        if (dresserItems.Length == 0) return;

        bool hasAnyItem = false;
        for (int i = 0; i < dresserItems.Length; i++)
        {
            if (dresserItems[i] != 0)
            {
                hasAnyItem = true;
                break;
            }
        }

        // If the memory array is completely zeroed out (e.g., player left the inn), do not wipe the config
        if (!hasAnyItem) return;

        var validItems = new List<(uint ItemId, ulong ModelId, ulong SharedModelId, bool IsDyeable)>();
        for (int i = 0; i < dresserItems.Length; i++)
        {
            var itemId = dresserItems[i];
            if (itemId == 0) continue;

            uint noFlags = itemId & 0x00FFFFFF;
            uint actualItemId = noFlags % 100000;

            var modelId = _modelScanner.GetModelId(actualItemId);
            if (modelId != 0)
            {
                validItems.Add((actualItemId, modelId, _modelScanner.GetSharedModelId(actualItemId), _modelScanner.IsDyeable(actualItemId)));
            }
            else
            {
                foreach (var innerItem in _outfitProvider(actualItemId))
                {
                    var innerModelId = _modelScanner.GetModelId(innerItem);
                    if (innerModelId != 0)
                    {
                        validItems.Add((innerItem, innerModelId, _modelScanner.GetSharedModelId(innerItem), _modelScanner.IsDyeable(innerItem)));
                    }
                }
            }
        }

        var newDresserIds = new HashSet<ulong>(validItems.Select(x => x.ModelId));
        if (!_config.DresserModelIds.SetEquals(newDresserIds))
        {
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
        _config.DresserSharedModelScores.Clear();
        _config.DresserVisualGroupScores.Clear();
        foreach (var item in validItems)
        {
            if (!_config.DresserSharedModels.TryGetValue(item.SharedModelId, out bool isDyeable) || (!isDyeable && item.IsDyeable))
            {
                _config.DresserSharedModels[item.SharedModelId] = item.IsDyeable;
            }

            var score = GetItemVersatilityScore(item.ItemId);
            if (!_config.DresserSharedModelScores.TryGetValue(item.SharedModelId, out int existingScore) || score > existingScore)
            {
                _config.DresserSharedModelScores[item.SharedModelId] = score;
            }

            if (item.IsDyeable)
            {
                var visualId = _modelScanner.GetVisualGroupId(item.ItemId);
                if (visualId != 0)
                {
                    if (!_config.DresserVisualGroupScores.TryGetValue(visualId, out int existingVis) || score > existingVis)
                    {
                        _config.DresserVisualGroupScores[visualId] = score;
                    }
                }
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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public List<InventoryItemInfo> GetUnstoredItemsInBags()
    {
        ScanDresserAndArmoire();

        var result = new List<InventoryItemInfo>();

        foreach (var type in TypesToCheck)
        {
            var items = _memoryProvider.GetInventoryContainer(type);
            if (items.Length == 0) continue;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.ItemId != 0)
                {
                    var actualItemId = item.ItemId > 1000000 ? item.ItemId - 1000000 : item.ItemId;
                    if (_config.IgnoredItemIds.Contains(actualItemId)) continue;
                    var modelId = _modelScanner.GetModelId(actualItemId);
                    if (modelId == 0) continue;

                    var sharedModelId = _modelScanner.GetSharedModelId(actualItemId);
                    var visualGroupId = _modelScanner.GetVisualGroupId(actualItemId);
                    var isDyeable = _modelScanner.IsDyeable(actualItemId);
                    var itemVersatilityScore = GetItemVersatilityScore(actualItemId);

                    bool isSuperseded = false;

                    // 1. Check Native Shared Model
                    if (_config.DresserSharedModelScores.TryGetValue(sharedModelId, out int dresserScore))
                    {
                        if (itemVersatilityScore <= dresserScore)
                        {
                            isSuperseded = true;
                        }
                    }
                    else if (_config.DresserSharedModels.TryGetValue(sharedModelId, out bool dresserHasDyeable))
                    {
                        // Fallback for old caches without Scores
                        if (dresserHasDyeable)
                        {
                            isSuperseded = true;
                        }
                    }

                    // 2. Visual Group check removed! New Appearances now uses STRICT NATIVE mode only.

                    if (isSuperseded)
                    {
                        continue;
                    }

                    // It's an upgrade or completely missing! Add it.
                    if (!_config.HasModel(modelId))
                    {
                        result.Add(new InventoryItemInfo
                        {
                            ItemId = actualItemId,
                            ModelId = modelId,
                            ContainerType = type,
                            Slot = i,
                            IsDyeableUpgrade = _config.DresserSharedModels.ContainsKey(sharedModelId)
                        });
                    }
                }
            }
        }

        return result;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public List<DuplicateAppearance> GetDuplicates()
    {
        var allStoredItemIds = new List<uint>();

        if (_config.DresserItemsBySharedModel != null)
        {
            foreach (var list in _config.DresserItemsBySharedModel.Values)
            {
                foreach (var id in list)
                {
                    if (!_config.IgnoredDuplicateItemIds.Contains(id)) allStoredItemIds.Add(id);
                }
            }
        }
        if (_config.ArmoireItemsBySharedModel != null)
        {
            foreach (var list in _config.ArmoireItemsBySharedModel.Values)
            {
                foreach (var id in list)
                {
                    if (!_config.IgnoredDuplicateItemIds.Contains(id)) allStoredItemIds.Add(id);
                }
            }
        }

        var itemsByGroup = allStoredItemIds
            .GroupBy(id =>
            {
                var vis = _modelScanner.GetVisualGroupId(id);
                return vis != 0 ? vis : _modelScanner.GetSharedModelId(id);
            })
            .ToDictionary(g => g.Key, g => g.ToList());

        var rawDuplicates = new List<DuplicateAppearance>();

        foreach (var kvp in itemsByGroup)
        {
            var sharedModelId = kvp.Key;
            var itemIds = kvp.Value;

            if (itemIds.Count <= 1) continue;

            bool hasDyeable = itemIds.Any(id => IsItemDyeableForDuplicates(id));

            if (hasDyeable)
            {
                rawDuplicates.Add(new DuplicateAppearance
                {
                    ModelId = sharedModelId,
                    ItemIds = itemIds
                });
            }
            else
            {
                var exactGroups = new Dictionary<ulong, List<uint>>();

                foreach (var id in itemIds)
                {
                    var modelId = _modelScanner.GetModelId(id);
                    if (modelId != 0)
                    {
                        // Normal item
                        if (!exactGroups.ContainsKey(modelId)) exactGroups[modelId] = new List<uint>();
                        exactGroups[modelId].Add(id);
                    }
                    else
                    {
                        // Outfit Box or 0 model id
                        bool isOutfit = false;
                        foreach (var inner in _outfitProvider(id))
                        {
                            var innerModelId = _modelScanner.GetModelId(inner);
                            if (innerModelId != 0)
                            {
                                isOutfit = true;
                                if (!exactGroups.ContainsKey(innerModelId)) exactGroups[innerModelId] = new List<uint>();
                                exactGroups[innerModelId].Add(id);
                            }
                        }
                        if (!isOutfit)
                        {
                            // Fallback for mocked or invalid items with 0 model id
                            if (!exactGroups.ContainsKey(0)) exactGroups[0] = new List<uint>();
                            exactGroups[0].Add(id);
                        }
                    }
                }

                foreach (var kvpExact in exactGroups)
                {
                    var exactItemIds = kvpExact.Value;
                    // Because an Outfit Box could be the only item in this group, we need to make sure 
                    // we actually have more than 1 item before declaring it a duplicate!
                    if (exactItemIds.Count > 1)
                    {
                        rawDuplicates.Add(new DuplicateAppearance
                        {
                            ModelId = kvpExact.Key,
                            ItemIds = exactItemIds
                        });
                    }
                }
            }
        }

        foreach (var dup in rawDuplicates)
        {
            dup.ItemIds = dup.ItemIds.OrderByDescending(id => GetItemScore(id)).ToList();
        }

        return rawDuplicates;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private ulong GetItemModelIdForDuplicates(uint itemId)
    {
        var id = _modelScanner.GetModelId(itemId);
        if (id != 0) return id;

        foreach (var inner in _outfitProvider(itemId))
        {
            var innerId = _modelScanner.GetModelId(inner);
            if (innerId != 0) return innerId;
        }
        return 0;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private bool IsItemDyeableForDuplicates(uint itemId)
    {
        if (_modelScanner.IsDyeable(itemId)) return true;

        var id = _modelScanner.GetModelId(itemId);
        if (id == 0)
        {
            foreach (var inner in _outfitProvider(itemId))
            {
                if (_modelScanner.IsDyeable(inner)) return true;
            }
        }
        return false;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private int GetItemVersatilityScore(uint itemId)
    {
        int score = 0;
        var itemSheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRowOrDefault(itemId);

        if (itemSheet != null && itemSheet.Value.DyeCount > 0) score += 1000;
        else if (_modelScanner.IsDyeable(itemId)) score += 1000;

        if (itemSheet == null) return score;

        var catId = itemSheet.Value.ClassJobCategory.RowId;
        if (catId == 1) score += 500;
        else if (catId == 30 || catId == 31 || catId == 32 || catId == 33) score += 250;

        return score;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private int GetItemScore(uint itemId)
    {
        int score = GetItemVersatilityScore(itemId);
        var itemSheet = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRowOrDefault(itemId);
        if (itemSheet == null) return score;

        score += (int)itemSheet.Value.LevelItem.RowId;

        return score;
    }

}

[ExcludeFromCodeCoverage]
public class InventoryItemInfo
{
    public uint ItemId { get; set; }
    public ulong ModelId { get; set; }
    public InventoryType ContainerType { get; set; }
    public int Slot { get; set; }
    public bool IsDyeableUpgrade { get; set; }
}

[ExcludeFromCodeCoverage]
public class DuplicateAppearance
{
    public ulong ModelId { get; set; }
    public List<uint> ItemIds { get; set; } = new();
}
