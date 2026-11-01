using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class AncestryTests
    {
        [Test] public void TwoGenerationsRemainAfterParentsAreConsumedAndJsonRoundTrips()
        {
            var data=new GameSaveData();
            var grandA=new UnitSaveData{id="ga",displayName="祖父",speciesId="demo_00"};
            var grandB=new UnitSaveData{id="gb",displayName="祖母",speciesId="demo_00"};
            data.units.Add(grandA);data.units.Add(grandB);
            AncestrySystem.Record(data,grandA);AncestrySystem.Record(data,grandB);data.units.Clear();
            var parent=new UnitSaveData{id="parent",displayName="親",fatherId="ga",motherId="gb",generation=1};
            data.units.Add(parent);AncestrySystem.Record(data,parent);data.units.Clear();
            var child=new UnitSaveData{id="child",fatherId="parent",generation=2};data.units.Add(child);
            var loaded=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
            var savedParent=AncestrySystem.Find(loaded,child.fatherId);
            Assert.AreEqual("親",savedParent.displayName);
            Assert.AreEqual("祖父",AncestrySystem.Find(loaded,savedParent.fatherId).displayName);
            Assert.AreEqual("祖母",AncestrySystem.Find(loaded,savedParent.motherId).displayName);
            Assert.IsNull(AncestrySystem.Find(loaded,"lost-old-parent"));
        }
        [Test] public void LegacyLineageJsonAndCurrentLivingRecordAreSupported()
        {
            var data=new GameSaveData();
            var unit=new UnitSaveData{id="unit",displayName="最新の名前",lineageJson="{\"fatherId\":\"a\",\"motherId\":\"b\",\"generation\":2}"};
            data.units.Add(unit);var record=AncestrySystem.Find(data,"unit");
            Assert.AreEqual("a",record.fatherId);Assert.AreEqual("b",record.motherId);Assert.AreEqual(2,record.generation);
            AncestrySystem.Record(data,unit);unit.displayName="変更後";
            Assert.AreEqual("変更後",AncestrySystem.Find(data,"unit").displayName);
        }
    }
}
