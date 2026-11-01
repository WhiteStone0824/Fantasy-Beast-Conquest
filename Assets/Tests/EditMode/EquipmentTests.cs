using System.Linq;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class EquipmentTests
    {
        [Test] public void AutomaticDiscardRemovesOldestUnequippedAndPreservesSetting()
        {
            var data=new GameSaveData{autoDiscardOldEquipment=true};
            var equipped=new EquipmentItem{id="equipped",acquiredAtUtcTicks=1};data.units.Add(new UnitSaveData{equipment=equipped});
            data.equipmentInventory.Add(new EquipmentItem{id="new",acquiredAtUtcTicks=30});
            data.equipmentInventory.Add(new EquipmentItem{id="old",acquiredAtUtcTicks=10});
            data.equipmentInventory.Add(new EquipmentItem{id="middle",acquiredAtUtcTicks=20});
            Assert.AreEqual(2,EquipmentCatalog.DiscardOldestExcess(data,2));
            Assert.AreEqual("equipped",data.units[0].equipment.id);Assert.AreEqual("new",data.equipmentInventory[0].id);
            var restored=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
            Assert.IsTrue(restored.autoDiscardOldEquipment);Assert.AreEqual(30,restored.equipmentInventory[0].acquiredAtUtcTicks);
        }
        [Test] public void AutomaticDiscardUsesInventoryOrderForLegacyItemsAndProtectsEquippedDuplicates()
        {
            var data=new GameSaveData();
            data.equipmentInventory.Add(new EquipmentItem{id="legacy-first"});
            data.equipmentInventory.Add(new EquipmentItem{id="legacy-second"});
            var equipped=new EquipmentItem{id="protected"};data.equipmentInventory.Add(equipped);data.units.Add(new UnitSaveData{equipment=equipped});
            Assert.AreEqual(1,EquipmentCatalog.DiscardOldestExcess(data,2));
            Assert.AreEqual("legacy-second",data.equipmentInventory[0].id);Assert.AreEqual("protected",data.units[0].equipment.id);
        }
        [Test] public void OwnedCountIncludesEquippedItemsWithoutDoubleCountingIds()
        {
            var data=new GameSaveData();
            for(int i=0;i<100;i++)data.equipmentInventory.Add(EquipmentCatalog.Roll(i,1));
            Assert.AreEqual(100,EquipmentCatalog.CountOwned(data));
            data.units.Add(new UnitSaveData{equipment=data.equipmentInventory[0]});
            Assert.AreEqual(100,EquipmentCatalog.CountOwned(data));
            data.units.Add(new UnitSaveData{equipment=EquipmentCatalog.Roll(101,1)});
            Assert.AreEqual(101,EquipmentCatalog.CountOwned(data));
        }
        [Test] public void RollsHaveFiveDistinctBonusesAndAreReproducible()
        {
            for(int seed=0;seed<100;seed++)
            {
                var a=EquipmentCatalog.Roll(seed,25);var b=EquipmentCatalog.Roll(seed,25);
                Assert.AreEqual(5,Enumerable.Range(0,10).Count(i=>EquipmentCatalog.Bonus(a,i)>0));
                Assert.AreEqual(a.rarity,b.rarity);
                for(int i=0;i<10;i++)Assert.AreEqual(EquipmentCatalog.Bonus(a,i),EquipmentCatalog.Bonus(b,i));
            }
        }
        [Test] public void EquippedBonusesAffectStatsAndSurviveJsonRoundTrip()
        {
            var item=EquipmentCatalog.Roll(123,30,true);
            var data=new GameSaveData {equipmentInitialized=true};
            data.units.Add(new UnitSaveData {id="unit",equipment=item});
            var restored=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
            var plain=new UnitInstance {level=1};
            var equipped=new UnitInstance {level=1,equipment=restored.units[0].equipment};
            var species=new UnitStats();
            var without=UnitStatsCalculator.Calculate(species,plain,null,null);
            var with=UnitStatsCalculator.Calculate(species,equipped,null,null);
            for(int i=0;i<10;i++)Assert.AreEqual(without.Get(i)*(1f+EquipmentCatalog.Bonus(item,i)/100f),with.Get(i),.0001f);
            Assert.AreEqual(item.id,restored.units[0].equipment.id);
            Assert.GreaterOrEqual(item.rarity,1);
        }
        [Test] public void EquipmentScalesBaseAfterPersonalityButBeforeTemporaryModifiers()
        {
            var item=EquipmentCatalog.Roll(42,25);var plain=new UnitInstance {level=20};
            var equipped=new UnitInstance {level=20,equipment=item};
            var species=new UnitStats();var modifiers=new UnitStats();
            var personality=new PersonalityDefinition {boostedStatIndex=0,boostMultiplier=1.1f};
            var without=UnitStatsCalculator.Calculate(species,plain,modifiers,personality);
            var with=UnitStatsCalculator.Calculate(species,equipped,modifiers,personality);
            for(int i=0;i<6;i++)
                Assert.AreEqual((without.Get(i)-modifiers.Get(i))*(1f+EquipmentCatalog.Bonus(item,i)/100f)+modifiers.Get(i),with.Get(i),.001f);
        }
    }
}
