/*
 * Copyright (c) 2026 AbyssMoth
 * Author: RimuruDev
 * Licensed under the MIT License. See LICENSE in the package root for license information.
 */

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AbyssMoth
{
    public sealed class TMPFontReplacerWindow : EditorWindow
    {
        private enum FontReplacerTab
        {
            TMP = 0,
            Legacy = 1
        }

        [Serializable]
        private class FontReplacerTabState
        {
            public DefaultAsset folderAsset;
            public string folderPath = TMPFontReplacerConstants.DefaultFolderPath;
            public FontReplacerReport lastReport;
        }

        [Serializable]
        private sealed class TmpTabState : FontReplacerTabState
        {
            public UnityEngine.Object sourceFont;
            public UnityEngine.Object replacementFont;
        }

        [Serializable]
        private sealed class LegacyTabState : FontReplacerTabState
        {
            public Font sourceFont;
            public Font replacementFont;
        }

        [SerializeField] private FontReplacerTab selectedTab = FontReplacerTab.TMP;
        [SerializeField] private TmpTabState tmpState = new();
        [SerializeField] private LegacyTabState legacyState = new();

        private Vector2 scroll;

        [MenuItem(TMPFontReplacerConstants.MenuItemPath)]
        public static void OpenWindow()
        {
            var window = GetWindow<TMPFontReplacerWindow>();
            window.titleContent = new GUIContent(TMPFontReplacerConstants.WindowTitle);
            window.minSize = new Vector2(
                TMPFontReplacerConstants.WindowMinWidth,
                TMPFontReplacerConstants.WindowMinHeight);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(TMPFontReplacerConstants.SmallSpacing);
            selectedTab = (FontReplacerTab)GUILayout.Toolbar(
                (int)selectedTab,
                new[]
                {
                    TMPFontReplacerConstants.TmpSectionTitle,
                    TMPFontReplacerConstants.LegacySectionTitle
                });

            EditorGUILayout.Space(TMPFontReplacerConstants.SmallSpacing);

            using var scope = new EditorGUILayout.ScrollViewScope(scroll);
            scroll = scope.scrollPosition;

            switch (selectedTab)
            {
                case FontReplacerTab.TMP:
                    DrawTmpTab();
                    break;
                case FontReplacerTab.Legacy:
                    DrawLegacyTab();
                    break;
            }
        }

        private void DrawTmpTab()
        {
            EditorGUILayout.LabelField(TMPFontReplacerConstants.TmpSectionTitle, EditorStyles.boldLabel);
            DrawFolderFields(tmpState);

            if (!TMPFontReplacerIntegration.Instance.IsAvailable)
            {
                EditorGUILayout.HelpBox(TMPFontReplacerIntegration.Instance.UnavailableMessage, MessageType.Warning);
                DrawReport(tmpState.lastReport);
                return;
            }

            EditorGUILayout.HelpBox(
                "Analyzes prefab folders before replacing fonts. If Source TMP Font is empty, the tool targets every TMP text component except ones that already use the replacement font.",
                MessageType.Info);

            tmpState.sourceFont = TMPFontReplacerIntegration.Instance.DrawTmpFontField(
                "Source TMP Font (Optional)",
                tmpState.sourceFont);

            tmpState.replacementFont = TMPFontReplacerIntegration.Instance.DrawTmpFontField(
                "Replacement TMP Font",
                tmpState.replacementFont);

            EditorGUILayout.Space(TMPFontReplacerConstants.MediumSpacing);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(!CanAnalyze(tmpState.folderPath));
                if (GUILayout.Button("Analyze", GUILayout.Height(TMPFontReplacerConstants.ActionButtonHeight)))
                    RunTmpAnalyze();
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!CanReplace(tmpState.folderPath, tmpState.sourceFont, tmpState.replacementFont));
                if (GUILayout.Button("Replace TMP Fonts", GUILayout.Height(TMPFontReplacerConstants.ActionButtonHeight)))
                    RunTmpReplace();
                EditorGUI.EndDisabledGroup();
            }

            DrawReport(tmpState.lastReport);
        }

        private void DrawLegacyTab()
        {
            EditorGUILayout.LabelField(TMPFontReplacerConstants.LegacySectionTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                TMPFontReplacerIntegration.Instance.HasUguiSupport
                    ? "Legacy mode works with UGUI Text and 3D TextMesh components. If Source Legacy Font is empty, the tool targets every legacy text component except ones that already use the replacement font."
                    : TMPFontReplacerConstants.LegacyTextMeshOnlyMessage,
                MessageType.Info);

            DrawFolderFields(legacyState);

            legacyState.sourceFont = (Font)EditorGUILayout.ObjectField(
                "Source Legacy Font (Optional)",
                legacyState.sourceFont,
                typeof(Font),
                allowSceneObjects: false);

            legacyState.replacementFont = (Font)EditorGUILayout.ObjectField(
                "Replacement Legacy Font",
                legacyState.replacementFont,
                typeof(Font),
                allowSceneObjects: false);

            EditorGUILayout.Space(TMPFontReplacerConstants.MediumSpacing);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(!CanAnalyze(legacyState.folderPath));
                if (GUILayout.Button("Analyze", GUILayout.Height(TMPFontReplacerConstants.ActionButtonHeight)))
                    RunLegacyAnalyze();
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!CanReplace(legacyState.folderPath, legacyState.sourceFont, legacyState.replacementFont));
                if (GUILayout.Button("Replace Legacy Fonts", GUILayout.Height(TMPFontReplacerConstants.ActionButtonHeight)))
                    RunLegacyReplace();
                EditorGUI.EndDisabledGroup();
            }

            DrawReport(legacyState.lastReport);
        }

        private void DrawFolderFields(FontReplacerTabState state)
        {
            var selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Prefab Folder",
                state.folderAsset,
                typeof(DefaultAsset),
                allowSceneObjects: false);

            if (selectedFolder != state.folderAsset)
            {
                state.folderAsset = ResolveFolderAsset(selectedFolder);
                if (state.folderAsset != null)
                    state.folderPath = AssetDatabase.GetAssetPath(state.folderAsset);
            }

            var updatedPath = EditorGUILayout.TextField("Folder Path", state.folderPath);
            if (!string.Equals(updatedPath, state.folderPath, StringComparison.Ordinal))
            {
                state.folderPath = updatedPath;
                state.folderAsset = ResolveFolderAssetFromPath(updatedPath);
            }

            if (!AssetDatabase.IsValidFolder(state.folderPath))
            {
                EditorGUILayout.HelpBox(
                    "Select a folder inside Assets. The replacer only scans prefabs from a valid project folder.",
                    MessageType.Warning);
            }
        }

        private void DrawReport(FontReplacerReport report)
        {
            if (report == null)
                return;

            EditorGUILayout.Space(TMPFontReplacerConstants.LargeSpacing);
            EditorGUILayout.LabelField("Last Report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                report.BuildSummary(),
                report.HasError ? MessageType.Error : MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Console Report"))
                    EditorGUIUtility.systemCopyBuffer = report.BuildConsoleReport();

                if (GUILayout.Button("Clear Report"))
                {
                    if (selectedTab == FontReplacerTab.TMP)
                        tmpState.lastReport = null;
                    else
                        legacyState.lastReport = null;

                    return;
                }
            }

            if (report.MatchCount <= 0)
                return;

            EditorGUILayout.Space(TMPFontReplacerConstants.SmallSpacing);
            var visibleCount = Mathf.Min(TMPFontReplacerConstants.MaxVisibleMatches, report.MatchCount);
            EditorGUILayout.LabelField($"Matches Preview ({visibleCount}/{report.MatchCount})", EditorStyles.boldLabel);

            for (var i = 0; i < visibleCount; i++)
            {
                var match = report.Matches[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"[{FontReplacerReport.GetMatchKindLabel(match.Kind)}] {match.PrefabPath}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Object", match.ObjectPath);
                    EditorGUILayout.LabelField("Current Font", match.CurrentFontName);
                    EditorGUILayout.LabelField("Replacement", match.ReplacementFontName);
                }
            }

            if (report.MatchCount > visibleCount)
            {
                EditorGUILayout.HelpBox(
                    $"Only the first {visibleCount} matches are shown in the window. Copy the console report for the full list.",
                    MessageType.None);
            }
        }

        private void RunTmpAnalyze()
        {
            tmpState.lastReport = TMPFontReplacerService.AnalyzeTmp(
                tmpState.folderPath,
                tmpState.sourceFont,
                tmpState.replacementFont);

            Debug.Log(tmpState.lastReport.BuildConsoleReport());
            Repaint();
        }

        private void RunTmpReplace()
        {
            if (!CanReplace(tmpState.folderPath, tmpState.sourceFont, tmpState.replacementFont))
                return;

            var confirmed = EditorUtility.DisplayDialog(
                "Replace TMP Fonts",
                BuildReplaceConfirmationMessage(
                    tmpState.folderPath,
                    tmpState.sourceFont,
                    tmpState.replacementFont,
                    tmpState.lastReport),
                "Replace",
                "Cancel");

            if (!confirmed)
                return;

            tmpState.lastReport = TMPFontReplacerService.ReplaceTmp(
                tmpState.folderPath,
                tmpState.sourceFont,
                tmpState.replacementFont);

            Debug.Log(tmpState.lastReport.BuildConsoleReport());
            Repaint();
        }

        private void RunLegacyAnalyze()
        {
            legacyState.lastReport = TMPFontReplacerService.AnalyzeLegacy(
                legacyState.folderPath,
                legacyState.sourceFont,
                legacyState.replacementFont);

            Debug.Log(legacyState.lastReport.BuildConsoleReport());
            Repaint();
        }

        private void RunLegacyReplace()
        {
            if (!CanReplace(legacyState.folderPath, legacyState.sourceFont, legacyState.replacementFont))
                return;

            var confirmed = EditorUtility.DisplayDialog(
                "Replace Legacy Fonts",
                BuildReplaceConfirmationMessage(
                    legacyState.folderPath,
                    legacyState.sourceFont,
                    legacyState.replacementFont,
                    legacyState.lastReport),
                "Replace",
                "Cancel");

            if (!confirmed)
                return;

            legacyState.lastReport = TMPFontReplacerService.ReplaceLegacy(
                legacyState.folderPath,
                legacyState.sourceFont,
                legacyState.replacementFont);

            Debug.Log(legacyState.lastReport.BuildConsoleReport());
            Repaint();
        }

        private static bool CanAnalyze(string folderPath) =>
            !string.IsNullOrWhiteSpace(folderPath);

        private static bool CanReplace(string folderPath, UnityEngine.Object sourceFont, UnityEngine.Object replacementFont)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || replacementFont == null)
                return false;

            return sourceFont != replacementFont;
        }

        private static DefaultAsset ResolveFolderAsset(DefaultAsset folderAsset)
        {
            if (folderAsset == null)
                return null;

            var assetPath = AssetDatabase.GetAssetPath(folderAsset);
            return AssetDatabase.IsValidFolder(assetPath)
                ? folderAsset
                : null;
        }

        private static DefaultAsset ResolveFolderAssetFromPath(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return null;

            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
        }

        private static string BuildReplaceConfirmationMessage(
            string folderPath,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont,
            FontReplacerReport lastReport)
        {
            var reportSummary = lastReport == null
                ? "Run Analyze first if you want a preview before replacing."
                : lastReport.BuildSummary();

            return
                "The tool will scan prefabs inside the selected folder and replace matching font references.\n\n" +
                $"Folder:\n{folderPath}\n\n" +
                $"Source Filter:\n{GetAssetPathOrLabel(sourceFont, TMPFontReplacerConstants.AnyFontLabel)}\n\n" +
                $"Replacement:\n{GetAssetPathOrLabel(replacementFont, TMPFontReplacerConstants.NoReplacementLabel)}\n\n" +
                $"{reportSummary}\n\n" +
                "Make sure you have version control or a backup before replacing.";
        }

        private static string GetAssetPathOrLabel(UnityEngine.Object asset, string fallbackLabel)
        {
            if (asset == null)
                return fallbackLabel;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(assetPath)
                ? asset.name
                : assetPath;
        }
    }
}
#endif
