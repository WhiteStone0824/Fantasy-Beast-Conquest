using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using FusionAnyMonster.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FusionAnyMonster.Tests
{
    public sealed class BattleScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator PermanentExpeditionHasFiveMinuteGoal()
        {
            BattleStage.Select(StageDifficulty.Easy,false);
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
            var battle=Object.FindAnyObjectByType<BattleDemo>();Assert.NotNull(battle);
            Assert.AreEqual(300f,battle.GoalSeconds);
        }
        [UnityTest]
        public IEnumerator AllyApproachesAnEnemyOutsideDetectionRange()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");
            yield return null;
            var battle = Object.FindAnyObjectByType<BattleDemo>();
            Assert.NotNull(battle);
            Assert.Greater(battle.Allies.Count, 0);
            Assert.Greater(battle.Enemies.Count, 0);
            var ally = battle.Allies[0];
            var enemy = battle.Enemies[0];
            battle.enabled = false;
            for (int i = 1; i < battle.Allies.Count; i++) battle.Allies[i].gameObject.SetActive(false);
            for (int i = 1; i < battle.Enemies.Count; i++) battle.Enemies[i].gameObject.SetActive(false);
            enemy.enabled = false;
            ally.transform.position = new Vector3(-5f, 0f, 0f);
            enemy.transform.position = new Vector3(5f, 0f, 0f);
            float start = Vector2.Distance(ally.transform.position, enemy.transform.position);
            yield return new WaitForSeconds(2f);
            Assert.Less(Vector2.Distance(ally.transform.position, enemy.transform.position), start - .2f,
                "An ally must approach when no enemy is within its normal detection range.");
        }

        [UnityTest]
        public IEnumerator SpawnAndMovementKeepEntireCharactersInsideCamera()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");yield return null;
            var battle=Object.FindAnyObjectByType<BattleDemo>();var camera=Camera.main;
            Assert.NotNull(battle);Assert.NotNull(camera);
            foreach(var enemy in battle.Enemies)if(enemy!=null)AssertInsideCamera(enemy.GetComponent<SpriteRenderer>(),camera);
            Assert.Greater(battle.Enemies.Count,0);
            var unit=battle.Enemies[0];unit.enabled=false;
            var move=typeof(Combatant).GetMethod("MoveBounded",BindingFlags.Instance|BindingFlags.NonPublic);
            float originalAspect=camera.aspect;
            try
            {
                foreach(float aspect in new[]{16f/9f,4f/3f})
                {
                    camera.aspect=aspect;
                    foreach(Vector2 point in new[]{new Vector2(-100,0),new Vector2(100,0),new Vector2(0,-100),new Vector2(0,100)})
                    {
                        move.Invoke(unit,new object[]{point});yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                        AssertInsideCamera(unit.GetComponent<SpriteRenderer>(),camera);
                    }
                }
            }
            finally {camera.aspect=originalAspect;unit.enabled=true;}
        }
        private static void AssertInsideCamera(SpriteRenderer renderer,Camera camera)
        {
            Assert.NotNull(renderer);
            var min=camera.WorldToViewportPoint(renderer.bounds.min);var max=camera.WorldToViewportPoint(renderer.bounds.max);
            Assert.GreaterOrEqual(min.x,-.001f);Assert.GreaterOrEqual(min.y,-.001f);
            Assert.LessOrEqual(max.x,1.001f);Assert.LessOrEqual(max.y,1.001f);
        }
        [UnityTest]
        public IEnumerator BattleSceneStartsWithMultipleAutonomousAlliesAndEnemies()
        {
            byte[] originalFile=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllBytes(SaveSystem.SavePath):null;
            GameSaveData testSave=SaveSystem.Load();
            if(testSave.units.Count<2)testSave=SaveSystem.CreateDemoData();
            for(int i=0;i<testSave.units.Count;i++)testSave.units[i].deployed=i<3;
            Assert.IsTrue(SaveSystem.Save(testSave,out _));
            try
            {
            AsyncOperation load = SceneManager.LoadSceneAsync("Assets/Game/Scenes/Battle.unity");
            Assert.IsNotNull(load, "Battle scene must be included and loadable.");
            while (!load.isDone) yield return null;
            yield return null;

            Combatant[] combatants = Object.FindObjectsByType<Combatant>();
            int allies = 0;
            int enemies = 0;
            foreach (Combatant combatant in combatants)
            {
                if (combatant.IsFriendly) allies++;
                else enemies++;
            }
            Assert.GreaterOrEqual(allies, 2, "The formation must deploy multiple allies without player control.");
            Assert.GreaterOrEqual(enemies, 5, "The battle scene must spawn enemies automatically.");

            yield return new WaitForSeconds(5f);
            combatants = Object.FindObjectsByType<Combatant>();
            int magicCasts = 0;
            foreach (Combatant combatant in combatants)
                if (combatant.IsFriendly)
                    for (int i = 0; i < combatant.Magics.Count; i++) magicCasts += combatant.MagicCastCount(i);
            Assert.Greater(magicCasts, 0, "Allied AI should automatically use its generated magic during combat.");
            LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if(originalFile!=null)System.IO.File.WriteAllBytes(SaveSystem.SavePath,originalFile);
                else if(System.IO.File.Exists(SaveSystem.SavePath))System.IO.File.Delete(SaveSystem.SavePath);
            }
        }

        [UnityTest]
        public IEnumerator DestroyedImpactVisualsAreDiscardedFromStaticPool()
        {
            FieldInfo poolField = typeof(MagicImpactVisual).GetField("pool", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(poolField, "The impact visual pool should be available for lifecycle regression coverage.");
            var pool = (Queue<MagicImpactVisual>)poolField.GetValue(null);
            while (pool.Count > 0)
            {
                MagicImpactVisual existing = pool.Dequeue();
                if (existing != null) Object.Destroy(existing.gameObject);
            }

            MagicImpactVisual stale = new GameObject("Stale pooled impact test object").AddComponent<MagicImpactVisual>();
            pool.Enqueue(stale);
            Object.Destroy(stale.gameObject);
            yield return null;
            Assert.IsTrue(stale == null, "The test entry must simulate a destroyed Unity object retained by a static pool.");

            Assert.DoesNotThrow(() => MagicImpactVisual.Play(Vector2.zero, Color.cyan, .6f, .08f));
            yield return new WaitForSeconds(.2f);
            while (pool.Count > 0)
            {
                MagicImpactVisual pooled = pool.Dequeue();
                if (pooled != null) Object.Destroy(pooled.gameObject);
            }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ScreenReplacementHidesPreviousPageBeforeTheFrameEnds()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            while (!load.isDone) yield return null;
            yield return null;

            GameUIController controller = Object.FindAnyObjectByType<GameUIController>();
            Assert.IsNotNull(controller);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Transform contentRoot = (RectTransform)typeof(GameUIController).GetField("content", flags).GetValue(controller);
            MethodInfo navigate = typeof(GameUIController).GetMethod("Navigate", flags);
            var previousPage = new List<GameObject>();
            for (int i = 0; i < contentRoot.childCount; i++) previousPage.Add(contentRoot.GetChild(i).gameObject);

            navigate.Invoke(controller, new object[] { "UNITS" });
            // Inspect within the same frame: waiting here would conceal the original flicker.
            foreach (GameObject retired in previousPage)
            {
                Assert.IsFalse(retired.activeSelf, "The previous page must stop rendering immediately.");
                Assert.AreNotEqual(contentRoot, retired.transform.parent, "Retired controls must not participate in the new layout.");
            }
            GameObject roster = GameObject.Find("Roster");
            Assert.IsNotNull(roster);
            navigate.Invoke(controller, new object[] { "HOME" });
            Assert.IsFalse(roster.activeSelf, "Rapid navigation must hide the intermediate page before deferred destruction.");
            Assert.IsNotNull(GameObject.Find("遠征部隊パネル"));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BattleRoundTripKeepsTitleCanvasHiddenUntilHomeIsReady()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            while (!load.isDone) yield return null;
            yield return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            GameUIController controller = Object.FindAnyObjectByType<GameUIController>();
            Assert.IsNotNull(controller);
            Canvas titleCanvas = (Canvas)typeof(GameUIController).GetField("canvas", flags).GetValue(controller);
            MethodInfo navigate=typeof(GameUIController).GetMethod("Navigate",flags);
            navigate.Invoke(controller, new object[] { "SETTINGS" });
            navigate.Invoke(controller, new object[] { "HOME" });
            GameObject launch=GameObject.Find("Button 出撃する");
            Assert.IsNotNull(launch,"Home must retain the deployment button.");
            launch.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual("Battle",SceneManager.GetActiveScene().name,"The home deployment button must start battle.");
            Assert.IsFalse(titleCanvas.enabled, "The persistent title UI must never overlay the battle scene.");
            BattleDemo battle = Object.FindAnyObjectByType<BattleDemo>();
            Assert.IsNotNull(battle);
            typeof(BattleDemo).GetField("returnToTitleRequested", flags).SetValue(battle, true);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "Title" && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.AreEqual("Title", SceneManager.GetActiveScene().name);
            Assert.IsTrue(titleCanvas.enabled);
            Assert.AreEqual("HOME", typeof(GameUIController).GetField("currentScreen", flags).GetValue(controller));
            Assert.IsNotNull(GameObject.Find("遠征部隊パネル"));
            Assert.IsNull(GameObject.Find("Save Settings"), "The previous settings page must not reappear on return.");
            Assert.AreEqual(1, Object.FindObjectsByType<GameUIController>().Length, "Scene changes must retain only one UI controller.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TitleRosterAndParentPickerOpenWithoutErrors()
        {
            GameSaveData originalSave = SaveSystem.Load();
            string backupJson = JsonUtility.ToJson(originalSave);
            try
            {
                Assert.IsFalse(originalSave.readOnly, "A newer-version save must not be modified by the UI smoke test.");
                AsyncOperation load = SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
                Assert.IsNotNull(load, "Title scene must be included and loadable.");
                while (!load.isDone) yield return null;
                yield return null;

                GameUIController controller = Object.FindAnyObjectByType<GameUIController>();
                Assert.IsNotNull(controller, "The title scene should install the runtime UI controller.");
                Assert.IsNotNull(GameObject.Find("遠征部隊パネル"), "The home screen should show the current squad in its own panel.");
                Assert.IsNotNull(GameObject.Find("遠征操作パネル"), "The home screen should group deployment and collection actions together.");
                Assert.IsNotNull(GameObject.Find("Button 仲間編成"), "The home screen should provide a direct route to formation management.");
                Assert.IsNull(GameObject.Find("Button 魔法図鑑"), "Individual spells must not be presented as a global encyclopedia.");
                var saveField = typeof(GameUIController).GetField("save", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var selectedIdField = typeof(GameUIController).GetField("selectedUnitId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                GameSaveData controllerSave = (GameSaveData)saveField.GetValue(controller);
                UnitSaveData selected = controllerSave.units.Find(unit => unit != null && unit.deployed) ?? controllerSave.units.Find(unit => unit != null);
                Assert.IsNotNull(selected, "The roster needs at least one unit to exercise formation saving.");
                bool originalDeployment = selected.deployed;
                selectedIdField.SetValue(controller, selected.id);

                var navigate = typeof(GameUIController).GetMethod("Navigate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                navigate.Invoke(controller,new object[]{"SETTINGS"});
                yield return null;
                Assert.IsNotNull(GameObject.Find("Save Settings"));
                Assert.IsNull(GameObject.Find("Art Settings"),"Settings must not expose image generation.");
                Assert.IsNull(GameObject.Find("Endpoint Input"));
                Assert.IsNull(GameObject.Find("Button 選択中の仲間の画像を生成"));
                Assert.IsNull(controller.GetComponent<FusionAnyMonster.Art.ComfyUIGenerationService>(),"Gameplay UI must not install an image generation service.");
                Assert.IsNull(GameObject.Find("Button 戦闘"),"Only the home screen should expose deployment.");
                typeof(GameUIController).GetMethod("StartBattle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                navigate.Invoke(controller,new object[]{"BATTLE"});
                yield return null;
                Assert.AreEqual("Title",SceneManager.GetActiveScene().name,"A non-home screen must not start a battle.");
                Assert.IsNotNull(GameObject.Find("Save Settings"));
                navigate.Invoke(controller, new object[] { "UNITS" });
                yield return null;
                Assert.IsNotNull(GameObject.Find("Roster"), "The roster screen should render.");
                Button[] rosterButtons=GameObject.Find("Roster").GetComponentsInChildren<Button>();
                var unitIcons=new List<Button>();
                foreach(Button button in rosterButtons)if(button.name.StartsWith("Unit Icon "))unitIcons.Add(button);
                Assert.AreEqual(Mathf.Min(9,controllerSave.units.Count),unitIcons.Count,"The formation roster must use a three-column icon grid.");
                foreach(Button icon in unitIcons)
                {
                    Image portrait=icon.transform.Find("キャラクターアイコン").GetComponent<Image>();
                    Assert.IsTrue(portrait.preserveAspect);
                    Assert.IsNotNull(portrait.sprite,"Roster icons must use the existing character art.");
                }
                unitIcons[0].onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Unit Detail"));
                selected=controllerSave.units.Find(unit=>unit.id==(string)selectedIdField.GetValue(controller));
                originalDeployment=selected.deployed;
                string beforeCharts=JsonUtility.ToJson(selected);
                Transform statList=GameObject.Find("Stat Bar List").transform;
                Assert.AreEqual(10,statList.childCount,"All ten stats must be in a single vertical bar list.");
                var barSettings=Resources.Load<StatBarScaleSettings>("StatBarScaleSettings");Assert.IsNotNull(barSettings);
                var barDatabase=(SpeciesDatabase)typeof(GameUIController).GetField("speciesDatabase",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
                UnitInstance chartUnit=(UnitInstance)typeof(GameUIController).GetMethod("ToUnit",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{selected});
                var chartPersonalities=Resources.Load<PersonalityDatabase>("PersonalityDatabase");
                UnitStats chartStats=UnitStatsCalculator.Calculate(barDatabase.GetById(selected.speciesId),chartUnit,null,chartPersonalities);
                for(int i=0;i<10;i++)
                {
                    var row=statList.GetChild(i).GetComponent<RectTransform>();
                    Assert.AreEqual(0,row.anchorMin.x);Assert.AreEqual(1,row.anchorMax.x);
                    float ratio=GameObject.Find("Stat Fill "+i).GetComponent<RectTransform>().anchorMax.x;
                    Assert.That(ratio,Is.InRange(0f,1f));Assert.AreEqual(Mathf.Clamp01(chartStats.Get(i)/barSettings.GetMaximum(i)),ratio,.001f);
                }
                var radar=Object.FindAnyObjectByType<IndividualValueRadar>();Assert.IsNotNull(radar);
                for(int i=0;i<10;i++)Assert.AreEqual(IndividualValues.Clamp(selected.individualValues[i])/100f,radar.GetNormalizedValue(i),.001f);
                Canvas.ForceUpdateCanvases();Mesh radarMesh=radar.canvasRenderer.GetMesh();Assert.IsNotNull(radarMesh);Assert.Greater(radarMesh.vertexCount,0);
                GameObject.Find("Button 2 耐性").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Unit Resistance Page"));Assert.IsNull(GameObject.Find("Stat Bar List"));Assert.IsNull(Object.FindAnyObjectByType<IndividualValueRadar>());
                var chartDatabase=(SpeciesDatabase)typeof(GameUIController).GetField("speciesDatabase",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
                SpeciesDefinition chartMaster=chartDatabase.GetById(selected.speciesId);
                StringAssert.Contains(Mathf.Ceil(SpeciesPassiveCalculator.ElementReduction(chartMaster,AttributeElement.Fire)*100).ToString("0")+"%",GameObject.Find("Resistance 0 0").GetComponent<TMPro.TMP_Text>().text);
                StringAssert.Contains(Mathf.Ceil(chartMaster.GetResistance(AttributeElement.Fire).applicationResistance*100).ToString("0")+"%",GameObject.Find("Resistance 1 0").GetComponent<TMPro.TMP_Text>().text);
                Assert.IsNotNull(GameObject.Find("Resistance 2 7"));
                GameObject.Find("Button 1 能力・個体値").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsNotNull(Object.FindAnyObjectByType<IndividualValueRadar>());Assert.AreEqual(beforeCharts,JsonUtility.ToJson(selected),"Charts and page switches must not alter individual data.");
                GameObject.Find("Button 種族パッシブ").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsNotNull(GameObject.Find("Species Passive Details"));
                var passiveText=new System.Text.StringBuilder();foreach(TMPro.TMP_Text label in controller.GetComponentsInChildren<TMPro.TMP_Text>())passiveText.AppendLine(label.text);
                StringAssert.Contains(chartMaster.passive.displayName,passiveText.ToString());StringAssert.Contains(chartMaster.passive.Description,passiveText.ToString());
                GameObject.Find("Button 元の画面へ戻る").GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.IsNotNull(GameObject.Find("Unit Detail"));Assert.AreEqual(beforeCharts,JsonUtility.ToJson(selected));
                Assert.IsNull(GameObject.Find("Button 魔法"), "Global navigation must not include a magic encyclopedia.");
                GameObject spellButton=GameObject.Find("Button 固有魔法を見る");
                Assert.IsNotNull(spellButton);
                spellButton.GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Button 仲間の詳細へ戻る"));
                if(selected.magicGenomeJson != null && selected.magicGenomeJson.Length>0)
                {
                    typeof(GameUIController).GetMethod("ShowUnitMagicPreview",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{selected,0});
                    yield return null;
                    Assert.IsNotNull(GameObject.Find("Individual Magic Preview"));
                    MagicPreviewController preview=Object.FindAnyObjectByType<MagicPreviewController>();
                    Assert.IsNotNull(preview);
                    Assert.IsNotNull(preview.OutputTexture);
                    MagicGenome displayedGenome;
                    Assert.IsTrue(MagicGenome.TryParseJson(selected.magicGenomeJson[0],out displayedGenome));
                    BuiltMagic displayedMagic=MagicFactory.Build(displayedGenome,chartMaster);
                    var visibleText=new System.Text.StringBuilder();
                    foreach(TMPro.TMP_Text text in controller.GetComponentsInChildren<TMPro.TMP_Text>())
                    {
                        visibleText.AppendLine(text.text);
                        foreach(string hidden in new[]{"遺伝子","複雑度","親からの継承","突然変異の履歴"})
                            StringAssert.DoesNotContain(hidden,text.text,"Technical magic data must not be displayed to players.");
                    }
                    string[] elements={"火","水","風","土","光","闇","無"};
                    StringAssert.Contains("属性："+elements[(int)displayedMagic.element],visibleText.ToString());
                    StringAssert.Contains("威力："+Mathf.Ceil(displayedMagic.power).ToString("0"),visibleText.ToString());
                    StringAssert.Contains("消費MP："+Mathf.Ceil(displayedMagic.mpCost).ToString("0"),visibleText.ToString());
                    GameObject.Find("Button もう一度見る").GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(2,preview.PlaybackCount);
                    Assert.IsNull(GameObject.Find("Button 次の仲間"),"Spell details must stay attached to the selected individual.");
                    GameObject.Find("Button 効果・詳細を見る").GetComponent<Button>().onClick.Invoke();yield return null;
                    Assert.IsNotNull(GameObject.Find("Magic Details"));
                    Assert.AreEqual(MagicDescription.EffectDetails(displayedMagic),GameObject.Find("Magic Effect Details").GetComponent<TMPro.TMP_Text>().text);
                    GameObject.Find("Button プレビューへ戻る").GetComponent<Button>().onClick.Invoke();yield return null;
                    Assert.IsNotNull(GameObject.Find("Individual Magic Preview"));
                }
                navigate.Invoke(controller,new object[]{"UNITS"});
                yield return null;

                GameObject formationToggle = GameObject.Find("Button " + (originalDeployment ? "編成から外す" : "編成に入れる"));
                Assert.IsNotNull(formationToggle, "The selected unit should have a formation toggle.");
                formationToggle.GetComponent<Button>().onClick.Invoke();
                UnitSaveData savedUnit = SaveSystem.Load().units.Find(unit => unit != null && unit.id == selected.id);
                Assert.IsNotNull(savedUnit);
                Assert.AreEqual(!originalDeployment, savedUnit.deployed, "Formation changes must persist before starting a battle.");

                navigate.Invoke(controller,new object[]{"SPECIES"});
                yield return null;
                GameObject speciesGrid=GameObject.Find("Species Icon Grid");
                Assert.IsNotNull(speciesGrid);
                Button[] speciesIcons=speciesGrid.GetComponentsInChildren<Button>();
                Assert.AreEqual(18,speciesIcons.Length);
                string firstSpeciesIcon=speciesIcons[0].name;
                Button discoveredIcon=System.Array.Find(speciesIcons,icon=>icon.interactable);
                Assert.IsNotNull(discoveredIcon,"The first page should contain an acquired starter species.");
                discoveredIcon.onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Species Detail"));
                Assert.IsNotNull(GameObject.Find("種族の姿").GetComponent<Image>().sprite);
                GameObject.Find("Button 図鑑の一覧へ戻る").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.AreEqual(firstSpeciesIcon,GameObject.Find("Species Icon Grid").GetComponentsInChildren<Button>()[0].name);
                GameObject.Find("Button 次へ").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.AreNotEqual(firstSpeciesIcon,GameObject.Find("Species Icon Grid").GetComponentsInChildren<Button>()[0].name);

                navigate.Invoke(controller, new object[] { "BREEDING" });
                yield return null;
                GameObject parentSelector = GameObject.Find("Parent Slot A");
                Assert.IsNotNull(parentSelector, "Breeding should open a selectable parent list entry.");
                parentSelector.GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Button 戻る"), "Parent selection should provide a return action.");
                var parentIcons=GameObject.Find("Parent Icon Grid").GetComponentsInChildren<Button>();
                Assert.AreEqual(Mathf.Min(18,controllerSave.units.Count),parentIcons.Length);
                string firstParentId=parentIcons[0].name.Substring("Parent Icon ".Length);
                Assert.IsNotNull(parentIcons[0].transform.Find("キャラクターアイコン").GetComponent<Image>().sprite);
                parentIcons[0].onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Parent Slot A").transform.Find("キャラクターアイコン").GetComponent<Image>().sprite);
                GameObject.Find("Parent Slot B").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.IsFalse(GameObject.Find("Parent Icon "+firstParentId).GetComponent<Button>().interactable,"The other parent's individual must not be selectable twice.");
                if(controllerSave.units.Count>1)
                {
                    parentIcons=GameObject.Find("Parent Icon Grid").GetComponentsInChildren<Button>();
                    foreach(Button icon in parentIcons)if(icon.interactable){icon.onClick.Invoke();break;}
                    yield return null;
                    Assert.IsNotNull(GameObject.Find("Parent Slot B").transform.Find("キャラクターアイコン").GetComponent<Image>().sprite);
                    Assert.IsTrue(GameObject.Find("Button この組み合わせで配合").GetComponent<Button>().interactable);
                }

                navigate.Invoke(controller, new object[] { "HOME" });
                yield return null;
                Assert.IsNotNull(GameObject.Find("遠征部隊パネル"), "Returning from breeding should restore the squad panel.");
                Assert.IsNotNull(GameObject.Find("遠征操作パネル"), "Returning from breeding should restore the home actions.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                GameSaveData restore = JsonUtility.FromJson<GameSaveData>(backupJson);
                if (restore != null) SaveSystem.Save(restore, out _);
            }
        }
    }
}
