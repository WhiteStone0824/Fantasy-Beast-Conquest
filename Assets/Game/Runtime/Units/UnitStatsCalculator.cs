using System;
using UnityEngine;

namespace FusionAnyMonster
{
    public static class UnitStatsCalculator
    {
        public const float HealthMultiplier = 1.5f;
        public static UnitStats Calculate(SpeciesDefinition species, UnitInstance unit, UnitStats temporaryModifiers = null, PersonalityDatabase personalities = null)
        {
            if(species==null || unit==null) return new UnitStats();
            PersonalityDefinition p=personalities==null?null:personalities.Get(unit.personality);
            UnitStats stats=Calculate(species.baseStats, unit, temporaryModifiers, p);
            SpeciesPassiveCalculator.ApplyStats(species.passive,stats);return stats;
        }

        public static UnitStats Calculate(UnitStats speciesBaseStats, UnitInstance unit, UnitStats temporaryModifiers, PersonalityDefinition personality)
        {
            if (speciesBaseStats == null || unit == null) return new UnitStats();
            UnitStats result=new UnitStats();
            for(int i=0;i<10;i++)
            {
                float baseValue=speciesBaseStats.Get(i);
                int iv=unit.iv==null?0:unit.iv.Get(i);
                int level=Mathf.Clamp(unit.level,1,99);
                float value=baseValue*(.5f+(float)level/100f)+iv*(.25f+(float)level/200f);
                if(personality!=null) { if(personality.boostedStatIndex==i)value*=personality.boostMultiplier; if(personality.reducedStatIndex==i)value*=personality.reductionMultiplier; }
                value*=1f+EquipmentCatalog.Bonus(unit.equipment,i)/100f;
                if(temporaryModifiers!=null)value+=temporaryModifiers.Get(i);
                if(i==0)value*=HealthMultiplier;
                if(i==9)value*=.2f;
                result.Set(i,Mathf.Max(i==7?.1f:0f,value));
            }
            return result;
        }
    }

    public static class ExperienceSystem
    {
        public const int MaxLevel=99;
        public static long ExperienceForNextLevel(int level) { int l=Mathf.Clamp(level,1,98); return (long)(l*l*25L+75L*l); }
        public static int Grant(UnitInstance unit,long amount)
        {
            if (unit == null) return 0;
            unit.level = Mathf.Clamp(unit.level, 1, MaxLevel);
            unit.experience = Math.Max(0, unit.experience);
            if (unit.level >= MaxLevel) { unit.experience = 0; return 0; }
            if (amount <= 0) return 0;
            unit.experience = amount > long.MaxValue - unit.experience ? long.MaxValue : unit.experience + amount;
            int gained=0;
            while(unit.level<MaxLevel && unit.experience>=ExperienceForNextLevel(unit.level)) { unit.experience-=ExperienceForNextLevel(unit.level);unit.level++;gained++; }
            if(unit.level>=MaxLevel)unit.experience=0;
            return gained;
        }
        public static int Distribute(UnitInstance[] deployed,long totalExperience)
        {
            if (deployed == null || totalExperience <= 0) return 0;
            int validCount = 0;
            for (int i = 0; i < deployed.Length; i++) if (deployed[i] != null) validCount++;
            if (validCount == 0) return 0;
            long share = totalExperience / validCount;
            long remainder = totalExperience % validCount;
            int distributed = 0;
            int levelsGained = 0;
            for (int i = 0; i < deployed.Length; i++)
            {
                if (deployed[i] == null) continue;
                levelsGained += Grant(deployed[i], share + (distributed < remainder ? 1 : 0));
                distributed++;
            }
            return levelsGained;
        }
    }

    public static class ResistanceCalculator
    {
        public static float ApplyDefense(float rawDamage, float defense)
        {
            float safeDamage = Mathf.Max(0f, rawDamage);
            float safeDefense = Mathf.Max(0f, defense);
            return safeDamage * 100f / (100f + safeDefense * 2f);
        }
        public static float ApplyElementalDamage(SpeciesDefinition species,AttributeElement element,float rawDamage)
        { return Mathf.Max(0f,rawDamage*(1f-SpeciesPassiveCalculator.ElementReduction(species,element))); }
        public static float ApplyDebuffChance(SpeciesDefinition species,DebuffType debuff,float baseChance)
        { return Mathf.Clamp01(baseChance*(1f-SpeciesPassiveCalculator.DebuffReduction(species,debuff))); }
    }
}
