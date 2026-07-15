using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlamourChecker.Core;
using Xunit;

namespace GlamourChecker.Tests;

public class TestableUpgradeAdvisorService : UpgradeAdvisorService
{
    public uint[] MockDresserItems { get; set; } = System.Array.Empty<uint>();
    public uint[] MockArmoireItems { get; set; } = System.Array.Empty<uint>();
    public UpgradeItemData[] MockEquippedGear { get; set; } = System.Array.Empty<UpgradeItemData>();
    public Dictionary<uint, UpgradeItemData> MockItemData { get; set; } = new();
    public JobData MockJobData { get; set; } = new() { Role = 3, ModifierStrength = 100 }; // Default Melee

    public int NotificationPrintCount { get; private set; } = 0;

    public TestableUpgradeAdvisorService() : base(new FakeGameMemoryProvider())
    {
    }

    protected override uint[] GetDresserItems() => MockDresserItems;
    protected override uint[] GetArmoireItems() => MockArmoireItems;
    protected override UpgradeItemData[] GetEquippedGear(string jobAbbrev) => MockEquippedGear;
    protected override JobData? GetJobData(uint jobId) => MockJobData;

    protected override void PrintNotification(int count)
    {
        NotificationPrintCount++;
    }

    protected override UpgradeItemData? GetItemData(uint itemId, string jobAbbrev)
    {
        if (MockItemData.TryGetValue(itemId, out var data))
        {
            return data;
        }
        return null;
    }
}

public class UpgradeAdvisorServiceTests
{
    [Fact]
    public void CheckForUpgrades_ShouldFindUpgrades_WhenBetterItemInDresser()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockDresserItems = new uint[] { 101, 102 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50, Stats = new XIVMath.RawStats { Strength = 10, DamagePhys = 10 } } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Better Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true, Stats = new XIVMath.RawStats { Strength = 20, DamagePhys = 15 } },
            [102] = new UpgradeItemData { ItemId = 102, Name = "Worse Helmet", EquipSlotCategory = 1, LevelItem = 10, LevelEquip = 10, CanEquipJob = true, Stats = new XIVMath.RawStats { Strength = 5, DamagePhys = 5 } }
        };

        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Single(service.CurrentUpgrades);
        Assert.Equal(101u, service.CurrentUpgrades[0].ItemId);
        Assert.False(service.CurrentUpgrades[0].IsFromArmoire);
        Assert.Equal(1, service.NotificationPrintCount);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotFindUpgrades_WhenLevelTooLow()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50 } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "High Level Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true }
        };

        service.CheckForUpgrades(1, "PLD", 80);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotFindUpgrades_WhenWrongJob()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50 } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Healer Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = false }
        };

        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldIgnoreLevel1GlamourItems()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50 } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Emperor's New Hat", EquipSlotCategory = 1, LevelItem = 1, LevelEquip = 1, CanEquipJob = true }
        };

        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldFindUpgrades_InArmoire()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockArmoireItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50, Stats = new XIVMath.RawStats { Strength = 10, DamagePhys = 10 } } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Armoire Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true, Stats = new XIVMath.RawStats { Strength = 20, DamagePhys = 15 } }
        };

        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Single(service.CurrentUpgrades);
        Assert.Equal(101u, service.CurrentUpgrades[0].ItemId);
        Assert.True(service.CurrentUpgrades[0].IsFromArmoire);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotAddDuplicates_IfItemInBothDresserAndArmoire()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockDresserItems = new uint[] { 101 };
        service.MockArmoireItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50, Stats = new XIVMath.RawStats { Strength = 10, DamagePhys = 10 } } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Duplicate Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true, Stats = new XIVMath.RawStats { Strength = 20, DamagePhys = 15 } }
        };

        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Single(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldBlockOffHand_WhenMainHandIsTwoHanded()
    {
        var service = new TestableUpgradeAdvisorService();

        service.MockDresserItems = new uint[] { 101 }; // Shield

        service.MockEquippedGear = new[] { 
            // 13 is two-handed category
            new UpgradeItemData { ItemId = 200, EquipSlotCategory = 13, LevelItem = 50, Stats = new XIVMath.RawStats { Intelligence = 10, DamageMag = 10 } }
        };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Dresser Shield", EquipSlotCategory = 2, LevelItem = 100, LevelEquip = 90, CanEquipJob = true, Stats = new XIVMath.RawStats { Intelligence = 5, DamageMag = 5 } }
        };

        service.CheckForUpgrades(1, "BLM", 90);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotifyOnlyOncePerSession()
    {
        var service = new TestableUpgradeAdvisorService();
        service.MockDresserItems = new uint[] { 101 };

        var mockGear = new UpgradeItemData
        {
            ItemId = 99,
            EquipSlotCategory = 4,
            LevelItem = 100,
            Stats = new XIVMath.RawStats { Strength = 50, DamagePhys = 10 }
        };
        service.MockEquippedGear = new[] { mockGear };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            { 101, new UpgradeItemData { ItemId = 101, Name = "Better Body", EquipSlotCategory = 4, LevelItem = 110, LevelEquip = 50, CanEquipJob = true, Stats = new XIVMath.RawStats { Strength = 60, DamagePhys = 12 } } }
        };

        service.CheckForUpgrades(1, "DNC", 60);
        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Equal(1, service.NotificationPrintCount);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotCheck_IfJobAbbrevIsEmpty()
    {
        var service = new TestableUpgradeAdvisorService();
        service.MockDresserItems = new uint[] { 101 };
        service.CheckForUpgrades(1, "", 90);
        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void BaseService_CheckForUpgrades_ShouldExit_WhenDresserAndArmoireEmpty()
    {
        var mockMemory = new FakeGameMemoryProvider();
        mockMemory.DresserItems = System.Array.Empty<uint>();
        mockMemory.InventoryItems = System.Array.Empty<InventoryItem>();

        using var service = new UpgradeAdvisorService(mockMemory);

        service.CheckForUpgrades(1, "PLD", 90);

        Assert.Empty(service.CurrentUpgrades);
    }
}
