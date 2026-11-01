using System;
using FusionAnyMonster.Save;
using FusionAnyMonster.Spells;
using UnityEngine;

namespace FusionAnyMonster
{
    /// <summary>Wild individuals receive individual traits and cloned species GP defaults at spawn.</summary>
    public static class WildUnitFactory
    {
        public static UnitSaveData Create(SpeciesDefinition species,int level,int seed,string id=null)
        {
            if(species==null)throw new ArgumentNullException(nameof(species));
            var random=new System.Random(seed);var iv=IndividualValues.Random(random);
            var record=new UnitSaveData{id=id??Guid.NewGuid().ToString("N"),speciesId=species.speciesId,
                displayName=species.displayName,family=species.family.ToString(),rank=species.rank.ToString(),
                level=Mathf.Clamp(level,1,99),personality=((PersonalityType)random.Next(6)).ToString(),
                magicSlotGenomeJson=new string[SpellSlotStorage.SlotCount],deployed=false};
            for(int i=0;i<10;i++){record.individualValues[i]=iv.Get(i);record.baseStats[i]=species.baseStats.Get(i);}
            for(int i=0;i<7;i++){var r=species.GetResistance((AttributeElement)i);record.attributeDamageReduction[i]=r.damageReduction;record.attributeAilmentResistance[i]=r.applicationResistance;}
            for(int i=0;i<8;i++)record.debuffResistance[i]=species.GetResistance((DebuffType)i).applicationResistance;
            for(int i=0;i<SpellSlotStorage.SlotCount;i++)
            {
                record.magicSlotGenomeJson[i]=JsonUtility.ToJson(SpeciesDefaultMagicCatalog.Copy(species,i));
            }
            record.magicGenomeJson=new string[RankRules.MagicSlots(species.rank)];
            Array.Copy(record.magicSlotGenomeJson,record.magicGenomeJson,record.magicGenomeJson.Length);
            return record;
        }
    }
}
