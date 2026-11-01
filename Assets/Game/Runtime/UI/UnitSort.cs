using System;
using System.Collections.Generic;
using FusionAnyMonster.Save;

namespace FusionAnyMonster.UI
{
    public enum UnitSortMode { Acquired, RankDescending, RankAscending, LevelDescending, LevelAscending }
    public static class UnitSort
    {
        public static List<UnitSaveData> Apply(IList<UnitSaveData> units, UnitSortMode mode)
        {
            var indexed=new List<KeyValuePair<int,UnitSaveData>>();
            if(units!=null)for(int i=0;i<units.Count;i++)if(units[i]!=null)indexed.Add(new KeyValuePair<int,UnitSaveData>(i,units[i]));
            if(mode!=UnitSortMode.Acquired)indexed.Sort((a,b)=>
            {
                int result;
                if(mode==UnitSortMode.LevelDescending || mode==UnitSortMode.LevelAscending)
                    result=a.Value.level.CompareTo(b.Value.level);
                else result=Rank(a.Value.rank).CompareTo(Rank(b.Value.rank));
                if(mode==UnitSortMode.LevelDescending || mode==UnitSortMode.RankDescending)result=-result;
                return result!=0?result:a.Key.CompareTo(b.Key);
            });
            var resultList=new List<UnitSaveData>(indexed.Count);foreach(var item in indexed)resultList.Add(item.Value);return resultList;
        }
        private static int Rank(string value)
        { SpeciesRank rank;return Enum.TryParse(value,true,out rank) && Enum.IsDefined(typeof(SpeciesRank),rank)?(int)rank:-1; }
    }
}
