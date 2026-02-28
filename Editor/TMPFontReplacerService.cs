/*
 * Copyright (c) 2026 AbyssMoth
 * Author: RimuruDev
 * Licensed under the MIT License. See LICENSE in the package root for license information.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AbyssMoth
{
    internal static class TMPFontReplacerService
    {
        private const string PrefabSearchFilter = "t:Prefab";

        public static FontReplacerReport AnalyzeTmp(
            string folderPath,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont) =>
            TMPFontReplacerIntegration.Instance.AnalyzeTmp(folderPath, sourceFont, replacementFont);

        public static FontReplacerReport ReplaceTmp(
            string folderPath,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont) =>
            TMPFontReplacerIntegration.Instance.ReplaceTmp(folderPath, sourceFont, replacementFont);

        public static FontReplacerReport AnalyzeLegacy(
            string folderPath,
            Font sourceFont,
            Font replacementFont) =>
            ExecuteLegacy(folderPath, sourceFont, replacementFont, FontReplacerOperation.Analyze);

        public static FontReplacerReport ReplaceLegacy(
            string folderPath,
            Font sourceFont,
            Font replacementFont) =>
            ExecuteLegacy(folderPath, sourceFont, replacementFont, FontReplacerOperation.Replace);

        internal static bool TryCollectPrefabPaths(
            string folderPath,
            FontReplacerReport report,
            out List<string> prefabPaths)
        {
            prefabPaths = new List<string>();

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                report.SetError("Folder path cannot be empty.");
                return false;
            }

            if (!folderPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                report.SetError("Folder path must be inside the Assets folder.");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                report.SetError("Selected folder does not exist.");
                return false;
            }

            var guids = AssetDatabase.FindAssets(PrefabSearchFilter, new[] { folderPath });
            prefabPaths.Capacity = guids.Length;

            for (var i = 0; i < guids.Length; i++)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(prefabPath))
                    continue;

                prefabPaths.Add(prefabPath);
            }

            prefabPaths.Sort(StringComparer.OrdinalIgnoreCase);
            report.SetScannedPrefabCount(prefabPaths.Count);
            return true;
        }

        internal static string NormalizeFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return string.Empty;

            return folderPath.Trim().Replace('\\', '/').TrimEnd('/');
        }

        internal static string GetSourceFilterLabel(UnityEngine.Object sourceFont) =>
            sourceFont == null
                ? TMPFontReplacerConstants.AnyFontLabel
                : GetAssetLabel(sourceFont);

        internal static string GetAssetLabel(UnityEngine.Object asset)
        {
            if (asset == null)
                return TMPFontReplacerConstants.NoReplacementLabel;

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(assetPath))
                return string.IsNullOrWhiteSpace(asset.name)
                    ? TMPFontReplacerConstants.NonAssetLabel
                    : asset.name;

            return assetPath;
        }

        internal static string BuildObjectPath(Component component)
        {
            if (component == null)
                return string.Empty;

            return $"{BuildHierarchyPath(component.transform)} [{component.GetType().Name}]";
        }

        private static FontReplacerReport ExecuteLegacy(
            string folderPath,
            Font sourceFont,
            Font replacementFont,
            FontReplacerOperation operation)
        {
            var normalizedFolderPath = NormalizeFolderPath(folderPath);
            var report = new FontReplacerReport(
                FontReplacerSection.Legacy,
                operation,
                normalizedFolderPath,
                GetSourceFilterLabel(sourceFont),
                GetAssetLabel(replacementFont));

            if (operation == FontReplacerOperation.Replace && replacementFont == null)
            {
                report.SetError("Replacement legacy font is required.");
                return report;
            }

            if (!TryCollectPrefabPaths(normalizedFolderPath, report, out var prefabPaths))
                return report;

            for (var i = 0; i < prefabPaths.Count; i++)
                ProcessLegacyPrefab(prefabPaths[i], sourceFont, replacementFont, operation, report);

            if (operation == FontReplacerOperation.Replace && report.MatchCount > 0)
                AssetDatabase.SaveAssets();

            return report;
        }

        private static void ProcessLegacyPrefab(
            string prefabPath,
            Font sourceFont,
            Font replacementFont,
            FontReplacerOperation operation,
            FontReplacerReport report)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
                return;

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
                return;

            try
            {
                var modified = false;

                var textMeshComponents = prefabRoot.GetComponentsInChildren<TextMesh>(includeInactive: true);
                for (var i = 0; i < textMeshComponents.Length; i++)
                {
                    var textComponent = textMeshComponents[i];
                    if (textComponent == null || !ShouldMatch(textComponent.font, sourceFont, replacementFont))
                        continue;

                    report.AddMatch(
                        prefabPath,
                        BuildObjectPath(textComponent),
                        FontReplacerMatchKind.TextMesh,
                        GetAssetLabel(textComponent.font),
                        GetAssetLabel(replacementFont));

                    if (operation == FontReplacerOperation.Replace)
                    {
                        textComponent.font = replacementFont;
                        modified = true;
                    }
                }

                modified |= TMPFontReplacerIntegration.Instance.ProcessLegacyAdditionalComponents(
                    prefabRoot,
                    prefabPath,
                    sourceFont,
                    replacementFont,
                    operation,
                    report);

                if (operation == FontReplacerOperation.Replace && modified)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var names = new List<string>(capacity: 16);
            var current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static bool ShouldMatch(
            UnityEngine.Object currentFont,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont)
        {
            if (sourceFont != null)
            {
                if (currentFont != sourceFont)
                    return false;

                return replacementFont == null || currentFont != replacementFont;
            }

            return replacementFont == null || currentFont != replacementFont;
        }
    }
}
#endif
