using UnityEngine;

namespace FusionAnyMonster.Save
{
    public enum FarewellKind { Egg, Unit, Equipment }
    public static class FarewellSystem
    {
        public static bool TryRemove(GameSaveData source,FarewellKind kind,string id,out GameSaveData committed,out string error,EggSystem.SaveWriter writer=null)
        {
            committed=null;error=null;
            if(source==null || source.readOnly || source.schemaVersion>SaveSystem.CurrentSchemaVersion){error="セーブデータが書き込み禁止です。";return false;}
            if(string.IsNullOrEmpty(id)){error="対象が見つかりません。";return false;}
            var next=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(source));bool removed=false;
            if(kind==FarewellKind.Egg && next.eggs!=null)
                removed=next.eggs.RemoveAll(e=>e!=null && e.id==id)>0;
            else if(kind==FarewellKind.Unit && next.units!=null)
            {
                var unit=next.units.Find(u=>u!=null && u.id==id);
                if(unit!=null)
                {
                    SaveSystem.RecordOwnedSpecies(next);AncestrySystem.Record(next,unit);
                    if(EquipmentCatalog.HasItem(unit.equipment))
                    {
                        if(next.equipmentInventory==null)next.equipmentInventory=new System.Collections.Generic.List<EquipmentItem>();
                        if(!next.equipmentInventory.Exists(e=>e!=null && e.id==unit.equipment.id))next.equipmentInventory.Add(unit.equipment);
                    }
                    if(next.formationUnitIds!=null)next.formationUnitIds.RemoveAll(x=>x==id);
                    next.units.Remove(unit);removed=true;
                }
            }
            else if(kind==FarewellKind.Equipment)
            {
                if(next.equipmentInventory!=null)removed=next.equipmentInventory.RemoveAll(e=>e!=null && e.id==id)>0;
                if(next.units!=null)foreach(var unit in next.units)if(unit!=null && EquipmentCatalog.HasItem(unit.equipment) && unit.equipment.id==id){unit.equipment=null;removed=true;}
            }
            if(!removed){error="対象が見つかりません。";return false;}
            if(next.autoDiscardOldEquipment)EquipmentCatalog.DiscardOldestExcess(next);
            if(!(writer??SaveSystem.Save)(next,out error))return false;
            committed=next;return true;
        }
    }
}
