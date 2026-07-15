using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GlamourChecker.Core;

public class UpgradeItemInfo
{
    public uint ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint ItemLevel { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public uint EquippedItemLevel { get; set; }
    public bool IsFromArmoire { get; set; }
}

public class UpgradeItemData
{
    public uint ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint EquipSlotCategory { get; set; }
    public uint LevelItem { get; set; }
    public uint LevelEquip { get; set; }
    public bool CanEquipJob { get; set; }
}

public class UpgradeAdvisorService : IDisposable
{
    private readonly IGameMemoryProvider _memoryProvider;
    private uint _lastJobId = 0;
    private bool _hasNotifiedThisSession = false;

    public List<UpgradeItemInfo> CurrentUpgrades { get; private set; } = new();
    public event System.Action? OnUpgradesFound;

    public UpgradeAdvisorService(IGameMemoryProvider memoryProvider)
    {
        _memoryProvider = memoryProvider;
        if (GlamourChecker.Services.Framework != null)
        {
            GlamourChecker.Services.Framework.Update += OnFrameworkUpdate;
        }
    }

    public void Dispose()
    {
        if (GlamourChecker.Services.Framework != null)
        {
            GlamourChecker.Services.Framework.Update -= OnFrameworkUpdate;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null || localPlayer.ClassJob.RowId == 0) return;

        uint currentJobId = localPlayer.ClassJob.RowId;

        if (_lastJobId != currentJobId)
        {
            _lastJobId = currentJobId;
            _hasNotifiedThisSession = false;
            var jobAbbrev = localPlayer.ClassJob.Value.Abbreviation.ToString();
            var currentLevel = localPlayer.Level;
            CheckForUpgrades(jobAbbrev, currentLevel);
        }
    }

    protected virtual IPlayerCharacter? GetLocalPlayer()
    {
        return GlamourChecker.Services.ObjectTable?.LocalPlayer;
    }

    protected virtual void PrintNotification(int count)
    {
        GlamourChecker.Services.Chat?.Print($"[GlamourChecker] 💡 {count} upgrades encontrados no Dresser/Armoire para o seu Job atual! Digite /gc upgrades para ver a lista.");
    }

    protected virtual uint[] GetDresserItems()
    {
        var span = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        var arr = new uint[span.Length];
        span.CopyTo(arr);
        return arr;
    }

    protected virtual (uint itemId, uint slotCategory, uint levelItem)[] GetEquippedGear()
    {
        var equippedGear = _memoryProvider.GetInventoryContainer(InventoryType.EquippedItems);
        var itemSheet = GlamourChecker.Services.DataManager?.GetExcelSheet<Item>();
        if (itemSheet == null) return Array.Empty<(uint, uint, uint)>();

        var result = new List<(uint, uint, uint)>();
        for (int i = 0; i < equippedGear.Length; i++)
        {
            var gear = equippedGear[i];
            if (gear.ItemId == 0) continue;
            var itemOpt = itemSheet.GetRowOrDefault(gear.ItemId > 1000000 ? gear.ItemId - 1000000 : gear.ItemId);
            if (itemOpt != null && itemOpt.Value.EquipSlotCategory.RowId != 0)
            {
                result.Add((gear.ItemId, itemOpt.Value.EquipSlotCategory.RowId, itemOpt.Value.LevelItem.RowId));
            }
        }
        return result.ToArray();
    }

    protected virtual uint[] GetArmoireItems()
    {
        var cabinetSheet = GlamourChecker.Services.DataManager?.GetExcelSheet<Cabinet>();
        if (cabinetSheet == null) return Array.Empty<uint>();

        var list = new List<uint>();
        foreach (var cabinetEntry in cabinetSheet)
        {
            if (cabinetEntry.Item.RowId == 0) continue;
            if (_memoryProvider.IsItemInCabinet(cabinetEntry.Item.RowId))
            {
                list.Add(cabinetEntry.Item.RowId);
            }
        }
        return list.ToArray();
    }

    protected virtual UpgradeItemData? GetItemData(uint itemId, string jobAbbrev)
    {
        var itemSheet = GlamourChecker.Services.DataManager?.GetExcelSheet<Item>();
        if (itemSheet == null) return null;

        var itemOpt = itemSheet.GetRowOrDefault(itemId);
        if (itemOpt == null) return null;
        var item = itemOpt.Value;

        if (item.EquipSlotCategory.RowId == 0) return null;

        bool canEquip = false;
        if (item.ClassJobCategory.RowId != 0)
        {
            try
            {
                var category = item.ClassJobCategory.Value;
                var prop = category.GetType().GetProperty(jobAbbrev);
                if (prop != null)
                {
                    canEquip = (bool)(prop.GetValue(category) ?? false);
                }
            }
            catch (Exception ex)
            {
                GlamourChecker.Services.PluginLog?.Error(ex, $"Failed to check ClassJobCategory for {jobAbbrev}");
            }
        }

        return new UpgradeItemData
        {
            ItemId = itemId,
            Name = item.Name.ToString(),
            EquipSlotCategory = item.EquipSlotCategory.RowId,
            LevelItem = item.LevelItem.RowId,
            LevelEquip = item.LevelEquip,
            CanEquipJob = canEquip
        };
    }

    public void CheckForUpgrades(string jobAbbrev, uint currentLevel)
    {
        CurrentUpgrades.Clear();

        if (string.IsNullOrEmpty(jobAbbrev)) return;

        var dresserItemIds = GetDresserItems();
        var equippedGear = GetEquippedGear();
        var armoireItemIds = GetArmoireItems();

        if (dresserItemIds.Length == 0 && armoireItemIds.Length == 0) return;

        // Map currently equipped gear ILVL by Slot Group (to handle multi-slot items correctly)
        var equippedIlvlBySlotGroup = new Dictionary<string, uint>();
        foreach (var gear in equippedGear)
        {
            var groupKey = ItemCategoryHelper.GetEquipSlotGroupKey(gear.slotCategory);
            if (equippedIlvlBySlotGroup.ContainsKey(groupKey))
            {
                // Note: Rings share the same group "SlotGroup_Fingers", we take the min ilvl.
                // Same for items that map to the same SlotGroupKey.
                equippedIlvlBySlotGroup[groupKey] = Math.Min(equippedIlvlBySlotGroup[groupKey], gear.levelItem);
            }
            else
            {
                equippedIlvlBySlotGroup[groupKey] = gear.levelItem;
            }
        }

        // Check Dresser
        foreach (var itemId in dresserItemIds)
        {
            if (itemId == 0) continue;
            EvaluateItem(itemId, false, jobAbbrev, currentLevel, equippedIlvlBySlotGroup);
        }

        // Check Armoire
        foreach (var itemId in armoireItemIds)
        {
            if (itemId == 0) continue;
            EvaluateItem(itemId, true, jobAbbrev, currentLevel, equippedIlvlBySlotGroup);
        }

        if (CurrentUpgrades.Any() && !_hasNotifiedThisSession)
        {
            _hasNotifiedThisSession = true;
            PrintNotification(CurrentUpgrades.Count);
            OnUpgradesFound?.Invoke();
        }
    }

    private void EvaluateItem(uint itemId, bool isArmoire, string jobAbbrev, uint currentLevel, Dictionary<string, uint> equippedIlvlBySlotGroup)
    {
        var item = GetItemData(itemId, jobAbbrev);
        if (item == null) return;

        if (item.EquipSlotCategory == 0) return;

        // Check if item level is higher than 1 (level 1 items are pure glamour)
        if (item.LevelItem <= 1) return;

        // 1. Level Check
        if (item.LevelEquip > currentLevel) return;

        // 2. Job Check
        if (!item.CanEquipJob) return;

        // 3. Power Check (iLvl)
        uint currentIlvl = 0;
        var groupKey = ItemCategoryHelper.GetEquipSlotGroupKey(item.EquipSlotCategory);
        if (equippedIlvlBySlotGroup.TryGetValue(groupKey, out var ilvl))
        {
            currentIlvl = ilvl;
        }

        if (item.LevelItem > currentIlvl)
        {
            // Make sure we haven't already added this exact item
            if (!CurrentUpgrades.Any(u => u.ItemId == itemId))
            {
                CurrentUpgrades.Add(new UpgradeItemInfo
                {
                    ItemId = itemId,
                    Name = item.Name,
                    ItemLevel = item.LevelItem,
                    SlotName = ItemCategoryHelper.GetEquipSlotGroup(item.EquipSlotCategory),
                    EquippedItemLevel = currentIlvl,
                    IsFromArmoire = isArmoire
                });
            }
        }
    }
}
