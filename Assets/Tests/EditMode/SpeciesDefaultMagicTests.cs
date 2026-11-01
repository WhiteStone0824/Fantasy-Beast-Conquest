using System;
using System.Collections.Generic;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpeciesDefaultMagicTests
    {
        [Test]public void BredHealingTargetsAlliesWithoutModifyingOriginalGenes()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0];
            var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.NearestEnemy);
            var original=SpellSlotStorage.Prepare(new MagicGenome{programGenes=genes},0);string before=JsonUtility.ToJson(original);
            var child=GeneticAlgorithm.ImproveOffspring(original,species,0);var magic=MagicFactory.Build(child,species);
            Assert.IsTrue(magic.targetsAllies);Assert.AreEqual(SpellTarget.LowestHealthAlly,magic.program.instructions[1].parameters.target);
            Assert.AreEqual(before,JsonUtility.ToJson(original));
        }
        [Test]public void BredSpellWithoutAnEffectFallsBackToChildSpeciesDefault()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0];
            var p=new SpellProgram();var root=SpellProgramBuilder.Add(p,SpellNodeKind.PeriodicTrigger,SpellContext.Cast);p.root=root.id;p.regulation.splice=false;
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=new SpellGenotype{chromosomeA=p,chromosomeB=p.Clone()}},0);
            var child=GeneticAlgorithm.ImproveOffspring(genome,species,0);
            Assert.AreEqual(JsonUtility.ToJson(SpeciesDefaultMagicCatalog.Copy(species,0).programGenes.chromosomeA),JsonUtility.ToJson(child.programGenes.chromosomeA));
            Assert.IsTrue(child.programGenes.history.Exists(x=>x.Contains("有効な効果がない")));
        }
        [Test]public void FamilyStagesMatchBreedingTable()
        {
            var rules=Resources.Load<BreedingRuleDatabase>("BreedingRuleDatabase");Assert.NotNull(rules);
            var stages=new int[30];foreach(var f in new[]{SpeciesFamily.Slime,SpeciesFamily.Beast,SpeciesFamily.Bird,SpeciesFamily.Insect,SpeciesFamily.Plant,SpeciesFamily.Material})stages[(int)f]=1;
            for(int stage=2;stage<=30;stage++)
            {
                var next=(int[])stages.Clone();
                for(int a=0;a<30;a++)for(int b=a;b<30;b++)if(stages[a]>0 && stages[a]<stage && stages[b]>0 && stages[b]<stage)
                {int result=(int)rules.ResolveFamily((SpeciesFamily)a,(SpeciesFamily)b);if(result<30 && next[result]==0)next[result]=stage;}
                stages=next;
            }
            for(int i=0;i<30;i++){Assert.Greater(stages[i],0);Assert.AreEqual(stages[i],SpeciesBaseStatsCatalog.FamilyStage((SpeciesFamily)i),((SpeciesFamily)i).ToString());}
        }
        [TestCase("species_F08_R0_A",false)]
        [TestCase("species_F08_R0_B",false)]
        [TestCase("species_F08_R1_A",true)]
        [TestCase("species_F08_R1_B",false)]
        public void OnlyFlowerCapHasLowRankPlantHealingDefault(string id,bool heals)
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById(id);Assert.NotNull(species);
            foreach(var genome in new[]{SpeciesDefaultMagicCatalog.Copy(species,0),SpeciesDefaultMagicCatalog.Create(species,0)})
            {
                var magic=MagicFactory.Build(genome,species);
                Assert.AreEqual(heals,magic.targetsAllies);
                Assert.AreEqual(heals,System.Array.Exists(magic.program.instructions,n=>n.kind==SpellNodeKind.Heal));
            }
        }
        [Test]public void SpeciesDefaultMutationUsesChildSpeciesAndDoesNotModifyInheritedGenome()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById("species_F19_R0_A");Assert.NotNull(species);
            var inherited=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(AttributeElement.Fire,40)},1);
            string before=JsonUtility.ToJson(inherited);
            Assert.AreSame(inherited,GeneticAlgorithm.MutateToSpeciesDefault(inherited,species,0,new System.Random(10),0));
            var mutated=GeneticAlgorithm.MutateToSpeciesDefault(inherited,species,0,new System.Random(10),1);
            var expected=SpeciesDefaultMagicCatalog.Copy(species,0);
            Assert.AreEqual(JsonUtility.ToJson(expected.programGenes.chromosomeA),JsonUtility.ToJson(mutated.programGenes.chromosomeA));
            Assert.AreEqual(JsonUtility.ToJson(expected.programGenes.chromosomeB),JsonUtility.ToJson(mutated.programGenes.chromosomeB));
            Assert.IsTrue(mutated.programGenes.history.Exists(x=>x.Contains("標準魔法")));
            Assert.AreEqual(before,JsonUtility.ToJson(inherited));
        }
        [Test]public void All420SpeciesHaveFivePersistedValidDefaults()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.AreEqual(420,db.species.Length);
            var signatures=new HashSet<string>();var forms=new HashSet<SpellNodeKind>();var elements=new HashSet<AttributeElement>();
            foreach(var species in db.species)
            {
                Assert.AreEqual(5,species.defaultMagicGenomes.Length,species.displayName);
                Assert.IsTrue(signatures.Add(JsonUtility.ToJson(species.defaultMagicGenomes[0].programGenes)),species.speciesId);
                foreach(var genome in species.defaultMagicGenomes)
                {
                    Assert.IsNotNull(genome);string error;
                    Assert.IsTrue(SpellValidator.Validate(genome.programGenes.chromosomeA,out error),species.displayName+": "+error);
                    Assert.IsTrue(SpellValidator.Validate(genome.programGenes.chromosomeB,out error),error);
                    var built=MagicFactory.Build(genome,species);Assert.IsNotNull(built.program);
                    Assert.Greater(built.mpCost,0,species.displayName+": default spell must have a positive MP cost.");
                    foreach(var node in built.program.instructions){forms.Add(node.kind);elements.Add(node.parameters.element);}
                }
            }
            foreach(var kind in new[]{SpellNodeKind.Damage,SpellNodeKind.ApplyStatus,SpellNodeKind.Heal,SpellNodeKind.ApplyBuff,SpellNodeKind.Explosion,SpellNodeKind.SplitProjectile})Assert.Contains(kind,new List<SpellNodeKind>(forms));
            Assert.AreEqual(7,elements.Count);
        }
        [Test]public void SameSpeciesDifferentIndividualsKeepIdenticalDefaultGenes()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0];
            var a=WildUnitFactory.Create(species,1,1,"wild-a");var b=WildUnitFactory.Create(species,8,2,"wild-b");
            CollectionAssert.AreEqual(a.magicSlotGenomeJson,b.magicSlotGenomeJson);
            CollectionAssert.AreNotEqual(a.individualValues,b.individualValues);
            for(int i=0;i<5;i++)Assert.AreEqual(JsonUtility.ToJson(species.defaultMagicGenomes[i]),a.magicSlotGenomeJson[i]);
        }
        [Test]public void EditingIndividualGenesCannotMutateSpeciesDefaults()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0];
            string before=JsonUtility.ToJson(species.defaultMagicGenomes[0]);
            var copy=SpeciesDefaultMagicCatalog.Copy(species,0);copy.programGenes.chromosomeA.basePower=300;
            Assert.AreEqual(before,JsonUtility.ToJson(species.defaultMagicGenomes[0]));
        }
        [Test]public void CatalogCreationIsDeterministic()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            foreach(var species in db.species)
                Assert.AreEqual(JsonUtility.ToJson(SpeciesDefaultMagicCatalog.Create(species,0)),JsonUtility.ToJson(SpeciesDefaultMagicCatalog.Create(species,0)),species.displayName);
        }
    }
}
