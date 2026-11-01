using System;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;
namespace FusionAnyMonster.Tests
{
    public sealed class SpellSemanticTests
    {
        [Test]public void AttackAndAllyHealAreViableButEnemyHealingConflicts()
        {
            var attack=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Damage);
            var heal=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly);
            var wrong=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.NearestEnemy);
            Assert.AreEqual(SpellViabilityStatus.Viable,SpellSemanticAnalyzer.Analyze(attack.chromosomeA).status);
            Assert.AreEqual(SpellViabilityStatus.Viable,SpellSemanticAnalyzer.Analyze(heal.chromosomeA).status);
            Assert.AreEqual(SpellViabilityStatus.TargetConflict,SpellSemanticAnalyzer.Analyze(wrong.chromosomeA).status);
        }
        [Test]public void ConditionalHealthTriggerAndLowProbabilityAreSituational()
        {
            var p=SpellProgramBuilder.Projectile(effect:SpellNodeKind.ApplyStatus).chromosomeA;
            p.nodes[0].kind=SpellNodeKind.HPThresholdTrigger;p.nodes[2].parameters.probability=.01f;
            Assert.AreEqual(SpellViabilityStatus.Situational,SpellSemanticAnalyzer.Analyze(p).status);
            p.nodes[0].parameters.threshold=0;
            Assert.AreEqual(SpellViabilityStatus.NoUsefulPath,SpellSemanticAnalyzer.Analyze(p).status);
        }
        [Test]public void OnKillEffectWithoutKillSourceIsNotUseful()
        {
            var p=SpellProgramBuilder.Projectile().chromosomeA;p.nodes[1].edges.Clear();p.nodes.RemoveAt(2);
            var effect=SpellProgramBuilder.Add(p,SpellNodeKind.Explosion,SpellContext.Kill);p.nodes[1].edges.Add(new SpellEdge(SpellPort.OnKill,effect.id));
            string error;Assert.IsTrue(SpellValidator.Validate(p,out error),error);
            Assert.AreEqual(SpellViabilityStatus.NoUsefulPath,SpellSemanticAnalyzer.Analyze(p).status);
        }
        [Test]public void MaximumManaOnlyRejectsAvailableSlots()
        {
            var species=ScriptableObject.CreateInstance<SpeciesDefinition>();
            try
            {
                species.rank=SpeciesRank.F;species.baseStats=new UnitStats{mp=1};
                var unit=new UnitInstance{level=1,iv=new IndividualValues()};
                var magic=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(power:500,count:24)},0);
                Assert.AreEqual(SpellViabilityStatus.Unaffordable,SpellViabilityEvaluator.Evaluate(magic,species,unit,0).status);
                var locked=SpellViabilityEvaluator.Evaluate(magic,species,unit,1);
                Assert.IsTrue(locked.Acceptable);StringAssert.Contains("未解放",locked.warning);
            }
            finally{UnityEngine.Object.DestroyImmediate(species);}
        }
        [Test]public void ParameterMutationNeverChangesUnusedNodeFields()
        {
            var settings=ScriptableObject.CreateInstance<SpellSettings>();
            try
            {
                settings.parameterMutationChance=1;settings.nodeMutationChance=0;settings.structureMutationChance=0;
                var p=SpellProgramBuilder.Projectile().chromosomeA;
                for(int seed=0;seed<100;seed++)
                {
                    var result=SpellGenetics.Mutate(p,new System.Random(seed),settings,new System.Collections.Generic.List<string>());
                    Assert.AreEqual(p.nodes[2].parameters.trajectory,result.nodes[2].parameters.trajectory);
                    Assert.AreEqual(p.nodes[2].parameters.status,result.nodes[2].parameters.status);
                    string error;Assert.IsTrue(SpellValidator.Validate(result,out error,settings),error);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(settings);}
        }
    }
}
