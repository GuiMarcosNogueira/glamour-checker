using Xunit;
using GlamourChecker.Core;
using System.Collections.Generic;

namespace GlamourChecker.Tests;

public class UpgradeAdvisorAdditionalTests
{
    [Fact]
    public void TestMeleeSTR_ShouldPrioritizeStats()
    {
        var service = new TestableUpgradeAdvisorService();
        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50, Stats = new XIVMath.RawStats { Strength = 10, SkillSpeed = 100 } } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Str Weap", EquipSlotCategory = 1, LevelItem = 50, LevelEquip = 40, CanEquipJob = true, Stats = new XIVMath.RawStats { Strength = 10, CriticalHit = 100 } }
        };

        service.CheckForUpgrades(1, "MNK", 90);
        Assert.Single(service.CurrentUpgrades);
    }

    [Fact]
    public void TestCaster_ShouldPrioritizeStats()
    {
        var service = new TestableUpgradeAdvisorService();
        service.MockDresserItems = new uint[] { 101 };
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50, Stats = new XIVMath.RawStats { Intelligence = 10, SpellSpeed = 100 } } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "Cast Weap", EquipSlotCategory = 1, LevelItem = 50, LevelEquip = 40, CanEquipJob = true, Stats = new XIVMath.RawStats { Intelligence = 10, CriticalHit = 100 } }
        };

        service.CheckForUpgrades(1, "RDM", 90);
        Assert.Single(service.CurrentUpgrades);
    }

    [Fact]
    public void TestBLM_ShouldPrioritizeSpellSpeed()
    {
        var service = new TestableUpgradeAdvisorService();
        service.MockDresserItems = new uint[] { 101 };
        // BLM prioritizes SpS very high (Weight 4), while Det is Weight 2
        service.MockEquippedGear = new[] { new UpgradeItemData { ItemId = 200, EquipSlotCategory = 1, LevelItem = 50, Stats = new XIVMath.RawStats { Intelligence = 10, Determination = 100 } } };

        service.MockItemData = new Dictionary<uint, UpgradeItemData>
        {
            [101] = new UpgradeItemData { ItemId = 101, Name = "BLM Weap", EquipSlotCategory = 1, LevelItem = 50, LevelEquip = 40, CanEquipJob = true, Stats = new XIVMath.RawStats { Intelligence = 10, SpellSpeed = 100 } }
        };

        service.CheckForUpgrades(1, "BLM", 90);
        Assert.Single(service.CurrentUpgrades);
    }
}
