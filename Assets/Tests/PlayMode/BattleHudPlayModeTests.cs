using System.Collections;
using System.IO;
using System.Reflection;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FusionAnyMonster.Tests
{
    public sealed class BattleHudPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnityTest]
        public IEnumerator OverlappingCharacterFadesHudAndMovingAwayRestoresIt()
        {
            var oldDifficulty=BattleStage.Difficulty;bool oldRush=BattleStage.BossRush;
            try
            {
                BattleStage.Select(StageDifficulty.Easy,false);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=Object.FindAnyObjectByType<BattleDemo>();var hud=Object.FindAnyObjectByType<BattleHud>();var camera=Camera.main;
                Assert.NotNull(camera);Assert.Greater(battle.Allies.Count,0);
                battle.enabled=false;foreach(var unit in battle.Allies)if(unit!=null)unit.gameObject.SetActive(false);
                foreach(var unit in battle.Enemies)if(unit!=null)unit.gameObject.SetActive(false);
                var character=battle.Allies[0];character.gameObject.SetActive(true);character.enabled=false;
                var panel=GameObject.Find("Battle Overview").GetComponent<RectTransform>();var group=panel.GetComponent<CanvasGroup>();
                var corners=new Vector3[4];panel.GetWorldCorners(corners);
                Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,(corners[0]+corners[2])*.5f);
                float z=camera.WorldToScreenPoint(character.transform.position).z;
                character.transform.position=camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,z));
                typeof(BattleHud).GetMethod("UpdateOccludedPanels",Flags).Invoke(hud,new object[]{camera,1f});
                Assert.AreEqual(.35f,group.alpha,.001f);
                character.transform.position=camera.ScreenToWorldPoint(new Vector3(Screen.width*.5f,Screen.height*.5f,z));
                typeof(BattleHud).GetMethod("UpdateOccludedPanels",Flags).Invoke(hud,new object[]{camera,1f});
                Assert.AreEqual(1f,group.alpha,.001f);
            }
            finally {BattleStage.Select(oldDifficulty,oldRush);Time.timeScale=1;}
        }
        [UnityTest]
        public IEnumerator PauseMenuReturnsHomeAndSavesEarnedProgress()
        {
            byte[] backup=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            var oldDifficulty=BattleStage.Difficulty;bool oldRush=BattleStage.BossRush;
            try
            {
                var data=SaveSystem.CreateStarterData(Resources.Load<SpeciesDatabase>("SpeciesDatabase"));
                Assert.IsTrue(SaveSystem.Save(data,out string error),error);
                BattleStage.Select(StageDifficulty.Easy,false);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=Object.FindAnyObjectByType<BattleDemo>();var hud=Object.FindAnyObjectByType<BattleHud>();
                var unit=battle.Allies[0];string unitId=unit.UnitData.id;unit.UnitData.level=2;unit.UnitData.experience=17;
                hud.TogglePause();Assert.AreEqual(0,Time.timeScale);
                Assert.NotNull(GameObject.Find("Button 再開"));
                GameObject.Find("Button ホームに戻る").GetComponent<Button>().onClick.Invoke();
                for(int i=0;i<10 && SceneManager.GetActiveScene().name!="Title";i++)yield return null;
                Assert.AreEqual("Title",SceneManager.GetActiveScene().name);Assert.AreEqual(1,Time.timeScale);
                var saved=SaveSystem.Load().units.Find(u=>u.id==unitId);
                Assert.NotNull(saved);Assert.AreEqual(2,saved.level);Assert.AreEqual(17,saved.experience);
            }
            finally
            {
                BattleStage.Select(oldDifficulty,oldRush);Time.timeScale=1;
                if(backup!=null)File.WriteAllBytes(SaveSystem.SavePath,backup);else if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);
            }
        }
        [UnityTest]
        public IEnumerator ExpeditionIntroShowsPartyAndKeepsCombatStoppedUntilItFinishes()
        {
            var oldDifficulty=BattleStage.Difficulty;bool oldRush=BattleStage.BossRush;
            try
            {
                BattleStage.Select(StageDifficulty.Easy,false);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=Object.FindAnyObjectByType<BattleDemo>();Assert.NotNull(battle);
                Assert.NotNull(GameObject.Find("Expedition Intro"));
                Assert.NotNull(GameObject.Find("Intro stage"));
                yield return new WaitForSecondsRealtime(.5f);
                Assert.AreEqual(0,battle.ElapsedSeconds,.001f,"Combat must wait for the intro.");
                yield return new WaitForSecondsRealtime(2.4f);
                Assert.IsNull(GameObject.Find("Expedition Intro"));
                Assert.Greater(battle.ElapsedSeconds,0,"Combat must resume after the intro.");
            }
            finally {BattleStage.Select(oldDifficulty,oldRush);Time.timeScale=1;}
        }
        [UnityTest]
        public IEnumerator FeedbackPauseResultAndSceneCleanupWorkWithoutChangingPlayerSave()
        {
            byte[] backup=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                GameSaveData temporary=SaveSystem.Load();if(temporary.units.Count==0)temporary=SaveSystem.CreateDemoData();
                for(int i=0;i<temporary.units.Count;i++)temporary.units[i].deployed=i<3;
                Assert.IsTrue(SaveSystem.Save(temporary,out _));
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=Object.FindAnyObjectByType<BattleDemo>();var hud=Object.FindAnyObjectByType<BattleHud>();
                Assert.IsNotNull(hud);Assert.IsNotNull(GameObject.Find("Party Status"));
                battle.enabled=false;foreach(Combatant unit in Object.FindObjectsByType<Combatant>())unit.enabled=false;
                battle.StopAllCoroutines();foreach(FusionAnyMonster.Spells.SpellProjectile projectile in Object.FindObjectsByType<FusionAnyMonster.Spells.SpellProjectile>())Object.Destroy(projectile.gameObject);
                yield return null;
                Combatant ally=battle.Allies[0];
                typeof(Combatant).GetMethod("TakeDamage",Flags).Invoke(ally,new object[]{10f,AttributeElement.None,true});
                typeof(Combatant).GetMethod("Heal",Flags).Invoke(ally,new object[]{5f});
                typeof(Combatant).GetMethod("DrainMana",Flags).Invoke(ally,new object[]{5f});
                typeof(Combatant).GetMethod("RestoreMana",Flags).Invoke(ally,new object[]{5f});
                Assert.GreaterOrEqual(hud.ActiveFloatingLabels,3);
                if(ally.Magics.Count>0)
                {
                    var magic=ally.Magics[0];string originalName=magic.displayName;magic.displayName="検証の魔法";
                    typeof(BattleHud).GetMethod("RefreshSpells",Flags).Invoke(hud,null);
                    var spellText=(TMP_Text)typeof(BattleHud).GetField("spellText",Flags).GetValue(hud);StringAssert.Contains("検証の魔法",spellText.text);
                    var cooldowns=(System.Collections.Generic.List<float>)typeof(Combatant).GetField("magicCooldowns",Flags).GetValue(ally);
                    float originalCooldown=cooldowns[0];cooldowns[0]=2.1f;
                    typeof(BattleHud).GetMethod("RefreshSpells",Flags).Invoke(hud,null);
                    StringAssert.Contains("待機 あと3秒",spellText.text);
                    cooldowns[0]=0f;typeof(BattleHud).GetMethod("RefreshSpells",Flags).Invoke(hud,null);
                    StringAssert.DoesNotContain("待機 あと3秒",spellText.text);cooldowns[0]=originalCooldown;
                    magic.available=false;typeof(BattleHud).GetMethod("RefreshSpells",Flags).Invoke(hud,null);StringAssert.Contains("使用不可",spellText.text);
                    magic.available=true;magic.displayName=originalName;
                }
                for(int i=0;i<100;i++)hud.Emit(ally,CombatFeedbackKind.Status,0,"通知"+i);
                Assert.LessOrEqual(hud.ActiveFloatingLabels,BattleHud.MaximumFloatingLabels);
                battle.ChangeSpeed(2);hud.TogglePause();Assert.AreEqual(0,Time.timeScale);Assert.IsTrue(hud.IsPaused);
                int count=hud.ActiveFloatingLabels;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(count,hud.ActiveFloatingLabels,"Paused combat numbers must not fade away.");
                hud.TogglePause();Assert.AreEqual(2,Time.timeScale);Assert.IsFalse(hud.IsPaused);
                yield return new WaitForSecondsRealtime(1.2f);Assert.AreEqual(0,hud.ActiveFloatingLabels);
                typeof(BattleDemo).GetMethod("End",Flags).Invoke(battle,new object[]{"VICTORY"});
                yield return new WaitForSecondsRealtime(.2f);
                Assert.IsNotNull(GameObject.Find("Battle Modal"));
                StringAssert.Contains("遠征成功",GameObject.Find("Modal title").GetComponent<TMP_Text>().text);
                GameObject.Find("Battle Modal").GetComponentInChildren<Button>().onClick.Invoke();battle.enabled=true;
                for(int i=0;i<5;i++)yield return null;
                Assert.AreEqual("Title",SceneManager.GetActiveScene().name);Assert.IsNull(Object.FindAnyObjectByType<BattleHud>());
                Assert.AreEqual(1,Time.timeScale);LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Time.timeScale=1;
                if(backup!=null)File.WriteAllBytes(SaveSystem.SavePath,backup);else if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);
            }
        }
    }
}
