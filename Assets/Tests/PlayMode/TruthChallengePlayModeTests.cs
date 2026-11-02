using System.Collections;
using System.IO;
using System.Reflection;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class TruthChallengePlayModeTests
    {
        [UnityTest,Timeout(300000)]
        public IEnumerator Level99TruthFightsActualChallengeBoss()
        {
            byte[] original=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            float oldScale=Time.timeScale;var previousRandom=Random.state;
            var oldDifficulty=BattleStage.Difficulty;bool oldRush=BattleStage.BossRush;
            try
            {
                var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
                var truth=db.GetById("special_four_lineages");Assert.NotNull(truth);
                var save=SaveSystem.CreateStarterData(db);save.units.Clear();
                var record=WildUnitFactory.Create(truth,99,20261102,"truth-challenge-test");
                for(int i=0;i<record.individualValues.Length;i++)record.individualValues[i]=50;
                record.personality=PersonalityType.Calm.ToString();record.equipment=null;record.deployed=true;
                save.units.Add(record);save.formationUnitIds.Clear();save.formationUnitIds.Add(record.id);
                Assert.IsTrue(SaveSystem.Save(save,out string error),error);
                BattleStage.SelectLimited(LimitedStageKind.ChallengeBoss,SpeciesFamily.Slime);Random.InitState(20261102);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=Object.FindAnyObjectByType<BattleDemo>();Assert.NotNull(battle);
                Assert.AreEqual(1,battle.Allies.Count);Assert.AreEqual(1,battle.Enemies.Count);
                var ally=battle.Allies[0];var boss=battle.Enemies[0];
                Assert.AreEqual(99,ally.UnitData.level);Assert.AreEqual(1000,boss.UnitData.level);
                string bossName=boss.CharacterName;float initialBossHp=boss.MaximumHp,initialAllyHp=ally.MaximumHp;
                float previousBossHp=boss.CurrentHp,largestBossDrop=0f;
                Time.timeScale=3f;
                float deadline=Time.realtimeSinceStartup+180f;
                while(!battle.HasEnded && Time.realtimeSinceStartup<deadline)
                {
                    yield return null;
                    if(boss!=null){largestBossDrop=Mathf.Max(largestBossDrop,previousBossHp-boss.CurrentHp);previousBossHp=boss.CurrentHp;}
                }
                Assert.IsTrue(battle.HasEnded,"Battle did not finish within the test timeout.");
                bool revived=(bool)typeof(Combatant).GetField("revivePassiveUsed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ally);
                Debug.Log("TRUTH_CHALLENGE seed=20261102 boss="+bossName+" boss_level=1000 duration="+battle.ElapsedSeconds.ToString("F2")+" victory="+battle.WasVictory+" ally_hp="+ally.CurrentHp.ToString("F2")+"/"+initialAllyHp.ToString("F2")+" boss_hp="+(boss==null?"destroyed":boss.CurrentHp.ToString("F2"))+"/"+initialBossHp.ToString("F2")+" max_frame_boss_drop="+largestBossDrop.ToString("F2")+" revived="+revived);
            }
            finally
            {
                Time.timeScale=oldScale;Random.state=previousRandom;BattleStage.Select(oldDifficulty,oldRush);
                if(original==null){if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);}else File.WriteAllBytes(SaveSystem.SavePath,original);
            }
        }
    }
}
