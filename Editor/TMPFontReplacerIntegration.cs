/*
 * Copyright (c) 2026 AbyssMoth
 * Author: RimuruDev
 * Licensed under the MIT License. See LICENSE in the package root for license information.
 */

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AbyssMoth
{
    internal interface ITMPFontReplacerIntegration
    {
        bool IsAvailable { get; }
        bool HasUguiSupport { get; }
        string UnavailableMessage { get; }

        UnityEngine.Object DrawTmpFontField(string label, UnityEngine.Object currentValue);
        FontReplacerReport AnalyzeTmp(string folderPath, UnityEngine.Object sourceFont, UnityEngine.Object replacementFont);
        FontReplacerReport ReplaceTmp(string folderPath, UnityEngine.Object sourceFont, UnityEngine.Object replacementFont);
        bool ProcessLegacyAdditionalComponents(
            GameObject prefabRoot,
            string prefabPath,
            Font sourceFont,
            Font replacementFont,
            FontReplacerOperation operation,
            FontReplacerReport report);
    }

    internal static class TMPFontReplacerIntegration
    {
        private static readonly ITMPFontReplacerIntegration DefaultInstance = new TMPFontReplacerRuntimeIntegration();
        private static ITMPFontReplacerIntegration testOverride;

        public static ITMPFontReplacerIntegration Instance => testOverride ?? DefaultInstance;

        internal static void SetTestOverride(ITMPFontReplacerIntegration integration) =>
            testOverride = integration;

        internal static void ClearTestOverride() =>
            testOverride = null;
    }

    internal sealed class TMPFontReplacerRuntimeIntegration : ITMPFontReplacerIntegration
    {
        public bool IsAvailable =>
            TmpFontAssetType != null &&
            TmpTextType != null &&
            GetFontProperty(TmpTextType) != null;

        public bool HasUguiSupport =>
            UguiTextType != null &&
            GetFontProperty(UguiTextType) != null;

        public string UnavailableMessage => TMPFontReplacerConstants.MissingTmpMessage;

        public UnityEngine.Object DrawTmpFontField(string label, UnityEngine.Object currentValue)
        {
            if (!IsAvailable)
                return currentValue;

            return EditorGUILayout.ObjectField(label, currentValue, TmpFontAssetType, false);
        }

        public FontReplacerReport AnalyzeTmp(string folderPath, UnityEngine.Object sourceFont, UnityEngine.Object replacementFont)
        {
            if (!IsAvailable)
                return BuildUnavailableReport(folderPath, sourceFont, replacementFont, FontReplacerOperation.Analyze);

            return ExecuteTmp(folderPath, sourceFont, replacementFont, FontReplacerOperation.Analyze);
        }

        public FontReplacerReport ReplaceTmp(string folderPath, UnityEngine.Object sourceFont, UnityEngine.Object replacementFont)
        {
            if (!IsAvailable)
                return BuildUnavailableReport(folderPath, sourceFont, replacementFont, FontReplacerOperation.Replace);

            return ExecuteTmp(folderPath, sourceFont, replacementFont, FontReplacerOperation.Replace);
        }

        public bool ProcessLegacyAdditionalComponents(
            GameObject prefabRoot,
            string prefabPath,
            Font sourceFont,
            Font replacementFont,
            FontReplacerOperation operation,
            FontReplacerReport report)
        {
            var uguiTextType = UguiTextType;
            var fontProperty = GetFontProperty(uguiTextType);
            if (uguiTextType == null || fontProperty == null || prefabRoot == null)
                return false;

            var modified = false;
            var uiTextComponents = prefabRoot.GetComponentsInChildren(uguiTextType, includeInactive: true);
            for (var i = 0; i < uiTextComponents.Length; i++)
            {
                if (uiTextComponents[i] is not Component textComponent)
                    continue;

                var currentFont = fontProperty.GetValue(textComponent) as Font;
                if (!ShouldMatch(currentFont, sourceFont, replacementFont))
                    continue;

                report.AddMatch(
                    prefabPath,
                    TMPFontReplacerService.BuildObjectPath(textComponent),
                    FontReplacerMatchKind.UguiText,
                    TMPFontReplacerService.GetAssetLabel(currentFont),
                    TMPFontReplacerService.GetAssetLabel(replacementFont));

                if (operation != FontReplacerOperation.Replace)
                    continue;

                fontProperty.SetValue(textComponent, replacementFont);
                modified = true;
            }

            return modified;
        }

        private FontReplacerReport BuildUnavailableReport(
            string folderPath,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont,
            FontReplacerOperation operation)
        {
            var report = new FontReplacerReport(
                FontReplacerSection.TMP,
                operation,
                TMPFontReplacerService.NormalizeFolderPath(folderPath),
                TMPFontReplacerService.GetSourceFilterLabel(sourceFont),
                TMPFontReplacerService.GetAssetLabel(replacementFont));

            report.SetError(UnavailableMessage);
            return report;
        }

        private FontReplacerReport ExecuteTmp(
            string folderPath,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont,
            FontReplacerOperation operation)
        {
            var normalizedFolderPath = TMPFontReplacerService.NormalizeFolderPath(folderPath);
            var report = new FontReplacerReport(
                FontReplacerSection.TMP,
                operation,
                normalizedFolderPath,
                TMPFontReplacerService.GetSourceFilterLabel(sourceFont),
                TMPFontReplacerService.GetAssetLabel(replacementFont));

            if (operation == FontReplacerOperation.Replace && replacementFont == null)
            {
                report.SetError("Replacement TMP font is required.");
                return report;
            }

            if (!TMPFontReplacerService.TryCollectPrefabPaths(normalizedFolderPath, report, out var prefabPaths))
                return report;

            for (var i = 0; i < prefabPaths.Count; i++)
                ProcessTmpPrefab(prefabPaths[i], sourceFont, replacementFont, operation, report);

            if (operation == FontReplacerOperation.Replace && report.MatchCount > 0)
                AssetDatabase.SaveAssets();

            return report;
        }

        private void ProcessTmpPrefab(
            string prefabPath,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont,
            FontReplacerOperation operation,
            FontReplacerReport report)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
                return;

            var tmpTextType = TmpTextType;
            var fontProperty = GetFontProperty(tmpTextType);
            if (tmpTextType == null || fontProperty == null)
                return;

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
                return;

            try
            {
                var modified = false;
                var textComponents = prefabRoot.GetComponentsInChildren(tmpTextType, includeInactive: true);
                for (var i = 0; i < textComponents.Length; i++)
                {
                    if (textComponents[i] is not Component textComponent)
                        continue;

                    var currentFont = fontProperty.GetValue(textComponent) as UnityEngine.Object;
                    if (!ShouldMatch(currentFont, sourceFont, replacementFont))
                        continue;

                    report.AddMatch(
                        prefabPath,
                        TMPFontReplacerService.BuildObjectPath(textComponent),
                        ResolveTmpMatchKind(textComponent),
                        TMPFontReplacerService.GetAssetLabel(currentFont),
                        TMPFontReplacerService.GetAssetLabel(replacementFont));

                    if (operation != FontReplacerOperation.Replace)
                        continue;

                    fontProperty.SetValue(textComponent, replacementFont);
                    modified = true;
                }

                if (operation == FontReplacerOperation.Replace && modified)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static FontReplacerMatchKind ResolveTmpMatchKind(Component textComponent)
        {
            if (textComponent == null)
                return FontReplacerMatchKind.TmpTextMeshPro;

            var tmpTextUguiType = TmpTextUguiType;
            if (tmpTextUguiType != null && tmpTextUguiType.IsInstanceOfType(textComponent))
                return FontReplacerMatchKind.TmpTextMeshProUGUI;

            var tmpText3DType = TmpText3DType;
            if (tmpText3DType != null && tmpText3DType.IsInstanceOfType(textComponent))
                return FontReplacerMatchKind.TmpTextMeshPro;

            return FontReplacerMatchKind.TmpTextMeshPro;
        }

        private static PropertyInfo GetFontProperty(Type componentType) =>
            componentType?.GetProperty("font", BindingFlags.Instance | BindingFlags.Public);

        private static Type TmpFontAssetType => FindType("TMPro.TMP_FontAsset", "Unity.TextMeshPro");
        private static Type TmpTextType => FindType("TMPro.TMP_Text", "Unity.TextMeshPro");
        private static Type TmpText3DType => FindType("TMPro.TextMeshPro", "Unity.TextMeshPro");
        private static Type TmpTextUguiType => FindType("TMPro.TextMeshProUGUI", "Unity.TextMeshPro");
        private static Type UguiTextType => FindType("UnityEngine.UI.Text", "UnityEngine.UI");

        private static Type FindType(string fullTypeName, string preferredAssemblyName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
                return null;

            if (!string.IsNullOrWhiteSpace(preferredAssemblyName))
            {
                var qualifiedType = Type.GetType($"{fullTypeName}, {preferredAssemblyName}", throwOnError: false);
                if (qualifiedType != null)
                    return qualifiedType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolvedType = assemblies[i].GetType(fullTypeName, throwOnError: false);
                if (resolvedType != null)
                    return resolvedType;
            }

            return null;
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
