using System;

namespace GlamourChecker.Core;

public static class XIVMath
{
    public struct LevelStats
    {
        public uint Level;
        public uint BaseMainStat;
        public uint BaseSubStat;
        public uint LevelDiv;
    }

    public class RawStats
    {
        public uint Strength;
        public uint Dexterity;
        public uint Vitality;
        public uint Intelligence;
        public uint Mind;
        public uint Piety;
        public uint Tenacity;
        public uint CriticalHit;
        public uint DirectHit;
        public uint Determination;
        public uint SkillSpeed;
        public uint SpellSpeed;
        public uint DamagePhys;
        public uint DamageMag;

        public void Add(RawStats other)
        {
            Strength += other.Strength;
            Dexterity += other.Dexterity;
            Vitality += other.Vitality;
            Intelligence += other.Intelligence;
            Mind += other.Mind;
            Piety += other.Piety;
            Tenacity += other.Tenacity;
            CriticalHit += other.CriticalHit;
            DirectHit += other.DirectHit;
            Determination += other.Determination;
            SkillSpeed += other.SkillSpeed;
            SpellSpeed += other.SpellSpeed;
            DamagePhys += other.DamagePhys;
            DamageMag += other.DamageMag;
        }

        public void Subtract(RawStats other)
        {
            Strength -= Math.Min(Strength, other.Strength);
            Dexterity -= Math.Min(Dexterity, other.Dexterity);
            Vitality -= Math.Min(Vitality, other.Vitality);
            Intelligence -= Math.Min(Intelligence, other.Intelligence);
            Mind -= Math.Min(Mind, other.Mind);
            Piety -= Math.Min(Piety, other.Piety);
            Tenacity -= Math.Min(Tenacity, other.Tenacity);
            CriticalHit -= Math.Min(CriticalHit, other.CriticalHit);
            DirectHit -= Math.Min(DirectHit, other.DirectHit);
            Determination -= Math.Min(Determination, other.Determination);
            SkillSpeed -= Math.Min(SkillSpeed, other.SkillSpeed);
            SpellSpeed -= Math.Min(SpellSpeed, other.SpellSpeed);
            DamagePhys -= Math.Min(DamagePhys, other.DamagePhys);
            DamageMag -= Math.Min(DamageMag, other.DamageMag);
        }

        public void ApplyBaseParam(uint type, uint value)
        {
            switch (type)
            {
                case 1: Strength += value; break;
                case 2: Dexterity += value; break;
                case 3: Vitality += value; break;
                case 4: Intelligence += value; break;
                case 5: Mind += value; break;
                case 6: Piety += value; break;
                case 12: DamagePhys += value; break;
                case 13: DamageMag += value; break;
                case 19: Tenacity += value; break;
                case 22: DirectHit += value; break;
                case 27: CriticalHit += value; break;
                case 44: Determination += value; break;
                case 45: SkillSpeed += value; break;
                case 46: SpellSpeed += value; break;
            }
        }
    }

    public static LevelStats GetLevelStats(uint level)
    {
        return level switch
        {
            50 => new LevelStats { Level = 50, BaseMainStat = 202, BaseSubStat = 341, LevelDiv = 341 },
            60 => new LevelStats { Level = 60, BaseMainStat = 218, BaseSubStat = 354, LevelDiv = 600 },
            70 => new LevelStats { Level = 70, BaseMainStat = 292, BaseSubStat = 364, LevelDiv = 900 },
            80 => new LevelStats { Level = 80, BaseMainStat = 340, BaseSubStat = 380, LevelDiv = 1300 },
            90 => new LevelStats { Level = 90, BaseMainStat = 390, BaseSubStat = 400, LevelDiv = 1900 },
            100 => new LevelStats { Level = 100, BaseMainStat = 440, BaseSubStat = 420, LevelDiv = 2780 },
            _ => new LevelStats { Level = level, BaseMainStat = 440, BaseSubStat = 420, LevelDiv = 2780 }
        };
    }

    public static double CritChance(LevelStats levelStats, uint crit)
    {
        return Math.Floor(200.0 * (crit - levelStats.BaseSubStat) / levelStats.LevelDiv + 50.0) / 1000.0;
    }

    public static double CritDmg(LevelStats levelStats, uint crit)
    {
        return (1400.0 + Math.Floor(200.0 * (crit - levelStats.BaseSubStat) / levelStats.LevelDiv)) / 1000.0;
    }

    public static double DhitChance(LevelStats levelStats, uint dhit)
    {
        return Math.Floor(550.0 * (dhit - levelStats.BaseSubStat) / levelStats.LevelDiv) / 1000.0;
    }

    public static double DhitDmg()
    {
        return 1.25;
    }

    public static double DetMulti(LevelStats levelStats, uint det)
    {
        return (1000.0 + Math.Floor(140.0 * (det - levelStats.BaseMainStat) / levelStats.LevelDiv)) / 1000.0;
    }

    public static double TenacityMulti(LevelStats levelStats, uint tnc)
    {
        return (1000.0 + Math.Floor(100.0 * (tnc - levelStats.BaseSubStat) / levelStats.LevelDiv)) / 1000.0;
    }

    public static double MainStatMulti(LevelStats levelStats, uint jobMainStatMod, uint mainStat, bool isTank)
    {
        double apMod = 0;
        if (levelStats.Level >= 100) apMod = isTank ? 190.0 : 237.0;
        else if (levelStats.Level >= 90) apMod = isTank ? 156.0 : 195.0;
        else if (levelStats.Level >= 80) apMod = isTank ? 115.0 : 165.0;
        else if (levelStats.Level >= 70) apMod = isTank ? 105.0 : 125.0;
        else apMod = isTank ? 91.0 : 114.0;

        return Math.Max(0, (Math.Floor(apMod * (mainStat - levelStats.BaseMainStat) / levelStats.BaseMainStat) + 100.0) / 100.0);
    }

    public static double WdMulti(LevelStats levelStats, uint jobMainStatMod, uint wd)
    {
        return Math.Floor(levelStats.BaseMainStat * jobMainStatMod / 1000.0 + wd) / 100.0;
    }

    public static double CalculateExpectedDamage(
        uint level,
        uint jobMainStatMod,
        uint mainStat,
        uint wd,
        uint crit,
        uint dhit,
        uint det,
        uint tnc,
        bool isTank)
    {
        var ls = GetLevelStats(level);

        double mainStatMulti = MainStatMulti(ls, jobMainStatMod, mainStat, isTank);
        double wdMulti = WdMulti(ls, jobMainStatMod, wd);
        double detMulti = DetMulti(ls, det);
        double tncMulti = isTank ? TenacityMulti(ls, tnc) : 1.0;

        double baseDamage = 100 * mainStatMulti * detMulti * tncMulti * wdMulti;

        double critC = Math.Clamp(CritChance(ls, crit), 0.0, 1.0);
        double critD = CritDmg(ls, crit);
        double dhitC = Math.Clamp(DhitChance(ls, dhit), 0.0, 1.0);
        double dhitD = DhitDmg();

        double expectedMultiplier = 1.0 + (critC * (critD - 1.0)) + (dhitC * (dhitD - 1.0));

        return baseDamage * expectedMultiplier;
    }
}
