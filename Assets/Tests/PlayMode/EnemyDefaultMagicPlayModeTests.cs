using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class EnemyDefaultMagicPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private GameObject root;private BattleDemo battle;
        [UnitySetUp]public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            root=new GameObject("Enemy default spell tests");battle=root.AddComponent<BattleDemo>();battle.enabled=false;
        }
        [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(root!=null)Object.Destroy(root);yield return null;}
        private Combatant Unit(bool friendly,Vector2 point)
        {
            var unit=(Combatant)typeof(BattleDemo).GetMethod("CreateCombatant",Flags).Invoke(battle,new object[]{"Spell test",friendly,point,EnemyArchetype.Normal,Color.white});
            unit.transform.SetParent(root.transform,true);unit.enabled=false;return unit;
        }
        private Combatant Wild(SpeciesDefinition species,Vector2 point)
        {
            var unit=Unit(false,point);
            typeof(Combatant).GetMethod("ApplySpeciesCombatTraits",Flags).Invoke(unit,new object[]{species});
            typeof(Combatant).GetMethod("ConfigureWildMagic",Flags).Invoke(unit,new object[]{WildUnitFactory.Create(species,1,1),species});
            var cooldowns=(List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(unit);
            for(int i=0;i<cooldowns.Count;i++)cooldowns[i]=0;
            return unit;
        }
        [UnityTest]public IEnumerator EnemyCastsSpeciesDefaultAndSpendsMpWhileKeepingArchetypeHp()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById("demo_00");
            var caster=Wild(species,Vector2.zero);var victim=Unit(true,Vector2.right);
            float hp=victim.CurrentHp,mana=caster.CurrentMp;
            Assert.AreEqual(42,caster.MaximumHp,.01f);Assert.AreEqual(1,caster.Magics.Count);
            Assert.AreEqual(JsonUtility.ToJson(species.defaultMagicGenomes[0].programGenes),JsonUtility.ToJson(caster.UnitData.magicGenomes[0].programGenes));
            typeof(Combatant).GetMethod("CastPrograms",Flags).Invoke(caster,null);
            Assert.AreEqual(1,caster.MagicCastCount(0));Assert.AreEqual(mana-caster.Magics[0].mpCost,caster.CurrentMp,.001f);
            yield return new WaitForSeconds(.5f);Assert.Less(victim.CurrentHp,hp);
            CollectionAssert.AreEqual(caster.WildRecord.magicGenomeJson,new[]{JsonUtility.ToJson(caster.UnitData.magicGenomes[0])});
        }
        [UnityTest]public IEnumerator EnemySupportMagicHealsItsOwnTeamAndDoesNotHealPlayer()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var species=System.Array.Find(db.species,s=>s.family==SpeciesFamily.Fairy && s.rank==SpeciesRank.F);
            var healer=Wild(species,Vector2.zero);var friend=Unit(false,Vector2.right);var player=Unit(true,Vector2.right);
            typeof(Combatant).GetField("hp",Flags).SetValue(friend,1f);typeof(Combatant).GetField("hp",Flags).SetValue(player,1f);
            typeof(Combatant).GetMethod("CastPrograms",Flags).Invoke(healer,null);Assert.AreEqual(1,healer.MagicCastCount(0));
            yield return null;yield return null;
            Assert.Greater(friend.CurrentHp,1);Assert.AreEqual(1,player.CurrentHp);
        }
        [UnityTest]public IEnumerator SRankEnemyEvaluatesAllFiveDefaultSlotsIndependently()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");var species=System.Array.Find(db.species,s=>s.family==SpeciesFamily.Slime && s.rank==SpeciesRank.S);
            var caster=Wild(species,Vector2.zero);Unit(true,Vector2.right);typeof(Combatant).GetField("mana",Flags).SetValue(caster,1000f);
            typeof(Combatant).GetField("hp",Flags).SetValue(caster,1f);
            typeof(Combatant).GetMethod("CastPrograms",Flags).Invoke(caster,null);
            Assert.AreEqual(5,caster.Magics.Count);
            for(int i=0;i<5;i++)Assert.AreEqual(1,caster.MagicCastCount(i),"slot "+i);
            yield return null;
        }
    }
}
