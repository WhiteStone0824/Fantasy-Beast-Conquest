using System;
using System.Collections.Generic;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;
using F = FusionAnyMonster.SpeciesFamily;

namespace FusionAnyMonster.Tests
{
    public sealed class FamilyBreedingTests
    {
        [Test]public void OutcomeCountsAreAsUniformAs465IntegerRecipesAllow()
        {
            var counts=new int[30];foreach(var rule in FamilyBreedingCatalog.CreateRules())if(rule.first!=F.Truth && rule.second!=F.Truth)counts[(int)rule.result]++;
            int lower=0,higher=0;
            foreach(int count in counts){Assert.That(count,Is.InRange(15,16));if(count==15)lower++;else higher++;}
            Assert.AreEqual(15,lower);Assert.AreEqual(15,higher);
            // A diagonal contributes once and each mixed pair twice when ordered parents are uniform.
            foreach(int count in counts)Assert.That(count*2-1,Is.InRange(29,31));
        }
        [Test]public void FixedParentRowsHaveVariedOutcomesWithoutLargeDuplicateBlocks()
        {
            foreach(F a in Enum.GetValues(typeof(F)))
            {
                if(a==F.Truth)continue;
                var counts=new Dictionary<F,int>();
                foreach(F b in Enum.GetValues(typeof(F)))
                {if(b==F.Truth)continue;F child=FamilyBreedingCatalog.Resolve(a,b);if(!counts.ContainsKey(child))counts[child]=0;counts[child]++;}
                Assert.GreaterOrEqual(counts.Count,15,a.ToString());
                foreach(int count in counts.Values)Assert.LessOrEqual(count,5,a.ToString());
            }
        }
        [Test]public void All496RulesCoverEveryOrderedPairSymmetricallyAndMatchBothAssets()
        {
            var generated=FamilyBreedingCatalog.CreateRules();Assert.AreEqual(496,generated.Length);
            var keys=new HashSet<int>();foreach(var rule in generated)Assert.IsTrue(keys.Add((int)rule.first*31+(int)rule.second));
            var runtime=Resources.Load<BreedingRuleDatabase>("BreedingRuleDatabase");
            var data=UnityEditor.AssetDatabase.LoadAssetAtPath<BreedingRuleDatabase>("Assets/Game/Data/Breeding/BreedingRuleDatabase.asset");
            Assert.AreEqual(496,runtime.familyRules.Length);Assert.AreEqual(496,data.familyRules.Length);
            Assert.AreEqual(JsonUtility.ToJson(generated[0]),JsonUtility.ToJson(runtime.familyRules[0]));
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            foreach(F a in Enum.GetValues(typeof(F)))foreach(F b in Enum.GetValues(typeof(F)))
            {
                F result=FamilyBreedingCatalog.Resolve(a,b);Assert.IsTrue(Enum.IsDefined(typeof(F),result));
                Assert.AreEqual(result,FamilyBreedingCatalog.Resolve(b,a));Assert.AreEqual(result,runtime.ResolveFamily(a,b));Assert.AreEqual(result,data.ResolveFamily(a,b));
                if(result!=F.Truth){Assert.IsNotEmpty(species.FindCandidates(result,SpeciesRank.F));Assert.IsNotEmpty(species.FindCandidates(result,SpeciesRank.S));}
                else Assert.NotNull(species.GetById(SpecialBreedingRecipe.DefaultResultId));
                if(a==b)Assert.AreEqual(a,result);
                if(a==F.Truth || b==F.Truth)Assert.AreEqual(F.Truth,result);
            }
        }
        [TestCase(F.Spirit,F.Material,F.Arcane)]
        [TestCase(F.Beast,F.Machine,F.ArtificialLife)]
        [TestCase(F.Plant,F.DemiHuman,F.Fairy)]
        [TestCase(F.Beast,F.Reptile,F.Dragon)]
        [TestCase(F.Ghost,F.Material,F.CursedObject)]
        [TestCase(F.Angel,F.Demon,F.Chaos)]
        [TestCase(F.Astral,F.Abyssal,F.Chaos)]
        [TestCase(F.Bird,F.Beast,F.Beast)]
        [TestCase(F.Slime,F.Plant,F.Fungi)]
        [TestCase(F.Mineral,F.Spirit,F.Arcane)]
        public void AuthoredRecipesProduceExpectedFamilyInEitherParentOrder(F a,F b,F expected)
        {
            var rules=Resources.Load<BreedingRuleDatabase>("BreedingRuleDatabase");
            Assert.AreEqual(expected,rules.ResolveFamily(a,b));Assert.AreEqual(expected,rules.ResolveFamily(b,a));
        }
        [Test]public void MissingEntriesUseExplicitCatalogAndCustomRulesCanOverrideIt()
        {
            var rules=ScriptableObject.CreateInstance<BreedingRuleDatabase>();
            try
            {
                rules.familyRules=new FamilyPairRule[]{null};Assert.AreEqual(F.Arcane,rules.ResolveFamily(F.Spirit,F.Material));
                rules.familyRules=new[]{new FamilyPairRule{first=F.Material,second=F.Spirit,result=F.ArtificialLife}};
                Assert.AreEqual(F.ArtificialLife,rules.ResolveFamily(F.Spirit,F.Material));
                rules.familyRules=new[]{new FamilyPairRule{first=F.Slime,second=F.Slime,result=F.ArtificialLife}};
                Assert.AreEqual(F.ArtificialLife,rules.ResolveFamily(F.Slime,F.Slime));
            }
            finally{UnityEngine.Object.DestroyImmediate(rules);}
        }
        [TestCase(true)][TestCase(false)]
        public void SpiritAndMaterialActuallyBreedAnArcaneSpecies(bool useDatabase)
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase");var rules=ScriptableObject.CreateInstance<BreedingRuleDatabase>();
            try
            {
                rules.familyRules=FamilyBreedingCatalog.CreateRules();rules.rankBonusChance=rules.rankPenaltyChance=0;
                var a=Parent(species.FindCandidates(F.Spirit,SpeciesRank.F)[0],"spirit");
                var b=Parent(species.FindCandidates(F.Material,SpeciesRank.F)[0],"material");
                string originalA=JsonUtility.ToJson(a),originalB=JsonUtility.ToJson(b);
                var breeding=new BreedingSystem(species,useDatabase?rules:null,null);
                foreach(var child in new[]{breeding.Breed(a,b,12),breeding.Breed(b,a,12)})
                {
                    Assert.AreEqual(F.Arcane,child.species.family);Assert.IsNotNull(species.GetById(child.child.speciesId));
                    Assert.AreEqual(5,child.child.magicGeneSlots.Count);
                    foreach(var g in child.child.magicGeneSlots)Assert.IsNotNull(MagicFactory.Build(g).program);
                }
                Assert.AreEqual(originalA,JsonUtility.ToJson(a));Assert.AreEqual(originalB,JsonUtility.ToJson(b));
            }
            finally{UnityEngine.Object.DestroyImmediate(rules);}
        }
        [Test]public void MissingResultFamilyCannotSilentlyProduceAnotherFamily()
        {
            var all=Resources.Load<SpeciesDatabase>("SpeciesDatabase");var sparse=ScriptableObject.CreateInstance<SpeciesDatabase>();
            try
            {
                sparse.species=new[]{all.FindCandidates(F.Spirit,SpeciesRank.F)[0],all.FindCandidates(F.Material,SpeciesRank.F)[0]};
                var a=Parent(sparse.species[0],"a");var b=Parent(sparse.species[1],"b");var breeding=new BreedingSystem(sparse,null,null);
                Assert.IsEmpty(breeding.PreviewCandidates(a,b,1,out _));Assert.Throws<InvalidOperationException>(()=>breeding.Breed(a,b,1));
            }
            finally{UnityEngine.Object.DestroyImmediate(sparse);}
        }
        private static UnitInstance Parent(SpeciesDefinition species,string id)
        {
            var parent=new UnitInstance{id=id,speciesId=species.speciesId};
            for(int slot=0;slot<5;slot++)parent.magicGeneSlots.Add(SpeciesDefaultMagicCatalog.Copy(species,slot));return parent;
        }
    }
}
