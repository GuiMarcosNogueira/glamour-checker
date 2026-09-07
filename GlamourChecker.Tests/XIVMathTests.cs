using Xunit;
using GlamourChecker.Core;

namespace GlamourChecker.Tests;

public class XIVMathTests
{
    [Fact]
    public void RawStats_Add_ShouldCombineStats()
    {
        var stats1 = new XIVMath.RawStats { Strength = 10, DefensePhys = 5 };
        var stats2 = new XIVMath.RawStats { Strength = 20, Dexterity = 5 };
        stats1.Add(stats2);
        Assert.Equal(30u, stats1.Strength);
        Assert.Equal(5u, stats1.Dexterity);
        Assert.Equal(5u, stats1.DefensePhys);
    }

    [Fact]
    public void RawStats_Subtract_ShouldReduceStats()
    {
        var stats1 = new XIVMath.RawStats { Strength = 20, Dexterity = 10, Vitality = 10, Intelligence = 10, Mind = 10, Piety = 10, Tenacity = 10, CriticalHit = 10, DirectHit = 10, Determination = 10, SkillSpeed = 10, SpellSpeed = 10, DamagePhys = 10, DamageMag = 10, DefensePhys = 10, DefenseMag = 10 };
        var stats2 = new XIVMath.RawStats { Strength = 15, Dexterity = 5, Vitality = 5, Intelligence = 5, Mind = 5, Piety = 5, Tenacity = 5, CriticalHit = 5, DirectHit = 5, Determination = 5, SkillSpeed = 5, SpellSpeed = 5, DamagePhys = 5, DamageMag = 5, DefensePhys = 5, DefenseMag = 5 };
        stats1.Subtract(stats2);
        Assert.Equal(5u, stats1.Strength);
        Assert.Equal(5u, stats1.Dexterity);
    }

    [Fact]
    public void RawStats_ApplyBaseParam_ShouldApply()
    {
        var stats = new XIVMath.RawStats();
        stats.ApplyBaseParam(1, 10);
        stats.ApplyBaseParam(2, 10);
        stats.ApplyBaseParam(3, 10);
        stats.ApplyBaseParam(4, 10);
        stats.ApplyBaseParam(5, 10);
        stats.ApplyBaseParam(6, 10);
        stats.ApplyBaseParam(12, 10);
        stats.ApplyBaseParam(13, 10);
        stats.ApplyBaseParam(19, 10);
        stats.ApplyBaseParam(22, 10);
        stats.ApplyBaseParam(27, 10);
        stats.ApplyBaseParam(44, 10);
        stats.ApplyBaseParam(45, 10);
        stats.ApplyBaseParam(46, 10);

        Assert.Equal(10u, stats.Strength);
        Assert.Equal(10u, stats.Dexterity);
        Assert.Equal(10u, stats.SpellSpeed);
    }
}
