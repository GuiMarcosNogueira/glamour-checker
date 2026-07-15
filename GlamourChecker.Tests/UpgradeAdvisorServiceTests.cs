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
    public (uint itemId, uint slotCategory, uint levelItem)[] MockEquippedGear { get; set; } = System.Array.Empty<(uint, uint, uint)>();
    public Dictionary<uint, UpgradeItemData> MockItemData { get; set; } = new();

    public int NotificationPrintCount { get; private set; } = 0;

    public TestableUpgradeAdvisorService(IGameMemoryProvider memoryProvider) : base(memoryProvider)
    {
    }

    protected override uint[] GetDresserItems() => MockDresserItems;
    protected override uint[] GetArmoireItems() => MockArmoireItems;
    protected override (uint itemId, uint slotCategory, uint levelItem)[] GetEquippedGear() => MockEquippedGear;

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
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockDresserItems = new uint[] { 101, 102 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Better Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true },
            [102] = new UpgradeItemData { ItemId = 102, Name = "Worse Helmet", EquipSlotCategory = 1, LevelItem = 10, LevelEquip = 10, CanEquipJob = true }
        };

        service.CheckForUpgrades("PLD", 90);

        Assert.Single(service.CurrentUpgrades);
        Assert.Equal(101u, service.CurrentUpgrades[0].ItemId);
        Assert.False(service.CurrentUpgrades[0].IsFromArmoire);
        Assert.Equal(1, service.NotificationPrintCount);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotFindUpgrades_WhenLevelTooLow()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "High Level Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true }
        };

        service.CheckForUpgrades("PLD", 80);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotFindUpgrades_WhenWrongJob()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Healer Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = false }
        };

        service.CheckForUpgrades("PLD", 90);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldIgnoreLevel1GlamourItems()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Emperor's New Hat", EquipSlotCategory = 1, LevelItem = 1, LevelEquip = 1, CanEquipJob = true }
        };

        service.CheckForUpgrades("PLD", 90);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldFindUpgrades_InArmoire()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockArmoireItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Armoire Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true }
        };

        service.CheckForUpgrades("PLD", 90);

        Assert.Single(service.CurrentUpgrades);
        Assert.Equal(101u, service.CurrentUpgrades[0].ItemId);
        Assert.True(service.CurrentUpgrades[0].IsFromArmoire);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotAddDuplicates_IfItemInBothDresserAndArmoire()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockDresserItems = new uint[] { 101 };
        service.MockArmoireItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Duplicate Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true }
        };

        service.CheckForUpgrades("PLD", 90);

        Assert.Single(service.CurrentUpgrades);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotifyOnlyOncePerSession()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);

        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { (200u, 1u, 50u) };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Better Helmet", EquipSlotCategory = 1, LevelItem = 100, LevelEquip = 90, CanEquipJob = true }
        };

        service.CheckForUpgrades("PLD", 90);
        service.CheckForUpgrades("PLD", 90);

        Assert.Equal(1, service.NotificationPrintCount);
    }

    [Fact]
    public void CheckForUpgrades_ShouldNotCheck_IfJobAbbrevIsEmpty()
    {
        var mockMemory = new FakeGameMemoryProvider();
        var service = new TestableUpgradeAdvisorService(mockMemory);
        service.MockDresserItems = new uint[] { 101 };
        service.CheckForUpgrades("", 90);
        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void BaseService_CheckForUpgrades_ShouldExitEarly_WhenNoDependencies()
    {
        var mockMemory = new FakeGameMemoryProvider();
        mockMemory.DresserItems = System.Array.Empty<uint>();
        mockMemory.InventoryItems = System.Array.Empty<InventoryItem>();

        GlamourChecker.Services.Framework = null!;
        GlamourChecker.Services.DataManager = null!;
        GlamourChecker.Services.ObjectTable = null!;
        GlamourChecker.Services.Chat = null!;

        using var service = new UpgradeAdvisorService(mockMemory);

        service.CheckForUpgrades("PLD", 90);

        Assert.Empty(service.CurrentUpgrades);
    }

    [Fact]
    public void BaseService_Dispose_ShouldNotThrow_WhenFrameworkNull()
    {
        GlamourChecker.Services.Framework = null!;
        var mockMemory = new FakeGameMemoryProvider();
        using var service = new UpgradeAdvisorService(mockMemory);
    }

    [Fact]
    public void BaseService_CheckForUpgrades_ShouldExit_WhenDresserAndArmoireEmpty()
    {
        var mockMemory = new FakeGameMemoryProvider();
        mockMemory.DresserItems = System.Array.Empty<uint>();
        mockMemory.InventoryItems = System.Array.Empty<InventoryItem>();

        using var service = new UpgradeAdvisorService(mockMemory);
        service.CheckForUpgrades("PLD", 90);
        Assert.Empty(service.CurrentUpgrades);
    }
}
