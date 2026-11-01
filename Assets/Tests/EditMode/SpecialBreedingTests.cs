using System;
using System.Collections.Generic;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpecialBreedingTests
    {
        private SpeciesDatabase database;
        private BreedingRuleDatabase rules;
        private Dictionary<string,string> ancestors;
        private UnitInstance a,b;
        [SetUp] public void Setup()
        {
            database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            rules=Resources.Load<BreedingRuleDatabase>("BreedingRuleDatabase");
            Assert.NotNull(database);Assert.NotNull(rules);
            a=UnitInstance.Create("demo_00",PersonalityType.Brave,21);a.id="parent-a";
            b=UnitInstance.Create("species_F01_R0_A",PersonalityType.Calm,22);b.id="parent-b";
            a.lineage.fatherId="g1";a.lineage.motherId="g2";b.lineage.fatherId="g3";b.lineage.motherId="g4";
            ancestors=new Dictionary<string,string>{{"g1","species_F22_R6_B"},{"g2","species_F28_R6_B"},{"g3","species_F26_R6_B"},{"g4","species_F24_R6_B"}};
        }
        private BreedingSystem SystemUnderTest() { return new BreedingSystem(database,rules,null,ancestorSpeciesLookup:id=>ancestors.TryGetValue(id,out var value)?value:null); }
        [Test] public void FourGrandparentsProduceSpecialInAnyOrderAndPreserveInheritance()
        {
            var originalA=JsonUtility.ToJson(a);var originalB=JsonUtility.ToJson(b);
            foreach(bool swapParents in new[]{false,true})foreach(bool swapFirst in new[]{false,true})foreach(bool swapSecond in new[]{false,true})
            {
                a.lineage.fatherId=swapFirst?"g2":"g1";a.lineage.motherId=swapFirst?"g1":"g2";
                b.lineage.fatherId=swapSecond?"g4":"g3";b.lineage.motherId=swapSecond?"g3":"g4";
                var first=swapParents?b:a;var second=swapParents?a:b;
                var child=SystemUnderTest().Breed(first,second,123);
                Assert.IsTrue(child.isSpecial);Assert.AreEqual(SpecialBreedingRecipe.DefaultResultId,child.species.speciesId);
                Assert.AreEqual(SpeciesRank.S,child.species.rank);Assert.AreEqual(5,child.child.magicGenomes.Count);Assert.AreEqual(5,child.child.magicGeneSlots.Count);
                Assert.AreEqual(first.id,child.child.lineage.fatherId);Assert.AreEqual(second.id,child.child.lineage.motherId);
                foreach(var genome in child.child.magicGenomes)Assert.DoesNotThrow(()=>MagicFactory.Build(genome));
            }
            a.lineage.fatherId="g1";a.lineage.motherId="g2";b.lineage.fatherId="g3";b.lineage.motherId="g4";
            Assert.AreEqual(originalA,JsonUtility.ToJson(a));Assert.AreEqual(originalB,JsonUtility.ToJson(b));
        }
        [Test] public void MissingWrongOrRepeatedGrandparentsDoNotTriggerSpecial()
        {
            ancestors.Remove("g1");Assert.IsFalse(SystemUnderTest().Breed(a,b,123).isSpecial);
            ancestors["g1"]="species_F22_R6_A";Assert.IsFalse(SystemUnderTest().Breed(a,b,123).isSpecial);
            ancestors["g1"]="species_F22_R6_B";b.lineage.motherId="g1";Assert.IsFalse(SystemUnderTest().Breed(a,b,123).isSpecial);
        }
        [Test] public void SpecialSpeciesIsFoundAndNeverAppearsInOrdinaryCandidates()
        {
            var special=database.GetById(SpecialBreedingRecipe.DefaultResultId);
            Assert.NotNull(special);Assert.AreEqual(SpeciesFamily.Truth,special.family);Assert.AreEqual(SpeciesRank.S,special.rank);
            Assert.NotNull(special.passive);Assert.AreEqual(5,special.defaultMagicGenomes.Length);
            CollectionAssert.DoesNotContain(database.FindCandidates(SpeciesFamily.Truth,SpeciesRank.S),special);
            var record=new UnitSaveData{id="test",speciesId=special.speciesId,displayName=special.displayName};
            var restored=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(record));
            Assert.AreSame(special,database.GetById(restored.speciesId));
        }
        [Test] public void SpecialParentAlwaysProducesItsOwnSpeciesRegardlessOfPartnerAndOrder()
        {
            var special=UnitInstance.Create(SpecialBreedingRecipe.DefaultResultId,PersonalityType.Gentle,31);special.id="truth-parent";
            foreach(var partnerSpecies in new[]{"demo_00","species_F01_R0_A","species_F24_R6_B",SpecialBreedingRecipe.DefaultResultId})
            {
                var other=UnitInstance.Create(partnerSpecies,PersonalityType.Brave,32);other.id="other-parent";
                foreach(bool reversed in new[]{false,true})foreach(int seed in new[]{1,42,99})
                {
                    var first=reversed?other:special;var second=reversed?special:other;
                    var result=SystemUnderTest().Breed(first,second,seed);
                    Assert.AreEqual(SpecialBreedingRecipe.DefaultResultId,result.species.speciesId);
                    Assert.AreEqual(SpeciesFamily.Truth,result.species.family);Assert.AreEqual(SpeciesRank.S,result.species.rank);
                    Assert.AreEqual(first.id,result.child.lineage.fatherId);Assert.AreEqual(second.id,result.child.lineage.motherId);
                    Assert.AreEqual(5,result.child.magicGenomes.Count);
                }
            }
        }
    }
}
