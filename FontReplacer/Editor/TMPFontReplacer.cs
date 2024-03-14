// **************************************************************** //
//
//   Copyright (c) RimuruDev. All rights reserved.
//   Contact me: 
//          - Gmail:    rimuru.dev@gmail.com
//          - GitHub:   https://github.com/RimuruDev
//          - LinkedIn: https://www.linkedin.com/in/rimuru/
//          - GitHub Organizations: https://github.com/Rimuru-Dev
//
// **************************************************************** //

#if UNITY_EDITOR
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace RimuruDev.FontReplacer.Editor
{
    public sealed class TMPFontReplacer : EditorWindow
    {
        private TMP_FontAsset newFont;
        private string pathToPrefabFolder = "Assets/Internal";

        [MenuItem("RimuruDev Tools/TMP Font Replacer")]
        public static void ShowWindow() =>
            GetWindow<TMPFontReplacer>("Font Replacer");

        private void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);

            pathToPrefabFolder = EditorGUILayout.TextField("Path to Prefabs", pathToPrefabFolder);

            newFont = EditorGUILayout.ObjectField("New Font", newFont, typeof(TMP_FontAsset), false) as TMP_FontAsset;

            if (GUILayout.Button("Replace Fonts"))
                ReplaceFontsInPrefabs();
        }

        private void ReplaceFontsInPrefabs()
        {
            if (newFont == null)
            {
                Debug.LogError("You forgot to specify the font.");
                return;
            }

            if (string.IsNullOrWhiteSpace(pathToPrefabFolder))
            {
                Debug.LogError("The path to the prefab folders cannot be empty or null.");
                return;
            }

            try
            {
                var prefabs = Directory.GetFiles(pathToPrefabFolder, "*.prefab", SearchOption.AllDirectories);

                foreach (var prefabPath in prefabs)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    var textComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

                    foreach (var text in textComponents.Where(x => x != null))
                    {
                        text.font = newFont;
                        EditorUtility.SetDirty(text);
                    }

                    PrefabUtility.SavePrefabAsset(prefab);
                }

                AssetDatabase.SaveAssets();

                Debug.Log($"<color=magenta>Fonts replaced in {prefabs.Length} prefabs.</color>");
            }
            catch (DirectoryNotFoundException exception)
            {
                Debug.LogError($"You have specified a non-existent folder path. Error: {exception.Message}");
            }
        }
    }
}
#endif