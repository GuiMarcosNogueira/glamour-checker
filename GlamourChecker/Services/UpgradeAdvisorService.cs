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

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class JobData
{
    public uint ModifierStrength { get; set; }
    public uint ModifierDexterity { get; set; }
    public uint ModifierVitality { get; set; }
    public uint ModifierIntelligence { get; set; }
    public uint ModifierMind { get; set; }
    public uint ModifierPiety { get; set; }
    public byte Role { get; set; } // 1=Tank, 2=PureHealer, 3=Melee, 4=PRanged, 5=MRanged, 6=BarrierHealer
    public string Name { get; set; } = string.Empty;
}

public class UpgradeAdvisorService : IDisposable
{
    private readonly IGameMemoryProvider _memoryProvider;
    private uint _lastJobId = 0;
    private bool _hasNotifiedThisSession = false;

    public List<UpgradeItemInfo> CurrentUpgrades { get; private set; } = new();
    public string CurrentJobAbbrev { get; private set; } = string.Empty;

    public event System.Action? OnUpgradesFound;

    public UpgradeAdvisorService(IGameMemoryProvider memoryProvider)
    {
        _memoryProvider = memoryProvider;
        if (GlamourChecker.Services.Framework != null)
        {
            GlamourChecker.Services.Framework.Update += OnFrameworkUpdate;
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public void Dispose()
    {
        if (GlamourChecker.Services.Framework != null)
        {
            GlamourChecker.Services.Framework.Update -= OnFrameworkUpdate;
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected virtual void PrintNotification(int count)
    {
        var msg = string.Format(Loc.Localize("Message_UpgradesFoundChat", "[GlamourChecker] 💡 {0} upgrades encontrados no Dresser/Armoire para o {1}! Digite /gc upgrades para ver a lista."), count, CurrentJobAbbrev);
        GlamourChecker.Services.Chat?.Print(msg);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected virtual uint[] GetDresserItems()
    {
        var span = _memoryProvider.GetMirageManagerPrismBoxItemIds();
        var arr = new uint[span.Length];
        span.CopyTo(arr);
        return arr;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

        itemData.Stats.DefensePhys += item.DefensePhys;
        itemData.Stats.DefenseMag += item.DefenseMag;
        itemData.Stats.DamagePhys += item.DamagePhys;
        itemData.Stats.DamageMag += item.DamageMag;

        return itemData;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
            Role = c.Role,
            Name = c.Name.ToString()
        };
    }

    public void CheckForUpgrades(uint jobId, string jobAbbrev, uint currentLevel)
    {
        CurrentUpgrades.Clear();
        CurrentJobAbbrev = jobAbbrev;

        if (string.IsNullOrEmpty(jobAbbrev)) return;

        var dresserItemIds = GetDresserItems();
        var equippedGear = GetEquippedGear(jobAbbrev);
        var armoireItemIds = GetArmoireItems();

        if (dresserItemIds.Length == 0 && armoireItemIds.Length == 0) return;

        var jobData = GetJobData(jobId);
        if (jobData == null) return;

        CurrentJobAbbrev = string.IsNullOrEmpty(jobData.Name) ? jobAbbrev : jobData.Name;

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

        string roleType = GetRoleType(jobAbbrev);

        var potentialUpgrades = new List<(UpgradeItemInfo Info, double MetricScore, string SlotGroupKey)>();

        // Check Dresser
        foreach (var itemId in dresserItemIds)
        {
            if (itemId == 0) continue;
            var upgrade = EvaluateItem(itemId, false, jobAbbrev, currentLevel, roleType, equippedItemsBySlotGroup);
            if (upgrade.HasValue) potentialUpgrades.Add(upgrade.Value);
        }

        // Check Armoire
        foreach (var itemId in armoireItemIds)
        {
            if (itemId == 0) continue;
            var upgrade = EvaluateItem(itemId, true, jobAbbrev, currentLevel, roleType, equippedItemsBySlotGroup);
            if (upgrade.HasValue) potentialUpgrades.Add(upgrade.Value);
        }

        // Filter and add only the best items per slot
        foreach (var group in potentialUpgrades.GroupBy(u => u.SlotGroupKey))
        {
            int limit = group.Key == "SlotGroup_Fingers" ? 2 : 1;

            var bestItems = group
                .OrderByDescending(u => u.MetricScore)
                .ThenByDescending(u => u.Info.ItemLevel)
                .GroupBy(u => u.Info.ItemId).Select(g => g.First()) // Prevent duplicates of same item
                .Take(limit);

            foreach (var item in bestItems)
            {
                CurrentUpgrades.Add(item.Info);
            }
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

    private uint GetMainStatValue(XIVMath.RawStats stats, string jobAbbrev)
    {
        return jobAbbrev switch
        {
            "PLD" or "GLA" or "WAR" or "MRD" or "DRK" or "GNB" => stats.Strength,
            "MNK" or "PGL" or "DRG" or "LNC" or "SAM" or "RPR" => stats.Strength,
            "NIN" or "ROG" or "VPR" => stats.Dexterity,
            "BRD" or "ARC" or "MCH" or "DNC" => stats.Dexterity,
            "BLM" or "THM" or "SMN" or "ACN" or "RDM" or "PCT" => stats.Intelligence,
            "WHM" or "CNJ" or "SCH" or "AST" or "SGE" => stats.Mind,
            _ => stats.Strength // fallback
        };
    }

    private string GetRoleType(string jobAbbrev)
    {
        return jobAbbrev switch
        {
            "PLD" or "GLA" or "WAR" or "MRD" or "DRK" or "GNB" => "Tank",
            "WHM" or "CNJ" or "SCH" or "AST" or "SGE" => "Healer",
            "MNK" or "PGL" or "DRG" or "LNC" or "SAM" or "RPR" => "MeleeSTR",
            "NIN" or "ROG" or "VPR" or "BRD" or "ARC" or "MCH" or "DNC" => "MeleeDEX_Ranged",
            "BLM" => "BLM",
            "THM" or "SMN" or "ACN" or "RDM" or "PCT" => "Caster",
            _ => "MeleeSTR" // default
        };
    }

    private (UpgradeItemInfo Info, double MetricScore, string SlotGroupKey)? EvaluateItem(uint itemId, bool isArmoire, string jobAbbrev, uint currentLevel, string roleType, Dictionary<string, UpgradeItemData> equippedItemsBySlotGroup)
    {
        var item = GetItemData(itemId, jobAbbrev);
        if (item == null) return null;

        if (item.EquipSlotCategory == 0) return null;
        if (item.LevelEquip > currentLevel) return null;
        if (!item.CanEquipJob) return null;

        // Block glamour gear (level 1)
        if (item.LevelItem == 1) return null;

        uint mainStat = GetMainStatValue(item.Stats, jobAbbrev);
        // If the item doesn't provide the main stat for the job, it's not an upgrade (except for certain specific early-game oddities, but generally true)
        if (mainStat == 0) return null;

        var groupKey = ItemCategoryHelper.GetEquipSlotGroupKey(item.EquipSlotCategory);

        if (groupKey == "SlotGroup_OffHand")
        {
            if (equippedItemsBySlotGroup.TryGetValue("SlotGroup_MainHand", out var mainHand))
            {
                if (mainHand.EquipSlotCategory != 1) return null;
            }
        }

        uint currentIlvl = 0;
        double currentScore = 0;
        if (equippedItemsBySlotGroup.TryGetValue(groupKey, out var replacedItem))
        {
            currentIlvl = replacedItem.LevelItem;
            currentScore = CalculateItemScore(replacedItem.LevelItem, replacedItem.Stats, roleType);
        }

        double simulatedScore = CalculateItemScore(item.LevelItem, item.Stats, roleType);

        if (simulatedScore > currentScore)
        {
            return (new UpgradeItemInfo
            {
                ItemId = itemId,
                Name = item.Name,
                ItemLevel = item.LevelItem,
                SlotName = ItemCategoryHelper.GetEquipSlotGroup(item.EquipSlotCategory),
                EquippedItemLevel = currentIlvl,
                IsFromArmoire = isArmoire
            }, simulatedScore, groupKey);
        }

        return null;
    }

    private double CalculateItemScore(uint itemLevel, XIVMath.RawStats stats, string roleType)
    {
        double score = itemLevel * 100000.0;

        switch (roleType)
        {
            case "Tank":
                score += stats.CriticalHit * 4.0;
                score += stats.Determination * 3.0;
                score += stats.DirectHit * 3.0;
                score += stats.Tenacity * 1.0;
                score += stats.SkillSpeed * 1.0;
                break;
            case "Healer":
                score += stats.CriticalHit * 4.0;
                score += stats.Determination * 3.0;
                score += stats.DirectHit * 2.0;
                score += stats.Piety * 1.0;
                score += stats.SpellSpeed * 1.0;
                break;
            case "MeleeSTR":
                score += stats.CriticalHit * 4.0;
                score += stats.Determination * 3.0;
                score += stats.DirectHit * 3.0;
                score += stats.SkillSpeed * 1.0;
                break;
            case "MeleeDEX_Ranged":
                score += stats.CriticalHit * 4.0;
                score += stats.Determination * 3.0;
                score += stats.DirectHit * 3.0;
                score += stats.SkillSpeed * 1.0;
                break;
            case "Caster":
                score += stats.CriticalHit * 4.0;
                score += stats.Determination * 3.0;
                score += stats.DirectHit * 3.0;
                score += stats.SpellSpeed * 1.0;
                break;
            case "BLM":
                score += stats.CriticalHit * 4.0;
                score += stats.SpellSpeed * 4.0;
                score += stats.Determination * 2.0;
                score += stats.DirectHit * 2.0;
                break;
        }

        return score;
    }
}





