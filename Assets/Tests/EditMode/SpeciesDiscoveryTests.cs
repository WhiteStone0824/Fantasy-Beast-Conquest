using System.Collections.Generic;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpeciesDiscoveryTests
    {
        [Test]
        public void LegacySaveRegistersOnlyOwnedSpeciesAndNormalizesHistory()
        {
            var data=JsonUtility.FromJson<GameSaveData>("{\"units\":[{\"speciesId\":\"slime\"},{\"speciesId\":\"slime\"},{\"speciesId\":\"bird\"}]}");
            data.discoveredSpeciesIds=new List<string>{null,"","slime","slime"};
            Assert.IsTrue(SaveSystem.RecordOwnedSpecies(data));
            CollectionAssert.AreEquivalent(new[]{"slime","bird"},data.discoveredSpeciesIds);
            Assert.IsFalse(SaveSystem.RecordOwnedSpecies(data));
        }

        [Test]
        public void AcquisitionHistorySurvivesConsumedParentsAndJsonReload()
        {
            var data=new GameSaveData{units=new List<UnitSaveData>{new UnitSaveData{speciesId="father"},new UnitSaveData{speciesId="mother"}}};
            SaveSystem.RecordOwnedSpecies(data);
            data.units.Clear();data.units.Add(new UnitSaveData{speciesId="child"});
            SaveSystem.RecordOwnedSpecies(data);
            var restored=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
            CollectionAssert.AreEquivalent(new[]{"father","mother","child"},restored.discoveredSpeciesIds);
            restored.units.Clear();SaveSystem.RecordOwnedSpecies(restored);
            Assert.AreEqual(3,restored.discoveredSpeciesIds.Count);
        }
    }
}
