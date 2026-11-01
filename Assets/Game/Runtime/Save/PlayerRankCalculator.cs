using System;
using System.Collections.Generic;

namespace FusionAnyMonster.Save
{
    public sealed class PlayerRankBreakdown
    {
        public int catalogue, unitLevels, equipmentLevels, individualValues, magic;
        public int Total { get { return catalogue + unitLevels + equipmentLevels + individualValues + magic; } }
    }

    // Derived from the current save so it cannot become stale when units or equipment change.
    public static class PlayerRankCalculator
    {
        public static PlayerRankBreakdown Calculate(GameSaveData save)
        {
            var result=new PlayerRankBreakdown();
            if(save==null)return result;
            var species=new HashSet<string>(StringComparer.Ordinal);
            var equipment=new HashSet<string>(StringComparer.Ordinal);
            long levels=0, equipmentLevel=0, values=0, spellCount=0;
            if(save.discoveredSpeciesIds!=null)
                foreach(var id in save.discoveredSpeciesIds)if(!string.IsNullOrWhiteSpace(id))species.Add(id);
            if(save.equipmentInventory!=null)
                foreach(var item in save.equipmentInventory)AddEquipment(item,equipment,ref equipmentLevel);
            if(save.units!=null)foreach(var unit in save.units)
            {
                if(unit==null)continue;
                if(!string.IsNullOrWhiteSpace(unit.speciesId))species.Add(unit.speciesId);
                levels+=Math.Max(1,Math.Min(99,unit.level));
                if(unit.individualValues!=null)
                    for(int i=0;i<Math.Min(10,unit.individualValues.Length);i++)values+=Math.Max(0,Math.Min(100,unit.individualValues[i]));
                AddEquipment(unit.equipment,equipment,ref equipmentLevel);
                SpeciesRank rank;
                if(!Enum.TryParse(unit.rank,true,out rank) || (int)rank<0 || (int)rank>6)rank=SpeciesRank.F;
                int maximum=RankRules.MagicSlots(rank);
                if(unit.magicGenomeJson!=null)
                    for(int i=0;i<Math.Min(maximum,unit.magicGenomeJson.Length);i++)
                    { MagicGenome genome;if(MagicGenome.TryParseJson(unit.magicGenomeJson[i],out genome))spellCount++; }
            }
            result.catalogue=species.Count;
            result.unitLevels=ToInt(levels/10);result.equipmentLevels=ToInt(equipmentLevel/20);
            result.individualValues=ToInt(values/200);result.magic=ToInt(spellCount/2);
            return result;
        }
        private static void AddEquipment(EquipmentItem item,HashSet<string> seen,ref long total)
        {
            if(EquipmentCatalog.HasItem(item) && EquipmentCatalog.IsValid(item) && seen.Add(item.id))total+=item.level;
        }
        private static int ToInt(long value) { return (int)Math.Min(value,int.MaxValue/5); }
    }
}
