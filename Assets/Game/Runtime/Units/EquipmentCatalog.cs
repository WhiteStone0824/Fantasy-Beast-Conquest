using System;
using System.Collections.Generic;
using FusionAnyMonster.Save;

namespace FusionAnyMonster
{
    // A compact, reproducible item roll. Bonuses are derived from the seed, never trusted from the network.
    [Serializable] public sealed class EquipmentItem
    {
        public string id;
        public int seed;
        public int level = 1;
        public int rarity;
        public long acquiredAtUtcTicks;
    }

    public static class EquipmentCatalog
    {
        public const int ExpeditionLimit=100;
        public static int CountOwned(GameSaveData save)
        {
            if(save==null)return 0;
            var ids=new HashSet<string>();
            if(save.equipmentInventory!=null)foreach(var item in save.equipmentInventory)if(HasItem(item))ids.Add(item.id);
            if(save.units!=null)foreach(var unit in save.units)if(unit!=null && HasItem(unit.equipment))ids.Add(unit.equipment.id);
            return ids.Count;
        }
        public static int DiscardOldestExcess(GameSaveData save,int limit=ExpeditionLimit)
        {
            if(save==null || save.equipmentInventory==null)return 0;
            int excess=CountOwned(save)-Math.Max(0,limit);if(excess<=0)return 0;
            var equipped=new HashSet<string>();
            if(save.units!=null)foreach(var unit in save.units)if(unit!=null && HasItem(unit.equipment))equipped.Add(unit.equipment.id);
            var candidates=new List<KeyValuePair<int,EquipmentItem>>();var seen=new HashSet<string>();
            for(int i=0;i<save.equipmentInventory.Count;i++)
            {
                var item=save.equipmentInventory[i];
                if(HasItem(item) && !equipped.Contains(item.id) && seen.Add(item.id))candidates.Add(new KeyValuePair<int,EquipmentItem>(i,item));
            }
            candidates.Sort((a,b)=>{int order=a.Value.acquiredAtUtcTicks.CompareTo(b.Value.acquiredAtUtcTicks);return order!=0?order:a.Key.CompareTo(b.Key);});
            var removed=new HashSet<string>();
            for(int i=0;i<candidates.Count && removed.Count<excess;i++)removed.Add(candidates[i].Value.id);
            save.equipmentInventory.RemoveAll(item=>HasItem(item) && removed.Contains(item.id));
            return removed.Count;
        }
        private static readonly string[] Rarities={"並", "良質", "希少", "伝説"};
        private static readonly string[] Names={"護符", "紋章", "指輪", "腕輪", "首飾り", "羽飾り"};
        public static EquipmentItem Roll(int seed,int level,bool boss=false)
        {
            var rng=new Random(seed);int roll=rng.Next(100);
            int rarity=roll<55?0:roll<82?1:roll<97?2:3;
            if(boss)rarity=Math.Max(1,rarity);
            return new EquipmentItem {id=Guid.NewGuid().ToString("N"),seed=seed,level=Math.Max(1,Math.Min(99,level)),rarity=rarity,acquiredAtUtcTicks=DateTime.UtcNow.Ticks};
        }
        public static bool HasItem(EquipmentItem item) { return item!=null && !string.IsNullOrWhiteSpace(item.id); }
        public static bool IsValid(EquipmentItem item)
        { return !HasItem(item) || (item.id.Length<=64 && item.level>=1 && item.level<=99 && item.rarity>=0 && item.rarity<4); }
        public static string Name(EquipmentItem item)
        { return !HasItem(item)?"なし":Rarities[Math.Max(0,Math.Min(3,item.rarity))]+"の"+Names[(int)((uint)item.seed%Names.Length)]+" Lv."+item.level; }
        // Returns whole percentage points, e.g. 5 means +5%.
        public static float Bonus(EquipmentItem item,int stat)
        {
            if(!HasItem(item) || !IsValid(item) || stat<0 || stat>=6)return 0;
            var rng=new Random(item.seed);rng.Next(100);
            var indices=new[]{0,1,2,3,4,5};
            for(int i=5;i>0;i--){int j=rng.Next(i+1);int tmp=indices[i];indices[i]=indices[j];indices[j]=tmp;}
            float power=(1+item.level/10f)*(1+item.rarity*.3f);
            for(int i=0;i<5;i++)
            {
                int index=indices[i];
                int amount=Math.Max(1,(int)Math.Ceiling(power*1.5f*(.75+rng.NextDouble()*.5)));
                if(index==stat)return amount;
            }
            return 0;
        }
    }
}
