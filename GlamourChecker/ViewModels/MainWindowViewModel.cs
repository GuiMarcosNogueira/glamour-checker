using System;
using System.Collections.Generic;
using System.Linq;
using GlamourChecker.Core;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourChecker.ViewModels;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class MainWindowViewModel
{
    private readonly GlamourLogic _logic;
    private readonly InventoryWatcher _watcher;
    public Configuration Config { get; }
    private readonly Func<uint, (string Name, uint Category, uint LevelItem)?> _itemSheetLookup;
    private readonly Func<uint, string?>? _outfitNameLookup;

    public string[] Categories { get; private set; } = Array.Empty<string>();

    public string? GetOutfitName(uint itemId)
    {
        return _outfitNameLookup?.Invoke(itemId);
    }

    private int _selectedCategoryIndex = 0;
    public int SelectedCategoryIndex
    {
        get => _selectedCategoryIndex;
        set
        {
            if (_selectedCategoryIndex != value)
            {
                _selectedCategoryIndex = value;
                RefreshLists();
            }
        }
    }

    public void IgnoreNewAppearanceItem(uint itemId)
    {
        Config.IgnoredItemIds.Add(itemId);
        Config.Save();
        RefreshLists();
    }

    public void IgnoreDuplicateItem(uint itemId)
    {
        Config.IgnoredDuplicateItemIds.Add(itemId);
        Config.Save();
        RefreshLists();
    }

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                RefreshLists();
            }
        }
    }

    public bool HideGearsetItems
    {
        get => Config.HideGearsetItems;
        set
        {
            if (Config.HideGearsetItems != value)
            {
                Config.HideGearsetItems = value;
                Config.Save();
                RefreshLists();
            }
        }
    }

    public List<InventoryItemInfo> NewAppearances { get; private set; } = new();
    public List<DuplicateAppearance> Duplicates { get; private set; } = new();

    public List<InventoryItemInfo> IgnoredNewAppearances { get; private set; } = new();
    public List<DuplicateAppearance> IgnoredDuplicates { get; private set; } = new();

    public List<SlotGroup<IGrouping<ulong, InventoryItemInfo>>> GroupedNewAppearances { get; private set; } = new();
    public List<SlotGroup<DuplicateAppearance>> GroupedDuplicates { get; private set; } = new();
    public List<SlotGroup<IGrouping<ulong, InventoryItemInfo>>> GroupedIgnoredNewAppearances { get; private set; } = new();
    public List<SlotGroup<DuplicateAppearance>> GroupedIgnoredDuplicates { get; private set; } = new();

    public MainWindowViewModel(GlamourLogic logic, InventoryWatcher watcher, Configuration config, Func<uint, (string Name, uint Category, uint LevelItem)?> itemSheetLookup, Func<uint, string?>? outfitNameLookup = null)
    {
        _logic = logic;
        _watcher = watcher;
        Config = config;
        _itemSheetLookup = itemSheetLookup;
        _outfitNameLookup = outfitNameLookup;

        ReloadCategories();
        RefreshLists();
    }

    public void ReloadCategories()
    {
        Categories = new[] {
            Loc.Localize("Category_All", "All"),
            Loc.Localize("Category_Inventory", "Inventory"),
            Loc.Localize("Category_ArmoryMainOff", "Armoury Chest (Main Hand/Off Hand)"),
            Loc.Localize("Category_ArmoryHeadBodyHands", "Armoury Chest (Head/Body/Hands)"),
            Loc.Localize("Category_ArmoryLegsFeet", "Armoury Chest (Legs/Feet)"),
            Loc.Localize("Category_ArmoryEarsNeck", "Armoury Chest (Ears/Neck)"),
            Loc.Localize("Category_ArmoryWristsFingers", "Armoury Chest (Wrists/Fingers)"),
            Loc.Localize("Category_Saddlebag", "Chocobo Saddlebag"),
            Loc.Localize("Category_Retainer", "Retainer Inventory")
        };
    }

    public void ScanDresserAndArmoire()
    {
        _watcher.ScanDresserAndArmoire();
        RefreshLists();
    }

    public void RefreshLists()
    {
        var categoryName = Categories[SelectedCategoryIndex];
        NewAppearances = _logic.GetFilteredNewAppearances(SelectedCategoryIndex, SearchQuery, categoryName, _itemSheetLookup);
        Duplicates = _logic.GetFilteredDuplicates(SelectedCategoryIndex, SearchQuery, categoryName, _itemSheetLookup);

        IgnoredNewAppearances = BuildIgnoredNewAppearances();
        IgnoredDuplicates = BuildIgnoredDuplicates();

        GroupedNewAppearances = ItemCategoryHelper.GroupInventoryItems(NewAppearances, _itemSheetLookup);
        GroupedDuplicates = ItemCategoryHelper.GroupDuplicates(Duplicates, _itemSheetLookup);
        GroupedIgnoredNewAppearances = ItemCategoryHelper.GroupInventoryItems(IgnoredNewAppearances, _itemSheetLookup);
        GroupedIgnoredDuplicates = ItemCategoryHelper.GroupDuplicates(IgnoredDuplicates, _itemSheetLookup);
    }

    private List<InventoryItemInfo> BuildIgnoredNewAppearances()
    {
        var result = new List<InventoryItemInfo>();
        var categoryName = Categories[SelectedCategoryIndex];

        foreach (var id in Config.IgnoredItemIds)
        {
            var sheet = _itemSheetLookup(id);
            if (sheet.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(SearchQuery) && !sheet.Value.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) continue;
                if (SelectedCategoryIndex > 0)
                {
                    var itemCategoryName = _logic.GetCategoryName(_logic.MapEquipSlotToInventoryType(sheet.Value.Category));
                    if (itemCategoryName != categoryName) continue;
                }

                result.Add(new InventoryItemInfo
                {
                    ItemId = id,
                    ContainerType = InventoryType.Inventory1,
                    ModelId = _watcher.GetModelId(id),
                    IsDyeableUpgrade = false
                });
            }
        }
        return result
            .OrderByDescending(x => _itemSheetLookup(x.ItemId)?.LevelItem ?? 0)
            .ThenBy(x => x.ItemId)
            .ToList();
    }

    private List<DuplicateAppearance> BuildIgnoredDuplicates()
    {
        var groups = new Dictionary<ulong, List<uint>>();
        var zeroGroupItems = new List<uint>();
        var categoryName = Categories[SelectedCategoryIndex];

        foreach (var id in Config.IgnoredDuplicateItemIds)
        {
            var sheet = _itemSheetLookup(id);
            if (sheet.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(SearchQuery) && !sheet.Value.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) continue;
                if (SelectedCategoryIndex > 0)
                {
                    var itemCategoryName = _logic.GetCategoryName(_logic.MapEquipSlotToInventoryType(sheet.Value.Category));
                    if (itemCategoryName != categoryName) continue;
                }
                ulong duplicateGroupId = _watcher.GetDuplicateGroupId(id);
                if (duplicateGroupId == 0)
                {
                    zeroGroupItems.Add(id);
                }
                else
                {
                    if (!groups.ContainsKey(duplicateGroupId)) groups[duplicateGroupId] = new();
                    groups[duplicateGroupId].Add(id);
                }
            }
        }

        var result = new List<DuplicateAppearance>();
        foreach (var kvp in groups)
        {
            result.Add(new DuplicateAppearance { ModelId = kvp.Key, ItemIds = kvp.Value });
        }
        foreach (var id in zeroGroupItems)
        {
            result.Add(new DuplicateAppearance { ModelId = 0, ItemIds = new List<uint> { id } });
        }

        return result
            .OrderByDescending(x => _itemSheetLookup(x.ItemIds.FirstOrDefault())?.LevelItem ?? 0)
            .ThenBy(x => x.ItemIds.FirstOrDefault())
            .ToList();
    }

    public string GetCategoryName(InventoryType type)
    {
        return _logic.GetCategoryName(type);
    }

    public bool IsDyeable(uint itemId)
    {
        return _watcher.IsDyeable(itemId);
    }
}
