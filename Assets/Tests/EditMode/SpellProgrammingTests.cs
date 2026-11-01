using System;
using System.Collections.Generic;
using FusionAnyMonster.Spells;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;
namespace FusionAnyMonster.Tests
{
    public sealed class SpellProgrammingTests
    {
        [Test]public void MagicManaCostIsOneAndAHalfTimesThePreviousCost()
        {
            var spell=SpellCompiler.Compile(SpellProgramBuilder.Projectile());
            float previous=Mathf.Clamp((spell.complexity*.8f+spell.basePower*.025f)*2f,1f,250f);
            Assert.AreEqual(previous*1.5f,spell.mpCost,.0001f);
        }
        [Test]public void SavedGenomeContainsOnlyProgramData()
        {
            string json=JsonUtility.ToJson(DemoDataFactory.DemoGenome(3));
            foreach(string field in new[]{"activation","spawn","trajectory","effects","power","projectileSpeed","projectileCount","pierce","buffStrength","mutationHistory","inheritedGenes"})
                Assert.IsNull(typeof(MagicGenome).GetField(field),field);
            MagicGenome restored;Assert.IsTrue(MagicGenome.TryParseJson(json,out restored));
            Assert.IsNotNull(restored.programGenes.chromosomeA);Assert.IsNotNull(restored.programGenes.chromosomeB);
        }
        [Test]public void OldFormatSaveIsProtectedAndCannotBeWritten()
        {
            string path=SaveSystem.SavePath;byte[] original=System.IO.File.Exists(path)?System.IO.File.ReadAllBytes(path):null;
            try
            {
                var data=new GameSaveData{units=new List<UnitSaveData>{new UnitSaveData{id="old-format",magicGenomeJson=new[]{"{\"power\":20,\"effects\":[0]}"}}}};
                string json=JsonUtility.ToJson(data);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllText(path,json);
                var loaded=SaveSystem.Load();Assert.IsTrue(loaded.readOnly);StringAssert.Contains("GP形式",loaded.readOnlyReason);
                Assert.IsFalse(SaveSystem.Save(loaded,out _));Assert.IsFalse(SaveSystem.Save(data,out _));Assert.AreEqual(json,System.IO.File.ReadAllText(path));
            }
            finally{if(original!=null)System.IO.File.WriteAllBytes(path,original);else if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
        }
        [TestCase(SpeciesRank.F,1)][TestCase(SpeciesRank.E,1)][TestCase(SpeciesRank.D,2)][TestCase(SpeciesRank.C,2)]
        [TestCase(SpeciesRank.B,3)][TestCase(SpeciesRank.A,4)][TestCase(SpeciesRank.S,5)]
        public void EveryUsableSlotUsesTheCorrespondingSavedProgram(SpeciesRank rank,int activeCount)
        {
            var unit=new UnitSaveData{id="all-slots",rank=rank.ToString(),magicGenomeJson=new[]{JsonUtility.ToJson(DemoDataFactory.DemoGenome(0))}};
            SpellSlotStorage.EnsureSavedSlots(unit);Assert.AreEqual(5,unit.magicSlotGenomeJson.Length);Assert.AreEqual(activeCount,unit.magicGenomeJson.Length);
            for(int i=0;i<activeCount;i++)Assert.AreEqual(unit.magicSlotGenomeJson[i],unit.magicGenomeJson[i]);
            string before=JsonUtility.ToJson(unit);SpellSlotStorage.EnsureSavedSlots(unit);Assert.AreEqual(before,JsonUtility.ToJson(unit));
        }
        [Test]public void UnsupportedGenotypeVersionIsRejectedByJsonParser()
        {
            var genome=new MagicGenome{programGenes=SpellExamples.Create(0),programVersion=1};genome.programGenes.version=2;
            MagicGenome result;Assert.IsFalse(MagicGenome.TryParseJson(JsonUtility.ToJson(genome),out result));
        }
        [Test]public void InvalidDormantSlotCannotBeSilentlyReplacedDuringInheritance()
        {
            var unit=new UnitInstance{id="broken-dormant",magicGeneSlots=new List<MagicGenome>{new MagicGenome{programGenes=SpellExamples.Create(0)},null,new MagicGenome{programGenes=SpellExamples.Create(1)}}};
            Assert.Throws<InvalidOperationException>(()=>SpellSlotStorage.Read(unit));
        }
        [Test]public void SavedSlotParsingPreservesBrokenSlotPosition()
        {
            string first=JsonUtility.ToJson(new MagicGenome{programGenes=SpellExamples.Create(0)}),third=JsonUtility.ToJson(new MagicGenome{programGenes=SpellExamples.Create(1)});
            var slots=SpellSlotStorage.ParseSavedSlots(new[]{first,"broken json",third});Assert.AreEqual(3,slots.Count);Assert.IsNull(slots[1]);Assert.IsNotNull(slots[2]);
            Assert.AreEqual(JsonUtility.ToJson(SpellExamples.Create(1)),JsonUtility.ToJson(slots[2].programGenes));
        }
        [Test]public void BrokenActiveGenomeIsNotReplacedByRandomDormantGenes()
        {
            var unit=new UnitSaveData{id="broken-active",magicGenomeJson=new[]{"broken json",JsonUtility.ToJson(new MagicGenome{programGenes=SpellExamples.Create(1)})}};
            SpellSlotStorage.EnsureSavedSlots(unit);Assert.AreEqual("broken json",unit.magicSlotGenomeJson[0]);
            var genes=SpellSlotStorage.ForBattle(new UnitInstance{id=unit.id},new string[0],unit.magicGenomeJson);
            Assert.AreEqual(5,genes.Count);Assert.IsNull(genes[0]);Assert.IsNotNull(genes[1]);
        }
        [Test]public void CorruptSaveIsReadOnlyAndCannotBeOverwrittenAtStartup()
        {
            byte[] original=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SaveSystem.SavePath));System.IO.File.WriteAllText(SaveSystem.SavePath,"broken json");
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("Failed to load save data"));
                var data=SaveSystem.Load();Assert.IsTrue(data.readOnly);Assert.IsFalse(SaveSystem.Save(data,out _));Assert.AreEqual("broken json",System.IO.File.ReadAllText(SaveSystem.SavePath));
            }
            finally{if(original!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,original);else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);}
        }
        [Test]public void OversizedSpliceRollsBackWithoutThrowingAtMaximumSettings()
        {
            var program=new SpellProgram();var root=SpellProgramBuilder.Add(program,SpellNodeKind.PeriodicTrigger,SpellContext.Cast);int branch=FullCastTree(program,2);root.edges.Add(new SpellEdge(SpellPort.Next,branch));
            program.regulation.dominance=1;program.regulation.recipientIndex=program.nodes.Count-2;program.regulation.donorIndex=0;
            var genes=new SpellGenotype{expressionMaxNodes=128,expressionMaxDepth=6,chromosomeA=program,chromosomeB=program.Clone()};string report;
            var phenotype=SpellGenetics.Express(genes,out report);StringAssert.Contains("取消",report);Assert.AreEqual(JsonUtility.ToJson(program),JsonUtility.ToJson(phenotype));
        }
        private static int FullCastTree(SpellProgram program,int depth)
        {
            var node=SpellProgramBuilder.Add(program,depth==6?SpellNodeKind.Delay:SpellNodeKind.Sequence,SpellContext.Cast);
            if(depth<6)foreach(var port in new[]{SpellPort.Next,SpellPort.True,SpellPort.False})node.edges.Add(new SpellEdge(port,FullCastTree(program,depth+1)));
            return node.id;
        }
        [Test]public void ExamplesCompileDeterministicallyWithoutChangingChromosomes()
        {
            for(int i=0;i<3;i++)
            {
                var genes=SpellExamples.Create(i);string original=JsonUtility.ToJson(genes),report;
                var one=SpellGenetics.Express(genes,out report);var two=SpellGenetics.Express(genes,out report);
                Assert.AreEqual(JsonUtility.ToJson(one),JsonUtility.ToJson(two));Assert.AreEqual(original,JsonUtility.ToJson(genes));
                var spell=SpellCompiler.Compile(genes);Assert.AreEqual(48,spell.maxProjectiles);Assert.AreEqual(128,spell.maxEvents);Assert.AreEqual(6,spell.maxLifetime);
                Assert.IsNotEmpty(spell.name);Assert.IsTrue(SpellValidator.Validate(one,out report));
            }
        }
        [Test]public void ValidatorRejectsCyclesSharedNodesAndWrongEventTypes()
        {
            string error;var p=SpellExamples.Create(0).chromosomeA;p.nodes[2].edges.Add(new SpellEdge(SpellPort.OnKill,p.root));Assert.IsFalse(SpellValidator.Validate(p,out error));
            p=SpellExamples.Create(0).chromosomeA;p.nodes[1].edges.Add(new SpellEdge(SpellPort.OnExpire,2));Assert.IsFalse(SpellValidator.Validate(p,out error));
            p=SpellExamples.Create(0).chromosomeA;p.nodes[2].edges[0].child=2;Assert.IsFalse(SpellValidator.Validate(p,out error));
            p=SpellExamples.Create(0).chromosomeA;p.nodes[2].parameters.speed=float.NaN;Assert.IsFalse(SpellValidator.Validate(p,out error));
        }
        [Test]public void CrossAndAllMutationModesRemainValidAndPreserveParentsAcrossSeeds()
        {
            var a=SpellProgramBuilder.Random(11);var b=SpellProgramBuilder.Random(17);string originalA=JsonUtility.ToJson(a),originalB=JsonUtility.ToJson(b);
            var settings=ScriptableObject.CreateInstance<SpellSettings>();settings.parameterMutationChance=settings.nodeMutationChance=settings.structureMutationChance=settings.crossoverChance=1;
            try
            {
                var shapes=new HashSet<string>();
                for(int seed=0;seed<100;seed++)
                {
                    var child=SpellGenetics.Cross(a,b,seed,settings);string error;
                    Assert.IsTrue(SpellValidator.Validate(child.chromosomeA,out error,settings),error);Assert.IsTrue(SpellValidator.Validate(child.chromosomeB,out error,settings),error);
                    var phenotype=SpellGenetics.Express(child,out error,settings);Assert.IsTrue(SpellValidator.Validate(phenotype,out error,settings),error);
                    Assert.AreEqual(JsonUtility.ToJson(child),JsonUtility.ToJson(SpellGenetics.Cross(a,b,seed,settings)));
                    shapes.Add(JsonUtility.ToJson(phenotype));
                }
                Assert.Greater(shapes.Count,10);Assert.AreEqual(originalA,JsonUtility.ToJson(a));Assert.AreEqual(originalB,JsonUtility.ToJson(b));
            }
            finally{UnityEngine.Object.DestroyImmediate(settings);}
        }
        [Test]public void DominanceRetainsRecessiveProgramAndInvalidSpliceFallsBack()
        {
            var g=SpellExamples.Create(0);g.chromosomeB=SpellExamples.Create(1).chromosomeB;g.chromosomeA.regulation.dominance=1;g.chromosomeB.regulation.dominance=0;
            g.chromosomeA.regulation.splice=false;string report;
            var result=SpellGenetics.Express(g,out report);Assert.AreEqual(JsonUtility.ToJson(g.chromosomeA),JsonUtility.ToJson(result));
            var saved=JsonUtility.FromJson<SpellGenotype>(JsonUtility.ToJson(g));Assert.AreEqual(JsonUtility.ToJson(g.chromosomeB),JsonUtility.ToJson(saved.chromosomeB));
            var delay=SpellProgramBuilder.Add(g.chromosomeA,SpellNodeKind.Delay,SpellContext.Hit);g.chromosomeA.nodes[3].edges.Add(new SpellEdge(SpellPort.Next,delay.id));
            var deep=SpellProgramBuilder.Add(g.chromosomeA,SpellNodeKind.Damage,SpellContext.Hit);delay.edges.Add(new SpellEdge(SpellPort.Next,deep.id));
            g.chromosomeA.regulation.splice=true;g.chromosomeA.regulation.recipientIndex=4;g.chromosomeA.regulation.donorIndex=1;
            var expression=SpellGenetics.Express(g,out report);StringAssert.Contains("取消",report);Assert.AreEqual(JsonUtility.ToJson(g.chromosomeA),JsonUtility.ToJson(expression));
            var changedSettings=ScriptableObject.CreateInstance<SpellSettings>();changedSettings.maxDepth=12;
            try{Assert.AreEqual(JsonUtility.ToJson(expression),JsonUtility.ToJson(SpellGenetics.Express(g,out report,changedSettings)),"Saved expression constraints must keep the same genotype deterministic after setting changes.");}finally{UnityEngine.Object.DestroyImmediate(changedSettings);}
        }
        [Test]public void FiveSavedSlotsSurviveRankLocksAndGeneJsonRoundTrip()
        {
            var unit=new UnitSaveData{id="locked",rank="F",magicGenomeJson=new[]{JsonUtility.ToJson(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly)})}};
            SpellSlotStorage.EnsureSavedSlots(unit);Assert.AreEqual(5,unit.magicSlotGenomeJson.Length);Assert.AreEqual(1,unit.magicGenomeJson.Length);
            var restored=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(unit));
            for(int i=0;i<5;i++){MagicGenome genes;Assert.IsTrue(MagicGenome.TryParseJson(restored.magicSlotGenomeJson[i],out genes));Assert.IsNotNull(genes.programGenes.chromosomeB);string error;Assert.IsTrue(SpellValidator.Validate(genes.expressedProgram,out error),error);}
        }
        [Test]public void SessionBudgetsBoundTotalGenerationAndEvents()
        {
            var session=new CastSession{spell=SpellCompiler.Compile(SpellExamples.Create(1))};
            for(int i=0;i<48;i++)Assert.IsTrue(session.TryProjectile(1));Assert.IsFalse(session.TryProjectile(1));Assert.IsFalse(session.TryProjectile(4));
            for(int i=0;i<128;i++)Assert.IsTrue(session.TryEvent());Assert.IsFalse(session.TryEvent());
        }
        [Test]public void BreedingKeepsCorrespondingFiveDiploidSlotsWithoutChangingParents()
        {
            var db=DemoDataFactory.CreateSpeciesDatabase();var rules=ScriptableObject.CreateInstance<BreedingRuleDatabase>();var rank=ScriptableObject.CreateInstance<RankBreedingRule>();
            try
            {
                var a=UnitInstance.Create(db.species[0].speciesId,PersonalityType.Brave,1);var b=UnitInstance.Create(db.species[1].speciesId,PersonalityType.Calm,2);
                for(int i=0;i<5;i++){a.magicGeneSlots.Add(new MagicGenome{programGenes=SpellExamples.Create(i%3)});b.magicGeneSlots.Add(new MagicGenome{programGenes=SpellExamples.Create((i+1)%3)});}
                string original=JsonUtility.ToJson(a);var child=new BreedingSystem(db,rules,rank).Breed(a,b,6);
                Assert.AreEqual(5,child.child.magicGeneSlots.Count);Assert.AreEqual(RankRules.MagicSlots(child.species.rank),child.child.magicGenomes.Count);Assert.AreEqual(original,JsonUtility.ToJson(a));
            }
            finally{foreach(var species in db.species)UnityEngine.Object.DestroyImmediate(species);UnityEngine.Object.DestroyImmediate(db);UnityEngine.Object.DestroyImmediate(rules);UnityEngine.Object.DestroyImmediate(rank);}
        }
        [Test]public void CustomNamesSurviveJsonWithoutChangingCompiledPerformance()
        {
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellExamples.Create(0)},0);var before=MagicFactory.Build(genome);
            genome.programGenes.customName="はじまりの炎";genome.customName="はじまりの炎";
            MagicGenome restored;Assert.IsTrue(MagicGenome.TryParseJson(JsonUtility.ToJson(genome),out restored));var after=MagicFactory.Build(restored);
            Assert.AreEqual("はじまりの炎",after.displayName);Assert.AreEqual(before.mpCost,after.mpCost);Assert.AreEqual(before.cooldown,after.cooldown);Assert.AreEqual(before.power,after.power);
            Assert.AreEqual(JsonUtility.ToJson(genome.expressedProgram),JsonUtility.ToJson(restored.expressedProgram));
        }
        [Test]public void OnlyTypedProgramsAreAcceptedAndAllGeneratedProgramsFitLimits()
        {
            MagicGenome result;
            Assert.IsFalse(MagicGenome.TryParseJson("{\"activation\":0,\"effects\":[0],\"power\":20}",out result));
            Assert.IsNull(result);
            for(int i=0;i<100;i++){var genes=SpellProgramBuilder.Random(i);string error;Assert.IsTrue(SpellValidator.Validate(genes.chromosomeA,out error),error);Assert.IsTrue(SpellValidator.Validate(genes.chromosomeB,out error),error);}
        }
    }
}
