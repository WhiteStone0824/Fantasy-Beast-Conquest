using System;
using System.Collections.Generic;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class CoreSystemTests
    {
        private SpeciesDatabase species;
        private BreedingRuleDatabase rules;
        private RankBreedingRule rankRule;

        [SetUp]
        public void SetUp()
        {
            species = DemoDataFactory.CreateSpeciesDatabase();
            rules = ScriptableObject.CreateInstance<BreedingRuleDatabase>();
            var pairs = new List<FamilyPairRule>();
            for (int a = 0; a < 30; a++)
            for (int b = a; b < 30; b++)
                pairs.Add(new FamilyPairRule { first = (SpeciesFamily)a, second = (SpeciesFamily)b, result = (SpeciesFamily)Mathf.Min(a, b) });
            rules.familyRules = pairs.ToArray();
            rankRule = ScriptableObject.CreateInstance<RankBreedingRule>();
        }

        [TearDown]
        public void TearDown()
        {
            if (species != null && species.species != null)
                foreach (SpeciesDefinition definition in species.species)
                    if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
            if (species != null) UnityEngine.Object.DestroyImmediate(species);
            if (rules != null) UnityEngine.Object.DestroyImmediate(rules);
            if (rankRule != null) UnityEngine.Object.DestroyImmediate(rankRule);
        }

        [Test]
        public void SpeciesMasterValuesAreFixedAndDistinct()
        {
            Assert.GreaterOrEqual(species.species.Length, 10);
            SpeciesDefinition first = species.species[0];
            float originalHp = first.baseStats.hp;
            Assert.AreSame(first, species.GetById(first.speciesId));
            Assert.AreNotEqual(species.species[0].baseStats.hp, species.species[1].baseStats.hp);
            var unit = new UnitInstance { speciesId = first.speciesId, level = 80, iv = IndividualValues.Random(new System.Random(9)), personality = PersonalityType.Aggressive };
            UnitStatsCalculator.Calculate(first, unit, null, null);
            Assert.AreEqual(originalHp, first.baseStats.hp, "Runtime stat calculation must not mutate shared species data.");
        }

        [Test]
        public void AllSpeciesHaveFamilyResistancesAndMatchGeneratedMasterStats()
        {
            var stored=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            Assert.AreEqual(420,stored.species.Length);
            foreach(var generated in species.species)
            {
                var master=stored.GetById(generated.speciesId);
                Assert.IsNotNull(master,generated.speciesId);
                var profile=SpeciesFamilyTraits.For(master.family);
                Assert.GreaterOrEqual(master.GetResistance((AttributeElement)profile.primary).damageReduction,.45f,master.speciesId);
                Assert.LessOrEqual(master.GetResistance((AttributeElement)profile.weak).damageReduction,.08001f,master.speciesId);
                Assert.GreaterOrEqual(master.GetResistance((DebuffType)profile.debuff).applicationResistance,.45f,master.speciesId);
                for(int i=0;i<10;i++)Assert.AreEqual(generated.baseStats.Get(i),master.baseStats.Get(i),.001f,master.speciesId+" stat "+i);
                for(int i=0;i<7;i++)Assert.AreEqual(generated.GetResistance((AttributeElement)i).damageReduction,master.GetResistance((AttributeElement)i).damageReduction,.0001f,master.speciesId);
                for(int i=0;i<8;i++)Assert.AreEqual(generated.GetResistance((DebuffType)i).applicationResistance,master.GetResistance((DebuffType)i).applicationResistance,.0001f,master.speciesId);
            }
        }

        [Test]
        public void EachFamilyAndRankHasTwoDifferentFixedStatProfiles()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            foreach(SpeciesFamily family in Enum.GetValues(typeof(SpeciesFamily)))
            foreach(SpeciesRank rank in Enum.GetValues(typeof(SpeciesRank)))
            {
                if(family==SpeciesFamily.Truth)continue; // Special-only family has one S-rank species.
                var candidates=db.FindCandidates(family,rank);
                Assert.AreEqual(2,candidates.Length,family+" "+rank);
                bool different=false;
                for(int i=0;i<10;i++)if(!Mathf.Approximately(candidates[0].baseStats.Get(i),candidates[1].baseStats.Get(i)))different=true;
                Assert.IsTrue(different,family+" "+rank);
            }
        }

        [Test]
        public void FinalStatsGrowWithLevelAndUseTheSameCalculatorForBattleData()
        {
            var lowLevel = new UnitInstance { level = 1, iv = new IndividualValues(), personality = PersonalityType.Brave };
            var highLevel = new UnitInstance { level = 99, iv = new IndividualValues(), personality = PersonalityType.Brave };
            var brave = new PersonalityDefinition { type = PersonalityType.Brave, boostedStatIndex = 2, reducedStatIndex = 4, boostMultiplier = 1.1f, reductionMultiplier = .9f };
            UnitStats low = UnitStatsCalculator.Calculate(species.species[0].baseStats, lowLevel, null, brave);
            UnitStats high = UnitStatsCalculator.Calculate(species.species[0].baseStats, highLevel, null, brave);
            Assert.Greater(high.hp, low.hp);
            Assert.Greater(high.physicalAttack, low.physicalAttack);
            Assert.AreEqual(low.physicalAttack, species.species[0].baseStats.physicalAttack * .51f * 1.1f, .001f);
        }

        [Test]
        public void CatalogContainsTwoSpeciesForEveryFamilyAndRank()
        {
            Assert.AreEqual(DemoDataFactory.ExpectedSpeciesCount, species.species.Length);
            var ids = new HashSet<string>();
            for (int family = 0; family < 30; family++)
            for (int rank = 0; rank < 7; rank++)
            {
                SpeciesDefinition[] candidates = species.FindCandidates((SpeciesFamily)family, (SpeciesRank)rank);
                Assert.AreEqual(2, candidates.Length, "Expected two entries for " + (SpeciesFamily)family + " rank " + (SpeciesRank)rank);
                Assert.AreNotEqual(candidates[0].displayName, candidates[1].displayName);
                foreach (SpeciesDefinition candidate in candidates)
                {
                    Assert.IsTrue(ids.Add(candidate.speciesId), "Species IDs must be unique.");
                    Assert.AreEqual(7, candidate.elementResistances.Length);
                    Assert.AreEqual(8, candidate.debuffResistances.Length);
                }
            }
            Assert.AreEqual("demo_00", species.species[0].speciesId);
            Assert.AreEqual("スライム", species.species[0].displayName);
        }

        [Test]
        public void IndividualValuesAndLevelsStayInRange()
        {
            var iv = new IndividualValues();
            for (int i = 0; i < 10; i++) iv.Set(i, i % 2 == 0 ? -5 : int.MaxValue);
            for (int i = 0; i < 10; i++) Assert.AreEqual(i % 2 == 0 ? 0 : 100,iv.Get(i));
            var unit = new UnitInstance { level = 98, experience = 0 };
            Assert.AreEqual(1, ExperienceSystem.Grant(unit, long.MaxValue));
            Assert.AreEqual(99, unit.level);
            Assert.AreEqual(0, unit.experience);

            unit.experience = 600;
            Assert.AreEqual(0, ExperienceSystem.Grant(unit, 10));
            Assert.AreEqual(0, unit.experience, "Max-level units must not retain unspendable experience.");

            unit.level = 1;
            unit.experience = -50;
            ExperienceSystem.Grant(unit, 1);
            Assert.AreEqual(1, unit.experience, "Negative save data must not overflow experience arithmetic.");
        }

        [Test]
        public void RandomIndividualValuesIncludeBothEndpointsAndValuesAboveThirtyOne()
        {
            var rng=new System.Random(711);
            bool foundZero=false,foundHundred=false,foundAboveOldLimit=false;
            for(int sample=0;sample<1000;sample++)
            {
                IndividualValues iv=IndividualValues.Random(rng);
                for(int stat=0;stat<10;stat++)
                {
                    int value=iv.Get(stat);
                    Assert.That(value,Is.InRange(0,100));
                    foundZero |= value==0; foundHundred |= value==100; foundAboveOldLimit |= value>31;
                }
            }
            Assert.IsTrue(foundZero && foundHundred && foundAboveOldLimit);
        }

        [Test]
        public void ManaRegenerationIsOneFifthOfCalculatedBaseAndIndividualGrowth()
        {
            var baseStats=new UnitStats{mpRegen=20};var unit=new UnitInstance{level=50,iv=new IndividualValues()};unit.iv.Set(9,40);
            var result=UnitStatsCalculator.Calculate(baseStats,unit,null,null);
            Assert.AreEqual((20f+40f*.5f)*.2f,result.mpRegen,.001f);
        }

        [Test]
        public void BreedingRankAverageRandomlyRoundsHalfUpOrDownForBothRulePaths()
        {
            rankRule.rankOffset=0;rankRule.promotionChance=0;rankRule.demotionChance=0;
            rules.rankBonusChance=0;rules.rankPenaltyChance=0;
            var fallback=typeof(BreedingSystem).GetMethod("ResolveAverageRank",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(fallback);
            for(int a=0;a<=6;a++)for(int b=0;b<=6;b++)for(int upper=0;upper<=1;upper++)
            {
                var expected=(SpeciesRank)((a+b)/2+((a+b)%2==1?upper:0));
                Assert.AreEqual(expected,rankRule.Resolve((SpeciesRank)a,(SpeciesRank)b,new FixedRankRandom(upper)));
                Assert.AreEqual(expected,fallback.Invoke(null,new object[]{(SpeciesRank)a,(SpeciesRank)b,new FixedRankRandom(upper),rules}));
            }
        }
        private sealed class FixedRankRandom:System.Random
        {
            private readonly int value;
            public FixedRankRandom(int value){this.value=value;}
            public override int Next(int maxValue){return value;}
            public override double NextDouble(){return .99;}
        }

        [Test]
        public void BreedingWithRecordedSeedReproducesInheritedTraits()
        {
            UnitInstance a=Parent(species.species[0],1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            UnitInstance b=Parent(species.species[1],2,PersonalityType.Timid,DemoDataFactory.DemoGenome(1));
            var breeding=new BreedingSystem(species,rules,rankRule);int seed=BreedingSystem.CreateRandomSeed();
            var first=breeding.Breed(a,b,seed);var second=breeding.Breed(a,b,seed);
            Assert.AreEqual(first.species.speciesId,second.species.speciesId);
            Assert.AreEqual(first.child.personality,second.child.personality);
            for(int i=0;i<10;i++)Assert.AreEqual(first.child.iv.Get(i),second.child.iv.Get(i));
            for(int i=0;i<first.child.magicGeneSlots.Count;i++)Assert.AreEqual(JsonUtility.ToJson(first.child.magicGeneSlots[i]),JsonUtility.ToJson(second.child.magicGeneSlots[i]));
            StringAssert.Contains("seed "+seed,first.child.lineage.breedingSummary);
        }

        [Test]
        public void BreedingUsesConfiguredNormalDistributionAndBoundsIndividualValues()
        {
            UnitInstance a=Parent(species.species[0],1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            UnitInstance b=Parent(species.species[1],2,PersonalityType.Timid,DemoDataFactory.DemoGenome(1));
            for(int stat=0;stat<10;stat++){a.iv.Set(stat,100);b.iv.Set(stat,99);}
            var breeding=new BreedingSystem(species,rules,rankRule);
            rules.ivStandardDeviation=0;
            UnitInstance inherited=breeding.Breed(a,b,91).child;
            for(int stat=0;stat<10;stat++) Assert.AreEqual(100,inherited.iv.Get(stat));
            rules.ivStandardDeviation=100;
            for(int seed=0;seed<30;seed++)
            {
                UnitInstance child=breeding.Breed(a,b,seed).child;
                for(int stat=0;stat<10;stat++) Assert.That(child.iv.Get(stat),Is.InRange(0,100));
            }
        }

        [Test]
        public void SaveNormalizationPreservesOldAndNewValidIndividualValues()
        {
            var record=new UnitSaveData {id="iv-boundaries",individualValues=new[]{-1,0,31,32,99,100,101,int.MaxValue,int.MinValue,50}};
            var data=new GameSaveData {units=new List<UnitSaveData>{record}};
            GameSaveData restored=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
            typeof(SaveSystem).GetMethod("Normalize",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{restored});
            CollectionAssert.AreEqual(new[]{0,0,31,32,99,100,100,100,0,50},restored.units[0].individualValues);
        }

        [Test]
        public void HundredIndividualValuesContributeToStatsWithoutOldLimit()
        {
            var low=new UnitInstance {level=50,iv=new IndividualValues()};
            var high=new UnitInstance {level=50,iv=new IndividualValues()};
            for(int stat=0;stat<10;stat++) high.iv.Set(stat,100);
            UnitStats lowStats=UnitStatsCalculator.Calculate(new UnitStats(),low,null,null);
            UnitStats highStats=UnitStatsCalculator.Calculate(new UnitStats(),high,null,null);
            for(int stat=0;stat<10;stat++) Assert.AreEqual(stat==9?10f:50f,highStats.Get(stat)-lowStats.Get(stat),.001f);
            high.iv.hp=int.MaxValue;
            Assert.AreEqual(100,high.iv.Get(0),"Directly deserialized values must also be bounded when used for calculation.");
        }

        [Test]
        public void ExperienceDistributionIgnoresNullRosterEntries()
        {
            var first = new UnitInstance { level = 1 };
            var second = new UnitInstance { level = 1 };
            Assert.AreEqual(0, ExperienceSystem.Distribute(new[] { first, null, second }, 20));
            Assert.AreEqual(10, first.experience);
            Assert.AreEqual(10, second.experience);
        }

        

        [Test]
        public void ProjectileCollisionUsesTheWholeFrameSegment()
        {
            Assert.IsTrue(Spells.SpellGeometry.SegmentHitsPoint(new Vector2(-1f, 0f), new Vector2(1f, 0f), Vector2.zero, .1f));
            Assert.IsFalse(Spells.SpellGeometry.SegmentHitsPoint(new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), .1f));
        }

        [Test]
        public void ResistanceCalculationsUseSeparatePercentages()
        {
            SpeciesDefinition definition = species.species[0];
            definition.GetResistance(AttributeElement.Fire).damageReduction = .4f;
            definition.GetResistance(AttributeElement.Fire).applicationResistance = .1f;
            definition.GetResistance(DebuffType.Poison).damageReduction = .8f;
            definition.GetResistance(DebuffType.Poison).applicationResistance = .75f;
            Assert.AreEqual(60f, ResistanceCalculator.ApplyElementalDamage(definition, AttributeElement.Fire, 100f), .001f);
            Assert.AreEqual(.25f, ResistanceCalculator.ApplyDebuffChance(definition, DebuffType.Poison, 1f), .001f);
            Assert.AreEqual(100f, ResistanceCalculator.ApplyDefense(100f, 0f), .001f);
            Assert.AreEqual(50f, ResistanceCalculator.ApplyDefense(100f, 100f), .001f);
            Assert.AreEqual(0f, ResistanceCalculator.ApplyDefense(-10f, 100f), .001f);
        }

        [Test]
        public void GenomeCrossoverIsSeededAndMutationRemainsBounded()
        {
            var a=DemoDataFactory.DemoGenome(0);var b=DemoDataFactory.DemoGenome(4);
            var left=GeneticAlgorithm.Cross(a,b,SpeciesRank.C,rules,new System.Random(731));
            var right=GeneticAlgorithm.Cross(a,b,SpeciesRank.C,rules,new System.Random(731));
            Assert.AreEqual(JsonUtility.ToJson(left),JsonUtility.ToJson(right));
            string error;Assert.IsTrue(Spells.SpellValidator.Validate(left.programGenes.chromosomeA,out error),error);
            Assert.IsTrue(Spells.SpellValidator.Validate(left.programGenes.chromosomeB,out error),error);
        }

        [Test]
        public void FactoryBoundsMagicComplexityAndProjectileLimits()
        {
            var built=MagicFactory.Build(DemoDataFactory.DemoGenome(1));
            Assert.AreEqual(48,built.program.maxProjectiles);Assert.AreEqual(3,built.program.maxGeneration);
            Assert.AreEqual(128,built.program.maxEvents);Assert.LessOrEqual(built.duration,6);
            Assert.That(built.mpCost,Is.InRange(1.5,375));Assert.That(built.cooldown,Is.InRange(.1f,60));
            Assert.AreEqual(0,StatusApplication.Chance(1,1,1));
        }

        

        

        [Test]
        public void SegmentCollisionReportsTheFirstContactPoint()
        {
            float fraction;
            Assert.IsTrue(Spells.SpellGeometry.TrySegmentHit(Vector2.zero,new Vector2(10,0),new Vector2(4,0),1f,out fraction));
            Assert.AreEqual(.3f,fraction,.0001f);
            Assert.IsFalse(Spells.SpellGeometry.TrySegmentHit(Vector2.zero,Vector2.right,new Vector2(-2,0),.1f,out fraction));
            Vector2 direction=Spells.SpellGeometry.Rotate(Vector2.right,30f);
            Assert.AreEqual(30f,Vector2.SignedAngle(Vector2.right,direction),.001f);
        }

        

        [Test]
        public void InvalidGenomeValuesCannotProduceNonFiniteRuntimeParameters()
        {
            var genome=DemoDataFactory.DemoGenome(0);genome.programGenes.chromosomeA.basePower=float.NaN;
            Assert.Throws<InvalidOperationException>(()=>MagicFactory.Build(genome));
            Assert.IsFalse(MagicGenome.TryParseJson("{}",out _));
            Assert.IsFalse(MagicGenome.TryParseJson("{\"power\":20,\"spawn\":0}",out _));
            Assert.Throws<InvalidOperationException>(()=>MagicFactory.Build(new MagicGenome()));
        }

        

        [Test]
        public void MagicGenomeCloneIsDeepAndFactoryDoesNotMutateSource()
        {
            var genome=DemoDataFactory.DemoGenome(0);string before=JsonUtility.ToJson(genome);
            var clone=genome.Clone();clone.programGenes.chromosomeA.nodes[1].parameters.count=8;
            Assert.AreEqual(before,JsonUtility.ToJson(genome));MagicFactory.Build(genome);
            Assert.AreEqual(before,JsonUtility.ToJson(genome));
            Assert.IsFalse(MagicGenome.TryParseJson("{ broken",out _));
        }

        [Test]public void DatabaseLookupsReturnSafeDefaultsForMissingArraysAndEntries()
        {
            var empty=ScriptableObject.CreateInstance<SpeciesDatabase>();
            try{empty.species=null;Assert.IsNull(empty.GetById("missing"));}
            finally{UnityEngine.Object.DestroyImmediate(empty);}
        }
        [Test]public void BreedingProducesExistingSpeciesBoundedMagicAndStableSeededTraits()
        {
            var a=Parent(species.species[0],1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            var b=Parent(species.species[1],2,PersonalityType.Calm,DemoDataFactory.DemoGenome(1));
            var breeding=new BreedingSystem(species,rules,rankRule);var left=breeding.Breed(a,b,42);var right=breeding.Breed(a,b,42);
            Assert.IsNotNull(species.GetById(left.child.speciesId));Assert.AreEqual(left.child.speciesId,right.child.speciesId);
            Assert.AreEqual(JsonUtility.ToJson(left.child.iv),JsonUtility.ToJson(right.child.iv));
            Assert.AreEqual(a.id,left.child.lineage.fatherId);Assert.AreEqual(b.id,left.child.lineage.motherId);
            Assert.AreEqual(5,left.child.magicGeneSlots.Count);Assert.AreEqual(RankRules.MagicSlots(left.species.rank),left.child.magicGenomes.Count);
        }
        [Test]public void BreedingRejectsUsingTheSameIndividualAsBothParents()
        {
            var a=Parent(species.species[0],1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            Assert.Throws<ArgumentException>(()=>new BreedingSystem(species,rules,rankRule).Breed(a,a,1));
        }
        [TestCase(SpeciesRank.F,SpeciesRank.E)][TestCase(SpeciesRank.E,SpeciesRank.D)]
        [TestCase(SpeciesRank.D,SpeciesRank.C)][TestCase(SpeciesRank.C,SpeciesRank.B)]
        [TestCase(SpeciesRank.B,SpeciesRank.A)][TestCase(SpeciesRank.A,SpeciesRank.S)][TestCase(SpeciesRank.S,SpeciesRank.S)]
        public void DifferentSpeciesOfSameFamilyAndRankAlwaysPromote(SpeciesRank parentRank,SpeciesRank expected)
        {
            var pair=System.Array.FindAll(species.species,s=>s.family==SpeciesFamily.Slime && s.rank==parentRank);
            var a=Parent(pair[0],1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            var b=Parent(pair[1],2,PersonalityType.Calm,DemoDataFactory.DemoGenome(1));
            for(int seed=0;seed<8;seed++)Assert.AreEqual(expected,new BreedingSystem(species,rules,rankRule).Breed(a,b,seed).species.rank);
        }
        [Test]public void NonQualifyingPairsKeepExistingRankRule()
        {
            var first=System.Array.Find(species.species,s=>s.family==SpeciesFamily.Slime && s.rank==SpeciesRank.C);
            var a=Parent(first,1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            var b=Parent(first,2,PersonalityType.Calm,DemoDataFactory.DemoGenome(1));
            var breeding=new BreedingSystem(species,rules,rankRule);
            Assert.IsNotEmpty(breeding.PreviewCandidates(a,b,3,out _));
        }
        [Test]public void SameSpeciesAndDifferentRankSameFamilyAlwaysPromoteAboveHigherParent()
        {
            var first=System.Array.Find(species.species,s=>s.family==SpeciesFamily.Slime && s.rank==SpeciesRank.C);
            var higher=System.Array.Find(species.species,s=>s.family==SpeciesFamily.Slime && s.rank==SpeciesRank.A);
            var a=Parent(first,1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));
            var same=Parent(first,2,PersonalityType.Calm,DemoDataFactory.DemoGenome(1));
            var b=Parent(higher,3,PersonalityType.Timid,DemoDataFactory.DemoGenome(2));
            var breeding=new BreedingSystem(species,rules,rankRule);
            Assert.AreEqual(SpeciesRank.B,breeding.Breed(a,same,12).species.rank);
            Assert.AreEqual(SpeciesRank.S,breeding.Breed(a,b,12).species.rank);
        }
        [Test]public void LegacyDemotionSettingsNeverLowerRank()
        {
            rankRule.rankOffset=0;rankRule.promotionChance=0;rankRule.demotionChance=1;
            rules.rankBonusChance=0;rules.rankPenaltyChance=1;
            var fallback=typeof(BreedingSystem).GetMethod("ResolveAverageRank",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            for(int seed=0;seed<20;seed++)
            {
                Assert.AreEqual(SpeciesRank.C,rankRule.Resolve(SpeciesRank.C,SpeciesRank.C,new System.Random(seed)));
                Assert.AreEqual(SpeciesRank.C,fallback.Invoke(null,new object[]{SpeciesRank.C,SpeciesRank.C,new System.Random(seed),rules}));
            }
        }
        [Test]public void GuaranteedPromotionRejectsMissingHigherRankInsteadOfReturningLowerSpecies()
        {
            var pair=System.Array.FindAll(species.species,s=>s.family==SpeciesFamily.Slime && s.rank==SpeciesRank.F);
            var a=Parent(pair[0],1,PersonalityType.Brave,DemoDataFactory.DemoGenome(0));var b=Parent(pair[1],2,PersonalityType.Calm,DemoDataFactory.DemoGenome(1));
            var all=species.species;
            try{species.species=pair;var breeding=new BreedingSystem(species,rules,rankRule);SpeciesRank predicted;Assert.IsEmpty(breeding.PreviewCandidates(a,b,1,out predicted));Assert.AreEqual(SpeciesRank.E,predicted);Assert.Throws<InvalidOperationException>(()=>breeding.Breed(a,b,1));}
            finally{species.species=all;}
        }
        [Test]public void SaveJsonRestoresGenomeAndLineage()
        {
            var genome=DemoDataFactory.DemoGenome(3);genome.programGenes.history.Add("test mutation");
            var record=new UnitSaveData{id="offspring",fatherId="father-id",motherId="mother-id",generation=2,magicGenomeJson=new[]{JsonUtility.ToJson(genome)}};
            var restored=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(record));MagicGenome loaded;
            Assert.IsTrue(MagicGenome.TryParseJson(restored.magicGenomeJson[0],out loaded));Assert.AreEqual(JsonUtility.ToJson(genome),JsonUtility.ToJson(loaded));
            Assert.AreEqual("father-id",restored.fatherId);Assert.AreEqual("mother-id",restored.motherId);Assert.AreEqual(2,restored.generation);
        }
        [Test]public void SaveSystemRejectsNewerSchemaWithoutWritingOverIt()
        {
            Assert.IsFalse(SaveSystem.Save(new GameSaveData{schemaVersion=SaveSystem.CurrentSchemaVersion+1},out string error));StringAssert.Contains("newer game version",error);
        }
        private static UnitInstance Parent(SpeciesDefinition definition,int seed,PersonalityType personality,MagicGenome genome)
        {return new UnitInstance{id="parent-"+seed,speciesId=definition.speciesId,personality=personality,iv=IndividualValues.Random(new System.Random(seed)),magicGenomes=new List<MagicGenome>{genome}};}
    }
}
