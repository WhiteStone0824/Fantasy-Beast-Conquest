using System.Collections.Generic;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpeciesPassiveTests
    {
        [Test] public void TruthSpeciesHasFiftyPercentResistancesAndItsRequestedPassive()
        {
            var species=Resources.Load<SpeciesDefinition>("SpecialSpecies");Assert.NotNull(species);
            Assert.AreEqual("真理の御魂",species.passive.displayName);
            Assert.AreEqual(7,species.passive.effects.Length);
            Assert.AreEqual(JsonUtility.ToJson(SpeciesPassiveCatalog.Create(species)),JsonUtility.ToJson(species.passive));
            for(int i=0;i<7;i++)
            {
                var resistance=species.GetResistance((AttributeElement)i);
                Assert.AreEqual(.5f,resistance.damageReduction,.0001f);Assert.AreEqual(.5f,resistance.applicationResistance,.0001f);
                Assert.AreEqual(.5f,SpeciesPassiveCalculator.ElementReduction(species,(AttributeElement)i),.0001f);
            }
            for(int i=0;i<8;i++)Assert.AreEqual(.5f,SpeciesPassiveCalculator.DebuffReduction(species,(DebuffType)i),.0001f);
            var stats=new UnitStats();var baseline=stats.Copy();SpeciesPassiveCalculator.ApplyStats(species.passive,stats);
            for(int i=0;i<10;i++)Assert.AreEqual(baseline.Get(i)*1.2f,stats.Get(i),.0001f);
            Assert.AreEqual(.05f,SpeciesPassiveCalculator.Sum(species.passive,PassiveEffectType.OnHitInstantDeath),.0001f);
            Assert.AreEqual(0f,SpeciesPassiveCalculator.Sum(species.passive,PassiveEffectType.DurationBonus),.0001f);
            Assert.AreEqual(1f,SpeciesPassiveCalculator.Sum(species.passive,PassiveEffectType.ReviveAlliesOnDeath),.0001f);
            StringAssert.Contains("復活",species.passive.Description);StringAssert.Contains("毒",species.passive.Description);
        }
        [TestCase("species_F11_R2_B", "\u96f7\u9cf4\u306e\u5de8\u5175", SpeciesRank.D, "\u96f7\u9cf4\u306e\u596e\u8d77", "species_F11_R2_B")]
        [TestCase("species_F11_R4_A", "\u6eb6\u5ca9\u30bf\u30a4\u30bf\u30f3", SpeciesRank.B, "\u6eb6\u5ca9\u306e\u77f3\u93a7", "species_F11_R4_A")]
        [TestCase("species_F21_R4_B", "\u6681\u5149\u306e\u5927\u5929\u4f7f", SpeciesRank.B, "\u795d\u7ffc\u306e\u6b69\u6cd5", "species_F21_R4_B")]
        [TestCase("species_F21_R5_A", "\u767d\u9280\u306e\u71be\u5929\u4f7f", SpeciesRank.A, "\u8056\u706b\u306e\u5438\u5149", "species_F21_R5_A")]
        public void ReassignedSpeciesKeepTheirNamesPassivesAndArtworkTogether(string id,string name,SpeciesRank rank,string passive,string spriteSource)
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById(id);
            Assert.AreEqual(name,species.displayName);Assert.AreEqual(rank,species.rank);
            Assert.AreEqual(passive,species.passive.displayName);
            Assert.AreEqual(JsonUtility.ToJson(SpeciesPassiveCatalog.Create(species)),JsonUtility.ToJson(species.passive));
            Assert.AreEqual(spriteSource,DemoDataFactory.SpriteSourceId(id));
            string actual=UnityEditor.AssetDatabase.GetAssetPath(species.sprite);
            Assert.AreEqual("Assets/Game/Sprites/Generated/"+spriteSource+".png",actual);
        }
        [Test]
        public void All420MasterPassivesAreUniqueCompleteAndMatchRuntimeFallback()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.AreEqual(420,db.species.Length);
            var ids=new HashSet<string>();var names=new HashSet<string>();
            foreach(SpeciesDefinition species in db.species)
            {
                SpeciesPassive passive=species.passive;Assert.IsNotNull(passive,species.displayName);
                Assert.IsTrue(ids.Add(passive.id));Assert.IsTrue(names.Add(passive.displayName));
                int expected=species.rank==SpeciesRank.S?7:species.rank==SpeciesRank.A?5:species.rank==SpeciesRank.B?4:3;
                Assert.AreEqual(expected,passive.effects.Length,species.displayName);
                Assert.AreEqual(JsonUtility.ToJson(SpeciesPassiveCatalog.Create(species)),JsonUtility.ToJson(passive));
                foreach(PassiveEffect effect in passive.effects){Assert.IsTrue(System.Enum.IsDefined(typeof(PassiveEffectType),effect.type));Assert.Greater(effect.amount,0);Assert.LessOrEqual(effect.amount,1);}
                Assert.IsFalse(string.IsNullOrWhiteSpace(passive.Description));
            }
        }
        [Test]
        public void SlimeUsesRequestedWaterBonusesWithoutMutatingMasterOrGenome()
        {
            SpeciesDefinition slime=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById("demo_00");
            Assert.AreEqual("\u6c34\u306e\u7533\u3057\u5b50",slime.passive.displayName);
            string master=JsonUtility.ToJson(slime);
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(AttributeElement.Water)},0);string genes=JsonUtility.ToJson(genome);
            BuiltMagic baseline=MagicFactory.Build(genome),water=MagicFactory.Build(genome,slime);
            Assert.AreEqual(baseline.mpCost*.8f,water.mpCost,.0001f);
            Assert.AreEqual(water.mpCost,MagicFactory.Build(genome,slime).mpCost,.0001f);
            Assert.AreEqual(1.2f,SpeciesPassiveCalculator.DamageMultiplier(slime.passive,AttributeElement.Water,1),.0001f);
            Assert.AreEqual(1,SpeciesPassiveCalculator.DamageMultiplier(slime.passive,AttributeElement.Fire,1));
            Assert.AreEqual(Mathf.Clamp01(slime.GetResistance(AttributeElement.Water).damageReduction+.5f),SpeciesPassiveCalculator.ElementReduction(slime,AttributeElement.Water),.0001f);
            foreach(var tree in new[]{genome.programGenes.chromosomeA,genome.programGenes.chromosomeB})foreach(var node in tree.nodes)node.parameters.element=AttributeElement.Fire;
            Assert.AreEqual(MagicFactory.Build(genome).mpCost,MagicFactory.Build(genome,slime).mpCost,.0001f);
            Assert.AreEqual(master,JsonUtility.ToJson(slime));
            genome=JsonUtility.FromJson<MagicGenome>(genes);Assert.AreEqual(genes,JsonUtility.ToJson(genome));
        }
        [Test]
        public void PercentDescriptionsDoNotRoundFloatingPointNoiseUpToWrongBonuses()
        {
            StringAssert.Contains("60%",SpeciesPassiveCalculator.Describe(new PassiveEffect {type=PassiveEffectType.DebuffResistance,amount=.6f}));
            StringAssert.Contains("15%",SpeciesPassiveCalculator.Describe(new PassiveEffect {type=PassiveEffectType.StatBonus,amount=.15f}));
            StringAssert.Contains("16%",SpeciesPassiveCalculator.Describe(new PassiveEffect {type=PassiveEffectType.StatBonus,amount=.155f}));
        }
        [Test]
        public void PassiveStatsAreAppliedOnceAndResistancesStayInRange()
        {
            var species=ScriptableObject.CreateInstance<SpeciesDefinition>();
            try
            {
                species.passive=new SpeciesPassive {effects=new[]{new PassiveEffect {type=PassiveEffectType.StatBonus,statIndex=0,amount=.15f},new PassiveEffect {type=PassiveEffectType.ElementResistance,element=AttributeElement.Fire,amount=1},new PassiveEffect {type=PassiveEffectType.DebuffResistance,debuff=DebuffType.Poison,amount=1}}};
                var unit=new UnitInstance {level=30,iv=new IndividualValues()};string original=JsonUtility.ToJson(species.baseStats);
                UnitStats baseline=UnitStatsCalculator.Calculate(species.baseStats,unit,null,null),first=UnitStatsCalculator.Calculate(species,unit),second=UnitStatsCalculator.Calculate(species,unit);
                Assert.AreEqual(baseline.hp*1.15f,first.hp,.001f);Assert.AreEqual(first.hp,second.hp);Assert.AreEqual(original,JsonUtility.ToJson(species.baseStats));
                Assert.AreEqual(1,SpeciesPassiveCalculator.ElementReduction(species,AttributeElement.Fire));Assert.AreEqual(1,SpeciesPassiveCalculator.DebuffReduction(species,DebuffType.Poison));
            }
            finally {Object.DestroyImmediate(species);}
        }
        [Test]
        public void ChildGetsItsNewSpeciesPassiveRatherThanEitherParentsTrait()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");var pair=db.FindCandidates(SpeciesFamily.Slime,SpeciesRank.F);
            UnitInstance a=new UnitInstance {id="passive-parent-a",speciesId=pair[0].speciesId,iv=new IndividualValues()},b=new UnitInstance {id="passive-parent-b",speciesId=pair[1].speciesId,iv=new IndividualValues()};
            BreedingResult child=new BreedingSystem(db,null,null).Breed(a,b,91);
            Assert.AreEqual(SpeciesRank.E,child.species.rank);Assert.AreEqual("passive_"+child.child.speciesId,child.species.passive.id);
            Assert.AreNotEqual(pair[0].passive.id,child.species.passive.id);Assert.AreNotEqual(pair[1].passive.id,child.species.passive.id);
        }
        [Test]
        public void InnateCooldownAndDurationBonusesRespectRuntimeSafetyLimits()
        {
            var species=ScriptableObject.CreateInstance<SpeciesDefinition>();
            try
            {
                species.passive=new SpeciesPassive {effects=new[]{new PassiveEffect {type=PassiveEffectType.CooldownReduction,amount=.1f},new PassiveEffect {type=PassiveEffectType.DurationBonus,amount=.2f}}};
                var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile()},0);string original=JsonUtility.ToJson(genome);
                BuiltMagic baseline=MagicFactory.Build(genome),modified=MagicFactory.Build(genome,species);
                Assert.AreEqual(baseline.cooldown*.9f,modified.cooldown,.001f);Assert.LessOrEqual(modified.duration,6);Assert.AreEqual(baseline.program.instructions[1].parameters.seconds*1.2f,modified.program.instructions[1].parameters.seconds,.001f);
                Assert.AreEqual(baseline.mpCost,modified.mpCost);Assert.AreEqual(original,JsonUtility.ToJson(genome));
            }
            finally {Object.DestroyImmediate(species);}
        }
    }
}
