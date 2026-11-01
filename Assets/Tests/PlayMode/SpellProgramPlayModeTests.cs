using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace FusionAnyMonster.Tests
{
    public sealed class SpellProgramPlayModeTests
    {
        [UnityTest]public IEnumerator ParameterCrossedOffspringExecutesItsInheritedPower()
        {
            var settings=ScriptableObject.CreateInstance<SpellSettings>();
            try
            {
                settings.crossoverChance=settings.parameterMutationChance=settings.nodeMutationChance=settings.structureMutationChance=0;
                settings.nodeParameterCrossoverChance=1;
                var a=SpellProgramBuilder.Projectile(AttributeElement.Fire,20);
                var b=SpellProgramBuilder.Projectile(AttributeElement.Water,80,trajectory:SpellTrajectory.Homing);
                var child=SpellGenetics.Cross(a,b,918,settings);var spell=SpellCompiler.Compile(child,settings:settings);
                Assert.That(spell.basePower,Is.GreaterThan(20).And.LessThan(80));
                var caster=Unit(true,Vector2.zero);var victim=Unit(false,Vector2.right);float before=victim.CurrentHp;
                SpellExecutor.Cast(battle,caster,victim,spell);
                yield return new WaitForSeconds(.5f);
                float magicAttack=(float)typeof(Combatant).GetField("magicAttack",Flags).GetValue(caster);
                Assert.AreEqual(spell.basePower+magicAttack,before-victim.CurrentHp,.01f);
                var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=child},0);MagicGenome loaded;
                Assert.IsTrue(MagicGenome.TryParseJson(JsonUtility.ToJson(genome),out loaded));
                Assert.AreEqual(spell.basePower,MagicFactory.Build(loaded).power,.001f);
            }
            finally{Object.Destroy(settings);}
        }
        [UnityTest]public IEnumerator SelfHealingProjectileStillHitsItsCaster()
        {
            var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.Self);
            var caster=Unit(true,Vector2.zero);typeof(Combatant).GetField("hp",Flags).SetValue(caster,10f);
            SpellExecutor.Cast(battle,caster,caster,SpellCompiler.Compile(genes));
            yield return null;yield return null;yield return null;
            Assert.Greater(caster.CurrentHp,10);
        }
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        private GameObject root;private BattleDemo battle;
        [UnitySetUp]public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            root=new GameObject("GP runtime tests");battle=root.AddComponent<BattleDemo>();battle.enabled=false;
        }
        [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;if(root!=null)Object.Destroy(root);yield return null;}
        private Combatant Unit(bool friendly,Vector2 position,SpellGenotype genes=null,bool ai=false)
        {
            var unit=(Combatant)typeof(BattleDemo).GetMethod("CreateCombatant",Flags).Invoke(battle,new object[]{"GP test",friendly,position,EnemyArchetype.Normal,Color.white});
            unit.transform.SetParent(root.transform,true);
            var data=new UnitSaveData{id="gp-test",rank="S",baseStats=new float[]{10000,10000,10,20,0,0,2,1,15,0},magicGenomeJson=genes==null?new string[0]:new[]{JsonUtility.ToJson(SpellSlotStorage.Prepare(new MagicGenome{programGenes=genes},0))}};
            typeof(Combatant).GetMethod("Configure",Flags).Invoke(unit,new object[]{data,null,null});unit.enabled=ai;
            return unit;
        }
        [UnityTest]public IEnumerator MagicProjectilesKeepElementColorWhenReused()
        {
            var ally=Unit(true,Vector2.zero);
            var parameters=new SpellParameters{element=AttributeElement.Fire,radius=.2f};
            var spell=new CompiledSpell{instructions=new[]{new SpellInstruction{parameters=parameters}}};
            var go=new GameObject("Projectile color test");go.transform.SetParent(root.transform,false);
            var projectile=go.AddComponent<SpellProjectile>();projectile.enabled=false;
            projectile.Launch(null,new CastSession{spell=spell,caster=ally},0,new SpellEventContext(),null,1,0);
            var renderer=go.GetComponent<SpriteRenderer>();
            Assert.That(renderer.color,Is.EqualTo(new Color(1f,.31f,.12f)));
            parameters.element=AttributeElement.Water;
            projectile.Launch(null,new CastSession{spell=spell,caster=ally},0,new SpellEventContext(),null,1,0);
            Assert.That(renderer.color,Is.EqualTo(new Color(.18f,.58f,1f)),"Magic must retain its element color when the projectile is reused.");
            yield return null;
        }
        [UnityTest]public IEnumerator BasicAttackChargesAndHitsOnceWithoutProjectile()
        {
            var caster=Unit(true,Vector2.zero);var victim=Unit(false,new Vector2(1.8f,0));
            typeof(Combatant).GetField("mana",Flags).SetValue(caster,0f);
            float before=victim.CurrentHp;
            float damage=(float)typeof(Combatant).GetField("damage",Flags).GetValue(caster);
            bool started=(bool)typeof(Combatant).GetMethod("BasicAttack",Flags).Invoke(caster,new object[]{victim,damage});
            Assert.IsTrue(started);typeof(Combatant).GetField("cooldown",Flags).SetValue(caster,100f);caster.enabled=true;
            float deadline=Time.time+1f;while(victim.CurrentHp>=before && Time.time<deadline)yield return null;
            Assert.Greater(caster.transform.position.x,.5f,"The attacker must move toward the target.");
            Assert.AreEqual(damage*.5f,before-victim.CurrentHp,.01f,"Contact must deal half physical attack once.");
            float contactDistance=Vector2.Distance(caster.transform.position,victim.transform.position);
            yield return new WaitForSeconds(.35f);
            Assert.Greater(Vector2.Distance(caster.transform.position,victim.transform.position),contactDistance+.1f,"The attacker must retreat after contact.");
            Assert.AreEqual(0,root.GetComponentsInChildren<SpellProjectile>().Length,"Normal attacks must not spawn magic projectiles.");
        }
        [UnityTest]public IEnumerator BasicAttackUsesNeutralResistanceAndAtLeastOneSecondCooldown()
        {
            var caster=Unit(true,Vector2.zero);var victim=Unit(false,new Vector2(1.8f,0));
            typeof(Combatant).GetField("mana",Flags).SetValue(caster,0f);
            typeof(Combatant).GetField("attackInterval",Flags).SetValue(caster,.01f);
            var personality=new PersonalityDefinition{chargeCooldownMultiplier=.5f};
            typeof(Combatant).GetField("personality",Flags).SetValue(caster,personality);
            Assert.GreaterOrEqual((float)typeof(Combatant).GetMethod("ChargeCooldown",Flags).Invoke(caster,null),1f);
            ((float[])typeof(Combatant).GetField("elementReduction",Flags).GetValue(victim))[(int)AttributeElement.None]=.5f;
            float before=victim.CurrentHp;
            Assert.IsTrue((bool)typeof(Combatant).GetMethod("BasicAttack",Flags).Invoke(caster,new object[]{victim,40f}));
            typeof(Combatant).GetField("cooldown",Flags).SetValue(caster,100f);caster.enabled=true;
            float deadline=Time.time+1f;while(victim.CurrentHp>=before && Time.time<deadline)yield return null;
            Assert.AreEqual(10f,before-victim.CurrentHp,.01f,"Half damage must be reduced by the target's neutral resistance.");
        }
        [UnityTest]public IEnumerator UnitChargesWithManaWhileAllSpellsAreCoolingDown()
        {
            var caster=Unit(true,Vector2.zero,SpellProgramBuilder.Projectile());
            var victim=Unit(false,new Vector2(1.8f,0));
            var cooldowns=(List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(caster);
            Assert.Greater(cooldowns.Count,0);
            cooldowns[0]=10f;
            float before=victim.CurrentHp;
            Assert.Greater(caster.CurrentMp,0f);
            caster.enabled=true;
            float deadline=Time.time+2f;
            while(victim.CurrentHp>=before && Time.time<deadline)yield return null;
            Assert.Less(victim.CurrentHp,before,"A unit should use its normal attack during spell cooldown even with MP remaining.");
            Assert.Greater(caster.CurrentMp,0f);
        }
        [UnityTest]public IEnumerator UnitCanAttackWhenItsSpellCostsMoreThanMaximumMana()
        {
            var caster=Unit(true,Vector2.zero,SpellProgramBuilder.Projectile());
            var victim=Unit(false,new Vector2(1.8f,0));
            caster.Magics[0].mpCost=caster.MaximumMp+1f;
            ((List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(caster))[0]=0;
            float before=victim.CurrentHp;caster.enabled=true;
            float deadline=Time.time+2f;while(victim.CurrentHp>=before && Time.time<deadline)yield return null;
            Assert.Less(victim.CurrentHp,before,"An unaffordable spell must not prevent the unit from attacking.");
        }
        [UnityTest]public IEnumerator UnitAttacksWhenManaIsBelowSpellCostEvenIfRegenerating()
        {
            var caster=Unit(true,Vector2.zero,SpellProgramBuilder.Projectile());var victim=Unit(false,new Vector2(1.8f,0));
            typeof(Combatant).GetField("mana",Flags).SetValue(caster,1f);typeof(Combatant).GetField("manaRegen",Flags).SetValue(caster,.1f);
            ((List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(caster))[0]=0;
            Assert.Greater(caster.Magics[0].mpCost,1.5f);float before=victim.CurrentHp;caster.enabled=true;
            float deadline=Time.time+2f;while(victim.CurrentHp>=before && Time.time<deadline)yield return null;
            Assert.Less(victim.CurrentHp,before,"Waiting for mana must not prevent normal attacks.");
        }
        [UnityTest]public IEnumerator ChargeMissesIfTargetEscapes()
        {
            var caster=Unit(true,Vector2.zero);var victim=Unit(false,new Vector2(1.8f,0));float before=victim.CurrentHp;
            typeof(Combatant).GetField("mana",Flags).SetValue(caster,0f);
            Assert.IsTrue((bool)typeof(Combatant).GetMethod("BasicAttack",Flags).Invoke(caster,new object[]{victim,10f}));
            victim.transform.position=new Vector2(12,0);typeof(Combatant).GetField("cooldown",Flags).SetValue(caster,100f);caster.enabled=true;
            yield return new WaitForSeconds(.45f);
            float distanceAfterMiss=Vector2.Distance(caster.transform.position,victim.transform.position);
            yield return new WaitForSeconds(.3f);
            Assert.AreEqual(before,victim.CurrentHp,"An escaped target must not take damage.");
            Assert.Greater(Vector2.Distance(caster.transform.position,victim.transform.position),distanceAfterMiss+.1f,"The attacker must also retreat after a missed charge.");
        }
        [UnityTest]public IEnumerator IncapacitationCancelsCharge()
        {
            var caster=Unit(true,Vector2.zero);var victim=Unit(false,new Vector2(1.8f,0));float before=victim.CurrentHp;
            typeof(Combatant).GetField("mana",Flags).SetValue(caster,0f);
            Assert.IsTrue((bool)typeof(Combatant).GetMethod("BasicAttack",Flags).Invoke(caster,new object[]{victim,10f}));
            typeof(Combatant).GetField("freezeTime",Flags).SetValue(caster,1f);
            typeof(Combatant).GetField("cooldown",Flags).SetValue(caster,100f);caster.enabled=true;
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(before,victim.CurrentHp);
            Assert.Less(caster.transform.position.x,.01f,"A frozen unit must not continue charging.");
            Assert.IsNull(typeof(Combatant).GetField("chargeTarget",Flags).GetValue(caster));
        }
        [UnityTest]public IEnumerator AggressivePersonalityChargesMoreOftenThanTimid()
        {
            var database=Resources.Load<PersonalityDatabase>("PersonalityDatabase");
            var aggressive=Unit(true,Vector2.zero);var timid=Unit(true,new Vector2(0,20));
            var aggressiveTarget=Unit(false,new Vector2(1.8f,0));var timidTarget=Unit(false,new Vector2(1.8f,20));
            foreach(var pair in new[]{new { unit=aggressive, type=PersonalityType.Aggressive },new { unit=timid, type=PersonalityType.Timid }})
            {
                var data=new UnitSaveData{id="charge-test",rank="S",personality=pair.type.ToString(),baseStats=new float[]{10000,10000,10,20,0,0,2,1,15,0}};
                typeof(Combatant).GetMethod("Configure",Flags).Invoke(pair.unit,new object[]{data,null,database});
                typeof(Combatant).GetField("speed",Flags).SetValue(pair.unit,4f);
                typeof(Combatant).GetField("damage",Flags).SetValue(pair.unit,10f);
                typeof(Combatant).GetField("attackInterval",Flags).SetValue(pair.unit,1f);
                typeof(Combatant).GetField("maxMana",Flags).SetValue(pair.unit,0f);
                typeof(Combatant).GetField("mana",Flags).SetValue(pair.unit,0f);
                pair.unit.enabled=true;
            }
            float a=aggressiveTarget.CurrentHp,b=timidTarget.CurrentHp;
            yield return new WaitForSeconds(8f);
            Assert.Greater(a-aggressiveTarget.CurrentHp,b-timidTarget.CurrentHp,"With equal speed and attack power, the aggressive unit should charge more often.");
            Assert.Greater(b-timidTarget.CurrentHp,0f,"Timid units must still be able to charge.");
        }
        [UnityTest]public IEnumerator UnitsWithNoUsableSpellChargeEvenWithMana()
        {
            var ally=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(1.8f,0));
            float initialHp=enemy.CurrentHp;
            typeof(Combatant).GetField("mana",Flags).SetValue(ally,5f);
            typeof(Combatant).GetField("manaRegen",Flags).SetValue(ally,1f);
            Assert.IsFalse((bool)typeof(Combatant).GetMethod("BasicAttack",Flags).Invoke(ally,new object[]{enemy,10f}));
            ally.enabled=true;
            float deadline=Time.time+1.5f;
            while(enemy.CurrentHp>=initialHp && Time.time<deadline)yield return null;
            Assert.Less(enemy.CurrentHp,initialHp,"A unit without a usable spell must not remain idle.");
            Assert.Greater(ally.CurrentMp,0f);
        }
        [UnityTest]public IEnumerator EnemyWithoutUsableMagicAlsoCharges()
        {
            var ally=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(1.8f,0));
            float initialHp=ally.CurrentHp;
            typeof(Combatant).GetField("mana",Flags).SetValue(enemy,5f);
            typeof(Combatant).GetField("manaRegen",Flags).SetValue(enemy,1f);
            enemy.enabled=true;
            float deadline=Time.time+1.5f;while(ally.CurrentHp>=initialHp && Time.time<deadline)yield return null;
            Assert.Less(ally.CurrentHp,initialHp,"An enemy without a usable spell should be able to charge.");
        }
        private static SpellGenotype SingleShot(float range,float seconds)
        {
            var genes=SpellExamples.Create(0);foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB})
            {p.range=range;p.nodes[1].parameters.count=1;p.nodes[1].parameters.angle=0;p.nodes[1].parameters.speed=20;p.nodes[1].parameters.seconds=seconds;p.nodes[1].parameters.trajectory=SpellTrajectory.Straight;p.nodes[2].edges.Clear();p.nodes.RemoveAt(3);}
            return genes;
        }
        [UnityTest]public IEnumerator ProjectilesCannotHitBeyondRangeDuringALongFrame(){return VerifyTravelBounds(1,6);}
        [UnityTest]public IEnumerator ProjectilesCannotHitAfterLifetimeDuringALongFrame(){return VerifyTravelBounds(10,.05f);}
        private IEnumerator VerifyTravelBounds(float range,float seconds)
        {
            var caster=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(2,0));float hp=enemy.CurrentHp,oldCapture=Time.captureDeltaTime;
            try
            {
                Time.captureDeltaTime=.2f;SpellExecutor.Cast(battle,caster,enemy,SpellCompiler.Compile(SingleShot(range,seconds)));
                yield return null;yield return null;yield return null;yield return null;
                Assert.AreEqual(hp,enemy.CurrentHp,"Travel after the spell's range or lifetime must not deal damage.");
            }
            finally{Time.captureDeltaTime=oldCapture;}
        }
        [UnityTest]public IEnumerator DestroyingProjectilesReleasesTheirCastSession()
        {
            var caster=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(3,0));var genes=SingleShot(10,6);
            genes.chromosomeA.nodes[1].parameters.speed=genes.chromosomeB.nodes[1].parameters.speed=0;
            var session=SpellExecutor.Cast(battle,caster,enemy,SpellCompiler.Compile(genes));yield return null;yield return null;
            Assert.AreEqual(1,session.projectilesAlive);
            foreach(var projectile in root.GetComponentsInChildren<SpellProjectile>())Object.Destroy(projectile.gameObject);
            yield return null;yield return null;
            Assert.AreEqual(0,session.projectilesAlive,"A destroyed projectile must not hold its cast forever.");
        }
        [UnityTest]public IEnumerator EndedBattleRefusesNewCastSessions()
        {
            var caster=Unit(true,Vector2.zero);var enemy=Unit(false,Vector2.right);typeof(BattleDemo).GetField("ended",Flags).SetValue(battle,true);
            Assert.IsNull(SpellExecutor.Cast(battle,caster,enemy,SpellCompiler.Compile(SpellExamples.Create(0))));yield return null;
        }
        [UnityTest]public IEnumerator InvalidFirstSlotDoesNotUnlockTheSecondSlotForFRank()
        {
            var caster=Unit(true,Vector2.zero);
            var data=new UnitSaveData{id="invalid-slot",rank="F",magicGenomeJson=new[]{"not json",JsonUtility.ToJson(new MagicGenome{programGenes=SpellExamples.Create(0)})}};
            LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Skipped invalid magic genome"));
            typeof(Combatant).GetMethod("Configure",Flags).Invoke(caster,new object[]{data,null,null});
            Assert.IsTrue(caster.Magics.Count==0 || caster.Magics[0].program==null,"A bad slot zero must not grant access to locked slot one.");yield return null;
        }
        [UnityTest]public IEnumerator TwoMinuteProjectileLoadStaysBoundedAndDrainsAllSessions()
        {
            var caster=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(10,0));var genes=SingleShot(20,6);
            foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){p.nodes[1].parameters.count=48;p.nodes[1].parameters.angle=30;p.nodes[1].parameters.speed=1;}
            var compiled=SpellCompiler.Compile(genes);float previousScale=Time.timeScale;
            try
            {
                Time.timeScale=20;float start=Time.time;
                while(Time.time-start<120)
                {
                    var session=SpellExecutor.Cast(battle,caster,enemy,compiled);yield return new WaitForSeconds(.05f);
                    var runtime=battle.GetComponent<SpellExecutor>();Assert.LessOrEqual(runtime.PendingEvents,2048);Assert.LessOrEqual(root.GetComponentsInChildren<SpellProjectile>(true).Length,256);
                    if(session!=null)Assert.LessOrEqual(session.projectilesGenerated,48);
                }
                yield return new WaitForSeconds(7);
                Assert.AreEqual(0,battle.GetComponent<SpellExecutor>().PendingEvents);Assert.IsEmpty(root.GetComponentsInChildren<SpellProjectile>());
            }
            finally{Time.timeScale=previousScale;}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator ChildProjectilePoisonUsesTargetMaximumHp()
        {
            var genes=SpellExamples.Create(1);foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){p.nodes[3].parameters.multiplier=.25f;p.nodes[4].parameters.multiplier=1;}
            var caster=Unit(true,Vector2.zero);var first=Unit(false,Vector2.right);var second=Unit(false,new Vector2(2,0));
            var compiled=SpellCompiler.Compile(genes);SpellExecutor.Cast(battle,caster,first,compiled);yield return new WaitForSeconds(.6f);
            var dot=(float[])typeof(Combatant).GetField("damageOverTime",Flags).GetValue(second);
            float maxHp=(float)typeof(Combatant).GetField("maxHp",Flags).GetValue(second);
            Assert.AreEqual(maxHp*.02f,dot[(int)DebuffType.Poison],.001f);
        }
        [UnityTest]public IEnumerator ChildHitsCannotStealHealthFromTheirParentsDamage()
        {
            var genes=SpellExamples.Create(1);foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){p.nodes[4].kind=SpellNodeKind.LifeSteal;p.nodes[4].parameters.multiplier=1;}
            var caster=Unit(true,Vector2.zero);typeof(Combatant).GetField("hp",Flags).SetValue(caster,100f);
            var first=Unit(false,Vector2.right);Unit(false,new Vector2(2,0));
            SpellExecutor.Cast(battle,caster,first,SpellCompiler.Compile(genes));yield return new WaitForSeconds(.6f);
            Assert.AreEqual(100,caster.CurrentHp,.001f,"A new hit with no Damage node must not reuse the parent hit's damage for LifeSteal.");
        }
        [UnityTest]public IEnumerator ChainChoosesAnUnvisitedTargetInsteadOfAimingAtAnExcludedOne()
        {
            var caster=Unit(true,Vector2.zero);var first=Unit(false,Vector2.right);var previous=Unit(false,new Vector2(1.1f,0));var next=Unit(false,new Vector2(1.4f,0));
            var session=SpellExecutor.Cast(battle,caster,first,SpellCompiler.Compile(SpellExamples.Create(1)));
            var runtime=battle.GetComponent<SpellExecutor>();
            var context=new SpellEventContext{target=first,previous=first,previous2=previous,position=first.transform.position,multiplier=1,generation=1};
            typeof(SpellExecutor).GetMethod("Spawn",Flags).Invoke(runtime,new object[]{session,3,context});
            foreach(var projectile in root.GetComponentsInChildren<SpellProjectile>())Assert.AreSame(next,typeof(SpellProjectile).GetField("target",Flags).GetValue(projectile));
            yield return null;
        }
        [UnityTest]public IEnumerator InactiveEnemiesDoNotSatisfyEnemyNearTrigger()
        {
            var genes=SpellExamples.Create(2);foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB})p.nodes[0].parameters.radius=1;
            var caster=Unit(true,Vector2.zero,genes,true);Unit(false,new Vector2(.5f,0)).gameObject.SetActive(false);Unit(false,new Vector2(3,0));
            var cooldowns=(List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(caster);cooldowns[0]=0;
            yield return new WaitForSeconds(.2f);Assert.AreEqual(0,caster.MagicCastCount(0));
        }
        [UnityTest]public IEnumerator ExampleHomingHitExecutesDamageThenExplosion()
        {
            var caster=Unit(true,Vector2.zero);var target=Unit(false,new Vector2(1,0));var near=Unit(false,new Vector2(1,.7f));
            float before=target.CurrentHp,other=near.CurrentHp;
            var session=SpellExecutor.Cast(battle,caster,target,SpellCompiler.Compile(SpellExamples.Create(0)),7);
            yield return new WaitForSeconds(.5f);
            Assert.Less(target.CurrentHp,before);Assert.Less(near.CurrentHp,other);Assert.AreEqual(3,session.projectilesGenerated);Assert.LessOrEqual(session.eventsReserved,128);
        }
        [UnityTest]public IEnumerator SplitChildExecutesItsOwnPoisonSubtreeInsteadOfParentDamage()
        {
            var caster=Unit(true,Vector2.zero);var first=Unit(false,new Vector2(1,0));var second=Unit(false,new Vector2(2,.3f));
            var genes=SpellExamples.Create(1);var session=SpellExecutor.Cast(battle,caster,first,SpellCompiler.Compile(genes),8);
            float firstHp=first.CurrentHp;yield return new WaitForSeconds(.6f);
            Assert.Less(first.CurrentHp,firstHp);StringAssert.Contains("毒",second.StatusSummary);Assert.AreEqual(4,session.projectilesGenerated);
            StringAssert.DoesNotContain("毒",first.StatusSummary);
        }
        [UnityTest]public IEnumerator DelayExpireAndConditionalUseTheirTypedContexts()
        {
            var genes=SpellExamples.Create(0);var p=genes.chromosomeA;p.nodes[1].parameters.speed=0;p.nodes[1].parameters.seconds=.1f;p.nodes[1].parameters.count=1;
            p.nodes[1].edges.Clear();var delay=SpellProgramBuilder.Add(p,SpellNodeKind.Delay,SpellContext.Expire);delay.parameters.seconds=.1f;p.nodes[1].edges.Add(new SpellEdge(SpellPort.OnExpire,delay.id));
            var explosion=SpellProgramBuilder.Add(p,SpellNodeKind.Explosion,SpellContext.Expire);explosion.parameters.radius=3;delay.edges.Add(new SpellEdge(SpellPort.Next,explosion.id));
            // Remove the former unreachable OnHit subtree and reindex the complete tree.
            p=SpellGenetics.Replace(p,int.MinValue,null,0);genes.chromosomeA=p;genes.chromosomeB=p.Clone();
            var caster=Unit(true,Vector2.zero);var target=Unit(false,new Vector2(1,0));float hp=target.CurrentHp;
            SpellExecutor.Cast(battle,caster,target,SpellCompiler.Compile(genes));yield return new WaitForSeconds(.05f);Assert.AreEqual(hp,target.CurrentHp);
            yield return new WaitForSeconds(.4f);Assert.Less(target.CurrentHp,hp);
        }
        [UnityTest]public IEnumerator KillPortAndSharedResourceLimitsPreventRunawayGeneration()
        {
            var genes=SpellExamples.Create(2);foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB})
            {
                p.nodes[2].parameters.multiplier=5;p.nodes[3].parameters.count=48;
                p.nodes[2].edges[0].port=SpellPort.OnKill;p.nodes[3].input=SpellContext.Kill;
            }
            var caster=Unit(true,Vector2.zero);var target=Unit(false,new Vector2(1,0));typeof(Combatant).GetField("hp",Flags).SetValue(target,1f);
            Unit(false,new Vector2(2,0));var session=SpellExecutor.Cast(battle,caster,target,SpellCompiler.Compile(genes));
            yield return new WaitForSeconds(.5f);
            Assert.IsFalse(target.IsAlive);Assert.LessOrEqual(session.projectilesGenerated,48);Assert.Greater(session.projectilesGenerated,1);Assert.LessOrEqual(session.eventsReserved,128);
        }
        [UnityTest]public IEnumerator AiCastsEachUsableProgramSlotIndependentlyAndSpendsActualMana()
        {
            var genes=SpellExamples.Create(0);var caster=Unit(true,Vector2.zero,genes,true);var target=Unit(false,new Vector2(1,0));
            var data=new UnitSaveData{id="gp-multi",rank="S",baseStats=new float[]{10000,10000,10,20,0,0,2,1,15,0},magicGenomeJson=new[]{JsonUtility.ToJson(new MagicGenome{programGenes=genes}),JsonUtility.ToJson(new MagicGenome{programGenes=SpellExamples.Create(1)})}};
            typeof(Combatant).GetMethod("Configure",Flags).Invoke(caster,new object[]{data,null,null});
            var cooldowns=(List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(caster);for(int i=0;i<cooldowns.Count;i++)cooldowns[i]=0;
            float mana=caster.CurrentMp,cost=caster.Magics[0].mpCost+caster.Magics[1].mpCost;
            yield return null;yield return null;
            Assert.AreEqual(1,caster.MagicCastCount(0));Assert.AreEqual(1,caster.MagicCastCount(1));Assert.AreEqual(mana-cost,caster.CurrentMp,.01f);
        }
        [UnityTest]public IEnumerator ProgramHealTargetsOnlyAlliesAndPauseStopsExecution()
        {
            var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly,area:true);
            var caster=Unit(true,Vector2.zero);var ally=Unit(true,new Vector2(1,0));var enemy=Unit(false,new Vector2(1,.5f));
            typeof(Combatant).GetField("hp",Flags).SetValue(ally,100f);float hp=ally.CurrentHp,enemyHp=enemy.CurrentHp;
            Time.timeScale=0;var session=SpellExecutor.Cast(battle,caster,ally,SpellCompiler.Compile(genes));yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(hp,ally.CurrentHp);
            Time.timeScale=1;yield return new WaitForSeconds(.15f);Assert.Greater(ally.CurrentHp,hp);Assert.AreEqual(enemyHp,enemy.CurrentHp);Assert.LessOrEqual(session.eventsReserved,128);
        }
        [UnityTest]public IEnumerator SelfCenteredDamageAreaKeepsEnemyTeamAndDoesNotHitCaster()
        {
            var genes=SpellProgramBuilder.Projectile(area:true,selfCentered:true);
            var caster=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(1,0));float casterHp=caster.CurrentHp,enemyHp=enemy.CurrentHp;
            SpellExecutor.Cast(battle,caster,enemy,SpellCompiler.Compile(genes));yield return new WaitForSeconds(.1f);
            Assert.AreEqual(casterHp,caster.CurrentHp);Assert.Less(enemy.CurrentHp,enemyHp);
        }
        [UnityTest]public IEnumerator ConditionalBranchesUseHealthAndRecastingDoesNotRecompileGenes()
        {
            var genes=SpellExamples.Create(0);var p=genes.chromosomeA;var shot=p.nodes[1];
            var branch=SpellProgramBuilder.Add(p,SpellNodeKind.Conditional,SpellContext.Hit);branch.parameters.condition=SpellCondition.CasterLowHealth;branch.parameters.threshold=.35f;
            shot.edges[0].child=branch.id;branch.edges.Add(new SpellEdge(SpellPort.True,2));
            var status=SpellProgramBuilder.Add(p,SpellNodeKind.ApplyStatus,SpellContext.Hit);status.parameters.status=DebuffType.Silence;branch.edges.Add(new SpellEdge(SpellPort.False,status.id));
            genes.chromosomeB=p.Clone();var compiled=SpellCompiler.Compile(genes);
            var caster=Unit(true,Vector2.zero);var enemy=Unit(false,new Vector2(1,0));float hp=enemy.CurrentHp;
            SpellExecutor.Cast(battle,caster,enemy,compiled,1);yield return new WaitForSeconds(.35f);Assert.AreEqual(hp,enemy.CurrentHp);StringAssert.Contains("沈黙",enemy.StatusSummary);
            typeof(Combatant).GetField("hp",Flags).SetValue(caster,1f);
            SpellExecutor.Cast(battle,caster,enemy,compiled,1);yield return new WaitForSeconds(.35f);Assert.Less(enemy.CurrentHp,hp);
        }
        [UnityTest]public IEnumerator ProjectileMultiplierChangesDamageAndAreaExpiresAtItsOwnDuration()
        {
            var genes=SpellExamples.Create(0);
            foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){p.nodes[1].parameters.count=1;p.nodes[1].parameters.angle=0;p.nodes[1].parameters.multiplier=2;p.nodes[2].edges.Clear();p.nodes.RemoveAt(3);}
            var caster=Unit(true,Vector2.zero);var target=Unit(false,new Vector2(1,0));float hp=target.CurrentHp;
            var compiled=SpellCompiler.Compile(genes);SpellExecutor.Cast(battle,caster,target,compiled);yield return new WaitForSeconds(.3f);
            Assert.AreEqual((compiled.basePower+(float)typeof(Combatant).GetProperty("MagicAttack",Flags).GetValue(caster))*2,hp-target.CurrentHp,.01f);
            var area=new SpellProgram();area.regulation.splice=false;
            var trigger=SpellProgramBuilder.Add(area,SpellNodeKind.PeriodicTrigger,SpellContext.Cast);var spawn=SpellProgramBuilder.Add(area,SpellNodeKind.SpawnArea,SpellContext.Cast);spawn.parameters.seconds=.1f;spawn.parameters.radius=2;spawn.parameters.centerOnCaster=true;trigger.edges.Add(new SpellEdge(SpellPort.Next,spawn.id));
            var expire=SpellProgramBuilder.Add(area,SpellNodeKind.Explosion,SpellContext.Expire);expire.parameters.radius=2;spawn.edges.Add(new SpellEdge(SpellPort.OnExpire,expire.id));
            hp=target.CurrentHp;SpellExecutor.Cast(battle,caster,target,SpellCompiler.Compile(new SpellGenotype{chromosomeA=area,chromosomeB=area.Clone()}));
            yield return new WaitForSeconds(.25f);Assert.Less(target.CurrentHp,hp);
        }
        [UnityTest]public IEnumerator PlayerRenamePersistsProgramNameAndKeepsDormantSlots()
        {
            var ui=Object.FindAnyObjectByType<UI.GameUIController>();var field=ui.GetType().GetField("save",Flags);var original=(GameSaveData)field.GetValue(ui);
            byte[] originalFile=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            var unit=new UnitSaveData{id="gp-rename",displayName="名前の検証",rank="F",magicGenomeJson=new[]{JsonUtility.ToJson(new MagicGenome{programGenes=SpellExamples.Create(0)})}};
            SpellSlotStorage.EnsureSavedSlots(unit);string dormant=unit.magicSlotGenomeJson[4];
            var data=new GameSaveData{units=new List<UnitSaveData>{unit}};
            try
            {
                field.SetValue(ui,data);ui.GetType().GetMethod("ShowMagicRename",Flags).Invoke(ui,new object[]{unit,0});yield return null;
                Object.FindAnyObjectByType<TMPro.TMP_InputField>().text="はじまりの炎";
                GameObject.Find("Button 保存する").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
                var saved=SaveSystem.Load().units[0];MagicGenome restored;Assert.IsTrue(MagicGenome.TryParseJson(saved.magicGenomeJson[0],out restored));
                Assert.AreEqual("はじまりの炎",MagicFactory.Build(restored).displayName);Assert.AreEqual(5,saved.magicSlotGenomeJson.Length);Assert.AreEqual(dormant,saved.magicSlotGenomeJson[4]);
                Assert.IsNotNull(GameObject.Find("Magic Details"));
            }
            finally
            {
                field.SetValue(ui,original);ui.GetType().GetMethod("Navigate",Flags).Invoke(ui,new object[]{"HOME"});
                if(originalFile!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,originalFile);else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);
            }
        }
        [UnityTest]public IEnumerator ShortRangeOffensiveAndHealingProgramsApproachTheirRecipients()
        {
            var genes=SpellExamples.Create(0);foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){p.range=1;p.nodes[1].parameters.count=1;p.nodes[1].parameters.angle=0;}
            var caster=Unit(true,Vector2.zero,genes,true);Unit(false,new Vector2(3,0));
            yield return new WaitForSeconds(4);
            Assert.Greater(caster.MagicCastCount(0),0);Assert.Greater(caster.transform.position.x,.5f);
            caster.enabled=false;
            var heal=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly,area:true);
            foreach(var p in new[]{heal.chromosomeA,heal.chromosomeB})p.range=1;
            var healer=Unit(true,new Vector2(0,5),heal,true);var ally=Unit(true,new Vector2(3,5));typeof(Combatant).GetField("hp",Flags).SetValue(ally,10f);
            yield return new WaitForSeconds(4);
            Assert.Greater(healer.MagicCastCount(0),0);Assert.Greater(ally.CurrentHp,10);Assert.Greater(healer.transform.position.x,.5f);
        }
    }
}
