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
    public sealed class MagicRuntimeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private BattleDemo battle;
        private readonly List<GameObject> objects = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            while (!load.isDone) yield return null;
            var root = new GameObject("Magic regression arena");
            objects.Add(root);
            battle = root.AddComponent<BattleDemo>();
            battle.enabled = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (SpellProjectile projectile in Object.FindObjectsByType<SpellProjectile>())
                if (projectile != null) Object.Destroy(projectile.gameObject);
            foreach (GameObject go in objects) if (go != null) Object.Destroy(go);
            objects.Clear();
            yield return null;
        }

        private static object Call(object target, string method, params object[] args)
        {
            return target.GetType().GetMethod(method,Flags).Invoke(target,args);
        }
        private static T Get<T>(object target, string field) { return (T)target.GetType().GetField(field,Flags).GetValue(target); }
        private static void Set(object target, string field, object value) { target.GetType().GetField(field,Flags).SetValue(target,value); }
        private Combatant Unit(bool friendly, Vector2 position)
        {
            var unit = (Combatant)Call(battle,"CreateCombatant","Magic test unit",friendly,position,EnemyArchetype.Normal,Color.white);
            unit.enabled=false;
            objects.Add(unit.gameObject);
            return unit;
        }
        [UnityTest]
        public IEnumerator ClientPoseInterpolatesBetweenSnapshots()
        {
            Set(battle,"previewMode",true);
            var unit=Unit(true,Vector2.zero);Configure(unit);Set(unit,"maxHp",100f);
            Call(unit,"ApplyPvpPose",Vector2.zero,100f,10f,true);
            Call(unit,"ApplyPvpPose",new Vector2(2f,0),90f,5f,true);
            Assert.AreEqual(0f,unit.transform.position.x,.001f);
            Assert.AreEqual(90f,unit.CurrentHp,.001f);
            Call(unit,"SmoothPvpPose",.075f,.15f);
            Assert.AreEqual(1f,unit.transform.position.x,.001f);
            Call(unit,"SmoothPvpPose",.075f,.15f);
            Assert.AreEqual(2f,unit.transform.position.x,.001f);
            yield return null;
        }
        private void Configure(Combatant unit, params MagicGenome[] genomes)
        {
            var data = new UnitSaveData { id="magic-test",speciesId="test",rank="S",personality="Brave",
                baseStats=new float[] {100,10,10,10,0,0,2,1,10,1},magicGenomeJson=new string[genomes.Length] };
            for(int i=0;i<genomes.Length;i++) data.magicGenomeJson[i]=JsonUtility.ToJson(genomes[i]);
            Call(unit,"Configure",data,null,null);
            List<float> cooldowns=Get<List<float>>(unit,"magicCooldowns");
            for(int i=0;i<cooldowns.Count;i++)cooldowns[i]=0f;
        }
        private static SpeciesDefinition PassiveSpecies(string id,params PassiveEffect[] effects)
        {
            var species=ScriptableObject.CreateInstance<SpeciesDefinition>();species.speciesId=id;
            species.baseStats=new UnitStats {hp=1000,mp=100,physicalAttack=10,magicAttack=10,physicalDefense=0,magicDefense=0,moveSpeed=2,attackSpeed=1,searchRange=10,mpRegen=1};
            species.passive=new SpeciesPassive {id="passive-"+id,displayName="検証用",effects=effects};return species;
        }
        private void ConfigureSpecies(Combatant unit,SpeciesDefinition species,MagicGenome genome=null)
        {
            var data=new UnitSaveData {id=species.speciesId+"-unit",speciesId=species.speciesId,rank=species.rank.ToString(),personality="Brave",magicGenomeJson=genome==null?new string[0]:new[]{JsonUtility.ToJson(genome)}};
            Call(unit,"Configure",data,species,null);
            foreach(var cooldowns in new[]{Get<List<float>>(unit,"magicCooldowns")})for(int i=0;i<cooldowns.Count;i++)cooldowns[i]=0;
        }

        [UnityTest] public IEnumerator DeathPassiveRevivesOwnerAndAlliesOnlyOnce()
        {
            var species=PassiveSpecies("reviver",new PassiveEffect{type=PassiveEffectType.ReviveAlliesOnDeath,amount=1});
            try
            {
                Set(battle,"pvp",true);
                var reviver=Unit(true,Vector2.zero);ConfigureSpecies(reviver,species);
                var ally=Unit(true,Vector2.right);Configure(ally);
                var enemy=Unit(false,Vector2.left);Configure(enemy);
                Call(ally,"ApplyDebuff",DebuffType.Poison,5f,1f,1f,AttributeElement.None);
                Call(ally,"ApplyInstantDeath");Assert.IsFalse(ally.IsAlive);
                Call(reviver,"ApplyInstantDeath");
                Assert.IsTrue(reviver.IsAlive);Assert.AreEqual(reviver.MaximumHp,reviver.CurrentHp,.001f);Assert.AreEqual(reviver.MaximumMp,reviver.CurrentMp,.001f);Assert.IsTrue(ally.IsAlive);
                Assert.AreEqual(ally.MaximumHp,ally.CurrentHp,.001f);Assert.AreEqual(ally.MaximumMp,ally.CurrentMp,.001f);
                Assert.AreEqual(0f,Get<float[]>(ally,"damageOverTimeTime")[(int)DebuffType.Poison]);
                Assert.IsTrue(enemy.IsAlive);
                Call(ally,"ApplyInstantDeath");Call(reviver,"ApplyInstantDeath");
                Assert.IsFalse(ally.IsAlive,"The revival passive must activate only once per combatant per battle.");
                Assert.IsFalse(reviver.IsAlive);
                yield return null;
            }
            finally {Object.Destroy(species);}
        }
        [UnityTest] public IEnumerator PvpTracksDirectAndDamageOverTimeByAttackingSide()
        {
            Set(battle,"pvp",true);
            var source=Unit(true,Vector2.zero);Configure(source);
            var target=Unit(false,Vector2.right);Configure(target);
            float direct=(float)Call(battle,"DealDamage",source,target,10f,AttributeElement.None,false);
            Assert.AreEqual(direct,Get<double>(battle,"hostDamage"),.001);
            Call(target,"ApplyDebuffFrom",DebuffType.Poison,1f,10f,1f,AttributeElement.None,source);
            Set(target,"freezeTime",1f); // Tick poison without letting the target counterattack.
            target.enabled=true;yield return new WaitForSeconds(.2f);target.enabled=false;
            Assert.Greater(Get<double>(battle,"hostDamage"),direct);
            Assert.AreEqual(0d,Get<double>(battle,"guestDamage"));
        }

        [UnityTest] public IEnumerator OnHitInstantDeathAndPoisonUseIndependentTriggers()
        {
            var attacker=PassiveSpecies("instant-attacker",new PassiveEffect{type=PassiveEffectType.OnHitDebuff,debuff=DebuffType.Poison,amount=1},new PassiveEffect{type=PassiveEffectType.OnHitInstantDeath,amount=1});
            try
            {
                Set(battle,"pvp",true); // Exercise the real death callback without writing battle rewards to a save.
                var source=Unit(true,Vector2.zero);ConfigureSpecies(source,attacker);
                var target=Unit(false,Vector2.right);Configure(target);
                Call(battle,"DealDamage",source,target,1f,AttributeElement.None,true);
                Assert.IsFalse(target.IsAlive);Assert.AreEqual(0f,target.CurrentHp);
                Assert.AreEqual(1,battle.DefeatedCount);
                var immune=Unit(false,Vector2.right*2);Configure(immune);Set(source,"nextPassiveStatusTime",Time.time+10f);
                Call(battle,"DealDamage",source,immune,1f,AttributeElement.None,true);
                Assert.IsFalse(immune.IsAlive,"Poison cooldown must not block instant death.");
                var boss=(Combatant)Call(battle,"CreateCombatant","Boss",false,Vector2.right*3,EnemyArchetype.Boss,Color.red);objects.Add(boss.gameObject);boss.enabled=false;
                Call(battle,"DealDamage",source,boss,1f,AttributeElement.None,true);
                Assert.IsFalse(boss.IsAlive,"Instant death also applies to bosses.");Assert.AreEqual(3,battle.DefeatedCount);
                yield return null;LogAssert.NoUnexpectedReceived();
            }
            finally {Object.Destroy(attacker);}
        }
        [UnityTest] public IEnumerator PassivePoisonCanBeResistedAndIsAppliedToUnresistantTargets()
        {
            var attacker=PassiveSpecies("poison-attacker",new PassiveEffect{type=PassiveEffectType.OnHitDebuff,debuff=DebuffType.Poison,amount=1});
            try
            {
                var source=Unit(true,Vector2.zero);ConfigureSpecies(source,attacker);
                var target=Unit(false,Vector2.right);Configure(target);
                Call(battle,"DealDamage",source,target,1f,AttributeElement.None,true);
                Assert.Greater(Get<float[]>(target,"damageOverTimeTime")[(int)DebuffType.Poison],0f);
                var resistant=Unit(false,Vector2.right*2);Configure(resistant);Get<float[]>(resistant,"debuffResistance")[(int)DebuffType.Poison]=1f;Set(source,"nextPassiveStatusTime",0f);
                Call(battle,"DealDamage",source,resistant,1f,AttributeElement.None,true);
                Assert.AreEqual(0f,Get<float[]>(resistant,"damageOverTimeTime")[(int)DebuffType.Poison]);
                yield return null;LogAssert.NoUnexpectedReceived();
            }
            finally {Object.Destroy(attacker);}
        }

        

        [UnityTest]
        public IEnumerator MagicDebuffEffectsApplyTheirCorrespondingStatusesAndVisuals()
        {
            var owner=Unit(true,Vector2.zero);Configure(owner);
            for(int i=0;i<8;i++)
            {
                var target=Unit(false,Vector2.right);Configure(target);
                var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.ApplyStatus,area:true);
                foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){p.nodes[1].parameters.radius=.5f;p.nodes[2].parameters.status=(DebuffType)i;p.nodes[2].parameters.seconds=4;}
                SpellExecutor.Cast(battle,owner,target,SpellCompiler.Compile(genes));
                yield return null;yield return null;
                Assert.AreEqual(1<<i,target.GetComponentInChildren<DebuffVisual>().VisibleMask);
                target.gameObject.SetActive(false);
            }
        }

        [UnityTest]
        public IEnumerator EveryDebuffShowsItsOwnEffectAndFadesAfterExpiry()
        {
            var targets=new Combatant[8];
            for(int i=0;i<8;i++)
            {
                targets[i]=Unit(true,new Vector2(i*2,0));Configure(targets[i]);
                Assert.IsTrue((bool)Call(targets[i],"ApplyDebuff",(DebuffType)i,.35f,0f,1f,AttributeElement.None));
                targets[i].enabled=true;
            }
            yield return new WaitForSeconds(.07f);
            for(int i=0;i<8;i++)
            {
                var visual=targets[i].GetComponentInChildren<DebuffVisual>();
                Assert.IsNotNull(visual);Assert.AreEqual(1<<i,visual.VisibleMask);
                Assert.IsTrue(visual.GetComponent<MeshRenderer>().enabled);
                Assert.Greater(visual.GetComponent<MeshFilter>().sharedMesh.vertexCount,0);
            }
            // Shorten the fixed poison and burn timers to exercise the same expiry animation.
            Get<float[]>(targets[0],"damageOverTimeTime")[0]=.28f;
            Get<float[]>(targets[1],"damageOverTimeTime")[1]=.28f;
            yield return new WaitForSeconds(.55f);
            for(int i=0;i<8;i++)
            {
                var visual=targets[i].GetComponentInChildren<DebuffVisual>();
                Assert.AreEqual(0,visual.VisibleMask);Assert.IsFalse(visual.GetComponent<MeshRenderer>().enabled);
            }
        }

        [UnityTest]
        public IEnumerator PoisonAndBurnUseFixedMaximumHpRatesAndRefreshIndependently()
        {
            var target=Unit(true,Vector2.zero);Configure(target);
            Set(target,"maxHp",1000f);Set(target,"hp",1000f);
            Assert.IsTrue((bool)Call(target,"ApplyDebuff",DebuffType.Poison,1f,999f,1f,AttributeElement.None));
            Assert.IsTrue((bool)Call(target,"ApplyDebuff",DebuffType.Burn,1f,999f,1f,AttributeElement.None));
            var damage=Get<float[]>(target,"damageOverTime");var remaining=Get<float[]>(target,"damageOverTimeTime");
            Assert.AreEqual(20f,damage[0],.001f);Assert.AreEqual(50f,damage[1],.001f);
            Assert.AreEqual(10f,remaining[0],.001f);Assert.AreEqual(4f,remaining[1],.001f);
            remaining[0]=1f;remaining[1]=2f;
            Call(target,"ApplyDebuff",DebuffType.Poison,.1f,0f,1f,AttributeElement.None);
            Assert.AreEqual(10f,remaining[0],.001f);Assert.AreEqual(2f,remaining[1],.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlappingDebuffsReuseOneEffectAndRejectedApplicationsCreateNothing()
        {
            Combatant target=Unit(true,Vector2.zero);Configure(target);
            Assert.IsFalse((bool)Call(target,"ApplyDebuff",DebuffType.Poison,1f,0f,0f,AttributeElement.None));
            Assert.IsNull(target.GetComponentInChildren<DebuffVisual>());
            for(int repeat=0;repeat<3;repeat++)for(int i=0;i<8;i++)
                Assert.IsTrue((bool)Call(target,"ApplyDebuff",(DebuffType)i,1f,0f,1f,AttributeElement.None));
            yield return null;yield return null;
            var effects=target.GetComponentsInChildren<DebuffVisual>();Assert.AreEqual(1,effects.Length);
            Assert.AreEqual(255,effects[0].VisibleMask);
            Call(target,"TakeDamage",100000f,AttributeElement.None,true);
            Assert.AreEqual(0,effects[0].VisibleMask);Assert.IsFalse(effects[0].GetComponent<MeshRenderer>().enabled);
        }

        [UnityTest]
        public IEnumerator DebuffEffectsPauseWithBattleAndDisappearWhenBattleEnds()
        {
            Combatant target=Unit(true,Vector2.zero);Configure(target);
            Call(target,"ApplyDebuff",DebuffType.Burn,2f,0f,1f,AttributeElement.None);
            yield return null;yield return null;
            var visual=target.GetComponentInChildren<DebuffVisual>();
            var mesh=visual.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] before=mesh.vertices;float previousScale=Time.timeScale;
            try
            {
                Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);
                CollectionAssert.AreEqual(before,mesh.vertices);
                Assert.AreEqual(2,visual.VisibleMask);
            }
            finally {Time.timeScale=previousScale;}
            Set(battle,"ended",true);
            yield return new WaitForSeconds(.25f);
            Assert.AreEqual(0,visual.VisibleMask);Assert.IsFalse(visual.GetComponent<MeshRenderer>().enabled);
        }

        [UnityTest]
        public IEnumerator SlimeWaterPassiveAffectsDamageResistanceAndCompiledManaCost()
        {
            SpeciesDefinition slime=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById("demo_00");string master=JsonUtility.ToJson(slime);
            Combatant source=Unit(true,Vector2.zero),victim=Unit(false,new Vector2(2,0));
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(AttributeElement.Water)},0);ConfigureSpecies(source,slime,genome);Call(victim,"ApplySpeciesCombatTraits",slime);
            float before=Get<float>(victim,"hp");float expected=ResistanceCalculator.ApplyDefense(20*1.2f,Get<float>(victim,"magicDefense"))*(1-SpeciesPassiveCalculator.ElementReduction(slime,AttributeElement.Water));
            float dealt=(float)Call(battle,"DealDamage",source,victim,20f,AttributeElement.Water,true);
            Assert.AreEqual(expected,dealt,.001f);Assert.AreEqual(before-expected,Get<float>(victim,"hp"),.001f);
            Assert.AreEqual(MagicFactory.Build(genome).mpCost*.8f,source.Magics[0].mpCost,.001f);
            Assert.AreEqual(master,JsonUtility.ToJson(slime));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator DiscountIsUsedBeforeAiChecksWhetherItCanAffordMagic()
        {
            SpeciesDefinition slime=Resources.Load<SpeciesDatabase>("SpeciesDatabase").GetById("demo_00");
            Combatant source=Unit(true,Vector2.zero);Unit(false,new Vector2(2,0));var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(AttributeElement.Water)},0);ConfigureSpecies(source,slime,genome);
            float available=MagicFactory.Build(genome).mpCost*.9f;Set(source,"mana",available);Set(source,"manaRegen",0f);Call(source,"Update");
            Assert.AreEqual(1,source.MagicCastCount(0));Assert.Less(Get<float>(source,"mana"),available);
            yield return null;LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator PassiveHealingAndRegenerationUseTheSameStatsAsTheDetailsScreen()
        {
            SpeciesDefinition healer=PassiveSpecies("innate-healer",new PassiveEffect {type=PassiveEffectType.HealingDone,amount=.25f});
            SpeciesDefinition receiver=PassiveSpecies("innate-receiver",new PassiveEffect {type=PassiveEffectType.HealingReceived,amount=.2f},new PassiveEffect {type=PassiveEffectType.HpRegeneration,amount=.01f},new PassiveEffect {type=PassiveEffectType.StatBonus,statIndex=0,amount=.15f});
            try
            {
                Combatant source=Unit(true,Vector2.zero),target=Unit(true,Vector2.one);ConfigureSpecies(source,healer);ConfigureSpecies(target,receiver);
                Assert.AreEqual(UnitStatsCalculator.Calculate(receiver,target.UnitData).hp,Get<float>(target,"maxHp"),.001f);
                Set(target,"hp",10f);Call(target,"Heal",10f);Assert.AreEqual(22f,Get<float>(target,"hp"),.001f);
                Call(target,"Heal",10*(float)source.GetType().GetProperty("HealingDoneMultiplier",Flags).GetValue(source));Assert.AreEqual(37f,Get<float>(target,"hp"),.001f);
                Set(target,"passiveRegenerationTime",1f);float expected=37+Get<float>(target,"maxHp")*.01f*(1+Time.deltaTime)*1.2f;
                Call(target,"Update");Assert.AreEqual(expected,Get<float>(target,"hp"),.01f);
                yield return null;LogAssert.NoUnexpectedReceived();
            }
            finally {Object.Destroy(healer);Object.Destroy(receiver);}
        }
        [UnityTest]
        public IEnumerator PassiveAbsorptionProtectionAndOnHitCooldownAreBounded()
        {
            SpeciesDefinition attacker=PassiveSpecies("innate-attacker",new PassiveEffect {type=PassiveEffectType.LifeSteal,amount=.08f},new PassiveEffect {type=PassiveEffectType.LowHpDamage,amount=.25f},new PassiveEffect {type=PassiveEffectType.OnHitDebuff,debuff=DebuffType.Freeze,amount=1});
            SpeciesDefinition defender=PassiveSpecies("innate-defender",new PassiveEffect {type=PassiveEffectType.LowHpProtection,amount=.25f});
            try
            {
                Combatant source=Unit(true,Vector2.zero),victim=Unit(false,new Vector2(2,0));ConfigureSpecies(source,attacker);Set(source,"hp",10f);
                float victimHp=Get<float>(victim,"hp");float actual=(float)Call(battle,"DealDamage",source,victim,1000f,AttributeElement.None,true);
                Assert.AreEqual(victimHp,actual);Assert.AreEqual(10+actual*.08f,Get<float>(source,"hp"),.001f);
                Combatant first=Unit(false,new Vector2(3,0)),second=Unit(false,new Vector2(4,0));Set(source,"nextPassiveStatusTime",0f);
                Assert.AreEqual(1.25f,(float)Call(battle,"DealDamage",source,first,1f,AttributeElement.None,true),.001f);Assert.Greater(Get<float>(first,"freezeTime"),0);
                Call(battle,"DealDamage",source,second,1f,AttributeElement.None,true);Assert.AreEqual(0,Get<float>(second,"freezeTime"));
                ConfigureSpecies(second,defender);Set(second,"hp",10f);Assert.AreEqual(7.5f,(float)Call(second,"TakeDamage",10f,AttributeElement.None,true),.001f);
                yield return null;LogAssert.NoUnexpectedReceived();
            }
            finally {Object.Destroy(attacker);Object.Destroy(defender);}
        }

        

        

        [UnityTest]
        public IEnumerator ManaCostIsUnmodifiedAndUnaffordableMagicDoesNotBlockCheaperMagic()
        {
            var owner=Unit(true,Vector2.zero);Unit(false,Vector2.right);
            var expensive=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(power:500,count:24)},0);
            var cheap=DemoDataFactory.DemoGenome(0);Configure(owner,expensive,cheap);
            float cost=MagicFactory.Build(cheap).mpCost;Set(owner,"maxMana",cost);Set(owner,"mana",cost);Set(owner,"manaRegen",0f);
            Assert.AreEqual(MagicFactory.Build(expensive).mpCost,owner.Magics[0].mpCost);
            Call(owner,"Update");Assert.AreEqual(0,owner.MagicCastCount(0));Assert.AreEqual(1,owner.MagicCastCount(1));
            yield return null;
        }

        

        [UnityTest]
        public IEnumerator AbsorptionUsesActualDamageAndEnemiesReceiveSpeciesManaAndResistances()
        {
            var source=Unit(true,Vector2.zero);var target=Unit(false,Vector2.right);
            Configure(source);Configure(target);Set(source,"hp",1f);Set(target,"hp",5f);
            var genes=SpellProgramBuilder.Projectile(power:100,area:true);
            foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB})
            {var steal=SpellProgramBuilder.Add(p,SpellNodeKind.LifeSteal,SpellContext.Hit);steal.parameters.multiplier=.5f;p.nodes[2].edges.Add(new SpellEdge(SpellPort.Next,steal.id));p.nodes[1].parameters.seconds=.1f;}
            SpellExecutor.Cast(battle,source,target,SpellCompiler.Compile(genes));yield return null;yield return null;
            Assert.AreEqual(3.5f,source.CurrentHp,.001f);
        }

        [UnityTest]
        public IEnumerator FreezeStopsMovementAndBurnDealsMaximumHpDamage()
        {
            Unit(true,Vector2.zero);
            Combatant target=Unit(false,new Vector2(5,0));
            Get<float[]>(target,"elementReduction")[(int)AttributeElement.Fire]=.5f;
            Call(target,"ApplyDebuff",DebuffType.Burn,.1f,10f,1f,AttributeElement.Fire);
            Call(target,"ApplyDebuff",DebuffType.Freeze,2f,0f,1f,AttributeElement.Water);
            float before=Get<float>(target,"hp");
            Vector2 position=target.GetComponent<Rigidbody2D>().position;
            float tick=Mathf.Min(4f,Time.deltaTime);
            Call(target,"Update");
            Assert.AreEqual(before-Get<float>(target,"maxHp")*.05f*tick,Get<float>(target,"hp"),.001f);
            Assert.AreEqual(position,target.GetComponent<Rigidbody2D>().position);
            Assert.Greater(Get<float>(target,"freezeTime"),0f);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SupportProjectilePassesEnemiesAndHealsOnlyItsOwnTeam()
        {
            var owner=Unit(true,Vector2.zero);Configure(owner);var enemy=Unit(false,Vector2.right);Configure(enemy);
            var ally=Unit(true,Vector2.right*2);Configure(ally);Set(ally,"hp",1f);float enemyHp=enemy.CurrentHp;
            var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly,trajectory:SpellTrajectory.Homing);
            SpellExecutor.Cast(battle,owner,ally,SpellCompiler.Compile(genes));yield return new WaitForSeconds(.6f);
            Assert.Greater(ally.CurrentHp,1);Assert.AreEqual(enemyHp,enemy.CurrentHp);
        }

        

        [UnityTest]
        public IEnumerator SupportAreaHealsAndBuffsAlliesWithoutHarmingEitherTeam()
        {
            var owner=Unit(true,Vector2.zero);Configure(owner);var ally=Unit(true,Vector2.right);Configure(ally);Set(ally,"hp",1f);
            var enemy=Unit(false,Vector2.right);Configure(enemy);float hp=enemy.CurrentHp;
            var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly,area:true);
            foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){var buff=SpellProgramBuilder.Add(p,SpellNodeKind.ApplyBuff,SpellContext.Hit);buff.parameters.multiplier=.3f;p.nodes[2].edges.Add(new SpellEdge(SpellPort.Next,buff.id));}
            SpellExecutor.Cast(battle,owner,ally,SpellCompiler.Compile(genes));yield return null;yield return null;
            Assert.Greater(ally.CurrentHp,1);Assert.AreEqual(hp,enemy.CurrentHp);Assert.AreEqual(1.3f,(float)ally.GetType().GetProperty("OutgoingDamageMultiplier",Flags).GetValue(ally),.001f);
        }

        [UnityTest]
        public IEnumerator HealingAiWorksWithoutEnemiesAndDoesNotSpendManaOnFullHealthAllies()
        {
            var owner=Unit(true,Vector2.zero);var ally=Unit(true,Vector2.right);Configure(ally);
            var magic=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly,area:true)},0);
            Configure(owner,magic);Set(owner,"maxMana",100f);Set(owner,"mana",100f);Set(owner,"manaRegen",0f);Call(owner,"Update");Assert.AreEqual(0,owner.MagicCastCount(0));
            Set(ally,"hp",1f);Call(owner,"Update");Assert.AreEqual(1,owner.MagicCastCount(0));
            yield return null;yield return null;Assert.Greater(ally.CurrentHp,1);
        }
    }
}
