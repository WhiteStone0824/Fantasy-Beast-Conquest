using System;
using System.Collections.Generic;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SpellParameterCrossoverTests
    {
        private SpellSettings settings;
        [SetUp]public void Setup()
        {
            settings=ScriptableObject.CreateInstance<SpellSettings>();
            settings.crossoverChance=0;
            settings.parameterMutationChance=settings.nodeMutationChance=settings.structureMutationChance=0;
            settings.nodeParameterCrossoverChance=1;
        }
        [TearDown]public void Cleanup(){UnityEngine.Object.DestroyImmediate(settings);}

        private static SpellGenotype Parent(bool second)
        {
            var genes=SpellProgramBuilder.Projectile(second?AttributeElement.Water:AttributeElement.Fire,
                second?80:20,second?3:5,second?SpellTrajectory.Homing:SpellTrajectory.Straight);
            foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB})
            {
                p.baseCooldown=second?6:2;p.range=second?12:4;
                var n=p.nodes[1].parameters;n.speed=second?16:4;n.radius=second?1:.2f;
                n.seconds=second?6:2;n.multiplier=second?2:.5f;n.homing=second?1:0;
                n.probability=second?.8f:.2f;n.threshold=second?.7f:.3f;n.angle=second?90:-90;
            }
            return genes;
        }

        [Test]public void OppositeParentsCanProduceFireHomingProjectilesWithoutMutation()
        {
            var a=Parent(false);var b=Parent(true);
            string originalA=JsonUtility.ToJson(a),originalB=JsonUtility.ToJson(b);bool found=false;
            for(int seed=0;seed<50;seed++)
            {
                var child=SpellGenetics.Cross(a,b,seed,settings);
                foreach(var p in new[]{child.chromosomeA,child.chromosomeB})
                {
                    var shot=p.nodes.Find(n=>n.kind==SpellNodeKind.SpawnProjectile).parameters;
                    found|=shot.element==AttributeElement.Fire && shot.trajectory==SpellTrajectory.Homing;
                    Assert.That(shot.speed,Is.InRange(4f,16f));Assert.That(shot.count,Is.InRange(3,5));
                    Assert.That(p.basePower,Is.InRange(20f,80f));
                    string error;Assert.IsTrue(SpellValidator.Validate(p,out error,settings),error);
                }
                Assert.IsTrue(child.history.Exists(line=>line.Contains("ノード内交叉")));
                Assert.AreEqual(JsonUtility.ToJson(child),JsonUtility.ToJson(SpellGenetics.Cross(a,b,seed,settings)));
                Assert.IsNotNull(SpellCompiler.Compile(child,settings:settings));
            }
            Assert.IsTrue(found);Assert.AreEqual(originalA,JsonUtility.ToJson(a));Assert.AreEqual(originalB,JsonUtility.ToJson(b));
        }

        [Test]public void NumericGenesAreMixedAndWiringIsPreserved()
        {
            var a=Parent(false).chromosomeA;var b=Parent(true).chromosomeA;
            var result=SpellParameterCrossover.Cross(a,b,new System.Random(24),settings);
            Assert.That(result.basePower,Is.GreaterThan(20).And.LessThan(80));
            Assert.That(result.baseCooldown,Is.GreaterThan(2).And.LessThan(6));
            Assert.That(result.range,Is.GreaterThan(4).And.LessThan(12));
            var p=result.nodes[1].parameters;
            Assert.That(p.speed,Is.GreaterThan(4).And.LessThan(16));
            Assert.That(p.multiplier,Is.GreaterThan(.5f).And.LessThan(2));
            Assert.That(p.seconds,Is.InRange(2f,6f));Assert.That(p.radius,Is.InRange(.2f,1f));
            Assert.That(p.homing,Is.InRange(0f,1f));Assert.That(p.probability,Is.InRange(.2f,.8f));
            Assert.That(p.threshold,Is.InRange(.3f,.7f));Assert.That(p.angle,Is.InRange(-90f,90f));
            Assert.AreEqual(a.root,result.root);Assert.AreEqual(a.nodes.Count,result.nodes.Count);
            foreach(var node in a.nodes)
            {
                var n=result.Find(node.id);Assert.AreEqual(node.kind,n.kind);Assert.AreEqual(node.input,n.input);
                Assert.AreEqual(JsonUtility.ToJson(new EdgeList{edges=node.edges}),JsonUtility.ToJson(new EdgeList{edges=n.edges}));
            }
        }
        [Serializable]private sealed class EdgeList {public List<SpellEdge> edges;}

        [Test]public void ZeroChanceLeavesGenesAndHistoryUnchanged()
        {
            settings.nodeParameterCrossoverChance=0;var a=Parent(false).chromosomeA;
            var history=new List<string>{"existing"};var result=SpellParameterCrossover.Cross(a,Parent(true).chromosomeA,new System.Random(1),settings,history);
            Assert.AreEqual(JsonUtility.ToJson(a),JsonUtility.ToJson(result));Assert.AreNotSame(a,result);
            CollectionAssert.AreEqual(new[]{"existing"},history);
        }

        [Test]public void DifferentNodeKindsDoNotExchangeParameters()
        {
            var a=Parent(false).chromosomeA;var b=Parent(true).chromosomeA;b.nodes[1].kind=SpellNodeKind.SpawnArea;
            var result=SpellParameterCrossover.Cross(a,b,new System.Random(1),settings);
            Assert.AreEqual(JsonUtility.ToJson(a.nodes[1].parameters),JsonUtility.ToJson(result.nodes[1].parameters));
        }

        [Test]public void ShiftedTreesCanMatchSameKindNodesByFallback()
        {
            var a=Parent(false).chromosomeA;var b=Parent(true).chromosomeA;
            var delay=SpellProgramBuilder.Add(b,SpellNodeKind.Delay,SpellContext.Cast);
            delay.edges.Add(new SpellEdge(SpellPort.Next,1));b.Find(b.root).edges[0].child=delay.id;
            var result=SpellParameterCrossover.Cross(a,b,new System.Random(9),settings);
            Assert.Greater(result.nodes[1].parameters.speed,4);
            Assert.AreEqual(a.nodes.Count,result.nodes.Count);Assert.AreEqual(1,result.Find(result.root).edges[0].child);
        }

        [Test]public void NodeAndEdgeListOrderDoNotChangeCorrespondingNodeParameters()
        {
            var a=Parent(false).chromosomeA;var b=Parent(true).chromosomeA;
            var expected=SpellParameterCrossover.Cross(a,b,new System.Random(17),settings);
            a.nodes.Reverse();b.nodes.Reverse();foreach(var n in a.nodes)n.edges.Reverse();foreach(var n in b.nodes)n.edges.Reverse();
            var actual=SpellParameterCrossover.Cross(a,b,new System.Random(17),settings);
            foreach(var node in expected.nodes)Assert.AreEqual(JsonUtility.ToJson(node.parameters),JsonUtility.ToJson(actual.Find(node.id).parameters));
        }

        [Test]public void StatusAndBuffKindsCanBeInheritedFromEitherParent()
        {
            foreach(var kind in new[]{SpellNodeKind.ApplyStatus,SpellNodeKind.ApplyBuff})
            {
                var a=SpellProgramBuilder.Projectile(effect:kind).chromosomeA;var b=a.Clone();
                a.nodes[2].parameters.status=DebuffType.Poison;b.nodes[2].parameters.status=DebuffType.Silence;
                a.nodes[2].parameters.buff=BuffType.AttackBoost;b.nodes[2].parameters.buff=BuffType.Haste;
                var foundStatus=new HashSet<DebuffType>();var foundBuff=new HashSet<BuffType>();
                for(int seed=0;seed<30;seed++)
                {
                    var p=SpellParameterCrossover.Cross(a,b,new System.Random(seed),settings).nodes[2].parameters;
                    foundStatus.Add(p.status);foundBuff.Add(p.buff);
                }
                CollectionAssert.AreEquivalent(new[]{DebuffType.Poison,DebuffType.Silence},foundStatus);
                CollectionAssert.AreEquivalent(new[]{BuffType.AttackBoost,BuffType.Haste},foundBuff);
            }
        }
    }
}
