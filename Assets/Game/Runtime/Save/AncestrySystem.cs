using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster.Save
{
    public static class AncestrySystem
    {
        public static void Record(GameSaveData data,UnitSaveData unit)
        {
            if(data==null || unit==null || string.IsNullOrWhiteSpace(unit.id))return;
            if(data.ancestry==null)data.ancestry=new List<AncestorRecord>();
            var record=data.ancestry.Find(item=>item!=null && item.id==unit.id);
            if(record==null){record=new AncestorRecord{id=unit.id};data.ancestry.Add(record);}
            record.speciesId=unit.speciesId;record.displayName=unit.displayName;record.family=unit.family;record.rank=unit.rank;
            record.fatherId=unit.fatherId;record.motherId=unit.motherId;record.generation=unit.generation;
            if((string.IsNullOrEmpty(record.fatherId) || string.IsNullOrEmpty(record.motherId)) && !string.IsNullOrEmpty(unit.lineageJson))
            {
                try
                {
                    var lineage=JsonUtility.FromJson<LineageRecord>(unit.lineageJson);
                    if(lineage!=null){if(string.IsNullOrEmpty(record.fatherId))record.fatherId=lineage.fatherId;if(string.IsNullOrEmpty(record.motherId))record.motherId=lineage.motherId;record.generation=Math.Max(record.generation,lineage.generation);}
                }
                catch(ArgumentException) { }
            }
        }
        public static AncestorRecord Find(GameSaveData data,string id)
        {
            if(data==null || string.IsNullOrEmpty(id))return null;
            var unit=data.units==null?null:data.units.Find(u=>u!=null && u.id==id);
            if(unit!=null)
            {
                var temporary=new GameSaveData();Record(temporary,unit);return temporary.ancestry[0];
            }
            return data.ancestry==null?null:data.ancestry.Find(item=>item!=null && item.id==id);
        }
    }
}
