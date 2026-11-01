using System.Collections;
using System.Collections.Generic;
using System.IO;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class DungeonBalancePlayModeTests
    {
        [UnityTest] public IEnumerator StarterPartyCanFightAnEasyStage()
        {
            byte[] original=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            float previousSpeed=Time.timeScale;
            var previousRandom=Random.state;
            var oldDifficulty=BattleStage.Difficulty;bool oldRush=BattleStage.BossRush;
            try
            {
                var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
                var save=SaveSystem.CreateStarterData(db);
                Assert.IsTrue(SaveSystem.Save(save,out _));
                BattleStage.Select(StageDifficulty.Easy,false);Random.InitState(274);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=Object.FindAnyObjectByType<BattleDemo>();Assert.IsNotNull(battle);
                Time.timeScale=3f;
                while(!battle.HasEnded && battle.ElapsedSeconds<120f)yield return null;
                int alive=0;foreach(var ally in battle.Allies)if(ally!=null && ally.IsAlive)alive++;
                Debug.Log("Dungeon balance: easy seed=274 elapsed="+battle.ElapsedSeconds.ToString("F1")+" kills="+battle.DefeatedCount+" alive="+alive+" victory="+battle.WasVictory+" eggs="+battle.EggsEarned);
                Assert.Greater(battle.DefeatedCount,0);
                Assert.GreaterOrEqual(battle.ElapsedSeconds,30f,"A starter party should have time to engage instead of being eliminated immediately.");
            }
            finally
            {
                Time.timeScale=previousSpeed;Random.state=previousRandom;BattleStage.Select(oldDifficulty,oldRush);
                if(original==null){if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);}else File.WriteAllBytes(SaveSystem.SavePath,original);
            }
        }
    }
}
