using System;
using System.Collections.Generic;
using GlamourChecker.Core;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourChecker.ViewModels;

public class MainWindowViewModel {
    private readonly GlamourLogic _logic;
    private readonly InventoryWatcher _watcher;
    private readonly Configuration _config;
    private readonly Func<uint, (string Name, uint Category)?> _itemSheetLookup;

    public string[] Categories { get; private set; } = Array.Empty<string>();
    
    private int _selectedCategoryIndex = 0;
    public int SelectedCategoryIndex {
        get => _selectedCategoryIndex;
        set {
            if (_selectedCategoryIndex != value) {
                _selectedCategoryIndex = value;
                RefreshLists();
            }
        }
    }

    private string _searchQuery = "";
    public string SearchQuery {
        get => _searchQuery;
        set {
            if (_searchQuery != value) {
                _searchQuery = value;
                RefreshLists();
            }
        }
    }

    public bool HideGearsetItems {
        get => _config.HideGearsetItems;
        set {
            if (_config.HideGearsetItems != value) {
                _config.HideGearsetItems = value;
                _config.Save();
                RefreshLists();
            }
        }
    }

    public List<InventoryItemInfo> NewAppearances { get; private set; } = new();
    public List<DuplicateAppearance> Duplicates { get; private set; } = new();

    public MainWindowViewModel(GlamourLogic logic, InventoryWatcher watcher, Configuration config, Func<uint, (string Name, uint Category)?> itemSheetLookup) {
        _logic = logic;
        _watcher = watcher;
        _config = config;
        _itemSheetLookup = itemSheetLookup;

        ReloadCategories();
        RefreshLists();
    }

    public void ReloadCategories() {
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

    public void ScanDresserAndArmoire() {
        _watcher.ScanDresserAndArmoire();
        RefreshLists();
    }

    public void RefreshLists() {
        var categoryName = Categories[SelectedCategoryIndex];
        NewAppearances = _logic.GetFilteredNewAppearances(SelectedCategoryIndex, SearchQuery, categoryName, _itemSheetLookup);
        Duplicates = _logic.GetFilteredDuplicates(SelectedCategoryIndex, SearchQuery, categoryName, _itemSheetLookup);
    }

    public string GetCategoryName(InventoryType type) {
        return _logic.GetCategoryName(type);
    }
}
