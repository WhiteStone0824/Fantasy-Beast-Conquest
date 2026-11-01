using System.Collections.Generic;
using FusionAnyMonster.Save;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class SupportMagicTests
    {
        [Test]
        public void CooldownIsShorterForAttackAndLongerForHealing()
        {
            var attack=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Damage);
            var heal=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly);
            var attackSpell=SpellCompiler.Compile(attack);var healSpell=SpellCompiler.Compile(heal);
            float attackOriginal=attack.chromosomeA.baseCooldown*(1+attackSpell.complexity*.06f);
            float healingOriginal=heal.chromosomeA.baseCooldown*(1+healSpell.complexity*.06f);
            Assert.AreEqual(attackOriginal*.8f,attackSpell.cooldown,.001f);
            Assert.AreEqual(healingOriginal*1.2f,healSpell.cooldown,.001f);
        }
        [Test]
        public void SupportCompilationProtectsAlliesWithoutChangingInheritedGenome()
        {
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly)},0);
            string before=JsonUtility.ToJson(genome);var magic=MagicFactory.Build(genome);
            Assert.IsTrue(magic.targetsAllies);Assert.AreEqual(before,JsonUtility.ToJson(genome));
            Assert.IsFalse(MagicFactory.Build(DemoDataFactory.DemoGenome(0)).targetsAllies);
        }

        [Test]
        public void BuffsRefreshWithoutStackingAndExpireBackToOriginalValues()
        {
            var buffs=new CombatantBuffState();
            buffs.Apply(BuffType.AttackBoost,.3f,2);
            buffs.Apply(BuffType.DefenseBoost,.25f,2);
            buffs.Apply(BuffType.Haste,.2f,2);
            for(int i=0;i<20;i++)buffs.Apply(BuffType.AttackBoost,.3f,2);
            Assert.AreEqual(1.3f,buffs.AttackMultiplier,.001f);
            Assert.AreEqual(.75f,buffs.DamageMultiplier,.001f);
            Assert.AreEqual(1.2f,buffs.SpeedMultiplier,.001f);
            buffs.Tick(2.1f);
            Assert.AreEqual(1,buffs.AttackMultiplier);
            Assert.AreEqual(1,buffs.DamageMultiplier);
            Assert.AreEqual(1,buffs.SpeedMultiplier);
            Assert.IsFalse(buffs.Active);
        }

        [Test]
        public void SupportGenesCrossMutateAndRoundTripThroughSave()
        {
            var a=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly)},0);
            var b=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.ApplyBuff,target:SpellTarget.LowestHealthAlly)},0);
            string original=JsonUtility.ToJson(a);var child=GeneticAlgorithm.Cross(a,b,SpeciesRank.S,null,new System.Random(23));
            Assert.AreEqual(JsonUtility.ToJson(child),JsonUtility.ToJson(GeneticAlgorithm.Cross(a,b,SpeciesRank.S,null,new System.Random(23))));
            MagicGenome loaded;Assert.IsTrue(MagicGenome.TryParseJson(JsonUtility.ToJson(child),out loaded));
            Assert.AreEqual(JsonUtility.ToJson(child),JsonUtility.ToJson(loaded));Assert.AreEqual(original,JsonUtility.ToJson(a));
        }

        [Test]
        public void NewGameContainsParentsForAllFourSupportEffects()
        {
            bool heals=false;var buffs=new HashSet<BuffType>();
            foreach(var unit in SaveSystem.CreateDemoData().units)foreach(string json in unit.magicGenomeJson)
            {
                MagicGenome g;Assert.IsTrue(MagicGenome.TryParseJson(json,out g));
                foreach(var n in MagicFactory.Build(g).program.instructions)
                {heals|=n.kind==SpellNodeKind.Heal;if(n.kind==SpellNodeKind.ApplyBuff)buffs.Add(n.parameters.buff);}
            }
            Assert.IsTrue(heals);Assert.AreEqual(3,buffs.Count);
        }
    }
}
