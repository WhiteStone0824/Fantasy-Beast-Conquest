using System.Collections.Generic;
using FusionAnyMonster.Save;
using NUnit.Framework;

namespace FusionAnyMonster.Tests
{
    public sealed class FarewellSystemTests
    {
        private static bool Accept(GameSaveData data,out string error){error=null;return true;}
        private static bool Reject(GameSaveData data,out string error){error="failed";return false;}
        [Test] public void UnitFarewellPreservesEquipmentDiscoveryAndAncestry()
        {
            var unit=new UnitSaveData{id="u",speciesId="species",displayName="仲間",fatherId="parent",deployed=true,equipment=new EquipmentItem{id="gear",level=3}};
            var source=new GameSaveData{units=new List<UnitSaveData>{unit},formationUnitIds=new List<string>{"u"}};
            Assert.IsTrue(FarewellSystem.TryRemove(source,FarewellKind.Unit,"u",out var next,out _,Accept));
            Assert.AreEqual(1,source.units.Count);Assert.IsEmpty(next.units);Assert.IsEmpty(next.formationUnitIds);
            Assert.AreEqual("gear",next.equipmentInventory[0].id);CollectionAssert.Contains(next.discoveredSpeciesIds,"species");
            Assert.AreEqual("parent",AncestrySystem.Find(next,"u").fatherId);
        }
        [Test] public void RemovingEggAndBothEquippedAndStoredEquipmentWorks()
        {
            var source=new GameSaveData();source.eggs.Add(new EggSaveData{id="egg",offspring=new UnitSaveData{speciesId="secret"}});
            source.units.Add(new UnitSaveData{id="u",equipment=new EquipmentItem{id="worn"}});source.equipmentInventory.Add(new EquipmentItem{id="bag"});
            Assert.IsTrue(FarewellSystem.TryRemove(source,FarewellKind.Egg,"egg",out var noEgg,out _,Accept));Assert.IsEmpty(noEgg.eggs);
            Assert.IsTrue(FarewellSystem.TryRemove(noEgg,FarewellKind.Equipment,"worn",out var noWorn,out _,Accept));Assert.IsNull(noWorn.units[0].equipment);
            Assert.IsTrue(FarewellSystem.TryRemove(noWorn,FarewellKind.Equipment,"bag",out var noGear,out _,Accept));Assert.IsEmpty(noGear.equipmentInventory);
            Assert.IsFalse(FarewellSystem.TryRemove(source,FarewellKind.Egg,"egg",out var failed,out _,Reject));Assert.IsNull(failed);Assert.AreEqual(1,source.eggs.Count);
        }
    }
}
