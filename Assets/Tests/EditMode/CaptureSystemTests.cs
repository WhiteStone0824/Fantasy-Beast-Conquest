using System.Collections.Generic;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class CaptureSystemTests
    {
        private CaptureRules rules;private UnitSaveData wild;
        [SetUp]public void Setup()
        {
            rules=ScriptableObject.CreateInstance<CaptureRules>();
            wild=WildUnitFactory.Create(Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0],5,42,"wild-test");
        }
        [TearDown]public void Cleanup(){Object.DestroyImmediate(rules);}

        [Test]public void LowerHealthRaisesChanceAndBossesAreHarder()
        {
            Assert.AreEqual(0,rules.Chance(0,false));Assert.AreEqual(0,rules.Chance(.36f,false));
            Assert.AreEqual(.4f,rules.Chance(.35f,false),.001f);
            Assert.Greater(rules.Chance(.1f,false),rules.Chance(.3f,false));
            Assert.AreEqual(rules.Chance(.1f,false)*.5f,rules.Chance(.1f,true),.001f);
            Assert.AreEqual(0,rules.Chance(float.NaN,false));
        }
        [Test]public void WildIdentityIsSeededAndContainsFiveValidGpSlots()
        {
            var copy=WildUnitFactory.Create(Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0],5,42,"wild-test");
            Assert.AreEqual(JsonUtility.ToJson(wild),JsonUtility.ToJson(copy));Assert.AreEqual(5,wild.magicSlotGenomeJson.Length);
            foreach(string json in wild.magicSlotGenomeJson){MagicGenome g;Assert.IsTrue(MagicGenome.TryParseJson(json,out g));Assert.IsNotNull(MagicFactory.Build(g).program);}
            foreach(int value in wild.individualValues)Assert.That(value,Is.InRange(0,100));
        }
        [Test]public void SuccessfulCapturePreservesTraitsAndAddsDiscoveryWithoutDeploying()
        {
            rules.minimumChance=rules.maximumChance=1;wild.deployed=true;
            var source=new GameSaveData();string original=JsonUtility.ToJson(source),wildOriginal=JsonUtility.ToJson(wild);
            GameSaveData committed;int writes=0;
            var result=CaptureSystem.TryCapture(source,wild,.1f,false,false,0,new System.Random(1),rules,out committed,
                delegate(GameSaveData data,out string error){writes++;error=null;return true;});
            Assert.AreEqual(CaptureStatus.Success,result.status);Assert.AreEqual(1,writes);Assert.AreEqual(1,committed.units.Count);
            Assert.IsFalse(result.unit.deployed);Assert.Contains(wild.speciesId,committed.discoveredSpeciesIds);
            CollectionAssert.AreEqual(wild.individualValues,result.unit.individualValues);
            CollectionAssert.AreEqual(wild.magicSlotGenomeJson,result.unit.magicSlotGenomeJson);
            Assert.AreEqual(wild.personality,result.unit.personality);Assert.AreEqual(wild.level,result.unit.level);
            Assert.AreEqual(original,JsonUtility.ToJson(source));Assert.AreEqual(wildOriginal,JsonUtility.ToJson(wild));
        }
        [Test]public void SaveFailureChangesNeitherRosterNorDiscovery()
        {
            rules.minimumChance=rules.maximumChance=1;var source=new GameSaveData();string original=JsonUtility.ToJson(source);
            GameSaveData committed;
            var result=CaptureSystem.TryCapture(source,wild,.1f,false,false,0,new System.Random(1),rules,out committed,
                delegate(GameSaveData data,out string error){error="test disk failure";return false;});
            Assert.AreEqual(CaptureStatus.SaveFailed,result.status);Assert.IsNull(committed);Assert.AreEqual(original,JsonUtility.ToJson(source));
        }
        [Test]public void RejectedAttemptsNeverWriteAndDuplicatesCannotBeAdded()
        {
            var source=new GameSaveData();GameSaveData committed;int writes=0;
            CaptureSystem.SaveWriter writer=delegate(GameSaveData data,out string error){writes++;error=null;return true;};
            foreach(var pair in new[]{new[]{true, false},new[]{false,true}})
                Assert.AreEqual(CaptureStatus.Unavailable,CaptureSystem.TryCapture(source,wild,.1f,false,pair[0],pair[1]?3:0,new System.Random(1),rules,out committed,writer).status);
            Assert.AreEqual(CaptureStatus.Unavailable,CaptureSystem.TryCapture(source,wild,1,false,false,0,new System.Random(1),rules,out committed,writer).status);
            source.units.Add(wild);
            Assert.AreEqual(CaptureStatus.Unavailable,CaptureSystem.TryCapture(source,wild,.1f,false,false,0,new System.Random(1),rules,out committed,writer).status);
            source.units.Clear();source.readOnly=true;
            Assert.AreEqual(CaptureStatus.Unavailable,CaptureSystem.TryCapture(source,wild,.1f,false,false,0,new System.Random(1),rules,out committed,writer).status);
            Assert.AreEqual(0,writes);
        }
        [Test]public void FailedRollIsSeededAndDoesNotAddAUnit()
        {
            rules.minimumChance=rules.maximumChance=.000001f;
            GameSaveData committed;int writes=0;
            var result=CaptureSystem.TryCapture(new GameSaveData(),wild,.1f,false,false,0,new System.Random(1),rules,out committed,
                delegate(GameSaveData data,out string error){writes++;error=null;return true;});
            Assert.AreEqual(CaptureStatus.Failed,result.status);Assert.IsNull(committed);Assert.AreEqual(0,writes);
        }
    }
}
