#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FusionAnyMonster.Save;
using FusionAnyMonster.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FusionAnyMonster.Tests
{
    public sealed class UILayoutPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        [UnityTest]
        public IEnumerator SevenEffectPassiveDetailsAreScrollable()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();var species=Resources.Load<SpeciesDefinition>("SpecialSpecies");
            typeof(GameUIController).GetMethod("ShowSpeciesPassive",Flags).Invoke(ui,new object[]{species,(Action)(()=>{})});yield return null;
            var root=GameObject.Find("Passive Effect Scroll");Assert.NotNull(root);Assert.NotNull(root.GetComponent<ScrollRect>());
            var text=GameObject.Find("Passive Effect Details").GetComponent<TMP_Text>();Assert.AreEqual(species.passive.Description,text.text);
            Assert.AreEqual(7,species.passive.effects.Length);
            Call(ui,"Navigate","HOME");
        }
        [UnityTest]
        public IEnumerator SaveResetRequiresConfirmationAndRestoresStarterParty()
        {
            byte[] backup=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
                var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();var field=typeof(GameUIController).GetField("save",Flags);
                var previous=(GameSaveData)field.GetValue(ui);var data=SaveSystem.CreateStarterData(Resources.Load<SpeciesDatabase>("SpeciesDatabase"));
                data.battlesCompleted=42;Assert.IsTrue(SaveSystem.Save(data,out string error),error);field.SetValue(ui,data);
                try
                {
                    Call(ui,"Navigate","SETTINGS");GameObject.Find("Button セーブデータを消す").GetComponent<Button>().onClick.Invoke();yield return null;
                    Assert.NotNull(GameObject.Find("Save Reset Confirmation"));
                    GameObject.Find("Button やめる").GetComponent<Button>().onClick.Invoke();yield return null;
                    Assert.AreEqual(42,SaveSystem.Load().battlesCompleted);
                    GameObject.Find("Button セーブデータを消す").GetComponent<Button>().onClick.Invoke();yield return null;
                    GameObject.Find("Button 消して最初から").GetComponent<Button>().onClick.Invoke();yield return null;
                    var reset=SaveSystem.Load();Assert.AreEqual(0,reset.battlesCompleted);Assert.AreEqual(3,reset.units.Count);
                    Assert.IsEmpty(reset.eggs);Assert.IsEmpty(reset.equipmentInventory);Assert.Less(reset.playTimeSeconds,1d);
                    Assert.AreEqual("HOME",typeof(GameUIController).GetField("currentScreen",Flags).GetValue(ui));
                }
                finally {field.SetValue(ui,previous);Call(ui,"Navigate","HOME");}
            }
            finally
            {
                if(backup!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,backup);else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);
            }
        }
        [UnityTest]
        public IEnumerator ChallengeBossUsesLevel1000AndHasTimeLimitWithoutWaveLabel()
        {
            byte[] backup=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                var data=SaveSystem.CreateStarterData(Resources.Load<SpeciesDatabase>("SpeciesDatabase"));Assert.IsTrue(SaveSystem.Save(data,out _));
                FusionAnyMonster.Battle.BattleStage.SelectLimited(FusionAnyMonster.Battle.LimitedStageKind.ChallengeBoss,SpeciesFamily.Slime);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
                var battle=UnityEngine.Object.FindAnyObjectByType<FusionAnyMonster.Battle.BattleDemo>();var hud=UnityEngine.Object.FindAnyObjectByType<FusionAnyMonster.Battle.BattleHud>();
                Assert.AreEqual(1,battle.Enemies.Count);Assert.AreEqual(1000,battle.Enemies[0].UnitData.level);Assert.AreEqual("S",battle.Enemies[0].WildRecord.rank);
                typeof(FusionAnyMonster.Battle.BattleHud).GetMethod("Refresh",Flags).Invoke(hud,null);
                var summary=(TMP_Text)typeof(FusionAnyMonster.Battle.BattleHud).GetField("summary",Flags).GetValue(hud);
                StringAssert.DoesNotContain("ウェーブ",summary.text);StringAssert.Contains("02:00",summary.text);
                typeof(FusionAnyMonster.Battle.BattleDemo).GetProperty("IntroActive",Flags).SetValue(battle,false);
                typeof(FusionAnyMonster.Battle.BattleDemo).GetField("elapsed",Flags).SetValue(battle,120f);
                typeof(FusionAnyMonster.Battle.BattleDemo).GetMethod("Update",Flags).Invoke(battle,null);
                Assert.IsTrue(battle.HasEnded);Assert.IsFalse(battle.WasVictory);
            }
            finally
            {
                FusionAnyMonster.Battle.BattleStage.Select(FusionAnyMonster.Battle.StageDifficulty.Easy,false);
                if(backup!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,backup);else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);
            }
        }
        [UnityTest]
        public IEnumerator FarewellConfirmationCanCancelAndRemoveAnEgg()
        {
            byte[] backup=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                var data=SaveSystem.CreateStarterData(Resources.Load<SpeciesDatabase>("SpeciesDatabase"));
                var offspring=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(data.units[0]));
                data.eggs.Add(new EggSaveData{id="farewell-test",offspring=offspring});
                Assert.IsTrue(SaveSystem.Save(data,out string error),error);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
                var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"Navigate","UNITS");Call(ui,"ShowEggs");
                GameObject.Find("Button 卵とお別れ").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.NotNull(GameObject.Find("Farewell Confirmation"));
                GameObject.Find("Button やめる").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.AreEqual(1,SaveSystem.Load().eggs.Count);
                GameObject.Find("Button 卵とお別れ").GetComponent<Button>().onClick.Invoke();yield return null;
                GameObject.Find("Button お別れする").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsEmpty(SaveSystem.Load().eggs);Assert.IsNull(GameObject.Find("Egg Icon farewell-test"));
            }
            finally
            {
                if(backup!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,backup);else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);
            }
        }
        [UnityTest]
        public IEnumerator HatchAllButtonHatchesEggsAndShowsRoster()
        {
            byte[] backup=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            try
            {
                var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");var data=SaveSystem.CreateStarterData(db);
                data.eggs.Add(new EggSaveData{id="egg-a",offspring=WildUnitFactory.Create(db.GetById("demo_00"),20,1)});
                data.eggs.Add(new EggSaveData{id="egg-b",offspring=WildUnitFactory.Create(db.GetById("demo_01"),30,2)});
                Assert.IsTrue(SaveSystem.Save(data,out string error),error);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
                var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"ShowEggs");
                var button=GameObject.Find("Button すべて孵化（2個）").GetComponent<Button>();Assert.IsTrue(button.interactable);
                button.onClick.Invoke();yield return null;
                var saved=SaveSystem.Load();Assert.AreEqual(0,saved.eggs.Count);Assert.AreEqual(data.units.Count+2,saved.units.Count);
                Assert.AreEqual(1,saved.units[saved.units.Count-1].level);Assert.NotNull(GameObject.Find("Roster"));
            }
            finally
            {
                if(backup!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,backup);else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);
            }
        }
        [UnityTest]
        public IEnumerator LimitedBossStageConsumesDailyAttemptAndSpawnsThreeSBosses()
        {
            byte[] backup=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            var oldDifficulty=FusionAnyMonster.Battle.BattleStage.Difficulty;bool oldRush=FusionAnyMonster.Battle.BattleStage.BossRush;
            try
            {
                var database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
                Assert.IsTrue(SaveSystem.Save(SaveSystem.CreateStarterData(database),out string error),error);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
                var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"Navigate","HOME");Call(ui,"ShowStageSelection");
                GameObject.Find("Button イベント遠征").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.NotNull(GameObject.Find("Button 常設遠征"));Assert.NotNull(GameObject.Find("Button ボスラッシュ"));
                typeof(GameUIController).GetField("eventStageSelection",Flags).SetValue(ui,0);Call(ui,"ShowLimitedStages");yield return null;
                var todayFamily=FusionAnyMonster.Battle.LimitedStageRules.Family(DateTime.Now.DayOfWeek);
                Assert.NotNull(GameObject.Find("遠征の魔物 "+todayFamily).GetComponent<Image>().sprite);
                GameObject.Find("Event Stage 2").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.NotNull(GameObject.Find("遠征の魔物 Beast").GetComponent<Image>().sprite);
                GameObject.Find("Button この遠征に出撃").GetComponent<Button>().onClick.Invoke();yield return null;
                var battle=UnityEngine.Object.FindAnyObjectByType<FusionAnyMonster.Battle.BattleDemo>();Assert.NotNull(battle);
                Assert.AreEqual(3,battle.Enemies.Count);
                foreach(var enemy in battle.Enemies){Assert.AreEqual(FusionAnyMonster.Battle.EnemyArchetype.Boss,enemy.Type);Assert.AreEqual(99,enemy.UnitData.level);Assert.AreEqual("S",enemy.WildRecord.rank);}
                Assert.AreEqual(0,FusionAnyMonster.Battle.LimitedStageRules.Remaining(SaveSystem.Load(),FusionAnyMonster.Battle.LimitedStageKind.DailyBoss,DateTime.Now));
                Assert.AreEqual(1,FusionAnyMonster.Battle.LimitedStageRules.Remaining(SaveSystem.Load(),FusionAnyMonster.Battle.LimitedStageKind.DailyExperience,DateTime.Now));
            }
            finally
            {
                FusionAnyMonster.Battle.BattleStage.Select(oldDifficulty,oldRush);
                if(backup==null){if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);}else System.IO.File.WriteAllBytes(SaveSystem.SavePath,backup);
            }
        }
        [UnityTest]
        public IEnumerator LanBattleScreenShowsFormationAndConnectionControls()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"Navigate","HOME");
            GameObject.Find("Button 通信対戦").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.NotNull(GameObject.Find("PvP Team"));Assert.NotNull(GameObject.Find("PvP Connection"));
            Assert.NotNull(GameObject.Find("PvP Address").GetComponent<TMP_InputField>());
            Assert.NotNull(GameObject.Find("Button 部屋を作る"));Assert.NotNull(GameObject.Find("Button 参加する"));
            Assert.IsTrue(Array.Exists(ui.GetComponentsInChildren<TMP_Text>(),t=>t.text.Contains("55555")));
            GameObject.Find("Button ホームへ戻る").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.NotNull(GameObject.Find("遠征部隊パネル"));
        }
        [UnityTest]
        public IEnumerator PvpAddressCanBeClickedAndTyped()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"ShowPvp");yield return null;
            var field=GameObject.Find("PvP Address").GetComponent<TMP_InputField>();
            try
            {
                var eventSystem=EventSystem.current;Assert.NotNull(eventSystem);
                var pointer=new PointerEventData(eventSystem){position=RectTransformUtility.WorldToScreenPoint(null,field.transform.position)};
                var hits=new List<RaycastResult>();eventSystem.RaycastAll(pointer,hits);
                Assert.IsTrue(hits.Exists(hit=>hit.gameObject==field.gameObject),"The address field must be reachable by pointer raycasts.");
                eventSystem.SetSelectedGameObject(field.gameObject,pointer);
                ExecuteEvents.Execute(field.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(field.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                yield return null;yield return null;
                Assert.IsTrue(field.isFocused,"Clicking the address field must focus it.");
                var process=typeof(TMP_InputField).GetMethod("ProcessEvent",Flags);Assert.NotNull(process);
                foreach(char c in "192.168.1.10")process.Invoke(field,new object[]{new Event{type=EventType.KeyDown,character=c}});
                yield return null;yield return null;
                Assert.AreEqual("192.168.1.10",field.text);
            }
            finally {Call(ui,"Navigate","HOME");}
        }
        [UnityTest]
        public IEnumerator HomeShowsPlayerRankWithoutCalculationDetails()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"Navigate","HOME");
            var save=(GameSaveData)typeof(GameUIController).GetField("save",Flags).GetValue(ui);
            int rank=PlayerRankCalculator.Calculate(save).Total;
            var button=GameObject.Find("Button ランク "+rank);Assert.NotNull(button);
            button.GetComponent<Button>().onClick.Invoke();yield return null;
            var labels=ui.GetComponentsInChildren<TMP_Text>();
            Assert.IsTrue(Array.Exists(labels,t=>t.text==rank.ToString()));
            Assert.IsFalse(Array.Exists(labels,t=>t.text.Contains("÷") || t.text.Contains("個体値合計") || t.text.Contains("レベル合計")));
            Assert.IsTrue(Array.Exists(labels,t=>t.text.Contains("プレイ時間") && t.text.Contains("図鑑の登録数") && t.text.Contains("遠征成功回数")));
            GameObject.Find("Button ホームへ戻る").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.NotNull(GameObject.Find("遠征部隊パネル"));
        }
        [UnityTest]
        public IEnumerator EggsHideTheirContentsUntilHatched()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();
            var field=typeof(GameUIController).GetField("save",Flags);var original=(GameSaveData)field.GetValue(ui);
            var child=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(original.units[0]));
            child.displayName="秘密のたまごの中身";child.rank="S";
            var data=new GameSaveData();data.eggs.Add(new EggSaveData{id="hidden-egg",offspring=child});
            try
            {
                field.SetValue(ui,data);Call(ui,"Navigate","UNITS");Call(ui,"ShowEggs");yield return null;
                var icon=GameObject.Find("Egg Icon hidden-egg");var detail=GameObject.Find("Egg Detail");
                Assert.NotNull(icon);Assert.NotNull(detail);
                foreach(var label in icon.GetComponentsInChildren<TMP_Text>())StringAssert.DoesNotContain(child.displayName,label.text);
                foreach(var label in detail.GetComponentsInChildren<TMP_Text>())StringAssert.DoesNotContain(child.displayName,label.text);
                Assert.IsFalse(Array.Exists(detail.GetComponentsInChildren<TMP_Text>(),t=>t.text.Contains("S級")));
                var iconSprite=icon.transform.Find("キャラクターアイコン").GetComponent<Image>().sprite;
                var detailSprite=GameObject.Find("卵の姿").GetComponent<Image>().sprite;
                Assert.NotNull(iconSprite);Assert.AreSame(iconSprite,detailSprite);
                Assert.IsTrue(GameObject.Find("Button 孵化する").GetComponent<Button>().interactable);
            }
            finally {field.SetValue(ui,original);Call(ui,"Navigate","HOME");}
        }
        [UnityTest]
        public IEnumerator ExpeditionLaunchStartsTheSelectedBossRush()
        {
            byte[] savedFile=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            var previousDifficulty=FusionAnyMonster.Battle.BattleStage.Difficulty;bool previousRush=FusionAnyMonster.Battle.BattleStage.BossRush;
            try
            {
                var database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
                Assert.IsTrue(SaveSystem.Save(SaveSystem.CreateStarterData(database),out string error),error);
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
                var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();Call(ui,"Navigate","HOME");Call(ui,"ShowStageSelection");
                GameObject.Find("Button ボスラッシュ").GetComponent<Button>().onClick.Invoke();
                GameObject.Find("Stage Easy").GetComponent<Button>().onClick.Invoke();
                GameObject.Find("Button 遠征に出撃").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.AreEqual("Battle",SceneManager.GetActiveScene().name);
                Assert.IsTrue(FusionAnyMonster.Battle.BattleStage.BossRush);
                Assert.AreEqual(FusionAnyMonster.Battle.StageDifficulty.Easy,FusionAnyMonster.Battle.BattleStage.Difficulty);
            }
            finally
            {
                FusionAnyMonster.Battle.BattleStage.Select(previousDifficulty,previousRush);
                if(savedFile==null){if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);}
                else System.IO.File.WriteAllBytes(SaveSystem.SavePath,savedFile);
            }
        }
        [UnityTest]
        public IEnumerator ExpeditionSelectionShowsLockedStagesAndIndependentModes()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();
            var field=typeof(GameUIController).GetField("save",Flags);var original=(GameSaveData)field.GetValue(ui);
            var unit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(original.units[0]));unit.deployed=true;
            var data=new GameSaveData {units=new List<UnitSaveData>{unit}};
            try
            {
                field.SetValue(ui,data);typeof(GameUIController).GetField("stageSelectionRush",Flags).SetValue(ui,false);typeof(GameUIController).GetField("stageSelectionDifficulty",Flags).SetValue(ui,0);
                Call(ui,"Navigate","HOME");Call(ui,"ShowStageSelection");yield return null;
                Assert.NotNull(GameObject.Find("Expedition Route"));Assert.NotNull(GameObject.Find("Expedition Details"));
                GameObject.Find("Stage Normal").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsFalse(GameObject.Find("Button 遠征に出撃").GetComponent<Button>().interactable);
                GameObject.Find("Button ボスラッシュ").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsFalse(GameObject.Find("Button 遠征に出撃").GetComponent<Button>().interactable);
                Assert.IsTrue(System.Array.Exists(GameObject.Find("Expedition Details").GetComponentsInChildren<TMP_Text>(),t=>t.text.Contains("ランク 50")));
                GameObject.Find("Stage Easy").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsTrue(GameObject.Find("Button 遠征に出撃").GetComponent<Button>().interactable);
                GameObject.Find("Button 常設遠征").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsFalse((bool)typeof(GameUIController).GetField("stageSelectionRush",Flags).GetValue(ui));
                GameObject.Find("Button ホームへ戻る").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.NotNull(GameObject.Find("遠征部隊パネル"));
            }
            finally {field.SetValue(ui,original);Call(ui,"Navigate","HOME");}
        }
        [UnityTest]
        public IEnumerator FamilyTreeShowsParentsAndGrandparentsAndReturnsToRoster()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();
            var field=typeof(GameUIController).GetField("save",Flags);var original=(GameSaveData)field.GetValue(ui);
            var data=new GameSaveData();
            var child=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(original.units[0]));
            child.id="child";child.fatherId="a";child.motherId="b";data.units.Add(child);
            data.ancestry.Add(new AncestorRecord{id="a",displayName="親その一",fatherId="aa",motherId="ab"});
            data.ancestry.Add(new AncestorRecord{id="b",displayName="親その二",fatherId="ba",motherId="bb"});
            foreach(var id in new[]{"aa","ab","ba","bb"})data.ancestry.Add(new AncestorRecord{id=id,displayName="祖先"+id});
            try
            {
                field.SetValue(ui,data);typeof(GameUIController).GetField("selectedUnitId",Flags).SetValue(ui,child.id);
                Call(ui,"Navigate","UNITS");GameObject.Find("Button 家系図").GetComponent<Button>().onClick.Invoke();yield return null;
                foreach(var pair in new[]{new[]{"親A","親その一"},new[]{"親B","親その二"},new[]{"親Aの親A","祖先aa"},new[]{"親Aの親B","祖先ab"},new[]{"親Bの親A","祖先ba"},new[]{"親Bの親B","祖先bb"}})
                {
                    var card=GameObject.Find("Family "+pair[0]);Assert.NotNull(card);
                    Assert.IsTrue(Array.Exists(card.GetComponentsInChildren<TMP_Text>(),t=>t.text==pair[1]),pair[0]);
                }
                GameObject.Find("Button 仲間の詳細へ戻る").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.NotNull(GameObject.Find("Unit Detail"));
            }
            finally {field.SetValue(ui,original);Call(ui,"Navigate","HOME");}
        }
        [UnityTest]
        public IEnumerator UnitExperienceGaugeShowsProgressAndMaximumLevel()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();
            var field=typeof(GameUIController).GetField("save",Flags);var original=(GameSaveData)field.GetValue(ui);
            var unit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(original.units[0]));unit.level=1;unit.experience=25;
            var data=new GameSaveData();data.units.Add(unit);
            try
            {
                field.SetValue(ui,data);typeof(GameUIController).GetField("selectedUnitId",Flags).SetValue(ui,unit.id);
                Call(ui,"Navigate","UNITS");yield return null;
                var gauge=GameObject.Find("Experience Gauge");Assert.NotNull(gauge);
                Assert.AreEqual(.25f,gauge.transform.Find("Experience Gauge Fill").GetComponent<RectTransform>().anchorMax.x,.001f);
                var detail=GameObject.Find("Unit Detail");
                Assert.IsTrue(Array.Exists(detail.GetComponentsInChildren<TMP_Text>(),t=>t.text.Contains("75") && t.text.Contains("25 / 100")));
                Assert.IsTrue(Array.Exists(detail.GetComponentsInChildren<TMP_Text>(),t=>t.text.Contains("性格：")));
                unit.level=99;unit.experience=0;Call(ui,"Navigate","HOME");Call(ui,"Navigate","UNITS");yield return null;
                gauge=GameObject.Find("Experience Gauge");Assert.NotNull(gauge);
                Assert.AreEqual(1f,gauge.transform.Find("Experience Gauge Fill").GetComponent<RectTransform>().anchorMax.x,.001f);
                Assert.IsTrue(Array.Exists(GameObject.Find("Unit Detail").GetComponentsInChildren<TMP_Text>(),t=>t.text=="経験値：レベル最大"));
            }
            finally {field.SetValue(ui,original);Call(ui,"Navigate","HOME");}
        }
        [UnityTest]
        public IEnumerator RosterPagingAndHomeShortcutsKeepTheVisibleSelectionInSync()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();
            var saveField=typeof(GameUIController).GetField("save",Flags);
            var original=(GameSaveData)saveField.GetValue(ui);
            Assert.Greater(original.units.Count,0);
            var save=new GameSaveData();
            for(int i=0;i<11;i++)
            {
                var unit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(original.units[0]));
                unit.id="ui-page-"+i;unit.displayName="仲間"+i;unit.deployed=i==10;save.units.Add(unit);
            }
            try
            {
                saveField.SetValue(ui,save);
                typeof(GameUIController).GetField("rosterPage",Flags).SetValue(ui,0);
                typeof(GameUIController).GetField("selectedUnitId",Flags).SetValue(ui,save.units[0].id);
                Call(ui,"Navigate","UNITS");yield return null;
                var roster=GameObject.Find("Roster");
                var previous=roster.transform.Find("Button 前へ").GetComponent<Button>();
                var next=roster.transform.Find("Button 次へ").GetComponent<Button>();
                Assert.IsFalse(previous.interactable);Assert.IsTrue(next.interactable);
                next.onClick.Invoke();yield return null;
                Assert.AreEqual(1,typeof(GameUIController).GetField("rosterPage",Flags).GetValue(ui));
                Assert.IsNotNull(GameObject.Find("Unit Icon "+save.units[9].id));
                Assert.IsTrue(System.Array.Exists(GameObject.Find("Unit Detail").GetComponentsInChildren<TMP_Text>(), t=>t.text==save.units[9].displayName));
                roster=GameObject.Find("Roster");Assert.IsFalse(roster.transform.Find("Button 次へ").GetComponent<Button>().interactable);
                Call(ui,"Navigate","HOME");yield return null;
                GameObject.Find("出撃仲間 "+save.units[10].displayName).GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.AreEqual(1,typeof(GameUIController).GetField("rosterPage",Flags).GetValue(ui));
                Assert.IsTrue(System.Array.Exists(GameObject.Find("Unit Detail").GetComponentsInChildren<TMP_Text>(),t=>t.text==save.units[10].displayName));
                Call(ui,"Navigate","HOME");yield return null;
                GameObject.Find("Button 卵を孵化する").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsNotNull(GameObject.Find("Egg List"));
                Assert.AreEqual("UNITS",typeof(GameUIController).GetField("currentScreen",Flags).GetValue(ui));
                save.units.Clear();Call(ui,"Navigate","UNITS");yield return null;
                Assert.IsNotNull(GameObject.Find("仲間がいない状態"));
                Assert.IsNotNull(GameObject.Find("Button 卵を確認する"));
            }
            finally { saveField.SetValue(ui,original);Call(ui,"Navigate","HOME"); }
        }
        [UnityTest]
        public IEnumerator EveryManagementScreenFitsWideStandardAndSmallGameViews()
        {
            var assembly=typeof(UnityEditor.EditorWindow).Assembly;
            Type sizesType=assembly.GetType("UnityEditor.GameViewSizes"),sizeType=assembly.GetType("UnityEditor.GameViewSize"),viewType=assembly.GetType("UnityEditor.GameView");
            object sizes=sizesType.BaseType.GetProperty("instance",Flags).GetValue(null);
            object group=sizesType.GetMethod("GetGroup",Flags).Invoke(sizes,new[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
            var view=UnityEditor.EditorWindow.GetWindow(viewType);PropertyInfo selectedSize=viewType.GetProperty("selectedSizeIndex",Flags);int originalSize=(int)selectedSize.GetValue(view);
            int count=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null),added=0;
            try
            {
                yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
                var ui=UnityEngine.Object.FindAnyObjectByType<GameUIController>();
                var save=(GameSaveData)typeof(GameUIController).GetField("save",Flags).GetValue(ui);
                UnitSaveData unit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(save.units[0]));
                unit.displayName="天蓋を支える巨人";unit.level=99;unit.experience=99999999;
                unit.magicGenomeJson=new string[5];for(int i=0;i<5;i++)unit.magicGenomeJson[i]=JsonUtility.ToJson(DemoDataFactory.DemoGenome(i));
                int[,] resolutions={{960,540},{1024,768},{1920,1080},{2560,1080}};
                for(int resolution=0;resolution<resolutions.GetLength(0);resolution++)
                {
                    object fixedType=Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");
                    object size=Activator.CreateInstance(sizeType,Flags,null,new object[]{fixedType,resolutions[resolution,0],resolutions[resolution,1],"UI validation"},null);
                    group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});added++;
                    selectedSize.SetValue(view,count+added-1);view.Focus();view.Repaint();yield return null;yield return null;
                    Assert.AreEqual(resolutions[resolution,0],Screen.width);Assert.AreEqual(resolutions[resolution,1],Screen.height);
                    for(int screen=0;screen<23;screen++)
                    {
                        switch(screen)
                        {
                            case 0:Call(ui,"Navigate","HOME");break;
                            case 1:Call(ui,"Navigate","SPECIES");break;
                            case 2:var database=(SpeciesDatabase)typeof(GameUIController).GetField("speciesDatabase",Flags).GetValue(ui);typeof(GameUIController).GetField("speciesDetailPage",Flags).SetValue(ui,0);Call(ui,"ShowSpeciesDetail",database.GetById(save.units[0].speciesId));break;
                            case 3:typeof(GameUIController).GetField("unitDetailPage",Flags).SetValue(ui,0);Call(ui,"Navigate","UNITS");break;
                            case 4:Call(ui,"ShowUnitMagic",unit);break;
                            case 5:Call(ui,"ShowUnitMagicPreview",unit,0);break;
                            case 6:typeof(GameUIController).GetField("choosingParentSlot",Flags).SetValue(ui,null);Call(ui,"Navigate","BREEDING");break;
                            case 7:GameObject.Find("Parent Slot A").GetComponent<Button>().onClick.Invoke();break;
                            case 8:Call(ui,"ShowBreedingOutcome",unit);break;
                            case 9:Call(ui,"Navigate","SETTINGS");break;
                            case 10:Call(ui,"Navigate","UNITS");GameObject.Find("Button 2 耐性").GetComponent<Button>().onClick.Invoke();break;
                            case 20:Call(ui,"Navigate","UNITS");GameObject.Find("Button 3 装備").GetComponent<Button>().onClick.Invoke();break;
                            case 21:Call(ui,"Navigate","UNITS");GameObject.Find("Button 家系図").GetComponent<Button>().onClick.Invoke();break;
                            case 22:Call(ui,"Navigate","HOME");Call(ui,"ShowLimitedStages");break;
                            case 11:
                                var passiveDatabase=(SpeciesDatabase)typeof(GameUIController).GetField("speciesDatabase",Flags).GetValue(ui);
                                SpeciesDefinition longest=passiveDatabase.species[0];
                                foreach(var candidate in passiveDatabase.species)if(candidate.passive.Description.Length>longest.passive.Description.Length)longest=candidate;
                                Call(ui,"ShowSpeciesPassive",longest,(Action)(()=>{}));break;
                            case 12:
                                var resistanceDatabase=(SpeciesDatabase)typeof(GameUIController).GetField("speciesDatabase",Flags).GetValue(ui);
                                Call(ui,"ShowSpeciesDetail",resistanceDatabase.GetById(save.units[0].speciesId));
                                GameObject.Find("Button 2 耐性").GetComponent<Button>().onClick.Invoke();break;
                            case 13:
                                var offensive=DemoDataFactory.DemoGenome(2);
                                unit.magicGenomeJson[0]=JsonUtility.ToJson(offensive);Call(ui,"ShowUnitMagicDetails",unit,0);break;
                            case 14:
                                var supportive=Spells.SpellSlotStorage.Prepare(new MagicGenome{programGenes=Spells.SpellProgramBuilder.Projectile(effect:Spells.SpellNodeKind.Heal,target:Spells.SpellTarget.LowestHealthAlly,area:true)},0);
                                unit.magicGenomeJson[0]=JsonUtility.ToJson(supportive);Call(ui,"ShowUnitMagicDetails",unit,0);break;
                            case 15:Call(ui,"ShowMagicRename",unit,0);break;
                            case 16:
                                unit.magicGenomeJson[0]=JsonUtility.ToJson(new MagicGenome{programGenes=Spells.SpellExamples.Create(1)});Call(ui,"ShowUnitMagicDetails",unit,0);break;
                            case 17:Call(ui,"ShowUnitMagicPreview",unit,0);break;
                            case 18:Call(ui,"Navigate","HOME");Call(ui,"ShowStageSelection");break;
                            case 19:Call(ui,"Navigate","HOME");Call(ui,"ShowPvp");break;
                        }
                        yield return null;Canvas.ForceUpdateCanvases();
                        var problems=new List<string>();
                        foreach(TMP_Text label in ui.GetComponentsInChildren<TMP_Text>())
                        {
                            if(!label.gameObject.activeInHierarchy || string.IsNullOrEmpty(label.text))continue;
                            label.ForceMeshUpdate();
                            if(label.isTextOverflowing || label.rectTransform.rect.width<=0 || label.rectTransform.rect.height<=0)problems.Add(label.text);
                        }
                        foreach(Image image in ui.GetComponentsInChildren<Image>())
                        {
                            if(!image.gameObject.activeInHierarchy || !image.enabled)continue;
                            var parent=image.transform.parent as RectTransform;if(parent==null)continue;
                            Rect outside=WorldRect(image.rectTransform),inside=WorldRect(parent);
                            if(outside.xMin<inside.xMin-2 || outside.xMax>inside.xMax+2 || outside.yMin<inside.yMin-2 || outside.yMax>inside.yMax+2)problems.Add("Image outside parent: "+image.name);
                        }
                        Button[] buttons=ui.GetComponentsInChildren<Button>();
                        for(int first=0;first<buttons.Length;first++)for(int second=first+1;second<buttons.Length;second++)
                        {
                            if(!buttons[first].gameObject.activeInHierarchy || !buttons[second].gameObject.activeInHierarchy || buttons[first].transform.IsChildOf(buttons[second].transform) || buttons[second].transform.IsChildOf(buttons[first].transform))continue;
                            Rect a=WorldRect(buttons[first].GetComponent<RectTransform>()),b=WorldRect(buttons[second].GetComponent<RectTransform>());
                            if(Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)>2 && Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin)>2)problems.Add("Overlapping buttons: "+buttons[first].name+" / "+buttons[second].name);
                        }
                        Assert.IsEmpty(problems,"Screen "+screen+" at "+Screen.width+"x"+Screen.height+": "+string.Join(" / ",problems));
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                selectedSize.SetValue(view,originalSize);
                for(int i=added-1;i>=0;i--)
                {
                    group.GetType().GetMethod("RemoveCustomSize",Flags).Invoke(group,new object[]{count+i});
                }
                view.Repaint();
            }
        }
        private static void Call(GameUIController target,string method,params object[] args) { typeof(GameUIController).GetMethod(method,Flags).Invoke(target,args); }
        private static Rect WorldRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }
    }
}
#endif
