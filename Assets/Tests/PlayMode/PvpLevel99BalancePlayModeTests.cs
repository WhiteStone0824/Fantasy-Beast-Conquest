using System;
using System.Collections;
using System.Reflection;
using FusionAnyMonster;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class PvpLevel99BalancePlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        [UnityTest,Timeout(900000)] public IEnumerator Level99SRankTeamsFightUsingPvpCombat()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.NotNull(db);
            var personalities=Resources.Load<PersonalityDatabase>("PersonalityDatabase");
            var previousRandom=UnityEngine.Random.state;float previousScale=Time.timeScale;
            string[][] sides={
                new[]{"species_F00_R6_A","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"species_F00_R6_A","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"species_F17_R6_A","species_F17_R6_B","species_F18_R6_A","species_F18_R6_B","species_F29_R6_A"},
                new[]{"species_F21_R6_A","species_F21_R6_B","species_F20_R6_A","species_F20_R6_B","species_F08_R6_A"},
                new[]{"species_F28_R6_A","species_F28_R6_B","species_F29_R6_A","species_F29_R6_B","species_F26_R6_A"},
                new[]{"species_F21_R6_A","species_F17_R6_A","species_F08_R6_A","species_F13_R6_A","species_F00_R6_A"},
                new[]{"special_four_lineages","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"species_F00_R6_A","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"species_F00_R6_A","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"special_four_lineages","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"special_four_lineages","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"},
                new[]{"special_four_lineages","species_F01_R6_A","species_F08_R6_A","species_F17_R6_A","species_F21_R6_A"}};
            try
            {
                Time.timeScale=1f;
                for(int match=0;match<sides.Length/2;match++)
                {
                    UnityEngine.Random.InitState(20261102+match);
                    var root=new GameObject("PvP balance simulation");var battle=root.AddComponent<BattleDemo>();battle.enabled=false;
                    typeof(BattleDemo).GetField("pvp",Flags).SetValue(battle,true);
                    var create=typeof(BattleDemo).GetMethod("CreateCombatant",Flags);
                    var configure=typeof(Combatant).GetMethod("Configure",Flags);
                    try
                    {
                        for(int side=0;side<2;side++)for(int i=0;i<5;i++)
                        {
                            var species=db.GetById(sides[match*2+side][i]);Assert.NotNull(species);Assert.AreEqual(SpeciesRank.S,species.rank);
                            var data=WildUnitFactory.Create(species,99,100+i,"simulation-"+side+"-"+i);
                            for(int stat=0;stat<10;stat++)data.individualValues[stat]=50;
                            data.personality=PersonalityType.Calm.ToString();
                            var unit=(Combatant)create.Invoke(battle,new object[]{species.displayName,side==0,new Vector2(side==0?-3f:3f,(i-2)*1.1f),EnemyArchetype.Normal,Color.white});
                            unit.transform.SetParent(root.transform,true);configure.Invoke(unit,new object[]{data,species,personalities});
                        }
                        float started=Time.time, deadline=Time.realtimeSinceStartup+360f;int left=5,right=5;
                        while(left>0 && right>0 && Time.time-started<PvpRules.TimeLimitSeconds && Time.realtimeSinceStartup<deadline)
                        {
                            yield return null;left=0;right=0;
                            foreach(var unit in battle.Allies)if(unit!=null && unit.IsAlive)left++;
                            foreach(var unit in battle.Enemies)if(unit!=null && unit.IsAlive)right++;
                        }
                        float elapsed=Time.time-started;
                        double host=(double)typeof(BattleDemo).GetField("hostDamage",Flags).GetValue(battle),guest=(double)typeof(BattleDemo).GetField("guestDamage",Flags).GetValue(battle);
                        string result=PvpRules.Resolve(left,right,host,guest);
                        var truthDetails=new System.Text.StringBuilder();
                        foreach(var unit in battle.Allies)if(unit!=null && unit.UnitData.speciesId=="special_four_lineages")truthDetails.Append(" host_truth_alive=").Append(unit.IsAlive).Append(" host_truth_revived=").Append(typeof(Combatant).GetField("revivePassiveUsed",Flags).GetValue(unit));
                        foreach(var unit in battle.Enemies)if(unit!=null && unit.UnitData.speciesId=="special_four_lineages")truthDetails.Append(" guest_truth_alive=").Append(unit.IsAlive).Append(" guest_truth_revived=").Append(typeof(Combatant).GetField("revivePassiveUsed",Flags).GetValue(unit));
                        Debug.Log("PVP_BALANCE match="+(match+1)+" simulated_seconds="+elapsed.ToString("F2")+" host_alive="+left+" guest_alive="+right+" host_damage="+host.ToString("F2")+" guest_damage="+guest.ToString("F2")+" result="+result+truthDetails);
                        Assert.IsTrue(left==0 || right==0 || elapsed>=PvpRules.TimeLimitSeconds,"Simulation timed out before combat finished.");
                    }
                    finally {UnityEngine.Object.Destroy(root);}
                    yield return null;
                }
            }
            finally {Time.timeScale=previousScale;UnityEngine.Random.state=previousRandom;}
        }
    }
}
