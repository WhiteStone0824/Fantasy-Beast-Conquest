using System;
using System.IO;
using FusionAnyMonster.Save;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class StarterPartyTests
    {
        [Test] public void StarterPartyUsesRealSpeciesAndTheirDefaultPrograms()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var save=SaveSystem.CreateStarterData(db);
            CollectionAssert.AreEqual(new[]{"demo_00","species_F01_R0_A","species_F19_R0_A"},save.units.ConvertAll(u=>u.speciesId));
            CollectionAssert.AreEqual(new[]{"Cautious","Brave","Gentle"},save.units.ConvertAll(u=>u.personality));
            Assert.AreEqual(3,save.formationUnitIds.Count);
            bool healing=false;
            foreach(var unit in save.units)
            {
                var species=db.GetById(unit.speciesId);
                Assert.NotNull(species);Assert.AreEqual(species.displayName,unit.displayName);
                Assert.AreEqual(SpeciesRank.F,species.rank);Assert.AreEqual(1,unit.level);Assert.IsTrue(unit.deployed);
                Assert.AreEqual(5,unit.magicSlotGenomeJson.Length);Assert.AreEqual(1,unit.magicGenomeJson.Length);
                Assert.IsTrue(MagicGenome.TryParseJson(unit.magicGenomeJson[0],out var genome));
                Assert.AreEqual(JsonUtility.ToJson(SpeciesDefaultMagicCatalog.Copy(species,0)),JsonUtility.ToJson(genome));
                foreach(var node in MagicFactory.Build(genome).program.instructions)healing|=node.kind==SpellNodeKind.Heal;
                Assert.Contains(unit.speciesId,save.discoveredSpeciesIds);
                Assert.IsTrue(Array.TrueForAll(unit.individualValues,iv=>iv>=0 && iv<=100));
            }
            Assert.IsTrue(healing);
        }
        [Test] public void ExistingEmptySaveDoesNotReceiveAnotherStarterParty()
        {
            byte[] previous=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                var empty=new GameSaveData {equipmentInitialized=true,battlesCompleted=7};
                Assert.IsTrue(SaveSystem.Save(empty,out var error),error);
                var restored=SaveSystem.LoadOrCreate();
                Assert.AreEqual(0,restored.units.Count);Assert.AreEqual(7,restored.battlesCompleted);
            }
            finally
            {
                if(previous==null){if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);}
                else File.WriteAllBytes(SaveSystem.SavePath,previous);
            }
        }
    }
}
