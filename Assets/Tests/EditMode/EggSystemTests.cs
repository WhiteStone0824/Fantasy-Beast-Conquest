using System.Collections.Generic;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class EggSystemTests
    {
        private static bool Succeed(GameSaveData data, out string error) { error = null; return true; }
        private static bool Fail(GameSaveData data, out string error) { error = "write failed"; return false; }

        [Test]
        public void AwardAndHatchPreserveBossGenesAndRecordDiscoveryOnlyAfterHatching()
        {
            var database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var species=database.GetById("demo_00");
            var boss=WildUnitFactory.Create(species, 8, 1234);
            var source=new GameSaveData { units=new List<UnitSaveData>(), discoveredSpeciesIds=new List<string>() };
            Assert.IsTrue(EggSystem.TryAward(source,boss,out var awarded,out var error,Succeed),error);
            Assert.AreEqual(0,source.eggs.Count);
            Assert.AreEqual(1,awarded.eggs.Count);
            Assert.AreEqual(0,awarded.discoveredSpeciesIds.Count);
            var loaded=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(awarded));
            Assert.IsTrue(EggSystem.TryHatch(loaded,boss.id,out var committed,out var unit,out error,Succeed),error);
            Assert.AreEqual(0,committed.eggs.Count);
            Assert.AreEqual(1,committed.units.Count);
            Assert.AreEqual(boss.speciesId,unit.speciesId);
            Assert.AreEqual(1,unit.level);
            Assert.AreEqual(0,unit.experience);
            Assert.AreEqual(8,loaded.eggs[0].offspring.level);
            Assert.AreEqual(boss.personality,unit.personality);
            CollectionAssert.AreEqual(boss.individualValues,unit.individualValues);
            CollectionAssert.AreEqual(boss.magicSlotGenomeJson,unit.magicSlotGenomeJson);
            Assert.AreNotEqual(boss.id,unit.id);
            Assert.IsFalse(unit.deployed);
            CollectionAssert.Contains(committed.discoveredSpeciesIds,boss.speciesId);
        }

        [Test]
        public void FailedWritesNeverConsumeEggOrMutateRoster()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById("demo_00");
            var boss=WildUnitFactory.Create(species, 3, 456);
            var source=new GameSaveData();
            Assert.IsFalse(EggSystem.TryAward(source,boss,out var ignored,out var error,Fail));
            Assert.AreEqual(0,source.eggs.Count);
            Assert.IsTrue(EggSystem.TryAward(source,boss,out var awarded,out error,Succeed));
            Assert.IsFalse(EggSystem.TryAward(awarded,boss,out ignored,out error,Succeed));
            Assert.IsFalse(EggSystem.TryHatch(awarded,boss.id,out ignored,out var unit,out error,Fail));
            Assert.IsNull(unit);
            Assert.AreEqual(1,awarded.eggs.Count);
            Assert.AreEqual(0,awarded.units.Count);
        }
    }
}
