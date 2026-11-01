using System.IO;
using System.Collections.Generic;
using FusionAnyMonster.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FusionAnyMonster;
using FusionAnyMonster.Art;

namespace FusionAnyMonster.Editor
{
    public static class FusionAnyMonsterSetup
    {
        [MenuItem("Fusion Any Monster/Localization/Apply Japanese Species Names")]
        public static void ApplyJapaneseSpeciesNames()
        {
            EnsureFolders();
            CreateSpeciesAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Applied Japanese names to all species master assets.");
        }

        [MenuItem("Fusion Any Monster/Setup/Create or Reset Battle Demo")]
        public static void CreateBattleDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolders();
            CreateMasterAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.backgroundColor = new Color(.055f, .08f, .13f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            var light = new GameObject("Main Light");
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.AddComponent<Light>().type = LightType.Directional;
            var arena = new GameObject("Battle Arena");
            arena.AddComponent<BattleDemo>();
            var path = "Assets/Game/Scenes/Battle.unity";
            EditorSceneManager.SaveScene(scene, path);
            AddToBuildSettings(path); PrioritizeTitleAndBattle();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log("Fusion Any Monster battle demo created at " + path);
        }

        [MenuItem("Fusion Any Monster/Setup/Create Master Data and UI Demo")]
        public static void CreateMasterDataAndUiDemo()
        {
            EnsureFolders();
            CreateMasterAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 7.5f; camera.backgroundColor = new Color(.055f, .08f, .13f); camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            string path = "Assets/Game/Scenes/Title.unity";
            EditorSceneManager.SaveScene(scene, path); AddToBuildSettings(path); PrioritizeTitleAndBattle(); AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log("Fusion Any Monster title and master-data demo created at " + path);
        }

        [MenuItem("Fusion Any Monster/Setup/Refresh 420 Species Catalog")]
        public static void RefreshSpeciesCatalog()
        {
            EnsureFolders();
            CreateSpeciesAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Species catalog refreshed: 420 species across all 30 families and 7 ranks (2 species per combination).");
        }

        [MenuItem("Fusion Any Monster/Setup/Assign Generated Species Sprites")]
        public static void AssignGeneratedSpeciesSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:SpeciesDefinition", new[] { "Assets/Game/Data/Species" });
            int assigned = 0;
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SpeciesDefinition definition = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(assetPath);
                if (definition == null) continue;
                string imagePath = "Assets/Game/Sprites/Generated/" + DemoDataFactory.SpriteSourceId(definition.speciesId) + ".png";
                if (definition.speciesId == "demo_00" && !File.Exists(imagePath))
                    imagePath = "Assets/Game/Sprites/Generated/demo_00_EmberSlime_PonyV6.png";
                if (!File.Exists(imagePath)) continue;

                TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency || importer.mipmapEnabled || !importer.isReadable))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
                Sprite sprite = LoadCharacterSprite(imagePath);
                if (sprite == null) continue;
                if(definition.sprite!=sprite){definition.sprite = sprite;EditorUtility.SetDirty(definition);}
                assigned++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Assigned generated sprites to " + assigned + " species master assets.");
        }
        private static Sprite LoadCharacterSprite(string path)
        {
            Sprite largest=null;
            foreach(Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                Sprite sprite=asset as Sprite;
                if(sprite!=null && (largest==null || sprite.rect.width*sprite.rect.height>largest.rect.width*largest.rect.height))largest=sprite;
            }
            return largest;
        }

        private static void CreateMasterAssets()
        {
            CreateSpeciesAssets();

            string[] personalityNames = { "Brave", "Timid", "Calm", "Cautious", "Aggressive", "Gentle" };
            var personalityDb = ScriptableObject.CreateInstance<PersonalityDatabase>();
            personalityDb.definitions = new PersonalityDefinition[6];
            for (int i = 0; i < 6; i++) personalityDb.definitions[i] = new PersonalityDefinition { type = (PersonalityType)i, displayName = personalityNames[i], boostedStatIndex = i == 0 ? 2 : i == 1 ? 6 : i == 2 ? 3 : i == 3 ? 4 : i == 4 ? 7 : 0, reducedStatIndex = i == 0 ? 4 : i == 1 ? 2 : i == 2 ? 6 : i == 3 ? 7 : i == 4 ? 4 : 7, aggression = i == 0 || i == 4 ? .9f : i == 1 ? .2f : .5f, caution = i == 1 || i == 3 ? .85f : .35f, teamwork = i == 5 ? .9f : .5f, lowHpPriority = i == 2 ? .95f : .5f };
            foreach (var definition in personalityDb.definitions) { definition.chargeCooldownMultiplier = PersonalityDefinition.DefaultChargeCooldown(definition.type); definition.chargeRetreatDistance = PersonalityDefinition.DefaultRetreatDistance(definition.type); }
            PersonalityDefinition[] personalityDefinitions = personalityDb.definitions;
            CreateOrReplaceAsset(personalityDb, "Assets/Game/Data/Personalities/PersonalityDatabase.asset");
            var runtimePersonalityDb = ScriptableObject.CreateInstance<PersonalityDatabase>(); runtimePersonalityDb.definitions = personalityDefinitions;
            CreateOrReplaceAsset(runtimePersonalityDb, "Assets/Game/Resources/PersonalityDatabase.asset");

            var breeding = ScriptableObject.CreateInstance<BreedingRuleDatabase>();
            var pairs = FamilyBreedingCatalog.CreateRules();
            breeding.familyRules = pairs;
            CreateOrReplaceAsset(breeding, "Assets/Game/Data/Breeding/BreedingRuleDatabase.asset");
            var runtimeBreedingDb = ScriptableObject.CreateInstance<BreedingRuleDatabase>(); runtimeBreedingDb.familyRules = FamilyBreedingCatalog.CreateRules();
            CreateOrReplaceAsset(runtimeBreedingDb, "Assets/Game/Resources/BreedingRuleDatabase.asset");
            var rankRule = ScriptableObject.CreateInstance<RankBreedingRule>();
            CreateOrReplaceAsset(rankRule, "Assets/Game/Data/Breeding/RankBreedingRule.asset");
            CreateOrReplaceAsset(ScriptableObject.CreateInstance<RankBreedingRule>(), "Assets/Game/Resources/RankBreedingRule.asset");
            CreateOrReplaceAsset(ScriptableObject.CreateInstance<RankMagicSlotDatabase>(), "Assets/Game/Resources/RankMagicSlotDatabase.asset");
            var settings = ScriptableObject.CreateInstance<ComfyUISettings>();
            CreateOrReplaceAsset(settings, "Assets/Game/Resources/ComfyUISettings.asset");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }

        private static void CreateSpeciesAssets()
        {
            string speciesFolder = "Assets/Game/Data/Species";
            if (!AssetDatabase.IsValidFolder(speciesFolder)) AssetDatabase.CreateFolder("Assets/Game/Data", "Species");
            SpeciesDefinition[] runtimeSpecies = DemoDataFactory.CreateSpecies();
            var db = ScriptableObject.CreateInstance<SpeciesDatabase>();
            var assets = new SpeciesDefinition[runtimeSpecies.Length];
            for (int i = 0; i < runtimeSpecies.Length; i++)
            {
                runtimeSpecies[i].defaultMagicGenomes=new MagicGenome[Spells.SpellSlotStorage.SlotCount];
                for(int slot=0;slot<runtimeSpecies[i].defaultMagicGenomes.Length;slot++)
                    runtimeSpecies[i].defaultMagicGenomes[slot]=Spells.SpeciesDefaultMagicCatalog.Create(runtimeSpecies[i],slot);
                string assetPath = speciesFolder + "/" + runtimeSpecies[i].speciesId + ".asset";
                SpeciesDefinition asset = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(assetPath);
                if (asset == null) { asset = runtimeSpecies[i]; asset.name = runtimeSpecies[i].speciesId; AssetDatabase.CreateAsset(asset, assetPath); }
                else
                {
                    Sprite existingSprite = asset.sprite;
                    EditorUtility.CopySerialized(runtimeSpecies[i], asset);
                    asset.sprite = existingSprite;
                    asset.name = runtimeSpecies[i].speciesId;
                    UnityEngine.Object.DestroyImmediate(runtimeSpecies[i]);
                }
                if (asset.sprite == null && asset.speciesId == "demo_00")
                    asset.sprite = LoadCharacterSprite("Assets/Game/Sprites/Generated/demo_00_EmberSlime_PonyV6.png");
                assets[i] = asset;
            }
            foreach (SpeciesDefinition definition in runtimeSpecies) if (definition != null && !AssetDatabase.Contains(definition)) UnityEngine.Object.DestroyImmediate(definition);
            db.species = assets;
            const string databasePath = "Assets/Game/Data/Species/SpeciesDatabase.asset";
            var existingDb = AssetDatabase.LoadAssetAtPath<SpeciesDatabase>(databasePath);
            if (existingDb == null) { if (File.Exists(databasePath)) AssetDatabase.DeleteAsset(databasePath); db.name = "SpeciesDatabase"; AssetDatabase.CreateAsset(db, databasePath); }
            else { existingDb.species = assets; UnityEngine.Object.DestroyImmediate(db); EditorUtility.SetDirty(existingDb); }
            CreateOrReplaceAsset(CloneDatabase(assets), "Assets/Game/Resources/SpeciesDatabase.asset");
        }

        private static SpeciesDatabase CloneDatabase(SpeciesDefinition[] assets)
        {
            var db = ScriptableObject.CreateInstance<SpeciesDatabase>(); db.name = "SpeciesDatabase"; db.species = assets; return db;
        }

        private static void CreateOrReplaceAsset(UnityEngine.Object value, string path)
        {
            value.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (existing == null) { if (File.Exists(path)) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(value, path); }
            else { EditorUtility.CopySerialized(value, existing); UnityEngine.Object.DestroyImmediate(value); EditorUtility.SetDirty(existing); }
        }

        private static void EnsureFolders()
        {
            string[] folders = { "Assets/Game", "Assets/Game/Editor", "Assets/Game/Runtime", "Assets/Game/Runtime/Battle", "Assets/Game/Data", "Assets/Game/Data/Species", "Assets/Game/Data/Personalities", "Assets/Game/Data/Breeding", "Assets/Game/Data/Magic", "Assets/Game/Resources", "Assets/Game/Prefabs", "Assets/Game/Scenes", "Assets/Game/Sprites", "Assets/Tests", "Assets/Tests/EditMode", "Assets/Tests/PlayMode" };
            foreach (string folder in folders) if (!AssetDatabase.IsValidFolder(folder)) { string parent = Path.GetDirectoryName(folder).Replace('\\', '/'); string name = Path.GetFileName(folder); AssetDatabase.CreateFolder(parent, name); }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++) if (scenes[i].path == scenePath) { scenes[i] = new EditorBuildSettingsScene(scenePath, true); EditorBuildSettings.scenes = scenes.ToArray(); return; }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void PrioritizeTitleAndBattle()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var ordered = new List<EditorBuildSettingsScene>();
            string[] priority = { "Assets/Game/Scenes/Title.unity", "Assets/Game/Scenes/Battle.unity" };
            foreach (string path in priority)
            {
                int index = scenes.FindIndex(s => s.path == path);
                if (index >= 0) { ordered.Add(new EditorBuildSettingsScene(path, true)); scenes.RemoveAt(index); }
            }
            ordered.AddRange(scenes); EditorBuildSettings.scenes = ordered.ToArray();
        }
    }
}
