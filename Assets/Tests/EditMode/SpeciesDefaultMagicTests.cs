using System;
using System.Collections.Generic;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpeciesDefaultMagicTests
    {
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
        [Test]public void All420SpeciesHaveFivePersistedValidDefaultsAndAffordableSpells()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.AreEqual(420,db.species.Length);
            var signatures=new HashSet<string>();var forms=new HashSet<SpellNodeKind>();var elements=new HashSet<AttributeElement>();
            foreach(var species in db.species)
            {
                Assert.AreEqual(5,species.defaultMagicGenomes.Length,species.displayName);
                Assert.IsTrue(signatures.Add(JsonUtility.ToJson(species.defaultMagicGenomes[0].programGenes)),species.speciesId);
                var unit=new UnitInstance{speciesId=species.speciesId,level=1,iv=new IndividualValues()};
                float minimumMp=UnitStatsCalculator.Calculate(species,unit).mp*.9f;
                foreach(var genome in species.defaultMagicGenomes)
                {
                    Assert.IsNotNull(genome);string error;
                    Assert.IsTrue(SpellValidator.Validate(genome.programGenes.chromosomeA,out error),species.displayName+": "+error);
                    Assert.IsTrue(SpellValidator.Validate(genome.programGenes.chromosomeB,out error),error);
                    var built=MagicFactory.Build(genome,species);Assert.IsNotNull(built.program);
                    Assert.LessOrEqual(built.mpCost,minimumMp,species.displayName+": default spell cannot be afforded at minimum IV.");
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
        [Test]public void CatalogCreationIsDeterministicAndMatchesPersistedDefaults()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            foreach(var species in db.species)
                Assert.AreEqual(JsonUtility.ToJson(species.defaultMagicGenomes[0]),JsonUtility.ToJson(SpeciesDefaultMagicCatalog.Create(species,0)),species.displayName);
        }
    }
}
