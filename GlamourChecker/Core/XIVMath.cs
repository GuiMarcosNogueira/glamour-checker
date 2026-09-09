using System;

namespace GlamourChecker.Core;

public static class XIVMath
{

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
        public uint DefensePhys;
        public uint DefenseMag;

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
            DefensePhys += other.DefensePhys;
            DefenseMag += other.DefenseMag;
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
            DefensePhys -= Math.Min(DefensePhys, other.DefensePhys);
            DefenseMag -= Math.Min(DefenseMag, other.DefenseMag);
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

}
