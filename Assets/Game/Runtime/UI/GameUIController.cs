using System;
using System.Collections;
using System.Collections.Generic;
using FusionAnyMonster.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace FusionAnyMonster.UI
{
    public sealed class GameUIController : MonoBehaviour
    {
        private const string FontResourcePath = "Fonts & Materials/KH-Dot-Kodenmachou-12-Ki SDF";
        private GameSaveData save;
        private Canvas canvas;
        private RectTransform content;
        private RectTransform headerRoot;
        private RectTransform navigationRoot;
        private TMP_FontAsset uiFont;
        private TMP_Text toast;
        private RectTransform toastPanel;
        private Coroutine toastRoutine;
        private string currentScreen = "HOME";
        private string selectedUnitId;
        private int rosterPage;
        private UnitSortMode unitSortMode;
        private int eggPage;
        private string selectedEggId;
        private int unitDetailPage;
        private bool stageSelectionRush;
        private int stageSelectionDifficulty;
        private int eventStageSelection;
        private int equipmentPage;
        private int speciesPage;
        private int speciesDetailPage;
        private int parentPickerPage;
        private string parentA, parentB;
        private string choosingParentSlot;
        private static readonly Dictionary<string, Sprite> generatedSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<Texture2D, Sprite> fullPortraitCache = new Dictionary<Texture2D, Sprite>();
        private SpeciesDatabase speciesDatabase;
        private PersonalityDatabase personalityDatabase;
        private StatBarScaleSettings statBarScaleSettings;
        private BreedingRuleDatabase breedingRuleDatabase;
        private RankBreedingRule rankBreedingRule;
        private readonly Color ink = GameUiTheme.Text;
        private readonly Color muted = GameUiTheme.Muted;
        private readonly Color panel = GameUiTheme.Surface;
        private readonly Color accent = GameUiTheme.Accent;
        private readonly Color gold = GameUiTheme.Gold;
        private static Sprite homeCircleSprite;
        private static Sprite eggSprite;
        private static Material catalogueSilhouetteMaterial;
        private static GameUIController instance;
        private float nextPlayTimeSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(SceneManager.GetActiveScene().name=="SpellProgrammingDemo")return;
            GameUIController existing = FindAnyObjectByType<GameUIController>();
            if (existing != null)
            {
                instance = existing;
                existing.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
                return;
            }
            new GameObject("Game UI").AddComponent<GameUIController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            save = SaveSystem.LoadOrCreate();
            PlayerPlayTime.Start(save);
            nextPlayTimeSave=Time.realtimeSinceStartup+60f;
            speciesDatabase = LoadOrCreateSpeciesDatabase();
            personalityDatabase = Resources.Load<PersonalityDatabase>("PersonalityDatabase");
            statBarScaleSettings=Resources.Load<StatBarScaleSettings>("StatBarScaleSettings");
            breedingRuleDatabase = Resources.Load<BreedingRuleDatabase>("BreedingRuleDatabase");
            rankBreedingRule = Resources.Load<RankBreedingRule>("RankBreedingRule");
            BindUnitsToSpecies();
            BuildCanvas();
            PresentTitle(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            instance = null;
        }
        private void Update()
        {
            if(instance!=this || Time.realtimeSinceStartup<nextPlayTimeSave)return;
            nextPlayTimeSave=Time.realtimeSinceStartup+60f;
            SavePlayTimeCheckpoint();
        }
        private void OnApplicationQuit() { if(instance==this)SavePlayTimeCheckpoint(); }
        private static void SavePlayTimeCheckpoint()
        {
            if(!System.IO.File.Exists(SaveSystem.SavePath))return;
            var latest=SaveSystem.Load();
            if(!latest.readOnly && !SaveSystem.Save(latest,out string error))Debug.LogWarning("Could not save play time: "+error);
        }
        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "Title" && canvas != null) canvas.enabled = false;
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive && scene != SceneManager.GetActiveScene()) return;
            if (canvas != null) canvas.enabled = false;
            if (scene.name != "Title") return;
            save = SaveSystem.LoadOrCreate();
            speciesDatabase = LoadOrCreateSpeciesDatabase(); BindUnitsToSpecies();
            PresentTitle(scene);
        }
        private void PresentTitle(Scene scene)
        {
            if (canvas == null) return;
            canvas.enabled = false;
            if (scene.name != "Title") return;
            choosingParentSlot = null;
            if (toastRoutine != null) { StopCoroutine(toastRoutine); toastRoutine = null; }
            if (toastPanel != null) toastPanel.gameObject.SetActive(false);
            Navigate("HOME");
            Canvas.ForceUpdateCanvases();
            canvas.enabled = true;
        }

        private static string Translate(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            string[,] map = {
                {"TACTICAL GENESIS", "魔物たちの自動戦闘"}, {"Build a lineage. Shape a genome. Watch your squad survive.", "仲間を育て、遺伝子を組み合わせて、戦場を生き抜こう"},
                {"COLLECTION", "仲間一覧"}, {"Species and individuals in your vault", "仲間の種族と個体情報"}, {"DEPLOYED", "出撃編成"}, {"Autonomous squad formation", "最大5体を編成して出撃"},
                {"BREEDING", "配合"}, {"Genome crossover", "遺伝子の交叉"}, {"Combine parents to create novel magic", "親を配合して新しい魔法を生み出す"},
                {"ENTER BATTLE", "戦闘を開始"}, {"OPEN BREEDING LAB", "配合画面へ"}, {"SAVE GAME", "セーブ"}, {"UNIT ARCHIVE", "仲間一覧"},
                {"SELECT A UNIT TO INSPECT STATS, RESISTANCES, GENOMES AND FORMATION", "仲間を選ぶと能力・耐性・魔法・編成を確認できます"},
                {"REMOVE FROM FORMATION", "編成から外す"}, {"ADD TO FORMATION", "編成に加える"}, {"MAGIC GENOMES", "固有魔法"},
                {"No inherited genome data yet", "遺伝子データがありません"}, {"BREEDING LAB", "配合"}, {"PARENT PAIRING", "親を選択"},
                {"OFFSPRING PREVIEW", "子どもの予測"}, {"CREATE OFFSPRING", "配合する"}, {"REROLL SEED", "結果を再抽選"},
                {"PARENT A", "親A"}, {"PARENT B", "親B"}, {"SELECT PARENT", "親を選ぶ"}, {"GENOME OBSERVATORY", "魔法の遺伝子"},
                {"SETTINGS & SAVE DATA", "設定・セーブデータ"}, {"SAVE NOW", "今すぐセーブ"}, {"RELOAD SAVE", "セーブを読み込む"},
                {"HOME", "ホーム"}, {"UNITS", "仲間管理"}, {"BREEDING", "配合"}, {"BATTLE", "戦闘開始"}, {"SETTINGS", "設定"},
                {"Brave", "勇敢"}, {"Timid", "臆病"}, {"Calm", "冷静"}, {"Cautious", "慎重"}, {"Aggressive", "好戦的"}, {"Gentle", "温厚"},
                {"Physical ATK", "物理攻撃"}, {"Magic ATK", "魔法攻撃"}, {"Physical DEF", "物理防御"}, {"Magic DEF", "魔法防御"},
                {"Move Speed", "移動速度"}, {"Attack Speed", "攻撃速度"}, {"Detection", "索敵範囲"}, {"MP Regen", "MP回復"},
                {"LEVEL ", "レベル "}, {"EXP ", "経験値 "}, {"PERSONALITY ", "性格 "}, {"RANK ", "ランク "}, {"IV ", "個体値 "},
                {"ELEMENT DR:", "属性ダメージ軽減:"}, {"DEBUFF RES:", "状態異常抵抗:"}, {"GENOME CROSSOVER", "遺伝子交叉"}, {"MUTATION", "突然変異"}
            };
            for (int i = 0; i < map.GetLength(0); i++) value = value.Replace(map[i, 0], map[i, 1]);
            return value.Replace("窶｢", "・").Replace("竊・", "・").Replace("ﾂｷ", "・");
        }
        private readonly Button[] navigationButtons = new Button[5];
        private readonly string[] navigationScreens = { "HOME", "SPECIES", "UNITS", "BREEDING", "SETTINGS" };

        private void BuildCanvas()
        {
            uiFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (uiFont == null) uiFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var canvasObject = new GameObject("Fusion UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }

            var background = Rect("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Texture2D backdropTexture = Resources.Load<Texture2D>("UI/MonsterGuildBackdrop");
            if (backdropTexture != null)
            {
                RawImage backdrop = background.gameObject.AddComponent<RawImage>();
                backdrop.texture = backdropTexture;
                backdrop.uvRect = new Rect(0f, .21875f, 1f, .5625f);
                backdrop.raycastTarget = false;
            }
            else Image(background, new Color(.025f, .037f, .065f));
            var atmosphere = Rect("Atmosphere Tint", background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image(atmosphere, new Color(.015f, .025f, .055f, .48f));
            var header = Rect("Header", background, new Vector2(0, .89f), Vector2.one, Vector2.zero, Vector2.zero);
            headerRoot = header;
            Image(header, new Color(.035f, .055f, .09f, .96f));
            Label("幻獣遠征録", header, new Vector2(.025f, 0), new Vector2(.29f, 1), 23, ink, TextAnchor.MiddleLeft);
            ColorBlock("Header Gold Rule", header, new Vector2(0, 0), new Vector2(1, .012f), new Color(gold.r, gold.g, gold.b, .78f));

            var nav = Rect("Navigation", background, new Vector2(.32f, .89f), new Vector2(.99f, 1f), Vector2.zero, Vector2.zero);
            navigationRoot = nav;
            string[] labels = { "ホーム", "図鑑", "仲間", "配合", "設定" };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                navigationButtons[i] = Button(nav, labels[i], new Vector2(i / (float)labels.Length, .08f), new Vector2((i + 1) / (float)labels.Length, .92f), () => Navigate(navigationScreens[index]), null, 15);
                RectTransform marker = Rect("Active Marker", navigationButtons[i].transform, new Vector2(.18f, .03f), new Vector2(.82f, .09f), Vector2.zero, Vector2.zero);
                Image markerImage = marker.gameObject.AddComponent<Image>(); markerImage.color = gold; markerImage.raycastTarget = false;
                marker.gameObject.SetActive(false);
            }
            content = Rect("Content", background, new Vector2(.02f, .075f), new Vector2(.98f, .87f), Vector2.zero, Vector2.zero);
            toastPanel = Rect("Toast Panel", background, new Vector2(.02f, .012f), new Vector2(.98f, .065f), Vector2.zero, Vector2.zero);
            Image(toastPanel, new Color(.025f, .045f, .08f, .88f));
            ColorBlock("Toast Accent", toastPanel, Vector2.zero, new Vector2(.006f,1f), gold);
            toast = Label("Toast", toastPanel, Vector2.zero, Vector2.one, 14, ink, TextAnchor.MiddleLeft);
            toast.text = string.Empty;
            toastPanel.gameObject.SetActive(false);
            RefreshNavigation();
        }

        private void RefreshNavigation()
        {
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                Button button = navigationButtons[i];
                if (button == null) continue;
                Image image = button.GetComponent<Image>();
                bool selected = navigationScreens[i] == currentScreen;
                if (image != null) image.color = selected ? new Color(.10f, .39f, .48f) : new Color(.075f, .105f, .16f, .94f);
                Transform marker = button.transform.Find("Active Marker");
                if (marker != null) marker.gameObject.SetActive(selected);
            }
        }
        private void Navigate(string screen)
        {
            if (Array.IndexOf(navigationScreens,screen) < 0) return;
            currentScreen = screen;
            bool home = screen == "HOME";
            headerRoot.gameObject.SetActive(!home);
            navigationRoot.gameObject.SetActive(!home);
            content.anchorMin = home ? new Vector2(.015f, .015f) : new Vector2(.02f, .075f);
            content.anchorMax = home ? new Vector2(.985f, .985f) : new Vector2(.98f, .87f);
            RefreshNavigation();
            switch (screen)
            {
                case "HOME": ShowHome(); break;
                case "SPECIES": ShowSpecies(); break;
                case "UNITS": ShowUnits(); break;
                case "BREEDING": ShowBreeding(); break;
                case "SETTINGS": ShowSettings(); break;
            }
        }

        private void ClearContent()
        {
            // Destroy is deferred. Retire the old page before resizing or rebuilding the content.
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform retired = content.GetChild(i);
                retired.gameObject.SetActive(false);
                retired.SetParent(null, false);
                Destroy(retired.gameObject);
            }
            if (currentScreen != "HOME") AddPageSurface();
        }
        private void AddPageSurface()
        {
            var background=Rect("画面の下地",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var image=background.gameObject.AddComponent<Image>();image.color=new Color(.015f,.035f,.06f,.87f);image.raycastTarget=false;
            GameUiTheme.ApplySurface(image);
        }
        private void ShowHome()
        {
            if (headerRoot != null) headerRoot.gameObject.SetActive(false);
            if (navigationRoot != null) navigationRoot.gameObject.SetActive(false);
            content.anchorMin = new Vector2(.015f, .015f);
            content.anchorMax = new Vector2(.985f, .985f);
            ClearContent();
            var heading=Rect("ホーム見出しの下地",content,new Vector2(.025f,.79f),new Vector2(.70f,.995f),Vector2.zero,Vector2.zero);
            var headingImage=heading.gameObject.AddComponent<Image>();headingImage.color=new Color(.015f,.035f,.06f,.84f);headingImage.raycastTarget=false;GameUiTheme.ApplySurface(headingImage);
            Label("幻獣遠征録", content, new Vector2(.035f,.87f), new Vector2(.60f,.99f), 32, gold, TextAnchor.MiddleLeft);
            Label("仲間を育て、魔法を継ぎ、夜の魔境を踏破しよう", content, new Vector2(.038f,.805f), new Vector2(.68f,.88f), 15, ink, TextAnchor.MiddleLeft);
            ColorBlock("Title Accent Rule", content, new Vector2(.038f,.79f), new Vector2(.39f,.797f), new Color(gold.r,gold.g,gold.b,.76f));
            Button(content, "設定", new Vector2(.88f,.88f), new Vector2(.97f,.98f), () => Navigate("SETTINGS"), GameUiTheme.Button, 15);
            Button(content, "通信対戦", new Vector2(.73f,.88f), new Vector2(.87f,.98f), ShowPvp, GameUiTheme.Button, 15);

            RectTransform party = Rect("遠征部隊パネル", content, new Vector2(.035f,.145f), new Vector2(.69f,.77f), Vector2.zero, Vector2.zero);
            Image(party, new Color(.018f,.035f,.075f,.80f));
            Shadow partyShadow = party.gameObject.AddComponent<Shadow>(); partyShadow.effectColor = new Color(.005f,.012f,.028f,.78f); partyShadow.effectDistance = new Vector2(0,-4f);
            Frame(party,"遠征部隊の縁取り",new Color(gold.r,gold.g,gold.b,.72f),.004f);
            ColorBlock("金の上飾り", party, new Vector2(0f,.992f), new Vector2(1f,1f), new Color(gold.r,gold.g,gold.b,.88f));
            Label("遠征部隊", party, new Vector2(.035f,.84f), new Vector2(.45f,.98f), 21, gold, TextAnchor.MiddleLeft);
            int slotCount = 5;
            Label("編成　"+CountDeployed()+" / "+slotCount, party, new Vector2(.66f,.84f), new Vector2(.965f,.98f), 15, new Color(.78f,.88f,.92f), TextAnchor.MiddleRight);
            int sourceIndex = 0;
            float gap = .018f;
            float cardWidth = (.94f - gap * (slotCount - 1)) / slotCount;
            float cardStart = (1f - (cardWidth * slotCount + gap * (slotCount - 1))) * .5f;
            for (int slot = 0; slot < slotCount; slot++)
            {
                UnitSaveData unit = null;
                while (sourceIndex < save.units.Count && unit == null)
                {
                    UnitSaveData candidate = save.units[sourceIndex++];
                    if (candidate != null && candidate.deployed) unit = candidate;
                }
                float left = cardStart + slot * (cardWidth + gap);
                RectTransform card = Rect(unit == null ? "空き編成枠" : "出撃仲間 " + unit.displayName, party, new Vector2(left,.13f), new Vector2(left+cardWidth,.81f), Vector2.zero, Vector2.zero);
                Image cardImage = card.gameObject.AddComponent<Image>();
                cardImage.color = unit == null ? new Color(.035f,.055f,.085f,.72f) : new Color(.035f,.075f,.12f,.94f);
                Outline cardOutline = card.gameObject.AddComponent<Outline>(); cardOutline.effectColor = unit == null ? new Color(.55f,.65f,.72f,.24f) : new Color(.28f,.70f,.72f,.78f); cardOutline.effectDistance = new Vector2(1f,-1f);
                Button cardButton = card.gameObject.AddComponent<Button>(); cardButton.targetGraphic = cardImage; ApplyButtonTheme(cardButton);
                if (unit == null) cardButton.onClick.AddListener(() => Navigate("UNITS"));
                else
                {
                    string unitId = unit.id;
                    cardButton.onClick.AddListener(() => { selectedUnitId = unitId;unitDetailPage=0;rosterPage=Mathf.Max(0,UnitSort.Apply(save.units,unitSortMode).FindIndex(item=>item.id==unitId)/9);Navigate("UNITS"); });
                }
                if (unit == null)
                {
                    Label("+", card, new Vector2(.08f,.48f), new Vector2(.92f,.76f), 36, new Color(.78f,.85f,.88f,.86f), TextAnchor.MiddleCenter);
                    Label("空き枠", card, new Vector2(.04f,.30f), new Vector2(.96f,.49f), 14, ink, TextAnchor.MiddleCenter);
                    Label("仲間を編成", card, new Vector2(.02f,.13f), new Vector2(.98f,.29f), 11, muted, TextAnchor.MiddleCenter);
                    continue;
                }
                SpeciesDefinition definition = speciesDatabase == null ? null : speciesDatabase.GetById(unit.speciesId);
                Sprite sprite = LoadGeneratedSprite(unit.generatedSpritePath) ?? (definition != null ? definition.sprite : null);
                AddImage("キャラクター画像", card, new Vector2(.07f,.30f), new Vector2(.93f,.84f), Color.white, sprite);
                RectTransform rankTag = Rect("ランク札", card, new Vector2(.58f,.85f), new Vector2(.95f,.97f), Vector2.zero, Vector2.zero);
                Image(rankTag, new Color(gold.r,gold.g,gold.b,.94f));
                Label(unit.rank+"級", rankTag, Vector2.zero, Vector2.one, 11, new Color(.11f,.13f,.16f), TextAnchor.MiddleCenter);
                Label(unit.displayName, card, new Vector2(.025f,.14f), new Vector2(.975f,.31f), 14, ink, TextAnchor.MiddleCenter);
                Label("Lv."+unit.level+"　"+Translate(unit.personality), card, new Vector2(.02f,.025f), new Vector2(.98f,.14f), 10, new Color(.72f,.83f,.87f), TextAnchor.MiddleCenter);
            }
            Label("仲間カードを選ぶと詳細を確認できます", party, new Vector2(.035f,.015f), new Vector2(.965f,.105f), 12, muted, TextAnchor.MiddleCenter);

            RectTransform controls = Rect("遠征操作パネル", content, new Vector2(.715f,.145f), new Vector2(.98f,.77f), Vector2.zero, Vector2.zero);
            Image(controls, new Color(.025f,.045f,.08f,.86f));
            Shadow controlsShadow = controls.gameObject.AddComponent<Shadow>(); controlsShadow.effectColor = new Color(.005f,.012f,.028f,.78f); controlsShadow.effectDistance = new Vector2(0,-4f);
            Frame(controls,"遠征操作の縁取り",new Color(accent.r,accent.g,accent.b,.55f),.004f);
            Label("遠征準備", controls, new Vector2(.045f,.86f), new Vector2(.62f,.99f), 18, gold, TextAnchor.MiddleLeft);
            Label(CountDeployed()+" / 5 体", controls, new Vector2(.62f,.86f), new Vector2(.955f,.99f), 13, muted, TextAnchor.MiddleRight);
            Button launch = Button(controls, CountDeployed() > 0 ? "出撃する" : "仲間を編成する", new Vector2(.045f,.48f), new Vector2(.955f,.82f), () => { if (CountDeployed() > 0) ShowStageSelection(); else Navigate("UNITS"); }, CountDeployed() > 0 ? new Color(.09f,.39f,.43f) : new Color(.25f,.29f,.33f), 23);
            Outline launchOutline = launch.gameObject.AddComponent<Outline>(); launchOutline.effectColor = new Color(gold.r,gold.g,gold.b,.94f); launchOutline.effectDistance = new Vector2(2f,-2f);
            RectTransform launchLabel = launch.transform.GetChild(0).GetComponent<RectTransform>(); launchLabel.anchorMin = new Vector2(.36f,.50f); launchLabel.offsetMin = new Vector2(3f,2f);
            launchLabel.anchorMax = new Vector2(.98f,.94f); launchLabel.offsetMax = new Vector2(-3f,-2f);
            Label(CountDeployed() > 0 ? "仲間のAI戦闘を開始" : "先に出撃仲間を選びます", launch.transform, new Vector2(.35f,.09f), new Vector2(.98f,.46f), 12, new Color(.84f,.91f,.92f), TextAnchor.MiddleCenter);
            RectTransform badgeRect = Rect("遠征章", launch.transform, new Vector2(.055f,.15f), new Vector2(.34f,.85f), Vector2.zero, Vector2.zero);
            var badge = badgeRect.gameObject.AddComponent<UnityEngine.UI.Image>(); badge.sprite = HomeCircleSprite(); badge.preserveAspect = true; badge.raycastTarget = false;
            badgeRect.SetAsFirstSibling();
            Label("出", badgeRect, new Vector2(.08f,.08f), new Vector2(.92f,.92f), 18, ink, TextAnchor.MiddleCenter);
            QuickButton(controls,"仲間編成","出撃仲間を選ぶ",new Vector2(.045f,.25f),new Vector2(.48f,.43f),()=>Navigate("UNITS"),GameUiTheme.Button);
            QuickButton(controls,"種族図鑑","種族と能力を確認",new Vector2(.52f,.25f),new Vector2(.955f,.43f),()=>Navigate("SPECIES"),GameUiTheme.Button);
            QuickButton(controls,"配合","親を選び特徴を受け継ぐ",new Vector2(.045f,.035f),new Vector2(.955f,.215f),()=>Navigate("BREEDING"),GameUiTheme.Button);
            var progress=Rect("遠征記録",content,new Vector2(.035f,.035f),new Vector2(.69f,.125f),Vector2.zero,Vector2.zero);Image(progress,panel);
            int known=0,total=0;
            if(speciesDatabase!=null && speciesDatabase.species!=null)
                foreach(var species in speciesDatabase.species)if(species!=null){total++;if(IsSpeciesDiscovered(species))known++;}
            int eggs=save.eggs==null?0:save.eggs.Count;
            Label("図鑑 "+known+" / "+total+"　卵 "+eggs+"個",progress,new Vector2(.03f,.08f),new Vector2(.37f,.92f),14,ink,TextAnchor.MiddleLeft);
            var playerRank=PlayerRankCalculator.Calculate(save);
            Button(progress,"ランク "+playerRank.Total,new Vector2(.38f,.07f),new Vector2(.60f,.93f),ShowPlayerRank,GameUiTheme.Button,14);
            Button(progress,"卵を孵化する",new Vector2(.62f,.07f),new Vector2(.98f,.93f),()=>{Navigate("UNITS");ShowEggs();},GameUiTheme.Button,15);
        }

        private void ShowPlayerRank()
        {
            ClearContent();AddPageSurface();
            var rank=PlayerRankCalculator.Calculate(save);
            Label("プレイヤーランク",content,new Vector2(.05f,.84f),new Vector2(.70f,.97f),29,gold,TextAnchor.MiddleLeft);
            Button(content,"ホームへ戻る",new Vector2(.76f,.86f),new Vector2(.96f,.96f),ShowHome,null,15);
            Label(rank.Total.ToString(),content,new Vector2(.08f,.67f),new Vector2(.92f,.83f),42,ink,TextAnchor.MiddleCenter);
            int known=0,total=0;
            if(speciesDatabase!=null && speciesDatabase.species!=null)foreach(var species in speciesDatabase.species)
                if(species!=null){total++;if(IsSpeciesDiscovered(species))known++;}
            int units=0,deployed=0,eggs=0;
            var equipmentIds=new HashSet<string>(StringComparer.Ordinal);
            if(save.units!=null)foreach(var unit in save.units)if(unit!=null)
            { units++;if(unit.deployed)deployed++;if(EquipmentCatalog.HasItem(unit.equipment))equipmentIds.Add(unit.equipment.id); }
            if(save.equipmentInventory!=null)foreach(var item in save.equipmentInventory)if(EquipmentCatalog.HasItem(item))equipmentIds.Add(item.id);
            if(save.eggs!=null)foreach(var egg in save.eggs)if(egg!=null)eggs++;
            var stats=Rect("プレイ記録",content,new Vector2(.14f,.13f),new Vector2(.86f,.65f),Vector2.zero,Vector2.zero);Image(stats,panel);
            Label("プレイ時間　　"+PlayerPlayTime.Format(PlayerPlayTime.Current(save))+"\n図鑑の登録数　　"+known+" / "+total+"種\n仲間の数　　"+units+"体（出撃編成 "+deployed+"体）\n装備の数　　"+equipmentIds.Count+"個\n所持している卵　　"+eggs+"個\n遠征成功回数　　"+Mathf.Max(0,save.battlesCompleted)+"回",stats,new Vector2(.08f,.06f),new Vector2(.92f,.94f),19,ink,TextAnchor.MiddleLeft);
        }

        private void ShowSpecies()
        {
            ClearContent();
            Label("種族図鑑", content, new Vector2(.02f,.90f), new Vector2(.7f,.99f), 30, ink, TextAnchor.MiddleLeft);
            SaveSystem.RecordOwnedSpecies(save);
            Label("仲間にした種族の詳細を確認できます。未入手の種族は ??? と表示されます。", content, new Vector2(.02f,.83f), new Vector2(.98f,.91f), 15, muted, TextAnchor.MiddleLeft);
            var sortedSpecies = new List<SpeciesDefinition>();
            if (speciesDatabase != null && speciesDatabase.species != null)
                foreach (SpeciesDefinition species in speciesDatabase.species)
                    if (species != null) sortedSpecies.Add(species);
            var specialSpecies=Resources.Load<SpeciesDefinition>("SpecialSpecies");
            if(specialSpecies!=null && !sortedSpecies.Exists(s=>s.speciesId==specialSpecies.speciesId))sortedSpecies.Add(specialSpecies);
            sortedSpecies.Sort((a, b) =>
            {
                int rankOrder = a.rank.CompareTo(b.rank);
                if (rankOrder != 0) return rankOrder;
                int familyOrder = a.family.CompareTo(b.family);
                return familyOrder != 0 ? familyOrder : string.CompareOrdinal(a.speciesId, b.speciesId);
            });
            SpeciesDefinition[] list = sortedSpecies.ToArray();
            int perPage=18, pages=Mathf.Max(1,Mathf.CeilToInt(list.Length/(float)perPage)); speciesPage=Mathf.Clamp(speciesPage,0,pages-1);
            var grid=Rect("Species Icon Grid",content,new Vector2(.02f,.09f),new Vector2(.98f,.81f),Vector2.zero,Vector2.zero);
            for(int i=0;i<perPage;i++)
            {
                int index=speciesPage*perPage+i;if(index>=list.Length)break;if(list[index]==null)continue;
                SpeciesDefinition entry=list[index];
                int col=i%6,row=i/6;float x=col/6f,top=1-row/3f;
                bool discovered=IsSpeciesDiscovered(entry);
                Button icon=CharacterIcon("Species Icon "+entry.speciesId,grid,new Vector2(x,top-1/3f),new Vector2(x+1/6f,top),discovered?entry.displayName:"???",entry.sprite,discovered?entry.rank.ToString():"?",null,false,discovered?(Action)(()=>{speciesDetailPage=0;ShowSpeciesDetail(entry);}):null,"???");
                if(!discovered)
                {
                    var portrait=icon.transform.Find("キャラクターアイコン").GetComponent<Image>();
                    portrait.color=new Color32(0x27,0x3e,0x57,0xff);
                    if(catalogueSilhouetteMaterial==null)
                    {
                        var shader=Resources.Load<Shader>("Shaders/CatalogueSilhouette");
                        if(shader!=null)catalogueSilhouetteMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
                    }
                    if(catalogueSilhouetteMaterial!=null)portrait.material=catalogueSilhouetteMaterial;
                    icon.interactable=false;
                }
            }
            Button(content,"前へ",new Vector2(.30f,.005f),new Vector2(.43f,.075f),()=>{speciesPage=Mathf.Max(0,speciesPage-1);ShowSpecies();},null,14).interactable=speciesPage>0;
            Label((speciesPage+1)+" / "+pages+" ページ（"+list.Length+"種）",content,new Vector2(.43f,.005f),new Vector2(.57f,.075f),13,muted,TextAnchor.MiddleCenter);
            Button(content,"次へ",new Vector2(.57f,.005f),new Vector2(.70f,.075f),()=>{speciesPage=Mathf.Min(pages-1,speciesPage+1);ShowSpecies();},null,14).interactable=speciesPage<pages-1;
        }

        private bool IsSpeciesDiscovered(SpeciesDefinition species)
        { return species!=null && save!=null && save.discoveredSpeciesIds!=null && save.discoveredSpeciesIds.Contains(species.speciesId); }

        private void ShowSpeciesDetail(SpeciesDefinition species)
        {
            if(!IsSpeciesDiscovered(species)){ShowSpecies();return;}
            ClearContent();
            Label(species.displayName,content,new Vector2(.02f,.88f),new Vector2(.98f,.99f),30,ink,TextAnchor.MiddleLeft);
            Label("種族図鑑",content,new Vector2(.02f,.81f),new Vector2(.98f,.89f),15,muted,TextAnchor.MiddleLeft);
            var detail=Rect("Species Detail",content,new Vector2(.02f,.15f),new Vector2(.98f,.79f),Vector2.zero,Vector2.zero);Image(detail,panel);
            AddImage("種族の姿",detail,new Vector2(.03f,.22f),new Vector2(.36f,.95f),Color.white,species.sprite);
            Label(FamilyJapanese(species.family.ToString())+"　・　"+species.rank+"級",detail,new Vector2(.03f,.07f),new Vector2(.38f,.21f),18,accent,TextAnchor.MiddleCenter);
            Button(detail,"1 種族値",new Vector2(.41f,.86f),new Vector2(.68f,.97f),()=>{speciesDetailPage=0;ShowSpeciesDetail(species);},speciesDetailPage==0?new Color(.12f,.35f,.40f):panel,14);
            Button(detail,"2 耐性",new Vector2(.70f,.86f),new Vector2(.97f,.97f),()=>{speciesDetailPage=1;ShowSpeciesDetail(species);},speciesDetailPage==1?new Color(.12f,.35f,.40f):panel,14);
            if(speciesDetailPage==0)ShowSpeciesStatBars(detail,species);
            else
            {
                var page=Rect("Species Resistance Page",detail,new Vector2(.40f,.05f),new Vector2(.98f,.83f),Vector2.zero,Vector2.zero);
                ShowResistanceCharts(page,species,null);
            }
            Button(content,"図鑑の一覧へ戻る",new Vector2(.03f,.025f),new Vector2(.33f,.105f),ShowSpecies,null,15);
            Button(content,"種族パッシブを見る",new Vector2(.67f,.025f),new Vector2(.97f,.105f),()=>ShowSpeciesPassive(species,()=>ShowSpeciesDetail(species)),null,15);
        }

        private void ShowSpeciesStatBars(RectTransform detail,SpeciesDefinition species)
        {
            Label("種族値（全種族共通基準）",detail,new Vector2(.41f,.77f),new Vector2(.97f,.84f),14,accent,TextAnchor.MiddleLeft);
            var bars=Rect("Species Stat Bar List",detail,new Vector2(.41f,.06f),new Vector2(.97f,.76f),Vector2.zero,Vector2.zero);
            var maximums=new float[10];
            // Use the entire master catalogue, including undiscovered species, never the selected roster.
            if(speciesDatabase!=null && speciesDatabase.species!=null)
                foreach(var candidate in speciesDatabase.species)
                    if(candidate!=null && candidate.baseStats!=null)
                        for(int i=0;i<10;i++)maximums[i]=Mathf.Max(maximums[i],candidate.baseStats.Get(i));
            for(int i=0;i<10;i++)
            {
                float value=species.baseStats==null?0:species.baseStats.Get(i);
                float maximum=Mathf.Max(1,Mathf.Ceil(Mathf.Max(maximums[i],value)));
                float top=1-i*.10f;
                var row=Rect("Species Stat Row "+i,bars,new Vector2(0,top-.094f),new Vector2(1,top),Vector2.zero,Vector2.zero);
                TMP_Text name=Label(StatNames[i],row,Vector2.zero,new Vector2(.25f,1),13,ink,TextAnchor.MiddleLeft);TightText(name);
                var track=Rect("Species Stat Track",row,new Vector2(.27f,.24f),new Vector2(.82f,.76f),Vector2.zero,Vector2.zero);Image(track,new Color(.025f,.045f,.075f));
                var fill=Rect("Species Stat Fill "+i,track,Vector2.zero,new Vector2(Mathf.Clamp01(value/maximum),1),Vector2.zero,Vector2.zero);Image(fill,i<2?gold:accent);
                TMP_Text number=Label(FormatNumber(value),row,new Vector2(.84f,0),Vector2.one,13,ink,TextAnchor.MiddleRight);TightText(number);
            }
        }

        private void ShowUnits()
        {
            ClearContent();
            Label("仲間の管理", content, new Vector2(.02f, .88f), new Vector2(.70f, .98f), 30, ink, TextAnchor.MiddleLeft);
            Button(content, "卵を孵化する（" + (save.eggs == null ? 0 : save.eggs.Count) + "）", new Vector2(.72f,.89f), new Vector2(.98f,.98f), ShowEggs, accent, 16);
            Label("仲間を選んで能力を確認。出撃する仲間は「編成に入れる」を押してください（最大5体）。", content, new Vector2(.02f, .80f), new Vector2(.98f, .89f), 16, muted, TextAnchor.MiddleLeft);
            var list = Rect("Roster", content, new Vector2(.02f, .04f), new Vector2(.38f, .77f), Vector2.zero, Vector2.zero);
            Image(list, panel);
            var sortedUnits=UnitSort.Apply(save.units,unitSortMode);
            int pageSize = 9;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(sortedUnits.Count / (float)pageSize));
            rosterPage = Mathf.Clamp(rosterPage, 0, pageCount - 1);
            int selectedIndex=sortedUnits.FindIndex(unit=>unit.id==selectedUnitId);
            UnitSaveData selected = selectedIndex>=rosterPage*pageSize && selectedIndex<Mathf.Min((rosterPage+1)*pageSize,sortedUnits.Count)
                ? sortedUnits[selectedIndex] : (sortedUnits.Count>0 ? sortedUnits[Mathf.Min(rosterPage*pageSize,sortedUnits.Count-1)] : null);
            Label("所持仲間 " + sortedUnits.Count + "体", list, new Vector2(.04f, .93f), new Vector2(.42f, .99f), 15, accent, TextAnchor.MiddleLeft);
            Button(list,"並び："+SortLabel(),new Vector2(.43f,.93f),new Vector2(.97f,.995f),()=>{NextUnitSort();rosterPage=0;selectedUnitId=null;ShowUnits();},panel,12);
            for (int slot = 0; slot < pageSize; slot++)
            {
                int index = rosterPage * pageSize + slot;
                if (index >= sortedUnits.Count) break;
                UnitSaveData unit = sortedUnits[index];
                int col=slot%3,row=slot/3;
                float x=.025f+col*.32f,top=.91f-row*.25f;
                string id = unit.id;
                SpeciesDefinition master=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
                Sprite sprite=LoadGeneratedSprite(unit.generatedSpritePath)??(master==null?null:master.sprite);
                bool isSelected=selected!=null && selected.id==id;
                string badge=isSelected ? (unit.deployed?"選択・編成":"選択中") : unit.deployed?"編成中":"Lv."+unit.level;
                CharacterIcon("Unit Icon "+id,list,new Vector2(x,top-.235f),new Vector2(x+.31f,top),unit.displayName,sprite,unit.rank,badge,isSelected,()=>{selectedUnitId=id;unitDetailPage=0;ShowUnits();});
            }
            Button(list, "前へ", new Vector2(.05f, .035f), new Vector2(.25f, .13f), () => { rosterPage = Mathf.Max(0, rosterPage - 1); selectedUnitId=null;unitDetailPage=0;ShowUnits(); }, null, 13).interactable=rosterPage>0;
            Label((rosterPage + 1) + " / " + pageCount + " ページ", list, new Vector2(.28f, .035f), new Vector2(.72f, .13f), 12, muted, TextAnchor.MiddleCenter);
            Button(list, "次へ", new Vector2(.75f, .035f), new Vector2(.95f, .13f), () => { rosterPage = Mathf.Min(pageCount - 1, rosterPage + 1); selectedUnitId=null;unitDetailPage=0;ShowUnits(); }, null, 13).interactable=rosterPage<pageCount-1;
            if (selected != null) ShowUnitDetail(selected);
            else
            {
                var empty=Rect("仲間がいない状態",content,new Vector2(.405f,.04f),new Vector2(.98f,.77f),Vector2.zero,Vector2.zero);Image(empty,panel);
                Label("仲間がいません",empty,new Vector2(.08f,.60f),new Vector2(.92f,.78f),24,ink,TextAnchor.MiddleCenter);
                Label("ボスを倒して手に入れた卵は、\nここから孵化できます。",empty,new Vector2(.08f,.35f),new Vector2(.92f,.59f),16,muted,TextAnchor.MiddleCenter);
                Button(empty,"卵を確認する",new Vector2(.25f,.15f),new Vector2(.75f,.30f),ShowEggs,GameUiTheme.PrimaryButton,16);
            }
        }
        private string SortLabel()
        { switch(unitSortMode){case UnitSortMode.RankDescending:return "ランク 高い順";case UnitSortMode.RankAscending:return "ランク 低い順";case UnitSortMode.LevelDescending:return "レベル 高い順";case UnitSortMode.LevelAscending:return "レベル 低い順";default:return "入手順";} }
        private void NextUnitSort(){unitSortMode=(UnitSortMode)(((int)unitSortMode+1)%5);}
        private void ShowEggs()
        {
            ClearContent();
            Label("卵の孵化",content,new Vector2(.02f,.88f),new Vector2(.70f,.98f),30,ink,TextAnchor.MiddleLeft);
            Button(content,"仲間一覧に戻る",new Vector2(.72f,.89f),new Vector2(.98f,.98f),ShowUnits,null,15);
            Label("卵を選んで孵化できます。",content,new Vector2(.02f,.80f),new Vector2(.61f,.88f),16,muted,TextAnchor.MiddleLeft);
            int count=save.eggs==null?0:save.eggs.Count;
            Button(content,"すべて孵化（"+count+"個）",new Vector2(.65f,.80f),new Vector2(.98f,.88f),HatchAllEggs,accent,15).interactable=count>0 && !save.readOnly;
            Sprite eggImage=EggSprite();
            int pages=Mathf.Max(1,Mathf.CeilToInt(count/9f));eggPage=Mathf.Clamp(eggPage,0,pages-1);
            var grid=Rect("Egg List",content,new Vector2(.02f,.13f),new Vector2(.58f,.78f),Vector2.zero,Vector2.zero);Image(grid,panel);
            EggSaveData selected=save.eggs==null?null:save.eggs.Find(e=>e!=null && e.id==selectedEggId);
            if(selected==null && count>0) selected=save.eggs[Mathf.Min(eggPage*9,count-1)];
            for(int i=0;i<9;i++)
            {
                int index=eggPage*9+i;if(index>=count)break;
                EggSaveData egg=save.eggs[index];if(egg==null || egg.offspring==null)continue;
                string id=egg.id;int col=i%3,row=i/3;float x=.025f+col*.32f,top=.97f-row*.31f;
                CharacterIcon("Egg Icon "+id,grid,new Vector2(x,top-.29f),new Vector2(x+.31f,top),"ふしぎな卵",eggImage,"？","",selected!=null && selected.id==id,()=>{selectedEggId=id;ShowEggs();},"ふしぎな卵");
            }
            Button(content,"前へ",new Vector2(.05f,.025f),new Vector2(.18f,.10f),()=>{eggPage=Mathf.Max(0,eggPage-1);selectedEggId=null;ShowEggs();},null,13).interactable=eggPage>0;
            Label((eggPage+1)+" / "+pages+" ページ　卵 "+count+"個",content,new Vector2(.19f,.025f),new Vector2(.41f,.10f),13,muted,TextAnchor.MiddleCenter);
            Button(content,"次へ",new Vector2(.42f,.025f),new Vector2(.55f,.10f),()=>{eggPage=Mathf.Min(pages-1,eggPage+1);selectedEggId=null;ShowEggs();},null,13).interactable=eggPage<pages-1;
            var detail=Rect("Egg Detail",content,new Vector2(.61f,.13f),new Vector2(.98f,.78f),Vector2.zero,Vector2.zero);Image(detail,panel);
            if(selected==null || selected.offspring==null){Label("卵はありません。\n戦闘でボスを倒すと手に入ります。",detail,new Vector2(.05f,.3f),new Vector2(.95f,.7f),18,muted,TextAnchor.MiddleCenter);return;}
            UnitSaveData offspring=selected.offspring;
            var master=speciesDatabase==null?null:speciesDatabase.GetById(offspring.speciesId);
            Label("ふしぎな卵",detail,new Vector2(.05f,.85f),new Vector2(.95f,.97f),20,gold,TextAnchor.MiddleCenter);
            AddImage("卵の姿",detail,new Vector2(.12f,.32f),new Vector2(.88f,.83f),Color.white,eggImage);
            Label("何が生まれるかは孵化してからのお楽しみ",detail,new Vector2(.05f,.23f),new Vector2(.95f,.31f),15,accent,TextAnchor.MiddleCenter);
            string chosenId=selected.id;
            Button(detail,"孵化する",new Vector2(.05f,.06f),new Vector2(.49f,.20f),()=>HatchEgg(chosenId),accent,16).interactable=master!=null && !save.readOnly;
            Button(detail,"卵とお別れ",new Vector2(.52f,.06f),new Vector2(.95f,.20f),()=>ConfirmFarewell(FarewellKind.Egg,chosenId,"ふしぎな卵",ShowEggs),panel,14).interactable=!save.readOnly;
        }
        private static Sprite EggSprite()
        {
            if(eggSprite!=null)return eggSprite;
            var texture=Resources.Load<Texture2D>("UI/MysteryEgg");
            if(texture!=null)eggSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f);
            return eggSprite;
        }
        private void HatchEgg(string eggId)
        {
            GameSaveData committed;UnitSaveData unit;string error;
            if(!EggSystem.TryHatch(save,eggId,out committed,out unit,out error)){Toast("孵化できませんでした："+error);return;}
            save=committed;selectedEggId=null;selectedUnitId=unit.id;unitDetailPage=0;ShowUnits();Toast(unit.displayName+"が孵化して仲間になりました！");
        }
        private void HatchAllEggs()
        {
            if(!EggSystem.TryHatchAll(save,speciesDatabase,out var committed,out var units,out int skipped,out string error))
            {Toast("一括孵化できませんでした："+error);return;}
            save=committed;selectedEggId=null;selectedUnitId=units[0].id;unitDetailPage=0;
            rosterPage=Mathf.Max(0,UnitSort.Apply(save.units,unitSortMode).FindIndex(u=>u.id==selectedUnitId)/9);
            ShowUnits();Toast(units.Count+"個の卵が孵化しました"+(skipped>0?"（孵化できない卵 "+skipped+"個）":""));
        }
        private void ConfirmFarewell(FarewellKind kind,string id,string displayName,Action redraw)
        {
            var overlay=Rect("Farewell Confirmation",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            Image(overlay,new Color(.01f,.02f,.04f,.93f));overlay.GetComponent<Image>().raycastTarget=true;
            var card=Rect("Farewell Card",overlay,new Vector2(.18f,.20f),new Vector2(.82f,.80f),Vector2.zero,Vector2.zero);Image(card,panel);Frame(card,"確認の枠",gold,.006f);
            Label("お別れの確認",card,new Vector2(.06f,.76f),new Vector2(.94f,.95f),24,gold,TextAnchor.MiddleCenter);
            string note=kind==FarewellKind.Unit?"装備は所持品に戻り、家系図と図鑑の記録は残ります。":kind==FarewellKind.Egg?"卵の中身は孵化するまで分かりません。":"この装備を手放します。";
            Label(displayName+"とお別れしますか？\n"+note+"\nお別れしたものは元に戻せません。",card,new Vector2(.07f,.34f),new Vector2(.93f,.73f),16,ink,TextAnchor.MiddleCenter);
            Button(card,"やめる",new Vector2(.08f,.09f),new Vector2(.46f,.25f),()=>Destroy(overlay.gameObject),panel,16);
            Button(card,"お別れする",new Vector2(.54f,.09f),new Vector2(.92f,.25f),()=>
            {
                GameSaveData next;string error;
                if(!FarewellSystem.TryRemove(save,kind,id,out next,out error)){ReportOperationFailure("お別れできませんでした。",error);return;}
                save=next;
                if(kind==FarewellKind.Unit){selectedUnitId=null;if(parentA==id)parentA=null;if(parentB==id)parentB=null;}
                if(kind==FarewellKind.Egg)selectedEggId=null;
                redraw();Toast("お別れしました");
            },new Color(.38f,.15f,.19f),16);
        }
        private static readonly string[] StatNames={"HP","MP","物理攻撃","魔法攻撃","物理防御","魔法防御","移動速度","攻撃速度","索敵範囲","MP回復"};
        private void ShowUnitDetail(UnitSaveData unit)
        {
            var detail=Rect("Unit Detail",content,new Vector2(.405f,.04f),new Vector2(.98f,.77f),Vector2.zero,Vector2.zero);Image(detail,panel);
            Label(unit.displayName,detail,new Vector2(.03f,.925f),new Vector2(.55f,.985f),23,ink,TextAnchor.MiddleLeft);
            Label(FamilyJapanese(unit.family)+" ・ "+unit.rank+"級",detail,new Vector2(.03f,.88f),new Vector2(.55f,.925f),14,accent,TextAnchor.MiddleLeft);
            Label("Lv."+unit.level+"　性格："+Translate(unit.personality),detail,new Vector2(.03f,.835f),new Vector2(.55f,.88f),13,muted,TextAnchor.MiddleLeft);
            ShowUnitExperienceGauge(detail,unit);
            SpeciesDefinition master=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
            var portraitCard=Rect("選択中の仲間の姿",detail,new Vector2(.56f,.815f),new Vector2(.70f,.98f),Vector2.zero,Vector2.zero);
            Image(portraitCard,new Color(.035f,.075f,.12f,.93f));
            Sprite sprite=LoadGeneratedSprite(unit.generatedSpritePath)??(master==null?null:master.sprite);
            AddImage("選択中キャラクター",portraitCard,new Vector2(.03f,.03f),new Vector2(.97f,.97f),Color.white,sprite);
            Button(detail,unit.deployed?"編成から外す":"編成に入れる",new Vector2(.73f,.855f),new Vector2(.97f,.96f),()=>ToggleDeployment(unit),unit.deployed?new Color(.42f,.18f,.22f):accent,14);
            Button(detail,"仲間とお別れ",new Vector2(.73f,.77f),new Vector2(.97f,.84f),()=>ConfirmFarewell(FarewellKind.Unit,unit.id,unit.displayName,ShowUnits),panel,12).interactable=!save.readOnly;
            Button(detail,"1 能力・個体値",new Vector2(.03f,.68f),new Vector2(.33f,.755f),()=>{unitDetailPage=0;ShowUnits();},unitDetailPage==0?new Color(.12f,.35f,.40f):panel,13);
            Button(detail,"2 耐性",new Vector2(.35f,.68f),new Vector2(.65f,.755f),()=>{unitDetailPage=1;ShowUnits();},unitDetailPage==1?new Color(.12f,.35f,.40f):panel,13);
            Button(detail,"3 装備",new Vector2(.67f,.68f),new Vector2(.97f,.755f),()=>{unitDetailPage=2;ShowUnits();},unitDetailPage==2?new Color(.12f,.35f,.40f):panel,13);
            if(unitDetailPage==0)ShowUnitStatCharts(detail,unit,master);else if(unitDetailPage==1)ShowUnitResistances(detail,unit,master);else ShowUnitEquipment(detail,unit);
            int count=unit.magicGenomeJson==null?0:unit.magicGenomeJson.Length;
            Button(detail,"種族パッシブ",new Vector2(.03f,.03f),new Vector2(.39f,.12f),()=>ShowSpeciesPassive(master,ShowUnits),null,13);
            Button(detail,"家系図",new Vector2(.405f,.03f),new Vector2(.595f,.12f),()=>ShowFamilyTree(unit),null,13);
            Button(detail,"固有魔法を見る",new Vector2(.61f,.03f),new Vector2(.97f,.12f),()=>ShowUnitMagic(unit),new Color(.20f,.26f,.43f),13);
        }
        private void ShowUnitExperienceGauge(RectTransform detail,UnitSaveData unit)
        {
            bool maxLevel=unit.level>=ExperienceSystem.MaxLevel;
            long required=maxLevel?0:ExperienceSystem.ExperienceForNextLevel(unit.level);
            long current=maxLevel?0:Math.Max(0,Math.Min(unit.experience,required));
            float progress=maxLevel?1f:(float)((double)current/required);
            var track=Rect("Experience Gauge",detail,new Vector2(.03f,.757f),new Vector2(.70f,.782f),Vector2.zero,Vector2.zero);
            Image(track,new Color(.035f,.075f,.12f,.98f));
            var fill=Rect("Experience Gauge Fill",track,Vector2.zero,new Vector2(progress,1f),Vector2.zero,Vector2.zero);
            Image(fill,accent);
            var text=Label(maxLevel?"経験値：レベル最大":"経験値 "+current.ToString("N0")+" / "+required.ToString("N0")+"　次まで "+(required-current).ToString("N0"),detail,new Vector2(.03f,.785f),new Vector2(.55f,.835f),12,ink,TextAnchor.MiddleLeft);
            text.gameObject.name="Experience Gauge Text";
        }
        private void ShowUnitEquipment(RectTransform detail,UnitSaveData unit)
        {
            var current=unit.equipment;
            if(!EquipmentCatalog.HasItem(current))current=null;
            Label("装備中："+EquipmentCatalog.Name(current),detail,new Vector2(.035f,.60f),new Vector2(.60f,.67f),15,gold,TextAnchor.MiddleLeft);
            Button(detail,"外す",new Vector2(.61f,.60f),new Vector2(.76f,.67f),()=>SetEquipment(unit.id,null),null,13).interactable=current!=null && !save.readOnly;
            Button(detail,"お別れ",new Vector2(.78f,.60f),new Vector2(.965f,.67f),()=>ConfirmFarewell(FarewellKind.Equipment,current.id,EquipmentCatalog.Name(current),ShowUnits),panel,12).interactable=current!=null && !save.readOnly;
            if(current!=null)Label(EquipmentBonuses(current),detail,new Vector2(.035f,.55f),new Vector2(.965f,.60f),12,accent,TextAnchor.MiddleLeft);
            int count=save.equipmentInventory==null?0:save.equipmentInventory.Count;
            int pages=Mathf.Max(1,Mathf.CeilToInt(count/3f));equipmentPage=Mathf.Clamp(equipmentPage,0,pages-1);
            for(int i=0;i<3;i++)
            {
                int index=equipmentPage*3+i;if(index>=count)break;
                var item=save.equipmentInventory[index];if(item==null)continue;float top=.54f-i*.105f;
                var row=Rect("Equipment "+item.id,detail,new Vector2(.035f,top-.095f),new Vector2(.965f,top),Vector2.zero,Vector2.zero);Image(row,new Color(.035f,.075f,.12f,.94f));
                Label(EquipmentCatalog.Name(item),row,new Vector2(.025f,.53f),new Vector2(.64f,.98f),13,ink,TextAnchor.MiddleLeft);
                Label(EquipmentBonuses(item),row,new Vector2(.025f,.04f),new Vector2(.64f,.52f),12,accent,TextAnchor.MiddleLeft);
                string itemId=item.id;
                Button(row,"装備",new Vector2(.66f,.12f),new Vector2(.81f,.88f),()=>SetEquipment(unit.id,itemId),accent,12).interactable=!save.readOnly;
                Button(row,"お別れ",new Vector2(.83f,.12f),new Vector2(.98f,.88f),()=>ConfirmFarewell(FarewellKind.Equipment,itemId,EquipmentCatalog.Name(item),ShowUnits),panel,12).interactable=!save.readOnly;
            }
            if(count==0)Label("所持装備はありません。敵を倒して探しましょう。",detail,new Vector2(.035f,.29f),new Vector2(.965f,.45f),13,muted,TextAnchor.MiddleCenter);
            Button(detail,"前へ",new Vector2(.05f,.15f),new Vector2(.25f,.21f),()=>{equipmentPage=Mathf.Max(0,equipmentPage-1);ShowUnits();},null,12).interactable=equipmentPage>0;
            Label("所持 "+count+"個　"+(equipmentPage+1)+" / "+pages,detail,new Vector2(.28f,.15f),new Vector2(.72f,.21f),12,muted,TextAnchor.MiddleCenter);
            Button(detail,"次へ",new Vector2(.75f,.15f),new Vector2(.95f,.21f),()=>{equipmentPage=Mathf.Min(pages-1,equipmentPage+1);ShowUnits();},null,12).interactable=equipmentPage<pages-1;
        }
        private void ShowFamilyTree(UnitSaveData unit)
        {
            ClearContent();
            Label(unit.displayName+"の家系図",content,new Vector2(.025f,.89f),new Vector2(.70f,.98f),27,ink,TextAnchor.MiddleLeft);
            Button(content,"仲間の詳細へ戻る",new Vector2(.72f,.89f),new Vector2(.98f,.98f),()=>{selectedUnitId=unit.id;ShowUnits();},null,15);
            Label("親と祖父母を表示します。以前の配合で失われた記録は「記録なし」と表示します。",content,new Vector2(.025f,.81f),new Vector2(.975f,.88f),14,muted,TextAnchor.MiddleLeft);
            var root=AncestrySystem.Find(save,unit.id);
            var a=AncestrySystem.Find(save,root==null?unit.fatherId:root.fatherId);
            var b=AncestrySystem.Find(save,root==null?unit.motherId:root.motherId);
            var tree=Rect("Family Tree",content,new Vector2(.025f,.025f),new Vector2(.975f,.80f),Vector2.zero,Vector2.zero);Image(tree,panel);
            Color line=new Color(.34f,.52f,.57f,.8f);
            ColorBlock("親Aと祖父母",tree,new Vector2(.14f,.68f),new Vector2(.365f,.686f),line);
            ColorBlock("親Bと祖父母",tree,new Vector2(.635f,.68f),new Vector2(.86f,.686f),line);
            foreach(float x in new[]{.14f,.365f,.635f,.86f})ColorBlock("祖父母の線",tree,new Vector2(x-.002f,.68f),new Vector2(x+.002f,.71f),line);
            foreach(float x in new[]{.2525f,.7475f})ColorBlock("親の線",tree,new Vector2(x-.002f,.635f),new Vector2(x+.002f,.685f),line);
            ColorBlock("両親の線",tree,new Vector2(.2525f,.34f),new Vector2(.7475f,.346f),line);
            foreach(float x in new[]{.2525f,.7475f})ColorBlock("親の下の線",tree,new Vector2(x-.002f,.34f),new Vector2(x+.002f,.39f),line);
            ColorBlock("本人の線",tree,new Vector2(.498f,.295f),new Vector2(.502f,.346f),line);
            DrawAncestor(tree,"親Aの親A",a==null?null:AncestrySystem.Find(save,a.fatherId),a==null || !string.IsNullOrEmpty(a.fatherId),new Vector2(.035f,.71f),new Vector2(.245f,.98f));
            DrawAncestor(tree,"親Aの親B",a==null?null:AncestrySystem.Find(save,a.motherId),a==null || !string.IsNullOrEmpty(a.motherId),new Vector2(.26f,.71f),new Vector2(.47f,.98f));
            DrawAncestor(tree,"親Bの親A",b==null?null:AncestrySystem.Find(save,b.fatherId),b==null || !string.IsNullOrEmpty(b.fatherId),new Vector2(.53f,.71f),new Vector2(.74f,.98f));
            DrawAncestor(tree,"親Bの親B",b==null?null:AncestrySystem.Find(save,b.motherId),b==null || !string.IsNullOrEmpty(b.motherId),new Vector2(.755f,.71f),new Vector2(.965f,.98f));
            DrawAncestor(tree,"親A",a,root!=null && !string.IsNullOrEmpty(root.fatherId),new Vector2(.145f,.39f),new Vector2(.36f,.635f));
            DrawAncestor(tree,"親B",b,root!=null && !string.IsNullOrEmpty(root.motherId),new Vector2(.64f,.39f),new Vector2(.855f,.635f));
            DrawAncestor(tree,"本人",root,true,new Vector2(.39f,.025f),new Vector2(.61f,.295f));
        }
        private void DrawAncestor(RectTransform tree,string label,AncestorRecord record,bool missingRecord,Vector2 min,Vector2 max)
        {
            var card=Rect("Family "+label,tree,min,max,Vector2.zero,Vector2.zero);Image(card,new Color(.035f,.075f,.12f,.96f));
            Label(label,card,new Vector2(.04f,.79f),new Vector2(.96f,.98f),12,gold,TextAnchor.MiddleCenter);
            if(record==null)
            {
                Label(missingRecord?"記録なし":"親の情報なし",card,new Vector2(.04f,.15f),new Vector2(.96f,.78f),13,muted,TextAnchor.MiddleCenter);return;
            }
            var species=speciesDatabase==null?null:speciesDatabase.GetById(record.speciesId);
            if(species!=null && species.sprite!=null)AddImage("家系の姿",card,new Vector2(.05f,.19f),new Vector2(.45f,.79f),Color.white,species.sprite);
            TMP_Text name=Label(string.IsNullOrEmpty(record.displayName)?"名前不明":record.displayName,card,new Vector2(.46f,.40f),new Vector2(.97f,.76f),13,ink,TextAnchor.MiddleCenter);TightText(name);
            TMP_Text info=Label(FamilyJapanese(record.family)+"\n"+record.rank+"級",card,new Vector2(.46f,.10f),new Vector2(.97f,.39f),11,accent,TextAnchor.MiddleCenter);TightText(info);
        }
        private static string EquipmentBonuses(EquipmentItem item)
        {
            var bonuses=new List<string>();for(int stat=0;stat<10;stat++){float value=EquipmentCatalog.Bonus(item,stat);if(value>0)bonuses.Add(StatNames[stat]+" +"+FormatNumber(value)+"%");}return string.Join("  ",bonuses);
        }
        private void SetEquipment(string unitId,string equipmentId)
        {
            if(save.readOnly)return;
            var next=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
            var unit=next.units.Find(u=>u!=null && u.id==unitId);if(unit==null)return;
            if(next.equipmentInventory==null)next.equipmentInventory=new List<EquipmentItem>();
            var item=string.IsNullOrEmpty(equipmentId)?null:next.equipmentInventory.Find(e=>e!=null && e.id==equipmentId);
            if(!string.IsNullOrEmpty(equipmentId) && item==null)return;
            if(item!=null)next.equipmentInventory.Remove(item);
            if(EquipmentCatalog.HasItem(unit.equipment))next.equipmentInventory.Add(unit.equipment);
            unit.equipment=item;
            if(next.autoDiscardOldEquipment)EquipmentCatalog.DiscardOldestExcess(next);
            if(!SaveSystem.Save(next,out string error)){Debug.LogWarning("Could not save equipment: "+error);return;}
            save=next;
            ShowUnits();Toast(string.IsNullOrEmpty(equipmentId)?"装備を外しました":"装備を変更しました");
        }
        private void ShowUnitStatCharts(RectTransform detail,UnitSaveData unit,SpeciesDefinition master)
        {
            UnitStats finalStats=master==null?null:UnitStatsCalculator.Calculate(master,ToUnit(unit),null,personalityDatabase);
            float[] values=new float[10];
            for(int i=0;i<10;i++)values[i]=finalStats==null?(unit.baseStats!=null && i<unit.baseStats.Length?unit.baseStats[i]:0):finalStats.Get(i);
            Label("能力値（全キャラ共通基準）",detail,new Vector2(.025f,.63f),new Vector2(.49f,.675f),12,accent,TextAnchor.MiddleLeft);
            var bars=Rect("Stat Bar List",detail,new Vector2(.025f,.15f),new Vector2(.49f,.63f),Vector2.zero,Vector2.zero);
            for(int i=0;i<10;i++)
            {
                float top=1-i*.10f;
                var row=Rect("Stat Row "+i,bars,new Vector2(0,top-.094f),new Vector2(1,top),Vector2.zero,Vector2.zero);
                TMP_Text name=Label(StatNames[i],row,new Vector2(0,0),new Vector2(.35f,1),13,ink,TextAnchor.MiddleLeft);TightText(name);
                var track=Rect("Stat Track",row,new Vector2(.36f,.25f),new Vector2(.82f,.75f),Vector2.zero,Vector2.zero);Image(track,new Color(.025f,.045f,.075f));
                var fill=Rect("Stat Fill "+i,track,Vector2.zero,new Vector2(StatBarScaleSettings.Normalize(i,values[i],statBarScaleSettings),1),Vector2.zero,Vector2.zero);Image(fill,i<2?gold:accent);
                TMP_Text number=Label(FormatNumber(values[i]),row,new Vector2(.84f,0),Vector2.one,13,ink,TextAnchor.MiddleRight);TightText(number);
            }
            Label("個体値（0～100）",detail,new Vector2(.52f,.63f),new Vector2(.98f,.675f),12,accent,TextAnchor.MiddleCenter);
            var space=Rect("IV Radar Space",detail,new Vector2(.52f,.14f),new Vector2(.98f,.63f),Vector2.zero,Vector2.zero);
            var square=Rect("IV Radar Square",space,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var aspect=square.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            var plot=Rect("IV Radar",square,new Vector2(.24f,.24f),new Vector2(.76f,.76f),Vector2.zero,Vector2.zero);
            var chart=plot.gameObject.AddComponent<IndividualValueRadar>();chart.color=accent;chart.SetValues(unit.individualValues);
            Vector2[] positions={new Vector2(.5f,.935f),new Vector2(.82f,.805f),new Vector2(.86f,.61f),new Vector2(.86f,.39f),new Vector2(.82f,.195f),new Vector2(.5f,.065f),new Vector2(.18f,.195f),new Vector2(.14f,.39f),new Vector2(.14f,.61f),new Vector2(.18f,.805f)};
            for(int i=0;i<10;i++)
            {
                int value=IndividualValues.Clamp(unit.individualValues!=null && i<unit.individualValues.Length?unit.individualValues[i]:0);
                TMP_Text label=Label(StatNames[i]+"\n"+value,square,positions[i]-new Vector2(.14f,.065f),positions[i]+new Vector2(.14f,.065f),10,ink,TextAnchor.MiddleCenter);
                label.gameObject.name="IV Axis "+i;TightText(label);
            }
        }
        private void ShowUnitResistances(RectTransform detail,UnitSaveData unit,SpeciesDefinition master)
        {
            var page=Rect("Unit Resistance Page",detail,new Vector2(.025f,.15f),new Vector2(.975f,.67f),Vector2.zero,Vector2.zero);
            ShowResistanceCharts(page,master,unit);
        }
        private void ShowResistanceCharts(RectTransform page,SpeciesDefinition master,UnitSaveData unit)
        {
            string[] headings={"属性ダメージ軽減","属性の状態異常抵抗","状態異常ごとの抵抗"};
            for(int category=0;category<3;category++)
            {
                float left=category/3f;
                var column=Rect("Resistance Column "+category,page,new Vector2(left,0),new Vector2(left+1/3f,1),Vector2.zero,Vector2.zero);
                Color tint=category==0?accent:category==1?gold:new Color(.70f,.57f,.96f);
                TMP_Text heading=Label(headings[category]+"\n0～100%",column,new Vector2(.02f,.83f),new Vector2(.98f,1),12,tint,TextAnchor.MiddleCenter);TightText(heading);
                int entries=category==2?8:7;
                for(int i=0;i<entries;i++)
                {
                    float amount=0;
                    if(master!=null){amount=category==0?SpeciesPassiveCalculator.ElementReduction(master,(AttributeElement)i):category==1?master.GetResistance((AttributeElement)i).applicationResistance:SpeciesPassiveCalculator.DebuffReduction(master,(DebuffType)i);}
                    else if(unit!=null){float[] source=category==0?unit.attributeDamageReduction:category==1?unit.attributeAilmentResistance:unit.debuffResistance;if(source!=null && i<source.Length)amount=source[i];}
                    amount=Mathf.Clamp01(amount);
                    float top=.82f-i*.10f;
                    var row=Rect("Resistance Row "+category+" "+i,column,new Vector2(.04f,top-.094f),new Vector2(.96f,top),Vector2.zero,Vector2.zero);
                    TMP_Text label=Label((category==2?DebuffJapanese((DebuffType)i):ElementJapanese((AttributeElement)i))+"　"+FormatPercentage(amount),row,new Vector2(0,.30f),Vector2.one,12,ink,TextAnchor.MiddleLeft);TightText(label);
                    label.gameObject.name="Resistance "+category+" "+i;
                    var track=Rect("Resistance Track",row,new Vector2(.02f,.04f),new Vector2(.98f,.25f),Vector2.zero,Vector2.zero);Image(track,new Color(.025f,.045f,.075f));
                    var fill=Rect("Resistance Fill "+category+" "+i,track,Vector2.zero,new Vector2(amount,1),Vector2.zero,Vector2.zero);Image(fill,tint);
                    var midpoint=Rect("50% Mark",track,new Vector2(.497f,0),new Vector2(.503f,1),Vector2.zero,Vector2.zero);Image(midpoint,new Color(.8f,.9f,1,.35f));
                }
            }
        }
        private static void TightText(TMP_Text text) { text.rectTransform.offsetMin=new Vector2(2,0);text.rectTransform.offsetMax=new Vector2(-2,0); }
        private void ShowSpeciesPassive(SpeciesDefinition species,Action back)
        {
            ClearContent();
            Label("種族パッシブ",content,new Vector2(.02f,.88f),new Vector2(.98f,.99f),28,gold,TextAnchor.MiddleLeft);
            Label("同じ種族の個体が共通で持つ、生まれつきの特性です。",content,new Vector2(.02f,.81f),new Vector2(.98f,.89f),15,muted,TextAnchor.MiddleLeft);
            var panelRoot=Rect("Species Passive Details",content,new Vector2(.02f,.20f),new Vector2(.98f,.79f),Vector2.zero,Vector2.zero);Image(panelRoot,panel);
            if(species!=null)
            {
                AddImage("パッシブの種族画像",panelRoot,new Vector2(.03f,.22f),new Vector2(.33f,.95f),Color.white,species.sprite);
                Label(species.displayName,panelRoot,new Vector2(.02f,.06f),new Vector2(.35f,.20f),18,ink,TextAnchor.MiddleCenter);
            }
            SpeciesPassive passive=species==null?null:species.passive;
            Label(passive==null?"パッシブ未設定":passive.displayName,panelRoot,new Vector2(.39f,.73f),new Vector2(.97f,.96f),25,accent,TextAnchor.MiddleLeft);
            var scrollRoot=Rect("Passive Effect Scroll",panelRoot,new Vector2(.39f,.10f),new Vector2(.97f,.72f),Vector2.zero,Vector2.zero);
            var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
            var viewport=Rect("Viewport",scrollRoot,Vector2.zero,new Vector2(.95f,1),Vector2.zero,Vector2.zero);Image(viewport,new Color(.025f,.045f,.075f,.6f));viewport.gameObject.AddComponent<RectMask2D>();scroll.viewport=viewport;
            var textContent=Rect("Passive Effect Content",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);textContent.pivot=new Vector2(.5f,1);scroll.content=textContent;
            var effectsText=Label(passive==null?"この種族のデータを確認してください。":passive.Description,textContent,Vector2.zero,Vector2.one,17,ink,TextAnchor.UpperLeft);
            effectsText.gameObject.name="Passive Effect Details";effectsText.enableAutoSizing=false;effectsText.overflowMode=TextOverflowModes.Overflow;effectsText.rectTransform.offsetMin=new Vector2(6,6);effectsText.rectTransform.offsetMax=new Vector2(-6,-6);
            Canvas.ForceUpdateCanvases();float height=Mathf.Max(viewport.rect.height,effectsText.GetPreferredValues(effectsText.text,Mathf.Max(1,viewport.rect.width-12),Mathf.Infinity).y+16);textContent.sizeDelta=new Vector2(0,height);
            var scrollbarRoot=Rect("Scrollbar",scrollRoot,new Vector2(.965f,0),Vector2.one,Vector2.zero,Vector2.zero);Image(scrollbarRoot,new Color(.025f,.045f,.075f));
            var handle=Rect("Handle",scrollbarRoot,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Image(handle,accent);
            var scrollbar=scrollbarRoot.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;scroll.verticalNormalizedPosition=1;
            Label("配合した子は、子の種族のパッシブを持ちます。",content,new Vector2(.02f,.115f),new Vector2(.98f,.19f),14,muted,TextAnchor.MiddleCenter);
            Button(content,"元の画面へ戻る",new Vector2(.35f,.025f),new Vector2(.65f,.105f),back,null,15);
        }
        private void ShowBreeding()
        {
            ClearContent();
            if (!string.IsNullOrEmpty(choosingParentSlot))
            {
                ShowParentSelection();
                return;
            }
            Label("配合", content, new Vector2(.02f, .88f), new Vector2(.70f, .98f), 30, ink, TextAnchor.MiddleLeft);
            Label("配合すると親2体は消滅し、子1体が誕生します。親が編成中なら子を編成に引き継ぎます。", content, new Vector2(.02f, .80f), new Vector2(.98f, .89f), 16, gold, TextAnchor.MiddleLeft);
            ParentPicker("親Aを選ぶ", parentA, new Vector2(.02f, .56f));
            ParentPicker("親Bを選ぶ", parentB, new Vector2(.51f, .56f));

            var result = Rect("Breeding Actions", content, new Vector2(.02f, .08f), new Vector2(.98f, .50f), Vector2.zero, Vector2.zero);
            Image(result, panel);
            UnitSaveData a = FindUnit(parentA);
            UnitSaveData b = FindUnit(parentB);
            bool canBreed = a != null && b != null && a.id != b.id;
            string guidance=a==null || b==null?"親Aと親Bをそれぞれ選択してください。":a.id==b.id?"別々の仲間を選んでください。":"親2体を選択しました。配合できます。";
            Label(guidance,result,new Vector2(.05f,.56f),new Vector2(.95f,.84f),18,ink,TextAnchor.MiddleCenter);
            Button(result,"この組み合わせで配合",new Vector2(.28f,.18f),new Vector2(.72f,.43f),BreedSelected,canBreed?accent:new Color(.20f,.23f,.28f),17).interactable=canBreed;
        }

        private void ParentPicker(string title, string selected, Vector2 position)
        {
            UnitSaveData chosen = FindUnit(selected);
            string slot=title.Contains("A")?"A":"B";
            SpeciesDefinition master=chosen==null || speciesDatabase==null?null:speciesDatabase.GetById(chosen.speciesId);
            Sprite sprite=chosen==null?null:LoadGeneratedSprite(chosen.generatedSpritePath)??(master==null?null:master.sprite);
            var card=CharacterIcon("Parent Slot "+slot,content,position,position+new Vector2(.46f,.22f),chosen==null?"所持仲間から選ぶ":chosen.displayName,sprite,chosen==null?"―":chosen.rank,"親"+slot,false,() =>
            {
                choosingParentSlot = slot;
                parentPickerPage = 0;
                ShowBreeding();
            },"＋");
            SetAnchors(card.transform.Find("キャラクターアイコン") as RectTransform,new Vector2(.03f,.05f),new Vector2(.30f,.95f));
            SetAnchors(card.transform.Find("画像なし") as RectTransform,new Vector2(.03f,.12f),new Vector2(.30f,.88f));
            var empty=card.transform.Find("画像なし");if(empty!=null){var text=empty.GetComponent<TMP_Text>();text.fontSize=text.fontSizeMax=30;text.fontSizeMin=18;text.color=muted;}
            var name=card.transform.Find("キャラクター名") as RectTransform;
            SetAnchors(name,new Vector2(.33f,.12f),new Vector2(.96f,.64f));
            if(name!=null){var text=name.GetComponent<TMP_Text>();text.fontSize=text.fontSizeMax=18;text.fontSizeMin=13;text.alignment=TextAlignmentOptions.MidlineLeft;}
            SetAnchors(card.transform.Find("ランク") as RectTransform,new Vector2(.34f,.70f),new Vector2(.48f,.95f));
            SetAnchors(card.transform.Find("編成状態") as RectTransform,new Vector2(.52f,.70f),new Vector2(.77f,.95f));
        }

        private void ShowParentSelection()
        {
            string parentLabel = choosingParentSlot == "A" ? "親A" : "親B";
            Label(parentLabel + "を選択", content, new Vector2(.02f,.90f), new Vector2(.75f,.99f), 28, ink, TextAnchor.MiddleLeft);
            Label("所持している仲間から選んでください。", content, new Vector2(.02f,.83f), new Vector2(.73f,.91f), 15, muted, TextAnchor.MiddleLeft);
            var sortedUnits=UnitSort.Apply(save.units,unitSortMode);
            Button(content,"並び："+SortLabel(),new Vector2(.75f,.835f),new Vector2(.98f,.905f),()=>{NextUnitSort();parentPickerPage=0;ShowBreeding();},panel,13);
            int pageSize=18, pages=Mathf.Max(1,Mathf.CeilToInt(sortedUnits.Count/(float)pageSize));
            parentPickerPage=Mathf.Clamp(parentPickerPage,0,pages-1);
            var grid=Rect("Parent Icon Grid",content,new Vector2(.02f,.09f),new Vector2(.98f,.81f),Vector2.zero,Vector2.zero);
            for(int i=0;i<pageSize;i++)
            {
                int index=parentPickerPage*pageSize+i;
                if(index>=sortedUnits.Count) break;
                UnitSaveData unit=sortedUnits[index];
                int col=i%6,row=i/6;
                float x=col/6f,top=1-row/3f;
                string id=unit.id;
                string other=choosingParentSlot=="A"?parentB:parentA;
                string selected=choosingParentSlot=="A"?parentA:parentB;
                SpeciesDefinition species=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
                Sprite portraitSprite=LoadGeneratedSprite(unit.generatedSpritePath)??(species==null?null:species.sprite);
                string badge=id==parentA?"親A":id==parentB?"親B":"Lv."+unit.level;
                Button entry=CharacterIcon("Parent Icon "+id,grid,new Vector2(x,top-1/3f),new Vector2(x+1/6f,top),unit.displayName,portraitSprite,unit.rank,badge,id==selected,() =>
                {
                    if(id==other){Toast("親Aと親Bには別々の仲間を選んでください");return;}
                    if(choosingParentSlot=="A")parentA=id;else parentB=id;
                    choosingParentSlot=null; ShowBreeding();
                });
                entry.interactable=id!=other;
            }
            Button(content,"戻る",new Vector2(.02f,.005f),new Vector2(.16f,.075f),()=>{choosingParentSlot=null;ShowBreeding();},null,14);
            Button(content,"前へ",new Vector2(.37f,.005f),new Vector2(.47f,.075f),()=>{parentPickerPage=Mathf.Max(0,parentPickerPage-1);ShowBreeding();},null,14).interactable=parentPickerPage>0;
            Label((parentPickerPage+1)+" / "+pages+" ページ（"+sortedUnits.Count+"体）",content,new Vector2(.47f,.005f),new Vector2(.59f,.075f),13,muted,TextAnchor.MiddleCenter);
            Button(content,"次へ",new Vector2(.59f,.005f),new Vector2(.69f,.075f),()=>{parentPickerPage=Mathf.Min(pages-1,parentPickerPage+1);ShowBreeding();},null,14).interactable=parentPickerPage<pages-1;
        }
        private void BreedSelected()
        {
            UnitSaveData a = FindUnit(parentA);
            UnitSaveData b = FindUnit(parentB);
            if (a == null || b == null) { Toast("親Aと親Bを選んでください"); return; }
            if (a.id == b.id) { Toast("異なる仲間を選んでください"); return; }
            try
            {
                var breeding = new BreedingSystem(speciesDatabase, breedingRuleDatabase, rankBreedingRule,ancestorSpeciesLookup:id=>AncestrySystem.Find(save,id)?.speciesId);
                BreedingResult outcome = breeding.Breed(ToUnit(a), ToUnit(b), BreedingSystem.CreateRandomSeed());
                UnitSaveData child = FromUnit(outcome.child, outcome.species);
                // Commit a separate snapshot so a failed save cannot consume either parent in memory.
                GameSaveData nextSave=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
                nextSave.readOnly=save.readOnly;
                SaveSystem.RecordOwnedSpecies(nextSave);
                foreach(var parent in nextSave.units)if(parent!=null && (parent.id==a.id || parent.id==b.id))AncestrySystem.Record(nextSave,parent);
                if(nextSave.equipmentInventory==null)nextSave.equipmentInventory=new List<EquipmentItem>();
                foreach(var parent in nextSave.units)if(parent!=null && (parent.id==a.id || parent.id==b.id) && EquipmentCatalog.HasItem(parent.equipment))nextSave.equipmentInventory.Add(parent.equipment);
                nextSave.units.RemoveAll(unit=>unit.id==a.id || unit.id==b.id);
                nextSave.formationUnitIds.RemoveAll(id=>id==a.id || id==b.id);
                child.deployed=a.deployed || b.deployed;
                nextSave.units.Add(child);
                if(nextSave.autoDiscardOldEquipment)EquipmentCatalog.DiscardOldestExcess(nextSave);
                if (!SaveSystem.Save(nextSave, out string saveError))
                {
                    ShowBreeding();
                    ReportOperationFailure("配合結果を保存できませんでした。空き容量や保存設定を確認してください。",saveError);
                    return;
                }
                save=nextSave;
                parentA=null;parentB=null;choosingParentSlot=null;
                selectedUnitId = child.id;unitDetailPage=0;
                ShowBreedingOutcome(child);
                Toast((outcome.isSpecial?"特別な配合で仲間が誕生しました：":"親2体を配合し、新しい仲間が誕生しました：") + child.displayName + "　・　" + child.rank + "級　・　魔法 " + child.magicGenomeJson.Length + "個");
            }
            catch (Exception exception) { ReportOperationFailure("配合に失敗しました。親の選択をやり直してください。",exception.ToString()); }
        }
        private void ShowBreedingOutcome(UnitSaveData child)
        {
            ClearContent();
            Label("新しい仲間が誕生しました",content,new Vector2(.02f,.88f),new Vector2(.98f,.99f),28,gold,TextAnchor.MiddleCenter);
            Label("アイコンを選ぶと、能力や固有魔法を確認できます。",content,new Vector2(.02f,.81f),new Vector2(.98f,.89f),15,muted,TextAnchor.MiddleCenter);
            var outcome=Rect("Breeding Outcome",content,new Vector2(.02f,.15f),new Vector2(.98f,.79f),Vector2.zero,Vector2.zero);Image(outcome,panel);
            SpeciesDefinition master=speciesDatabase==null?null:speciesDatabase.GetById(child.speciesId);
            Sprite sprite=LoadGeneratedSprite(child.generatedSpritePath)??(master==null?null:master.sprite);
            Action inspect=()=>
            {
                selectedUnitId=child.id;unitDetailPage=0;
                rosterPage=Mathf.Max(0,UnitSort.Apply(save.units,unitSortMode).FindIndex(unit=>unit.id==child.id)/9);
                Navigate("UNITS");
            };
            CharacterIcon("Breeding Child Icon "+child.id,outcome,new Vector2(.34f,.14f),new Vector2(.66f,.95f),child.displayName,sprite,child.rank,child.deployed?"編成中":"Lv."+child.level,true,inspect);
            Label(FamilyJapanese(child.family)+"　・　Lv."+child.level,outcome,new Vector2(.18f,.015f),new Vector2(.82f,.13f),16,accent,TextAnchor.MiddleCenter);
            Button(content,"続けて配合する",new Vector2(.03f,.025f),new Vector2(.31f,.105f),ShowBreeding,null,15);
            Button(content,"仲間の詳細を見る",new Vector2(.69f,.025f),new Vector2(.97f,.105f),inspect,accent,15);
        }
        private void ShowUnitMagic(UnitSaveData unit)
        {
            ClearContent();
            Label(unit.displayName + "の固有魔法", content, new Vector2(.02f, .88f), new Vector2(.98f, .98f), 28, ink, TextAnchor.MiddleLeft);
            Label("この個体が持つ魔法です。選択すると実際の動きを見ることができます。", content, new Vector2(.02f, .80f), new Vector2(.98f, .89f), 15, muted, TextAnchor.MiddleLeft);
            var profile = Rect("Magic Unit", content, new Vector2(.02f, .67f), new Vector2(.98f, .77f), Vector2.zero, Vector2.zero);
            Image(profile, panel);
            int magicCount = unit.magicGenomeJson == null ? 0 : unit.magicGenomeJson.Length;
            Label(unit.displayName + "　・　" + FamilyJapanese(unit.family) + "　・　" + unit.rank + "級　・　固有魔法 " + magicCount + "個", profile, new Vector2(.025f, .08f), new Vector2(.97f, .92f), 17, accent, TextAnchor.MiddleLeft);
            Label("見たい魔法を選んでください。", content, new Vector2(.02f, .61f), new Vector2(.98f, .67f), 13, muted, TextAnchor.MiddleLeft);
            if (magicCount == 0)
                Label("この仲間はまだ固有魔法を持っていません。配合で親の魔法の特徴を受け継げます。", content, new Vector2(.04f, .40f), new Vector2(.96f, .55f), 16, ink, TextAnchor.MiddleCenter);
            else
            {
                for (int i = 0; i < magicCount && i < 5; i++)
                {
                    float top = .60f - i * .102f;
                    float bottom = top - .084f;
                    int index = i;
                    MagicGenome genome;
                    string summary="";
                    if(MagicGenome.TryParseJson(unit.magicGenomeJson[i],out genome))
                    {
                        SpeciesDefinition species=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
                        BuiltMagic magic=MagicFactory.Build(genome,species);
                        summary="　"+magic.displayName+"　／　"+MagicDescription.Effects(magic,3);
                    }
                    Button(content,"固有魔法 " + (i+1) + summary,new Vector2(.02f,bottom),new Vector2(.98f,top),()=>ShowUnitMagicPreview(unit,index),panel,14);
                }
            }
            Button(content,"仲間の詳細へ戻る",new Vector2(.02f,.025f),new Vector2(.32f,.09f),()=>{selectedUnitId=unit.id;ShowUnits();},null,14);
        }
        private void ShowUnitMagicPreview(UnitSaveData unit, int index)
        {
            ClearContent();
            TMP_Text title=Label(unit.displayName + "の固有魔法 " + (index+1),content,new Vector2(.02f,.88f),new Vector2(.98f,.99f),26,ink,TextAnchor.MiddleLeft);
            MagicGenome genome;
            if (unit.magicGenomeJson == null || index < 0 || index >= unit.magicGenomeJson.Length || !MagicGenome.TryParseJson(unit.magicGenomeJson[index],out genome))
                Label("この魔法のプレビューを表示できません。",content,new Vector2(.04f,.45f),new Vector2(.96f,.65f),18,ink,TextAnchor.MiddleCenter);
            else
            {
                SpeciesDefinition species=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
                BuiltMagic magic=MagicFactory.Build(genome,species);
                title.text=unit.displayName+"の「"+magic.displayName+"」";
                Label("属性：" + ElementJapanese(magic.element),content,new Vector2(.02f,.81f),new Vector2(.32f,.89f),17,accent,TextAnchor.MiddleLeft);
                Label("威力：" + FormatNumber(magic.power),content,new Vector2(.34f,.81f),new Vector2(.64f,.89f),17,ink,TextAnchor.MiddleLeft);
                Label("消費MP：" + FormatNumber(magic.mpCost),content,new Vector2(.66f,.81f),new Vector2(.98f,.89f),17,ink,TextAnchor.MiddleLeft);
                RectTransform previewRoot=Rect("Individual Magic Preview",content,new Vector2(.02f,.25f),new Vector2(.98f,.79f),Vector2.zero,Vector2.zero);
                Image(previewRoot,panel);
                RectTransform imageRect=Rect("Magic Preview Image",previewRoot,new Vector2(.01f,.02f),new Vector2(.99f,.98f),Vector2.zero,Vector2.zero);
                imageRect.pivot=new Vector2(.5f,.5f);
                imageRect.anchoredPosition=Vector2.zero;
                RawImage image=imageRect.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
                AspectRatioFitter fit=imageRect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9f;
                Sprite sprite=LoadGeneratedSprite(unit.generatedSpritePath)??(species==null?null:species.sprite);
                MagicPreviewController preview=previewRoot.gameObject.AddComponent<MagicPreviewController>();
                preview.Initialize(unit,species,personalityDatabase,genome,FullPortrait(sprite),image);
                TMP_Text effects=Label("追加効果："+MagicDescription.Effects(magic,5),content,new Vector2(.02f,.14f),new Vector2(.98f,.24f),15,accent,TextAnchor.MiddleCenter);
                effects.gameObject.name="Magic Effect Summary";
                Button(content,"効果・詳細を見る",new Vector2(.37f,.025f),new Vector2(.66f,.105f),()=>ShowUnitMagicDetails(unit,index),null,14);
                Button(content,"もう一度見る",new Vector2(.70f,.025f),new Vector2(.98f,.105f),preview.Replay,accent,15);
            }
            Button(content,"この個体の魔法へ戻る",new Vector2(.02f,.025f),new Vector2(.34f,.105f),()=>ShowUnitMagic(unit),null,14);
        }
        private void ShowUnitMagicDetails(UnitSaveData unit,int index)
        {
            MagicGenome genome;
            if(unit.magicGenomeJson==null || index<0 || index>=unit.magicGenomeJson.Length || !MagicGenome.TryParseJson(unit.magicGenomeJson[index],out genome)){ShowUnitMagic(unit);return;}
            SpeciesDefinition species=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
            BuiltMagic magic=MagicFactory.Build(genome,species);
            ClearContent();
            Label(unit.displayName+"の「"+magic.displayName+"」",content,new Vector2(.02f,.88f),new Vector2(.98f,.99f),26,ink,TextAnchor.MiddleLeft);
            Label("属性："+ElementJapanese(magic.element)+"　　威力："+FormatNumber(magic.power)+"　　消費MP："+FormatNumber(magic.mpCost),content,new Vector2(.02f,.81f),new Vector2(.98f,.89f),17,accent,TextAnchor.MiddleLeft);
            var details=Rect("Magic Details",content,new Vector2(.02f,.15f),new Vector2(.98f,.79f),Vector2.zero,Vector2.zero);Image(details,panel);
            Label("発動と動き",details,new Vector2(.025f,.86f),new Vector2(.45f,.98f),18,gold,TextAnchor.MiddleLeft);
            Label(MagicDescription.Behavior(magic),details,new Vector2(.025f,.06f),new Vector2(.45f,.86f),15,ink,TextAnchor.UpperLeft);
            Label("追加効果（スクロールで確認）",details,new Vector2(.49f,.86f),new Vector2(.975f,.98f),17,accent,TextAnchor.MiddleLeft);
            var scrollRoot=Rect("Magic Effect Scroll",details,new Vector2(.49f,.04f),new Vector2(.98f,.85f),Vector2.zero,Vector2.zero);
            var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
            var viewport=Rect("Viewport",scrollRoot,Vector2.zero,new Vector2(.95f,1),Vector2.zero,Vector2.zero);Image(viewport,new Color(.025f,.045f,.075f,.6f));viewport.gameObject.AddComponent<RectMask2D>();scroll.viewport=viewport;
            var textContent=Rect("Effect Content",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);textContent.pivot=new Vector2(.5f,1);scroll.content=textContent;
            TMP_Text effectText=Label(MagicDescription.EffectDetails(magic),textContent,Vector2.zero,Vector2.one,14,ink,TextAnchor.UpperLeft);effectText.gameObject.name="Magic Effect Details";
            effectText.rectTransform.offsetMin=new Vector2(6,6);effectText.rectTransform.offsetMax=new Vector2(-6,-6);
            Canvas.ForceUpdateCanvases();
            float height=Mathf.Max(viewport.rect.height,effectText.GetPreferredValues(effectText.text,Mathf.Max(1,viewport.rect.width-12),Mathf.Infinity).y+16);
            textContent.sizeDelta=new Vector2(0,height);
            var scrollbarRoot=Rect("Scrollbar",scrollRoot,new Vector2(.965f,0),Vector2.one,Vector2.zero,Vector2.zero);Image(scrollbarRoot,new Color(.025f,.045f,.075f));
            var handle=Rect("Handle",scrollbarRoot,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Image(handle,accent);
            var scrollbar=scrollbarRoot.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar=scrollbar;scroll.verticalNormalizedPosition=1;
            Button(content,"プレビューへ戻る",new Vector2(.35f,.025f),new Vector2(.65f,.105f),()=>ShowUnitMagicPreview(unit,index),accent,15);
            Button(content,"魔法名を変更",new Vector2(.69f,.025f),new Vector2(.98f,.105f),()=>ShowMagicRename(unit,index),null,14);
        }
        private void ShowMagicRename(UnitSaveData unit,int index)
        {
            MagicGenome genome;
            if(!MagicGenome.TryParseJson(unit.magicGenomeJson[index],out genome))return;
            ClearContent();
            Label("固有魔法の名前",content,new Vector2(.02f,.86f),new Vector2(.98f,.99f),28,gold,TextAnchor.MiddleLeft);
            Label("24文字以内で入力してください。空欄にすると自動で付いた名前に戻ります。",content,new Vector2(.04f,.71f),new Vector2(.96f,.83f),17,muted,TextAnchor.MiddleLeft);
            var box=Rect("Spell Name Input",content,new Vector2(.12f,.44f),new Vector2(.88f,.62f),Vector2.zero,Vector2.zero);Image(box,panel);
            var text=Label("",box,new Vector2(.025f,.04f),new Vector2(.975f,.96f),22,ink,TextAnchor.MiddleLeft);
            var input=box.gameObject.AddComponent<TMP_InputField>();input.textComponent=text;input.textViewport=box;input.characterLimit=24;input.lineType=TMP_InputField.LineType.SingleLine;input.text=genome.customName??(genome.HasProgram?genome.programGenes.customName:"");
            Button(content,"保存する",new Vector2(.53f,.20f),new Vector2(.88f,.33f),()=>
            {
                string original=unit.magicGenomeJson[index];string[] originalSlots=unit.magicSlotGenomeJson==null?null:(string[])unit.magicSlotGenomeJson.Clone();
                genome.customName=input.text.Trim();if(genome.HasProgram)genome.programGenes.customName=genome.customName;
                unit.magicGenomeJson[index]=JsonUtility.ToJson(genome);
                if(unit.magicSlotGenomeJson!=null && index<unit.magicSlotGenomeJson.Length)
                {
                    MagicGenome slot;if(MagicGenome.TryParseJson(unit.magicSlotGenomeJson[index],out slot)){slot.customName=genome.customName;if(slot.HasProgram)slot.programGenes.customName=genome.customName;unit.magicSlotGenomeJson[index]=JsonUtility.ToJson(slot);}
                }
                if(!SaveSystem.Save(save,out string error)){unit.magicGenomeJson[index]=original;unit.magicSlotGenomeJson=originalSlots;ReportOperationFailure("魔法名を保存できませんでした。",error);return;}
                ShowUnitMagicDetails(unit,index);
            },accent,17);
            Button(content,"変更せずに戻る",new Vector2(.12f,.20f),new Vector2(.47f,.33f),()=>ShowUnitMagicDetails(unit,index),null,17);
        }
        private void ShowSettings()
        {
            ClearContent();
            Label("設定・セーブ", content, new Vector2(.02f, .88f), new Vector2(.70f, .98f), 30, ink, TextAnchor.MiddleLeft);
            Label(string.IsNullOrEmpty(save.readOnlyReason)?"ゲームの進行状況を保存・読み込みできます。":save.readOnlyReason, content, new Vector2(.02f, .80f), new Vector2(.98f, .89f), 15, muted, TextAnchor.MiddleLeft);
            var saveCard = Rect("Save Settings", content, new Vector2(.02f, .40f), new Vector2(.98f, .76f), Vector2.zero, Vector2.zero);
            Image(saveCard, panel);
            Label("セーブデータ", saveCard, new Vector2(.04f, .70f), new Vector2(.95f, .98f), 17, accent, TextAnchor.MiddleLeft);
            Label("仲間の育成・編成・配合の進行状況を保存します。", saveCard, new Vector2(.04f, .36f), new Vector2(.95f, .72f), 13, muted, TextAnchor.MiddleLeft);
            Button(saveCard, "今すぐ保存", new Vector2(.04f, .05f), new Vector2(.28f, .34f), SaveNow, accent, 15);
            Button(saveCard, "保存データを読み込む", new Vector2(.31f, .05f), new Vector2(.62f, .34f), () => { save = SaveSystem.Load(); BindUnitsToSpecies(); ShowSettings(); Toast(save.readOnly?"保存データは保護されています。内容を確認してください。":"セーブを読み込みました"); }, null, 14);
            Button(saveCard, "セーブデータを消す", new Vector2(.65f,.05f),new Vector2(.96f,.34f),ConfirmResetSave,new Color(.38f,.15f,.19f),14);
            var cleanup=Rect("Equipment Auto Discard Settings",content,new Vector2(.02f,.14f),new Vector2(.98f,.37f),Vector2.zero,Vector2.zero);Image(cleanup,panel);
            Label("装備の自動整理",cleanup,new Vector2(.04f,.61f),new Vector2(.60f,.95f),17,accent,TextAnchor.MiddleLeft);
            Label("100個を超えた分を、装備中を除き古い順にお別れします。オンにすると今すぐ整理します。",cleanup,new Vector2(.04f,.09f),new Vector2(.70f,.60f),13,muted,TextAnchor.MiddleLeft);
            Button(cleanup,save.autoDiscardOldEquipment?"自動整理：オン":"自動整理：オフ",new Vector2(.72f,.20f),new Vector2(.96f,.78f),ToggleAutoDiscard,save.autoDiscardOldEquipment?accent:panel,14).interactable=!save.readOnly;
        }
        private void ToggleAutoDiscard()
        {
            if(save==null || save.readOnly)return;
            var candidate=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
            candidate.readOnly=save.readOnly;candidate.readOnlyReason=save.readOnlyReason;
            candidate.autoDiscardOldEquipment=!candidate.autoDiscardOldEquipment;
            int removed=candidate.autoDiscardOldEquipment?EquipmentCatalog.DiscardOldestExcess(candidate):0;
            if(!SaveSystem.Save(candidate,out string error)){ReportOperationFailure("自動整理の設定を保存できませんでした",error);return;}
            save=candidate;ShowSettings();Toast(save.autoDiscardOldEquipment?(removed>0?"自動整理をオンにし、装備を"+removed+"個整理しました":"自動整理をオンにしました"):"自動整理をオフにしました");
        }
        private void ConfirmResetSave()
        {
            var overlay=Rect("Save Reset Confirmation",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            Image(overlay,new Color(.01f,.02f,.04f,.95f));overlay.GetComponent<Image>().raycastTarget=true;
            var card=Rect("Save Reset Card",overlay,new Vector2(.17f,.20f),new Vector2(.83f,.80f),Vector2.zero,Vector2.zero);Image(card,panel);Frame(card,"確認の枠",gold,.006f);
            Label("セーブデータを消しますか？",card,new Vector2(.06f,.76f),new Vector2(.94f,.96f),23,gold,TextAnchor.MiddleCenter);
            Label("仲間・卵・装備・図鑑・進行状況・プレイ時間が消え、初めからになります。\nこの操作は元に戻せません。",card,new Vector2(.08f,.34f),new Vector2(.92f,.73f),16,ink,TextAnchor.MiddleCenter);
            Button(card,"やめる",new Vector2(.08f,.08f),new Vector2(.46f,.25f),()=>Destroy(overlay.gameObject),panel,16);
            Button(card,"消して最初から",new Vector2(.54f,.08f),new Vector2(.92f,.25f),()=>
            {
                if(!SaveSystem.ResetToStarter(speciesDatabase,out GameSaveData fresh,out string error)){ReportOperationFailure("セーブデータを消せませんでした",error);return;}
                save=fresh;selectedUnitId=selectedEggId=parentA=parentB=null;rosterPage=eggPage=equipmentPage=unitDetailPage=0;
                nextPlayTimeSave=Time.realtimeSinceStartup+60f;BindUnitsToSpecies();Navigate("HOME");Toast("セーブデータを消去し、最初から始めます");
            },new Color(.38f,.15f,.19f),16);
        }
        private void ShowPvp()
        {
            ClearContent();
            AddPageSurface();
            Label("通信対戦",content,new Vector2(.04f,.88f),new Vector2(.60f,.99f),30,gold,TextAnchor.MiddleLeft);
            Label("仲間と挑む自動対戦",content,new Vector2(.045f,.81f),new Vector2(.72f,.88f),14,muted,TextAnchor.MiddleLeft);
            Button(content,"ホームへ戻る",new Vector2(.79f,.89f),new Vector2(.96f,.98f),()=>{if(FusionAnyMonster.Battle.PvpLanSession.Instance!=null)FusionAnyMonster.Battle.PvpLanSession.Instance.Close();ShowHome();},panel,15);

            var team=Rect("PvP Team",content,new Vector2(.035f,.13f),new Vector2(.48f,.79f),Vector2.zero,Vector2.zero);Image(team,new Color(.025f,.065f,.095f,.96f));Frame(team,"Team Frame",new Color(.26f,.70f,.70f,.72f),.006f);
            Label("あなたの部隊",team,new Vector2(.06f,.85f),new Vector2(.72f,.98f),21,ink,TextAnchor.MiddleLeft);
            Label(CountDeployed()+" / 5 体",team,new Vector2(.71f,.85f),new Vector2(.94f,.98f),16,accent,TextAnchor.MiddleRight);
            int index=0;
            foreach(var unit in save.units)
            {
                if(unit==null || !unit.deployed || index>=5)continue;
                int slot=index++;float left=.035f+slot*.185f;
                var species=speciesDatabase==null?null:speciesDatabase.GetById(unit.speciesId);
                var card=Rect("PvP Ally "+slot,team,new Vector2(left,.30f),new Vector2(left+.175f,.79f),Vector2.zero,Vector2.zero);Image(card,new Color(.04f,.10f,.14f));
                AddImage("仲間の姿",card,new Vector2(.04f,.28f),new Vector2(.96f,.98f),Color.white,LoadGeneratedSprite(unit.generatedSpritePath)??(species==null?null:species.sprite));
                Label(unit.displayName,card,new Vector2(.02f,.03f),new Vector2(.98f,.27f),12,ink,TextAnchor.MiddleCenter);
            }
            if(index==0)Label("仲間を編成してから対戦しましょう",team,new Vector2(.06f,.40f),new Vector2(.94f,.70f),16,muted,TextAnchor.MiddleCenter);
            Label("最大5体・同じ種族は1体まで・制限時間5分\n勝敗：全滅 → 生存数 → 与ダメージ　回復は最大HPの30%まで\n凍結・睡眠は最大2秒、解除後1秒間は再付与されません",team,new Vector2(.03f,.04f),new Vector2(.97f,.26f),12,muted,TextAnchor.MiddleCenter);
            Label("VS",content,new Vector2(.465f,.42f),new Vector2(.535f,.56f),27,gold,TextAnchor.MiddleCenter);

            var connection=Rect("PvP Connection",content,new Vector2(.52f,.13f),new Vector2(.965f,.79f),Vector2.zero,Vector2.zero);Image(connection,new Color(.07f,.045f,.065f,.96f));Frame(connection,"Connection Frame",new Color(gold.r,gold.g,gold.b,.65f),.006f);
            Label("対戦相手とつながる",connection,new Vector2(.06f,.85f),new Vector2(.94f,.98f),21,gold,TextAnchor.MiddleLeft);
            var localAddresses=FusionAnyMonster.Battle.PvpLanSession.GetLocalIPv4Addresses();
            Label(localAddresses.Length==0?"自分のIPを取得できませんでした":"自分のIP："+string.Join(" / ",localAddresses),connection,new Vector2(.06f,.75f),new Vector2(.94f,.84f),13,ink,TextAnchor.MiddleLeft);
            Label("ホストのIPアドレス",connection,new Vector2(.06f,.64f),new Vector2(.94f,.73f),14,muted,TextAnchor.MiddleLeft);
            var box=Rect("PvP Address",connection,new Vector2(.06f,.52f),new Vector2(.94f,.64f),Vector2.zero,Vector2.zero);Image(box,new Color(.025f,.035f,.06f));box.GetComponent<Image>().raycastTarget=true;Frame(box,"Address Frame",new Color(.42f,.52f,.59f,.7f),.008f);
            var inputText=Label("",box,new Vector2(.04f,.04f),new Vector2(.96f,.96f),19,ink,TextAnchor.MiddleLeft);
            var placeholder=Label("例：192.168.1.10",box,new Vector2(.04f,.04f),new Vector2(.96f,.96f),17,muted,TextAnchor.MiddleLeft);
            var input=box.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=box.GetComponent<Image>();input.textComponent=inputText;input.textViewport=box;input.placeholder=placeholder;input.characterLimit=45;input.lineType=TMP_InputField.LineType.SingleLine;input.text="";
            var ranks=Label("",connection,new Vector2(.06f,.22f),new Vector2(.94f,.33f),15,ink,TextAnchor.MiddleCenter);
            var status=Label("",connection,new Vector2(.06f,.01f),new Vector2(.66f,.20f),13,gold,TextAnchor.MiddleLeft);
            var session=FusionAnyMonster.Battle.PvpLanSession.Create();
            var ready=Button(connection,"準備完了",new Vector2(.69f,.04f),new Vector2(.96f,.19f),session.ConfirmReady,accent,14);
            var hostButton=Button(connection,"部屋を作る",new Vector2(.06f,.34f),new Vector2(.48f,.48f),()=>{if(!session.StartConnection(save,speciesDatabase,true,input.text,out string error))Toast(error);},accent,17);
            var joinButton=Button(connection,"参加する",new Vector2(.52f,.34f),new Vector2(.94f,.48f),()=>{if(!session.StartConnection(save,speciesDatabase,false,input.text,out string error))Toast(error);},GameUiTheme.Button,17);
            StartCoroutine(RefreshPvpStatus(status,ranks,ready,hostButton,joinButton,session));
            Label("同じネットワークで接続。ホストは自分のIPを相手に伝え、参加者は上に入力します。 UDP "+FusionAnyMonster.Battle.PvpLanSession.Port,content,new Vector2(.05f,.025f),new Vector2(.95f,.105f),13,muted,TextAnchor.MiddleCenter);
        }
        private IEnumerator RefreshPvpStatus(TMP_Text label,TMP_Text ranks,Button ready,Button hostButton,Button joinButton,FusionAnyMonster.Battle.PvpLanSession session)
        {
            int ownRank=PlayerRankCalculator.Calculate(save).Total;
            while(label!=null && label.gameObject.activeInHierarchy && session!=null)
            {
                label.text=session.Status;
                ranks.text="自分のランク "+(session.LocalPlayerRank ?? ownRank)+"　／　相手のランク "+(session.RemotePlayerRank.HasValue?session.RemotePlayerRank.Value.ToString():"---");
                ready.interactable=session.LobbyReady && !session.LocalReady && !session.MatchActive;
                ready.GetComponentInChildren<TMP_Text>().text=session.LocalReady?"準備済み":"準備完了";
                hostButton.interactable=joinButton.interactable=!session.LocalPlayerRank.HasValue;
                yield return null;
            }
        }

        private void ShowStageSelection()
        {
            ClearContent();
            AddPageSurface();
            Label("遠征の行き先", content, new Vector2(.04f,.88f), new Vector2(.76f,.98f), 30, gold, TextAnchor.MiddleLeft);
            Label("仲間たちと挑む、次の冒険を選びましょう。", content, new Vector2(.04f,.81f), new Vector2(.76f,.88f), 15, muted, TextAnchor.MiddleLeft);
            Button(content, "ホームへ戻る", new Vector2(.79f,.89f), new Vector2(.96f,.98f), ShowHome, panel, 15);
            ShowExpeditionTabs(stageSelectionRush?1:0);
            string[] names={"Easy","Normal","Hard","Very Hard","Hell"};
            string[] subtitles={"はじまりの地","深まる探索","試練の道","強敵の領域","果てなき戦い"};
            string[] details={"Fランク中心　Lv.〜30","Eランク中心　Lv.〜60","Dランク中心　Lv.〜90","Cランク中心　Lv.〜99","F〜Bランク　無限ウェーブ"};
            Color[] routeColors={new Color(.29f,.77f,.70f),new Color(.30f,.64f,.87f),new Color(.88f,.67f,.36f),new Color(.90f,.43f,.31f),new Color(.73f,.45f,.80f)};
            stageSelectionDifficulty=Mathf.Clamp(stageSelectionDifficulty,0,names.Length-1);
            var route=Rect("Expedition Route",content,new Vector2(.04f,.055f),new Vector2(.55f,.70f),Vector2.zero,Vector2.zero);Image(route,new Color(.025f,.055f,.085f,.94f));
            Frame(route,"遠征の枠",new Color(.40f,.58f,.55f,.55f),.004f);
            Label("進む道",route,new Vector2(.04f,.91f),new Vector2(.50f,.99f),15,gold,TextAnchor.MiddleLeft);
            Label("プレイヤーランクで解放",route,new Vector2(.30f,.91f),new Vector2(.96f,.99f),11,muted,TextAnchor.MiddleRight);
            var line=Rect("遠征ルート",route,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var graphic=line.gameObject.AddComponent<ExpeditionRouteGraphic>();graphic.color=new Color(.75f,.61f,.36f,.65f);graphic.raycastTarget=false;
            for(int i=0;i<names.Length;i++)
            {
                int index=i;bool selected=i==stageSelectionDifficulty;
                bool unlocked=FusionAnyMonster.Battle.BattleStage.IsUnlocked(save,(FusionAnyMonster.Battle.StageDifficulty)i,stageSelectionRush);
                float left=i%2==0?.055f:.225f,top=.885f-i*.175f;
                var stage=Button(route,"",new Vector2(left,top-.13f),new Vector2(left+.70f,top),()=>{stageSelectionDifficulty=index;ShowStageSelection();},selected?Color.Lerp(new Color(.055f,.12f,.16f,1),routeColors[i],.32f):new Color(.055f,.12f,.16f,.98f),14);
                stage.gameObject.name="Stage "+names[i];
                if(selected)Frame(stage.GetComponent<RectTransform>(),"選択中の枠",gold,.012f);
                var crest=Rect("道しるべ",stage.transform,new Vector2(.025f,.10f),new Vector2(.17f,.90f),Vector2.zero,Vector2.zero);
                var crestImage=crest.gameObject.AddComponent<Image>();crestImage.sprite=HomeCircleSprite();crestImage.preserveAspect=true;crestImage.color=unlocked?new Color(1,1,1,.8f):new Color(.45f,.55f,.58f,.65f);crestImage.raycastTarget=false;
                Label((i+1).ToString("00"),stage.transform,new Vector2(.015f,.10f),new Vector2(.18f,.90f),18,unlocked?gold:muted,TextAnchor.MiddleCenter);
                Label(names[i],stage.transform,new Vector2(.19f,.43f),new Vector2(.70f,.94f),17,unlocked?ink:muted,TextAnchor.MiddleLeft);
                Label(subtitles[i],stage.transform,new Vector2(.19f,.05f),new Vector2(.70f,.47f),11,muted,TextAnchor.MiddleLeft);
                Label(unlocked?(selected?"選択中":"挑戦可"):"ランク "+FusionAnyMonster.Battle.BattleStage.RequiredPlayerRank((FusionAnyMonster.Battle.StageDifficulty)i),stage.transform,new Vector2(.70f,.18f),new Vector2(.99f,.83f),12,selected?gold:unlocked?accent:muted,TextAnchor.MiddleCenter);
            }
            bool canEnter=FusionAnyMonster.Battle.BattleStage.IsUnlocked(save,(FusionAnyMonster.Battle.StageDifficulty)stageSelectionDifficulty,stageSelectionRush);
            var detail=Rect("Expedition Details",content,new Vector2(.575f,.055f),new Vector2(.96f,.70f),Vector2.zero,Vector2.zero);Image(detail,new Color(.035f,.075f,.105f,.97f));
            Frame(detail,"遠征詳細の枠",new Color(.66f,.53f,.32f,.66f),.004f);
            ColorBlock("見出しの飾り",detail,new Vector2(.05f,.91f),new Vector2(.18f,.918f),gold);
            Label(stageSelectionRush?"BOSS RUSH":"EXPEDITION",detail,new Vector2(.05f,.92f),new Vector2(.95f,.99f),12,gold,TextAnchor.MiddleLeft);
            Label(subtitles[stageSelectionDifficulty],detail,new Vector2(.05f,.78f),new Vector2(.65f,.91f),22,ink,TextAnchor.MiddleLeft);
            Label(names[stageSelectionDifficulty],detail,new Vector2(.05f,.70f),new Vector2(.57f,.78f),16,accent,TextAnchor.MiddleLeft);
            int rank=Mathf.Min(stageSelectionDifficulty,4);
            var seal=Rect("遠征の紋章",detail,new Vector2(.54f,.49f),new Vector2(.96f,.89f),Vector2.zero,Vector2.zero);
            var sealImage=seal.gameObject.AddComponent<Image>();sealImage.sprite=HomeCircleSprite();sealImage.preserveAspect=true;var sealColor=routeColors[stageSelectionDifficulty];sealColor.a=.2f;sealImage.color=sealColor;sealImage.raycastTarget=false;
            DrawStageCreature(detail,SpeciesFamily.Beast,rank,new Vector2(.47f,.57f),new Vector2(.76f,.88f),.70f);
            DrawStageCreature(detail,SpeciesFamily.Bird,rank,new Vector2(.72f,.60f),new Vector2(.99f,.91f),.72f);
            DrawStageCreature(detail,stageSelectionRush?SpeciesFamily.Material:SpeciesFamily.Slime,rank,new Vector2(.60f,.46f),new Vector2(.94f,.80f),1f);
            Label(stageSelectionRush?"立ちはだかるボスを\n次々と打ち破ろう。":"仲間とともに戦い、\n迫りくる敵をしのごう。",detail,new Vector2(.05f,.49f),new Vector2(.59f,.69f),14,muted,TextAnchor.MiddleLeft);
            ColorBlock("情報の区切り",detail,new Vector2(.05f,.455f),new Vector2(.95f,.459f),new Color(.47f,.58f,.53f,.65f));
            Label("出現する敵",detail,new Vector2(.05f,.39f),new Vector2(.38f,.455f),12,muted,TextAnchor.MiddleLeft);
            Label(details[stageSelectionDifficulty],detail,new Vector2(.38f,.39f),new Vector2(.95f,.455f),13,ink,TextAnchor.MiddleRight);
            Label("クリア条件",detail,new Vector2(.05f,.32f),new Vector2(.38f,.39f),12,muted,TextAnchor.MiddleLeft);
            string goal=stageSelectionDifficulty==4?"全滅するまで挑戦":stageSelectionRush?"第5波を突破":"5分間生存";
            Label(goal,detail,new Vector2(.38f,.32f),new Vector2(.95f,.39f),13,ink,TextAnchor.MiddleRight);
            Label("出撃編成　"+CountDeployed()+" / 5体",detail,new Vector2(.05f,.25f),new Vector2(.95f,.32f),13,accent,TextAnchor.MiddleLeft);
            bool hasParty=CountDeployed()>0;
            int ownedEquipment=EquipmentCatalog.CountOwned(save);bool equipmentFull=ownedEquipment>EquipmentCatalog.ExpeditionLimit;
            Label(equipmentFull?"装備 "+ownedEquipment+" / 100個：100個以下に整理してください":!canEnter?"プレイヤーランク "+FusionAnyMonster.Battle.BattleStage.RequiredPlayerRank((FusionAnyMonster.Battle.StageDifficulty)stageSelectionDifficulty)+" で解放":hasParty?"挑戦できます":"仲間を編成してから出撃できます",detail,new Vector2(.05f,.185f),new Vector2(.95f,.25f),12,canEnter && hasParty && !equipmentFull?accent:gold,TextAnchor.MiddleCenter);
            var launch=Button(detail,canEnter?"この遠征に出撃":"未解放",new Vector2(.09f,.045f),new Vector2(.91f,.175f),()=>{var difficulty=(FusionAnyMonster.Battle.StageDifficulty)stageSelectionDifficulty;if(!FusionAnyMonster.Battle.BattleStage.IsUnlocked(save,difficulty,stageSelectionRush))return;FusionAnyMonster.Battle.BattleStage.Select(difficulty,stageSelectionRush);StartBattle();},canEnter?GameUiTheme.PrimaryButton:panel,19);
            launch.gameObject.name="Button 遠征に出撃";launch.interactable=canEnter && hasParty;
        }
        private void ShowExpeditionTabs(int selected)
        {
            Button(content,"常設遠征",new Vector2(.04f,.72f),new Vector2(.335f,.795f),()=>{stageSelectionRush=false;ShowStageSelection();},selected==0?GameUiTheme.PrimaryButton:panel,16);
            Button(content,"ボスラッシュ",new Vector2(.352f,.72f),new Vector2(.648f,.795f),()=>{stageSelectionRush=true;ShowStageSelection();},selected==1?GameUiTheme.PrimaryButton:panel,16);
            Button(content,"イベント遠征",new Vector2(.665f,.72f),new Vector2(.96f,.795f),ShowLimitedStages,selected==2?GameUiTheme.PrimaryButton:panel,16);
        }
        private void ShowLimitedStages()
        {
            ClearContent();AddPageSurface();
            var now=DateTime.Now;var family=FusionAnyMonster.Battle.LimitedStageRules.Family(now.DayOfWeek);
            var kinds=new[]{FusionAnyMonster.Battle.LimitedStageKind.Weekday,FusionAnyMonster.Battle.LimitedStageKind.ChallengeBoss,FusionAnyMonster.Battle.LimitedStageKind.DailyBoss,FusionAnyMonster.Battle.LimitedStageKind.DailyExperience,FusionAnyMonster.Battle.LimitedStageKind.DailySuperExperience};
            eventStageSelection=Mathf.Clamp(eventStageSelection,0,kinds.Length-1);
            int playerRank=PlayerRankCalculator.Calculate(save).Total;
            Label("遠征の行き先",content,new Vector2(.04f,.88f),new Vector2(.72f,.98f),29,gold,TextAnchor.MiddleLeft);
            Button(content,"ホームへ戻る",new Vector2(.79f,.89f),new Vector2(.96f,.98f),ShowHome,panel,15);
            Label("回数は出撃時に消費し、毎日0時に回復します。",content,new Vector2(.045f,.81f),new Vector2(.96f,.88f),15,muted,TextAnchor.MiddleLeft);
            ShowExpeditionTabs(2);
            var route=Rect("Expedition Route",content,new Vector2(.04f,.055f),new Vector2(.55f,.70f),Vector2.zero,Vector2.zero);Image(route,new Color(.025f,.055f,.085f,.94f));Frame(route,"遠征の枠",new Color(.40f,.58f,.55f,.55f),.004f);
            Label("イベントを選ぶ",route,new Vector2(.04f,.88f),new Vector2(.96f,.98f),16,gold,TextAnchor.MiddleLeft);
            string[] titles={"曜日限定ボス","チャレンジボス","1日1回 強敵","1日1回 経験値","1日1回 超・経験値"};
            string[] descriptions={"日替わりの強敵", "Sランク Lv.1000", "Sランクのボス3体", "経験値100倍", "経験値1000倍"};
            for(int i=0;i<kinds.Length;i++)
            {
                int choice=i;bool selected=eventStageSelection==i;int remaining=FusionAnyMonster.Battle.LimitedStageRules.Remaining(save,kinds[i],now);int limit=i==0?5:1;float top=.885f-i*.175f;float left=i%2==0?.055f:.225f;
                var button=Button(route,"",new Vector2(left,top-.13f),new Vector2(left+.70f,top),()=>{eventStageSelection=choice;ShowLimitedStages();},selected?new Color(.09f,.24f,.26f):new Color(.055f,.12f,.16f,.98f),14);
                button.gameObject.name="Event Stage "+i;
                if(selected)Frame(button.GetComponent<RectTransform>(),"選択中の枠",gold,.012f);
                var crest=Rect("道しるべ",button.transform,new Vector2(.025f,.10f),new Vector2(.17f,.90f),Vector2.zero,Vector2.zero);
                var crestImage=crest.gameObject.AddComponent<Image>();crestImage.sprite=HomeCircleSprite();crestImage.preserveAspect=true;crestImage.raycastTarget=false;
                Label((i+1).ToString("00"),button.transform,new Vector2(.015f,.10f),new Vector2(.18f,.90f),18,gold,TextAnchor.MiddleCenter);
                Label(titles[i],button.transform,new Vector2(.19f,.48f),new Vector2(.72f,.94f),15,ink,TextAnchor.MiddleLeft);
                Label(descriptions[i],button.transform,new Vector2(.19f,.06f),new Vector2(.73f,.47f),11,muted,TextAnchor.MiddleLeft);
                int required=FusionAnyMonster.Battle.LimitedStageRules.RequiredRank(kinds[i]);
                Label(playerRank<required?"ランク "+required:i==1?"無制限":remaining+" / "+limit+" 回",button.transform,new Vector2(.73f,.18f),new Vector2(.99f,.83f),12,remaining>0?accent:muted,TextAnchor.MiddleCenter);
            }
            var selectedKind=kinds[eventStageSelection];bool isWeekday=selectedKind==FusionAnyMonster.Battle.LimitedStageKind.Weekday;
            int remainingCount=FusionAnyMonster.Battle.LimitedStageRules.Remaining(save,selectedKind,now);int requiredRank=FusionAnyMonster.Battle.LimitedStageRules.RequiredRank(selectedKind);
            var detail=Rect("Expedition Details",content,new Vector2(.575f,.055f),new Vector2(.96f,.70f),Vector2.zero,Vector2.zero);Image(detail,new Color(.035f,.075f,.105f,.97f));Frame(detail,"遠征詳細の枠",new Color(.66f,.53f,.32f,.66f),.004f);
            ColorBlock("見出しの飾り",detail,new Vector2(.05f,.91f),new Vector2(.18f,.918f),gold);
            Label("EVENT EXPEDITION",detail,new Vector2(.05f,.92f),new Vector2(.95f,.99f),12,gold,TextAnchor.MiddleLeft);
            Label(titles[eventStageSelection],detail,new Vector2(.05f,.78f),new Vector2(.65f,.91f),19,ink,TextAnchor.MiddleLeft);
            var seal=Rect("遠征の紋章",detail,new Vector2(.54f,.49f),new Vector2(.96f,.89f),Vector2.zero,Vector2.zero);
            var sealImage=seal.gameObject.AddComponent<Image>();sealImage.sprite=HomeCircleSprite();sealImage.preserveAspect=true;sealImage.color=new Color(.4f,.75f,.70f,.20f);sealImage.raycastTarget=false;
            if(isWeekday)
                DrawStageCreature(detail,family,(int)SpeciesRank.B,new Vector2(.60f,.46f),new Vector2(.94f,.80f),1f);
            else
            {
                int sampleRank=eventStageSelection>=3?(int)SpeciesRank.D:(int)SpeciesRank.S;
                DrawStageCreature(detail,SpeciesFamily.Beast,sampleRank,new Vector2(.47f,.57f),new Vector2(.76f,.88f),.70f);
                DrawStageCreature(detail,SpeciesFamily.Bird,sampleRank,new Vector2(.72f,.60f),new Vector2(.99f,.91f),.72f);
                DrawStageCreature(detail,SpeciesFamily.Slime,sampleRank,new Vector2(.60f,.46f),new Vector2(.94f,.80f),1f);
            }
            Label(isWeekday?"本日の強敵に\n挑戦しよう。":descriptions[eventStageSelection],detail,new Vector2(.05f,.49f),new Vector2(.53f,.70f),14,muted,TextAnchor.MiddleLeft);
            Label("出現例",detail,new Vector2(.65f,.45f),new Vector2(.95f,.50f),11,muted,TextAnchor.MiddleCenter);
            ColorBlock("情報の区切り",detail,new Vector2(.05f,.44f),new Vector2(.95f,.444f),new Color(.47f,.58f,.53f,.65f));
            string info=isWeekday?FusionAnyMonster.Battle.LimitedStageRules.FamilyName(now.DayOfWeek)+" B・Lv.75":eventStageSelection==1?"S・Lv.1000 1体":eventStageSelection==2?"S・Lv.99 3体":eventStageSelection==3?"60秒・経験値100倍":"60秒・経験値1000倍";
            Label("内容",detail,new Vector2(.05f,.36f),new Vector2(.28f,.43f),12,muted,TextAnchor.MiddleLeft);
            Label(info,detail,new Vector2(.28f,.36f),new Vector2(.95f,.43f),13,ink,TextAnchor.MiddleRight);
            Label("解放条件",detail,new Vector2(.05f,.29f),new Vector2(.36f,.36f),12,muted,TextAnchor.MiddleLeft);
            Label("ランク "+requiredRank,detail,new Vector2(.38f,.29f),new Vector2(.95f,.36f),13,ink,TextAnchor.MiddleRight);
            int ownedEquipment=EquipmentCatalog.CountOwned(save);bool equipmentFull=ownedEquipment>EquipmentCatalog.ExpeditionLimit;
            Label(equipmentFull?"装備 "+ownedEquipment+" / 100個：100個以下に整理してください":(eventStageSelection==1?"挑戦回数 無制限":"残り "+remainingCount+" / "+(isWeekday?5:1)+" 回")+"　編成 "+CountDeployed()+" / 5体",detail,new Vector2(.05f,.21f),new Vector2(.95f,.28f),13,equipmentFull?gold:accent,TextAnchor.MiddleCenter);
            bool canEnter=remainingCount>0 && CountDeployed()>0 && !save.readOnly && playerRank>=requiredRank;
            var launch=Button(detail,playerRank<requiredRank?"未解放":"この遠征に出撃",new Vector2(.08f,.05f),new Vector2(.92f,.20f),()=>StartLimitedStage(selectedKind,family),GameUiTheme.PrimaryButton,16);launch.interactable=canEnter;
        }
        private void StartLimitedStage(FusionAnyMonster.Battle.LimitedStageKind kind,SpeciesFamily family)
        {
            if(PlayerRankCalculator.Calculate(save).Total<FusionAnyMonster.Battle.LimitedStageRules.RequiredRank(kind)){Toast("プレイヤーランクが足りません");return;}
            if(kind==FusionAnyMonster.Battle.LimitedStageKind.Weekday)family=FusionAnyMonster.Battle.LimitedStageRules.Family(DateTime.Now.DayOfWeek);
            if(kind==FusionAnyMonster.Battle.LimitedStageKind.Weekday && FusionAnyMonster.Battle.LimitedStageRules.ChooseSpecies(speciesDatabase,family,(int)SpeciesRank.B)==null)
            { Toast("本日のボスを読み込めませんでした");return; }
            FusionAnyMonster.Battle.BattleStage.SelectLimited(kind,family);StartBattle();
        }
        private void DrawStageCreature(RectTransform parent,SpeciesFamily family,int rank,Vector2 min,Vector2 max,float opacity)
        {
            var choices=speciesDatabase==null?null:speciesDatabase.FindCandidates(family,(SpeciesRank)rank);
            var entry=choices==null || choices.Length==0?null:choices[0];
            if(entry!=null && entry.sprite!=null)AddImage("遠征の魔物 "+family,parent,min,max,new Color(1,1,1,opacity),entry.sprite);
        }

        private void StartBattle()
        {
            if (currentScreen != "HOME" || SceneManager.GetActiveScene().name != "Title") return;
            int ownedEquipment=EquipmentCatalog.CountOwned(save);
            if(ownedEquipment>EquipmentCatalog.ExpeditionLimit){Toast("装備が"+ownedEquipment+"個あります。100個以下に整理してください");return;}
            if (CountDeployed() == 0) { Toast("出撃する仲間を1体以上編成してください"); return; }
            var limited=FusionAnyMonster.Battle.BattleStage.Limited;
            if (limited==FusionAnyMonster.Battle.LimitedStageKind.None && !FusionAnyMonster.Battle.BattleStage.IsUnlocked(save, FusionAnyMonster.Battle.BattleStage.Difficulty, FusionAnyMonster.Battle.BattleStage.BossRush)) { Toast("この難易度はまだ解放されていません"); return; }
            if (limited!=FusionAnyMonster.Battle.LimitedStageKind.None && PlayerRankCalculator.Calculate(save).Total<FusionAnyMonster.Battle.LimitedStageRules.RequiredRank(limited)) { Toast("プレイヤーランクが足りません"); return; }
            int buildIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Game/Scenes/Battle.unity");
            if (buildIndex >= 0)
            {
                GameSaveData previous=save;
                if(limited!=FusionAnyMonster.Battle.LimitedStageKind.None)
                {
                    var next=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));next.readOnly=save.readOnly;next.readOnlyReason=save.readOnlyReason;
                    if(next.readOnly || !FusionAnyMonster.Battle.LimitedStageRules.Consume(next,limited,DateTime.Now)){Toast("本日の挑戦回数がありません");return;}
                    if(!SaveSystem.Save(next,out string reserveError)){ReportOperationFailure("挑戦回数を保存できませんでした",reserveError);return;}save=next;
                }
                if (canvas != null) canvas.enabled = false;
                try { SceneManager.LoadScene(buildIndex); }
                catch (Exception exception)
                {
                    if(limited!=FusionAnyMonster.Battle.LimitedStageKind.None && SaveSystem.Save(previous,out _))save=previous;
                    if (canvas != null) canvas.enabled = true;
                    ReportOperationFailure("戦闘を開始できませんでした。もう一度お試しください。",exception.ToString());
                }
            }
            else Toast("戦闘データを読み込めませんでした。");
        }

        private SpeciesDatabase LoadOrCreateSpeciesDatabase()
        {
            SpeciesDatabase[] databases = Resources.FindObjectsOfTypeAll<SpeciesDatabase>();
            for (int i = 0; i < databases.Length; i++) if (databases[i] != null && databases[i].species != null && databases[i].species.Length > 0) return databases[i];
            SpeciesDatabase resourceDatabase = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            if (resourceDatabase != null) return resourceDatabase;
            var database = DemoDataFactory.CreateSpeciesDatabase();
            var definitions = new List<SpeciesDefinition>();
            foreach (UnitSaveData record in save.units)
            {
                if (record == null || string.IsNullOrEmpty(record.speciesId) || definitions.Exists(s => s.speciesId == record.speciesId)) continue;
                var definition = ScriptableObject.CreateInstance<SpeciesDefinition>();
                definition.speciesId = record.speciesId;
                definition.displayName = record.displayName;
                definition.description = "Runtime fallback species record. Replace with SpeciesDatabase master data.";
                if (!Enum.TryParse(record.family, out definition.family)) definition.family = SpeciesFamily.Slime;
                if (!Enum.TryParse(record.rank, out definition.rank)) definition.rank = SpeciesRank.F;
                definition.baseStats = new UnitStats();
                definition.passive=SpeciesPassiveCatalog.Create(definition);
                if (record.baseStats != null) for (int stat = 0; stat < 10; stat++) definition.baseStats.Set(stat, record.baseStats[stat]);
                definitions.Add(definition);
            }
            database.species = definitions.ToArray();
            return database;
        }

        private void BindUnitsToSpecies()
        {
            if (speciesDatabase == null || speciesDatabase.species == null || speciesDatabase.species.Length == 0) return;
            bool changed = false;
            for (int i = 0; i < save.units.Count; i++)
            {
                UnitSaveData record = save.units[i];
                if (record == null) continue;
                SpeciesDefinition species = speciesDatabase.GetById(record.speciesId);
                if (species == null)
                {
                    // Do not replace an owned special species with an unrelated catalog entry if its asset is missing.
                    if(record.speciesId==SpecialBreedingRecipe.DefaultResultId) { Debug.LogError("特殊配合の種族データを読み込めません: "+record.speciesId);continue; }
                    species = speciesDatabase.species[i % speciesDatabase.species.Length];
                }
                if (species == null) continue;
                changed |= record.speciesId != species.speciesId || record.displayName != species.displayName || record.family != species.family.ToString() || record.rank != species.rank.ToString();
                record.speciesId = species.speciesId;
                record.displayName = species.displayName;
                record.family = species.family.ToString();
                record.rank = species.rank.ToString();
                var baseStats = new float[10];
                UnitStats masterStats = species.baseStats ?? new UnitStats();
                for (int stat = 0; stat < baseStats.Length; stat++) baseStats[stat] = masterStats.Get(stat);
                var elementDamage = new float[7];
                var elementAilment = new float[7];
                var debuffResistance = new float[8];
                for (int element = 0; element < elementDamage.Length; element++)
                {
                    ResistanceValue resistance = species.GetResistance((AttributeElement)element);
                    elementDamage[element] = resistance.damageReduction;
                    elementAilment[element] = resistance.applicationResistance;
                }
                for (int debuff = 0; debuff < debuffResistance.Length; debuff++)
                    debuffResistance[debuff] = species.GetResistance((DebuffType)debuff).applicationResistance;
                changed |= !SameValues(record.baseStats, baseStats) || !SameValues(record.attributeDamageReduction, elementDamage) || !SameValues(record.attributeAilmentResistance, elementAilment) || !SameValues(record.debuffResistance, debuffResistance);
                record.baseStats = baseStats;
                record.attributeDamageReduction = elementDamage;
                record.attributeAilmentResistance = elementAilment;
                record.debuffResistance = debuffResistance;
            }
            changed |= SaveSystem.RecordOwnedSpecies(save);
            if (changed) SaveSystem.Save(save, out _);
        }
        private static bool SameValues(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (!Mathf.Approximately(a[i], b[i])) return false;
            return true;
        }
        private static UnitInstance ToUnit(UnitSaveData data)
        {
            LineageRecord lineage = null;
            if (!string.IsNullOrEmpty(data.lineageJson))
            {
                try { lineage = JsonUtility.FromJson<LineageRecord>(data.lineageJson); }
                catch (Exception exception) { Debug.LogWarning("Could not read unit lineage; using saved parent fields. " + exception.Message); }
            }
            if (lineage == null) lineage = new LineageRecord();
            if (string.IsNullOrEmpty(lineage.fatherId)) lineage.fatherId = data.fatherId;
            if (string.IsNullOrEmpty(lineage.motherId)) lineage.motherId = data.motherId;
            if (lineage.generation < data.generation) lineage.generation = data.generation;
            var unit = new UnitInstance
            {
                id = data.id,
                speciesId = data.speciesId,
                level = Mathf.Clamp(data.level, 1, 99),
                experience = data.experience,
                personality = ParsePersonality(data.personality),
                equipment = data.equipment,
                iv = new IndividualValues(),
                magicGenomes = new List<MagicGenome>(),
                mutationHistory = new List<string>(),
                lineage = lineage
            };
            for (int i = 0; i < 10; i++) unit.iv.Set(i, data.individualValues[i]);
            if (data.magicGenomeJson != null) foreach (string json in data.magicGenomeJson) { MagicGenome genome; unit.magicGenomes.Add(MagicGenome.TryParseJson(json,out genome)?genome:null); }
            unit.magicGeneSlots=Spells.SpellSlotStorage.ParseSavedSlots(data.magicSlotGenomeJson);
            if (!string.IsNullOrEmpty(data.mutationHistory)) unit.mutationHistory.AddRange(data.mutationHistory.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            return unit;
        }

        private static UnitSaveData FromUnit(UnitInstance unit, SpeciesDefinition species)
        {
            var result = new UnitSaveData
            {
                id = unit.id,
                speciesId = species.speciesId,
                displayName = species.displayName,
                family = species.family.ToString(),
                rank = species.rank.ToString(),
                level = unit.level,
                experience = unit.experience,
                personality = unit.personality.ToString(),
                lineageJson = JsonUtility.ToJson(unit.lineage),
                fatherId = unit.lineage == null ? null : unit.lineage.fatherId,
                motherId = unit.lineage == null ? null : unit.lineage.motherId,
                generation = unit.lineage == null ? 0 : unit.lineage.generation,
                mutationHistory = unit.mutationHistory == null ? "" : string.Join("\n", unit.mutationHistory),
                individualValues = new int[10], baseStats = new float[10], deployed = false,
                magicGenomeJson = new string[unit.magicGenomes == null ? 0 : unit.magicGenomes.Count]
            };
            for (int i = 0; i < 10; i++) { result.individualValues[i] = unit.iv.Get(i); result.baseStats[i] = species.baseStats.Get(i); }
            for (int i = 0; i < 7; i++) { var resistance = species.GetResistance((AttributeElement)i); result.attributeDamageReduction[i] = resistance.damageReduction; result.attributeAilmentResistance[i] = resistance.applicationResistance; }
            for (int i = 0; i < 8; i++) result.debuffResistance[i] = species.GetResistance((DebuffType)i).applicationResistance;
            for (int i = 0; i < result.magicGenomeJson.Length; i++) result.magicGenomeJson[i] = JsonUtility.ToJson(unit.magicGenomes[i]);
            result.magicSlotGenomeJson=new string[unit.magicGeneSlots==null?0:unit.magicGeneSlots.Count];
            for(int i=0;i<result.magicSlotGenomeJson.Length;i++)result.magicSlotGenomeJson[i]=JsonUtility.ToJson(unit.magicGeneSlots[i]);
            return result;
        }

        private static PersonalityType ParsePersonality(string value) { PersonalityType result; return Enum.TryParse(value, out result) ? result : PersonalityType.Brave; }
        private static string FamilyJapanese(string family)
        {
            switch (family)
            {
                case "Slime": return "スライム系"; case "Beast": return "獣系"; case "Bird": return "鳥系"; case "Reptile": return "爬虫系";
                case "Amphibian": return "両生系"; case "Aquatic": return "水棲系"; case "Insect": return "虫系"; case "Crustacean": return "甲殻系";
                case "Plant": return "植物系"; case "Fungi": return "菌類系"; case "DemiHuman": return "亜人系"; case "Giant": return "巨人系";
                case "Material": return "物質系"; case "Mineral": return "鉱物系"; case "Machine": return "機械系"; case "ArtificialLife": return "人工生命系";
                case "Arcane": return "魔導系"; case "Dragon": return "竜系"; case "DivineBeast": return "幻獣系"; case "Fairy": return "妖精系";
                case "Spirit": return "精霊系"; case "Angel": return "天使系"; case "Demon": return "悪魔系"; case "Undead": return "死霊系";
                case "Ghost": return "霊体系"; case "CursedObject": return "呪物系"; case "Shadow": return "影系"; case "Astral": return "星界系";
                case "Abyssal": return "深淵系"; case "Chaos": return "混沌系"; case "Truth": return "真理系"; default: return string.IsNullOrEmpty(family) ? "系統不明" : family;
            }
        }
        private static string FormatNumber(float value) { return Mathf.Ceil(value).ToString("0"); }
        private static string FormatPercentage(float value) { return Math.Ceiling(Math.Round((double)value*100,4)).ToString("0")+"%"; }
        private static string FormatResistances(float[] values, int count, bool element)
        {
            if (values == null || values.Length == 0) return "-";
            string result = "";
            for (int i = 0; i < count && i < values.Length; i++)
            {
                string name = element ? ElementJapanese((AttributeElement)i) : DebuffJapanese((DebuffType)i);
                result += (i == 0 ? "" : " ・ ") + name + " " + FormatPercentage(values[i]);
            }
            return result;
        }
        private static string ElementJapanese(AttributeElement value)
        {
            switch (value) { case AttributeElement.Fire: return "火"; case AttributeElement.Water: return "水"; case AttributeElement.Wind: return "風"; case AttributeElement.Earth: return "土"; case AttributeElement.Light: return "光"; case AttributeElement.Dark: return "闇"; default: return "無"; }
        }



        private static string DebuffJapanese(DebuffType value)
        {
            switch (value) { case DebuffType.Poison: return "毒"; case DebuffType.Burn: return "火傷"; case DebuffType.Freeze: return "凍結"; case DebuffType.Paralysis: return "麻痺"; case DebuffType.Sleep: return "睡眠"; case DebuffType.Slow: return "鈍足"; case DebuffType.Silence: return "沈黙"; default: return "呪い"; }
        }

        private void ToggleDeployment(UnitSaveData unit)
        {
            bool previous = unit.deployed;
            unit.deployed = !unit.deployed;
            if (unit.deployed && CountDeployed() > 5) { unit.deployed = previous; Toast("編成できる仲間は5体までです"); return; }
            if (!SaveSystem.Save(save, out string error))
            {
                unit.deployed = previous;
                ReportOperationFailure("編成を保存できませんでした。空き容量や保存設定を確認してください。",error);
                return;
            }
            ShowUnits(); Toast(unit.deployed ? "出撃編成に加えました" : "出撃編成から外しました");
        }

        private int CountDeployed() { int total = 0; if (save == null || save.units == null) return total; foreach (var unit in save.units) if (unit != null && unit.deployed) total++; return total; }
        private UnitSaveData FindUnit(string id) { return string.IsNullOrEmpty(id) ? null : save.units.Find(u => u != null && u.id == id); }
        private void SaveNow() { if (SaveSystem.Save(save, out string error)) Toast("セーブしました"); else ReportOperationFailure("保存に失敗しました。空き容量や保存設定を確認してください。",error); }
        private void ReportOperationFailure(string message,string details) { Debug.LogWarning(details); Toast(message); }
        private static Sprite LoadGeneratedSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return null;
            Sprite cached;
            if (generatedSpriteCache.TryGetValue(path, out cached)) return cached;
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes)) { Destroy(texture); return null; }
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                generatedSpriteCache[path] = sprite;
                return sprite;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not load a generated unit sprite: " + exception.Message);
                return null;
            }
        }
        private void Toast(string message)
        {
            if (toast == null) return;
            toast.text = Translate(message);
            bool visible = !string.IsNullOrWhiteSpace(message);
            if (toastPanel != null) toastPanel.gameObject.SetActive(visible);
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = visible ? StartCoroutine(HideToast()) : null;
        }
        private IEnumerator HideToast()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (toast != null) toast.text = string.Empty;
            if (toastPanel != null) toastPanel.gameObject.SetActive(false);
            toastRoutine = null;
        }

        private void Card(string heading, string value, string description, Vector2 min, Vector2 max)
        {
            var card = Rect(heading, content, min, max, Vector2.zero, Vector2.zero); Image(card, panel);
            Label(heading, card, new Vector2(.08f, .70f), new Vector2(.92f, .95f), 16, accent, TextAnchor.MiddleLeft);
            Label(value, card, new Vector2(.08f, .36f), new Vector2(.92f, .76f), 22, ink, TextAnchor.MiddleLeft);
            Label(description, card, new Vector2(.08f, .06f), new Vector2(.92f, .40f), 14, muted, TextAnchor.MiddleLeft);
        }

        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; return rect;
        }
        private void Image(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            if (color == panel)
            {
                GameUiTheme.ApplySurface(image);
                Shadow shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.004f,.01f,.025f,.62f); shadow.effectDistance = new Vector2(0,-2f); shadow.useGraphicAlpha = true;
            }
        }
        private TMP_Text Label(string value, Transform parent, Vector2 min, Vector2 max, int size, Color color, TextAnchor alignment)
        {
            var rect = Rect("Text", parent, min, max, new Vector2(8, 2), new Vector2(-8, -2));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = uiFont; text.text = Translate(value); text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = Mathf.Max(9, size * .72f); text.fontSizeMax = size; text.color = color; text.alignment = ToTmpAlignment(alignment); text.textWrappingMode = TextWrappingModes.Normal; text.lineSpacing=20f; text.overflowMode = TextOverflowModes.Truncate; text.richText = false; text.raycastTarget = false;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.005f,.012f,.025f,.78f); shadow.effectDistance = new Vector2(.5f,-1.25f); shadow.useGraphicAlpha = true;
            return text;
        }
        private Button Button(Transform parent, string label, Vector2 min, Vector2 max, Action action, Color? color = null, int size = 14)
        {
            var rect = Rect("Button " + label, parent, min, max, new Vector2(3, 3), new Vector2(-3, -3));
            var image = rect.gameObject.AddComponent<Image>(); image.color = color.HasValue && color.Value==accent ? GameUiTheme.PrimaryButton : color ?? GameUiTheme.Button; GameUiTheme.ApplySurface(image);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action?.Invoke());
            ApplyButtonTheme(button);
            Shadow backgroundShadow = rect.gameObject.AddComponent<Shadow>(); backgroundShadow.effectColor = new Color(.005f,.012f,.025f,.76f); backgroundShadow.effectDistance = new Vector2(0,-3f); backgroundShadow.useGraphicAlpha = true;
            var textRect = Rect("Label", rect, Vector2.zero, Vector2.one, new Vector2(5, 4), new Vector2(-5, -4));
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = uiFont; text.text = Translate(label); text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = Mathf.Max(10, size * .78f); text.fontSizeMax = size; text.color = ink; text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal; text.lineSpacing=20f; text.overflowMode = TextOverflowModes.Truncate; text.raycastTarget = false;
            Shadow textShadow = textRect.gameObject.AddComponent<Shadow>(); textShadow.effectColor = new Color(.005f,.012f,.025f,.82f); textShadow.effectDistance = new Vector2(.5f,-1.25f); textShadow.useGraphicAlpha = true;
            return button;
        }
        private void ApplyButtonTheme(Button button)
        {
            if (button == null) return;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f,.91f,.70f,1f);
            colors.pressedColor = new Color(.72f,.83f,.88f,1f);
            colors.selectedColor = new Color(.98f,.84f,.61f,1f);
            colors.disabledColor = new Color(.58f,.62f,.68f,.62f);
            colors.fadeDuration = .12f;
            button.colors = colors;
        }
        private Button CharacterIcon(string name,Transform parent,Vector2 min,Vector2 max,string title,Sprite sprite,string rank,string badge,bool selected,Action action,string placeholder="画像準備中")
        {
            var rect=Rect(name,parent,min,max,new Vector2(4,4),new Vector2(-4,-4));
            var background=rect.gameObject.AddComponent<Image>();background.color=selected?new Color(.12f,.25f,.31f):panel;GameUiTheme.ApplySurface(background);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=background;button.onClick.AddListener(()=>action?.Invoke());ApplyButtonTheme(button);
            Frame(rect,"アイコン枠",selected?gold:new Color(.20f,.34f,.42f),selected?.022f:.009f);
            var portrait=AddImage("キャラクターアイコン",rect,new Vector2(.05f,.30f),new Vector2(.95f,.96f),Color.white,sprite);
            if(sprite==null){portrait.enabled=false;var placeholderText=Label(placeholder,rect,new Vector2(.05f,.32f),new Vector2(.95f,.70f),11,muted,TextAnchor.MiddleCenter);placeholderText.gameObject.name="画像なし";}
            var titleText=Label(title,rect,new Vector2(.015f,.005f),new Vector2(.985f,.28f),13,ink,TextAnchor.MiddleCenter);titleText.gameObject.name="キャラクター名";
            var tag=Rect("ランク",rect,new Vector2(.025f,.79f),new Vector2(.25f,.98f),Vector2.zero,Vector2.zero);Image(tag,new Color(.13f,.16f,.22f,.94f));
            Label(rank,tag,Vector2.zero,Vector2.one,11,gold,TextAnchor.MiddleCenter);
            if(!string.IsNullOrEmpty(badge))
            {
                var state=Rect("編成状態",rect,new Vector2(.38f,.79f),new Vector2(.975f,.98f),Vector2.zero,Vector2.zero);Image(state,new Color(.05f,.20f,.24f,.94f));
                Label(badge,state,Vector2.zero,Vector2.one,11,accent,TextAnchor.MiddleCenter);
            }
            return button;
        }
        private Button QuickButton(RectTransform parent, string title, string hint, Vector2 min, Vector2 max, Action action, Color color)
        {
            Button button = Button(parent,title,min,max,action,color,14);
            RectTransform titleRect = button.transform.GetChild(0).GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(.04f,.43f); titleRect.anchorMax = new Vector2(.96f,.93f);
            titleRect.offsetMin = Vector2.zero; titleRect.offsetMax = Vector2.zero;
            Label(hint,button.transform,new Vector2(.035f,.055f),new Vector2(.965f,.42f),12,new Color(.85f,.90f,.93f),TextAnchor.MiddleCenter);
            return button;
        }
        private static void SetAnchors(RectTransform rect,Vector2 min,Vector2 max)
        {
            if(rect==null)return;
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=new Vector2(4,2);rect.offsetMax=new Vector2(-4,-2);
        }
        private static TextAlignmentOptions ToTmpAlignment(TextAnchor value)
        {
            switch (value)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
        private static Sprite HomeCircleSprite()
        {
            if (homeCircleSprite != null) return homeCircleSprite;
            const int size=128; var texture=new Texture2D(size,size,TextureFormat.RGBA32,false); texture.name="Home Deploy Circle"; texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color[size*size]; Vector2 center=new Vector2((size-1)*.5f,(size-1)*.5f); float radius=size*.485f;
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float d=Vector2.Distance(new Vector2(x,y),center); float aa=Mathf.Clamp01(radius+.75f-d);
                float depth=Mathf.Clamp01(d/radius);
                Color fill=Color.Lerp(new Color(.17f,.78f,.78f),new Color(.035f,.18f,.30f),depth);
                if(d>radius-4.5f) fill=new Color(.95f,.68f,.28f,aa);
                else if(d>radius-8f) fill=new Color(.16f,.48f,.57f,aa);
                fill.a=aa; pixels[y*size+x]=fill;
            }
            texture.SetPixels(pixels); texture.Apply(false,true); homeCircleSprite = Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f); return homeCircleSprite;
        }
        private Image AddImage(string name,Transform parent,Vector2 min,Vector2 max,Color tint,Sprite sprite)
        {
            var rect=Rect(name,parent,min,max,new Vector2(3,3),new Vector2(-3,-3)); var image=rect.gameObject.AddComponent<Image>(); image.color=tint; image.sprite=FullPortrait(sprite); image.preserveAspect=true; image.raycastTarget=false; return image;
        }
        private static Sprite FullPortrait(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return sprite;
            Texture2D texture = sprite.texture;
            Rect rect = sprite.rect;
            if (rect.x == 0 && rect.y == 0 && rect.width == texture.width && rect.height == texture.height)
                return sprite;
            if (fullPortraitCache.TryGetValue(texture, out Sprite full) && full != null) return full;
            full = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            full.name = sprite.name + " (full portrait)";
            fullPortraitCache[texture] = full;
            return full;
        }
        private void ColorBlock(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var rect=Rect(name,parent,min,max,Vector2.zero,Vector2.zero); Image(rect,color);
        }
        private void Frame(RectTransform parent,string name,Color color,float thickness)
        {
            ColorBlock(name+" Top",parent,new Vector2(0,1f-thickness),Vector2.one,color);
            ColorBlock(name+" Bottom",parent,Vector2.zero,new Vector2(1,thickness),color);
            ColorBlock(name+" Left",parent,new Vector2(0,thickness),new Vector2(thickness,1f-thickness),color);
            ColorBlock(name+" Right",parent,new Vector2(1f-thickness,thickness),new Vector2(1,1f-thickness),color);
        }
    }
}
