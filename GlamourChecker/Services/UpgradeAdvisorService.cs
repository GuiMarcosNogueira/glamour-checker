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
    public XIVMath.RawStats Stats { get; set; } = new();
}

public class JobData
{
    public uint ModifierStrength { get; set; }
    public uint ModifierDexterity { get; set; }
    public uint ModifierVitality { get; set; }
    public uint ModifierIntelligence { get; set; }
    public uint ModifierMind { get; set; }
    public uint ModifierPiety { get; set; }
    public byte Role { get; set; } // 1=Tank, 2=PureHealer, 3=Melee, 4=PRanged, 5=MRanged, 6=BarrierHealer
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
            CheckForUpgrades(currentJobId, jobAbbrev, currentLevel);
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

    protected virtual UpgradeItemData[] GetEquippedGear(string jobAbbrev)
    {
        var equippedGear = _memoryProvider.GetInventoryContainer(InventoryType.EquippedItems);
        var itemSheet = GlamourChecker.Services.DataManager?.GetExcelSheet<Item>();
        if (itemSheet == null) return Array.Empty<UpgradeItemData>();

        var result = new List<UpgradeItemData>();
        for (int i = 0; i < equippedGear.Length; i++)
        {
            var gear = equippedGear[i];
            if (gear.ItemId == 0) continue;
            var data = GetItemData(gear.ItemId > 1000000 ? gear.ItemId - 1000000 : gear.ItemId, jobAbbrev);
            if (data != null)
            {
                result.Add(data);
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

        var itemData = new UpgradeItemData
        {
            ItemId = itemId,
            Name = item.Name.ToString(),
            EquipSlotCategory = item.EquipSlotCategory.RowId,
            LevelItem = item.LevelItem.RowId,
            LevelEquip = item.LevelEquip,
            CanEquipJob = canEquip
        };

        // Extract base params
        for (int i = 0; i < 6; i++)
        {
            uint type = item.BaseParam[i].RowId;
            uint value = (uint)item.BaseParamValue[i];
            if (type != 0 && value != 0)
            {
                itemData.Stats.ApplyBaseParam(type, value);
            }
        }

        return itemData;
    }

    protected virtual JobData? GetJobData(uint jobId)
    {
        var sheet = GlamourChecker.Services.DataManager?.GetExcelSheet<ClassJob>();
        if (sheet == null) return null;

        var row = sheet.GetRowOrDefault(jobId);
        if (row == null) return null;

        var c = row.Value;
        return new JobData
        {
            ModifierStrength = c.ModifierStrength,
            ModifierDexterity = c.ModifierDexterity,
            ModifierVitality = c.ModifierVitality,
            ModifierIntelligence = c.ModifierIntelligence,
            ModifierMind = c.ModifierMind,
            ModifierPiety = c.ModifierPiety,
            Role = c.Role
        };
    }

    public void CheckForUpgrades(uint jobId, string jobAbbrev, uint currentLevel)
    {
        CurrentUpgrades.Clear();

        if (string.IsNullOrEmpty(jobAbbrev)) return;

        var dresserItemIds = GetDresserItems();
        var equippedGear = GetEquippedGear(jobAbbrev);
        var armoireItemIds = GetArmoireItems();

        if (dresserItemIds.Length == 0 && armoireItemIds.Length == 0) return;

        var jobData = GetJobData(jobId);
        if (jobData == null) return;

        // Calculate currently equipped raw stats
        var currentRawStats = new XIVMath.RawStats();

        // Also map currently equipped gear by Slot Group so we know which items are being replaced
        var equippedItemsBySlotGroup = new Dictionary<string, UpgradeItemData>();

        foreach (var gear in equippedGear)
        {
            currentRawStats.Add(gear.Stats);

            var groupKey = ItemCategoryHelper.GetEquipSlotGroupKey(gear.EquipSlotCategory);
            // If multiple items map to same group (e.g. Fingers), we want to replace the weakest one
            if (equippedItemsBySlotGroup.TryGetValue(groupKey, out var existing))
            {
                if (gear.LevelItem < existing.LevelItem)
                {
                    equippedItemsBySlotGroup[groupKey] = gear;
                }
            }
            else
            {
                equippedItemsBySlotGroup[groupKey] = gear;
            }
        }

        bool isTank = jobData.Role == 1;
        uint mainStatValue = GetMainStatValue(currentRawStats, jobData);
        uint jobMainStatMod = GetMainStatModifier(jobData);
        uint wd = Math.Max(currentRawStats.DamagePhys, currentRawStats.DamageMag);

        double currentExpectedDamage = XIVMath.CalculateExpectedDamage(
            currentLevel, jobMainStatMod, mainStatValue, wd,
            currentRawStats.CriticalHit, currentRawStats.DirectHit,
            currentRawStats.Determination, currentRawStats.Tenacity, isTank);

        // Check Dresser
        foreach (var itemId in dresserItemIds)
        {
            if (itemId == 0) continue;
            EvaluateItem(itemId, false, jobAbbrev, currentLevel, jobData, currentRawStats, currentExpectedDamage, equippedItemsBySlotGroup);
        }

        // Check Armoire
        foreach (var itemId in armoireItemIds)
        {
            if (itemId == 0) continue;
            EvaluateItem(itemId, true, jobAbbrev, currentLevel, jobData, currentRawStats, currentExpectedDamage, equippedItemsBySlotGroup);
        }

        if (CurrentUpgrades.Any() && !_hasNotifiedThisSession)
        {
            NotifyUpgrades();
            _hasNotifiedThisSession = true;
        }
    }

    private void NotifyUpgrades()
    {
        PrintNotification(CurrentUpgrades.Count);
        OnUpgradesFound?.Invoke();
    }

    private uint GetMainStatValue(XIVMath.RawStats stats, JobData jobData)
    {
        uint maxMod = Math.Max(jobData.ModifierStrength, Math.Max(jobData.ModifierDexterity, Math.Max(jobData.ModifierIntelligence, jobData.ModifierMind)));

        if (maxMod == jobData.ModifierIntelligence) return stats.Intelligence;
        if (maxMod == jobData.ModifierMind) return stats.Mind;
        if (maxMod == jobData.ModifierDexterity) return stats.Dexterity;

        return stats.Strength; // Fallback
    }

    private uint GetMainStatModifier(JobData jobData)
    {
        return Math.Max(jobData.ModifierStrength, Math.Max(jobData.ModifierDexterity, Math.Max(jobData.ModifierIntelligence, jobData.ModifierMind)));
    }

    private void EvaluateItem(uint itemId, bool isArmoire, string jobAbbrev, uint currentLevel, JobData jobData, XIVMath.RawStats baseStats, double currentExpectedDamage, Dictionary<string, UpgradeItemData> equippedItemsBySlotGroup)
    {
        var item = GetItemData(itemId, jobAbbrev);
        if (item == null) return;

        if (item.EquipSlotCategory == 0) return;

        // 1. Level Check
        if (item.LevelEquip > currentLevel) return;

        // 2. Job Check
        if (!item.CanEquipJob) return;

        // 3. Power Check (Expected Damage)
        var groupKey = ItemCategoryHelper.GetEquipSlotGroupKey(item.EquipSlotCategory);

        uint currentIlvl = 0;
        var simulatedStats = new XIVMath.RawStats();
        simulatedStats.Add(baseStats);

        if (equippedItemsBySlotGroup.TryGetValue(groupKey, out var replacedItem))
        {
            currentIlvl = replacedItem.LevelItem;
            // Subtract the old item's stats
            simulatedStats.Subtract(replacedItem.Stats);
        }

        // Add the new item's stats
        simulatedStats.Add(item.Stats);

        bool isTank = jobData.Role == 1;
        uint mainStatValue = GetMainStatValue(simulatedStats, jobData);
        uint jobMainStatMod = GetMainStatModifier(jobData);
        uint wd = Math.Max(simulatedStats.DamagePhys, simulatedStats.DamageMag);

        double simulatedExpectedDamage = XIVMath.CalculateExpectedDamage(
            currentLevel, jobMainStatMod, mainStatValue, wd,
            simulatedStats.CriticalHit, simulatedStats.DirectHit,
            simulatedStats.Determination, simulatedStats.Tenacity, isTank);

        if (simulatedExpectedDamage > currentExpectedDamage)
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
