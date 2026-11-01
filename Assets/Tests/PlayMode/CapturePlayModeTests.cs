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
    public sealed class CapturePlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private byte[] originalFile;private GameObject root;private BattleDemo battle;private CaptureRules rules,previousRules;private Combatant target;
        private FieldInfo currentRules=typeof(CaptureRules).GetField("current",BindingFlags.NonPublic|BindingFlags.Static);
        [UnitySetUp]public IEnumerator Setup()
        {
            originalFile=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            previousRules=(CaptureRules)currentRules.GetValue(null);rules=ScriptableObject.CreateInstance<CaptureRules>();
            rules.minimumChance=rules.maximumChance=1;currentRules.SetValue(null,rules);
            root=new GameObject("Capture tests");battle=root.AddComponent<BattleDemo>();battle.enabled=false;
            typeof(BattleDemo).GetField("save",Flags).SetValue(battle,new GameSaveData());
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0];
            target=(Combatant)typeof(BattleDemo).GetMethod("CreateCombatant",Flags).Invoke(battle,new object[]{species.displayName,false,Vector2.zero,EnemyArchetype.Normal,Color.white});
            target.transform.SetParent(root.transform,true);target.enabled=false;
            typeof(Combatant).GetMethod("SetWildRecord",Flags).Invoke(target,new object[]{WildUnitFactory.Create(species,2,123,"capture-test-enemy")});
            typeof(Combatant).GetField("hp",Flags).SetValue(target,1f);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(battle!=null){battle.enabled=false;battle.StopAllCoroutines();}
            Time.timeScale=1;currentRules.SetValue(null,previousRules);
            if(root!=null)Object.Destroy(root);if(rules!=null)Object.Destroy(rules);yield return null;
            if(originalFile!=null)File.WriteAllBytes(SaveSystem.SavePath,originalFile);else if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
        }
        [UnityTest]public IEnumerator CapturePersistsAndRemovesEnemyWithoutKillExperienceOrDuplication()
        {
            string genes=JsonUtility.ToJson(target.WildRecord);
            var result=battle.TryCapture(target);
            Assert.AreEqual(CaptureStatus.Success,result.status);Assert.IsFalse(target.IsAlive);Assert.IsFalse(target.GetComponent<Collider2D>().enabled);
            Assert.AreEqual(0,battle.DefeatedCount);Assert.AreEqual(1,battle.CapturedUnits.Count);
            Assert.AreEqual(CaptureStatus.Unavailable,battle.TryCapture(target).status);
            var loaded=SaveSystem.Load();Assert.AreEqual(1,loaded.units.Count);Assert.AreEqual(genes,JsonUtility.ToJson(loaded.units[0]));
            Assert.Contains(result.unit.speciesId,loaded.discoveredSpeciesIds);
            yield return new WaitForSecondsRealtime(.6f);Assert.IsTrue(target==null);
        }
        [UnityTest]public IEnumerator FailedAttemptLeavesEnemyAliveAndCannotBeRepeated()
        {
            rules.minimumChance=rules.maximumChance=.000001f;
            var result=battle.TryCapture(target);Assert.AreEqual(CaptureStatus.Failed,result.status);
            Assert.IsTrue(target.IsAlive);Assert.IsTrue(target.CaptureAttempted);
            Assert.AreEqual(CaptureStatus.Unavailable,battle.TryCapture(target).status);Assert.AreEqual(0,battle.CapturedUnits.Count);
            yield return null;
        }
        [UnityTest]public IEnumerator HandicapPreventsLethalDamageAndReleasesAfterFailure()
        {
            typeof(Combatant).GetField("hp",Flags).SetValue(target,target.MaximumHp);
            Assert.IsTrue(battle.FocusForCapture(target));
            typeof(Combatant).GetMethod("TakeDamage",Flags).Invoke(target,new object[]{100000f,AttributeElement.None,true});
            Assert.IsTrue(target.IsAlive);Assert.AreEqual(1,target.CurrentHp,.001f);Assert.AreEqual(0,battle.DefeatedCount);
            rules.minimumChance=rules.maximumChance=.000001f;
            Assert.AreEqual(CaptureStatus.Failed,battle.TryCapture(target).status);Assert.IsNull(battle.CaptureFocus);
            typeof(Combatant).GetMethod("TakeDamage",Flags).Invoke(target,new object[]{100000f,AttributeElement.None,true});
            Assert.IsFalse(target.IsAlive);Assert.AreEqual(1,battle.DefeatedCount);
            yield return null;
        }
        [UnityTest]public IEnumerator CaptureUiCanArmAndCancelHandicap()
        {
            var hud=root.AddComponent<BattleHud>();hud.Initialize(battle);var ui=root.GetComponent<BattleCaptureUI>();
            ui.Open();ui.Select(target);
            var button=GameObject.Find("Button 手加減して再開").GetComponent<UnityEngine.UI.Button>();button.onClick.Invoke();
            Assert.AreSame(target,battle.CaptureFocus);Assert.IsFalse(ui.IsOpen);Assert.AreEqual(1,Time.timeScale);
            ui.Open();Assert.AreSame(target,ui.Selected);button.onClick.Invoke();Assert.IsNull(battle.CaptureFocus);
            Assert.IsTrue(ui.IsOpen);ui.Close();yield return null;
        }
        [UnityTest]public IEnumerator CaptureUiPausesSelectsAndSavesWhilePaused()
        {
            var hud=root.AddComponent<BattleHud>();hud.Initialize(battle);
            var ui=root.GetComponent<BattleCaptureUI>();ui.Open();Assert.IsTrue(ui.IsOpen);Assert.IsTrue(hud.IsPaused);Assert.AreEqual(0,Time.timeScale);
            ui.Select(target);Assert.AreSame(target,ui.Selected);
            var button=GameObject.Find("Capture Panel/Button 捕獲する").GetComponent<UnityEngine.UI.Button>();Assert.IsTrue(button.interactable);
            button.onClick.Invoke();Assert.AreEqual(1,battle.CapturedUnits.Count);
            Assert.IsNull(ui.Selected);Assert.AreEqual(0,Time.timeScale);
            ui.Close();Assert.AreEqual(battle.BattleSpeed,Time.timeScale);Assert.IsFalse(hud.IsPaused);
            yield return null;
        }
        [UnityTest]public IEnumerator LiveBattleSpawnsWildIndividualsAndRendersCapturePanel()
        {
            var species=Resources.Load<SpeciesDatabase>("SpeciesDatabase").species[0];
            var starter=WildUnitFactory.Create(species,5,71,"capture-starter");starter.deployed=true;
            var data=new GameSaveData();data.units.Add(starter);data.formationUnitIds.Add(starter.id);
            Assert.IsTrue(SaveSystem.Save(data,out _));
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
            battle=Object.FindAnyObjectByType<BattleDemo>();var ui=battle.GetComponent<BattleCaptureUI>();ui.Open();
            Assert.Greater(battle.Enemies.Count,0);
            target=battle.Enemies[0];Assert.IsNotNull(target.WildRecord);Assert.AreEqual("F",target.WildRecord.rank);
            target.transform.position=new Vector3(-2,1,0);
            Assert.AreEqual(5,target.WildRecord.magicSlotGenomeJson.Length);Assert.IsNotNull(target.GetComponent<SpriteRenderer>().sprite);
            rules.minimumChance=.4f;rules.maximumChance=.95f;
            typeof(Combatant).GetField("hp",Flags).SetValue(target,target.MaximumHp*.1f);ui.Select(target);
            Canvas.ForceUpdateCanvases();yield return null;
            string folder="Temp/Verification";Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(folder+"/capture-panel.png");
            for(int i=0;i<5;i++)yield return null;
            foreach(var text in GameObject.Find("Capture Panel").GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();Assert.LessOrEqual(text.textBounds.size.y,text.rectTransform.rect.height+2,text.name);
                Assert.LessOrEqual(text.textBounds.size.x,text.rectTransform.rect.width+2,text.name);
            }
            var image=GameObject.Find("Capture portrait").GetComponent<UnityEngine.UI.Image>();Assert.AreSame(target.GetComponent<SpriteRenderer>().sprite,image.sprite);
            rules.minimumChance=rules.maximumChance=1;ui.Select(target);
            GameObject.Find("Button 捕獲する").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.AreEqual(1,battle.CapturedUnits.Count);Assert.AreEqual(2,SaveSystem.Load().units.Count);
            Assert.IsTrue(ui.IsOpen);Assert.AreEqual(0,Time.timeScale);
        }
    }
}
