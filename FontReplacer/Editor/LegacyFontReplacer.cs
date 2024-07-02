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
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace RimuruDev.FontReplacer.Editor
{
    public class LegacyFontReplacer : EditorWindow
    {
        private Font newFont;
        private string pathToPrefabFolder = "Assets/Internal";

        [MenuItem("RimuruDev Tools/Legacy Font Replacer")]
        public static void ShowWindow() =>
            GetWindow<LegacyFontReplacer>("Font Replacer");

        private void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);

            pathToPrefabFolder = EditorGUILayout.TextField("Path to Prefabs", pathToPrefabFolder);

            newFont = EditorGUILayout.ObjectField("New Font", newFont, typeof(Font), false) as Font;

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

                // Legacy UnityEngine.UI.Text
                foreach (var prefabPath in prefabs)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    var textComponents = prefab.GetComponentsInChildren<Text>(true);

                    foreach (var text in textComponents.Where(x => x != null))
                    {
                        text.font = newFont;
                        EditorUtility.SetDirty(text);
                    }

                    PrefabUtility.SavePrefabAsset(prefab);
                }

                // Legacy UnityEngine.TextMesh
                foreach (var prefabPath in prefabs)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    var textComponents = prefab.GetComponentsInChildren<TextMesh>(true);

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
