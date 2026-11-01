#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FusionAnyMonster.Art
{
    public static class ComfyUISettingsCreator
    {
        [MenuItem("Assets/Create/Fusion Any Monster/ComfyUI Settings")]
        private static void Create()
        {
            var settings = ScriptableObject.CreateInstance<ComfyUISettings>();
            const string path = "Assets/Game/Data/ComfyUISettings.asset";
            AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
#endif
