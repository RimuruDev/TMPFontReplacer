/*
 * Copyright (c) 2026 AbyssMoth
 * Author: RimuruDev
 * Licensed under the MIT License. See LICENSE in the package root for license information.
 */

using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AbyssMoth.Tests
{
    public sealed class TMPFontReplacerTests
    {
        private const string TestRootFolder = "Assets/TMPFontReplacerTests.Generated";

        [SetUp]
        public void SetUp()
        {
            CleanupTestAssets();
            EnsureFolder(TestRootFolder);
        }

        [TearDown]
        public void TearDown() =>
            CleanupTestAssets();

        [Test]
        public void AnalyzeTmp_WithoutOptionalSupport_ReturnsSafeErrorReport()
        {
            if (TMPFontReplacerIntegration.Instance.IsAvailable)
                Assert.Ignore("TextMeshPro is available in this project.");

            var report = TMPFontReplacerService.AnalyzeTmp(TestRootFolder, null, null);

            Assert.That(report.HasError, Is.True);
            Assert.That(report.ErrorMessage, Does.Contain("TextMeshPro"));
        }

        [Test]
        public void AnalyzeTmp_WhenTextMeshProIsAvailable_CountsTmpMatches()
        {
            if (!TMPFontReplacerIntegration.Instance.IsAvailable)
                Assert.Ignore("TextMeshPro is not available in this project.");

            var sourceFont = CreateTmpFontAsset("SourceTmp.asset");
            var replacementFont = CreateTmpFontAsset("ReplacementTmp.asset");
            CreateTmpPrefab("TmpAnalyze.prefab", sourceFont, replacementFont);

            var report = TMPFontReplacerService.AnalyzeTmp(TestRootFolder, sourceFont, replacementFont);

            Assert.That(report.HasError, Is.False);
            Assert.That(report.ScannedPrefabCount, Is.EqualTo(1));
            Assert.That(report.GetMatchCount(FontReplacerMatchKind.TmpTextMeshPro), Is.EqualTo(1));
        }

        [Test]
        public void AnalyzeLegacy_CountsTextMeshMatches()
        {
            var sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            CreateLegacyPrefab("LegacyAnalyze.prefab", sourceFont);

            var report = TMPFontReplacerService.AnalyzeLegacy(TestRootFolder, sourceFont, null);

            Assert.That(report.HasError, Is.False);
            Assert.That(report.ScannedPrefabCount, Is.EqualTo(1));
            Assert.That(report.GetMatchCount(FontReplacerMatchKind.TextMesh), Is.EqualTo(1));
        }

        [Test]
        public void ReplaceLegacy_ChangesTextMeshFont()
        {
            var sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var replacementFont = CreateFontAsset("ReplacementLegacy.fontsettings");
            var prefabPath = CreateLegacyPrefab("LegacyReplace.prefab", sourceFont);

            var report = TMPFontReplacerService.ReplaceLegacy(TestRootFolder, sourceFont, replacementFont);

            Assert.That(report.HasError, Is.False);
            Assert.That(report.GetMatchCount(FontReplacerMatchKind.TextMesh), Is.EqualTo(1));

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var textMesh = prefabRoot.transform.Find("TextMesh").GetComponent<TextMesh>();
                Assert.That(textMesh.font, Is.EqualTo(replacementFont));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Font CreateFontAsset(string fileName)
        {
            var font = new Font("ReplacementLegacy");
            AssetDatabase.CreateAsset(font, $"{TestRootFolder}/{fileName}");
            AssetDatabase.SaveAssets();
            return font;
        }

        private static UnityEngine.Object CreateTmpFontAsset(string fileName)
        {
            var tmpFontAssetType = GetRequiredType("TMPro.TMP_FontAsset", "Unity.TextMeshPro");
            var createMethod = tmpFontAssetType.GetMethod(
                "CreateFontAsset",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Font) },
                modifiers: null);

            Assert.That(createMethod, Is.Not.Null, "TMP_FontAsset.CreateFontAsset(Font) was not found.");

            var builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var tmpFontAsset = createMethod.Invoke(null, new object[] { builtinFont }) as UnityEngine.Object;

            Assert.That(tmpFontAsset, Is.Not.Null, "Failed to create a TMP font asset.");

            AssetDatabase.CreateAsset(tmpFontAsset, $"{TestRootFolder}/{fileName}");
            AssetDatabase.SaveAssets();
            return tmpFontAsset;
        }

        private static string CreateLegacyPrefab(string prefabName, Font sourceFont)
        {
            var root = new GameObject("LegacyRoot");
            try
            {
                var textMeshObject = new GameObject("TextMesh");
                textMeshObject.transform.SetParent(root.transform);
                var textMesh = textMeshObject.AddComponent<TextMesh>();
                textMesh.font = sourceFont;

                var prefabPath = $"{TestRootFolder}/{prefabName}";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                return prefabPath;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateTmpPrefab(
            string prefabName,
            UnityEngine.Object sourceFont,
            UnityEngine.Object replacementFont)
        {
            var tmpTextType = GetRequiredType("TMPro.TextMeshPro", "Unity.TextMeshPro");
            var fontProperty = tmpTextType.GetProperty("font", BindingFlags.Instance | BindingFlags.Public);

            Assert.That(fontProperty, Is.Not.Null, "TMP font property was not found.");

            var root = new GameObject("TmpRoot");
            try
            {
                var tmpObject = new GameObject("TMP 3D");
                tmpObject.transform.SetParent(root.transform);
                var tmpComponent = tmpObject.AddComponent(tmpTextType);
                fontProperty.SetValue(tmpComponent, sourceFont);

                var alreadyReplacementObject = new GameObject("Already Replacement");
                alreadyReplacementObject.transform.SetParent(root.transform);
                var alreadyReplacementComponent = alreadyReplacementObject.AddComponent(tmpTextType);
                fontProperty.SetValue(alreadyReplacementComponent, replacementFont);

                PrefabUtility.SaveAsPrefabAsset(root, $"{TestRootFolder}/{prefabName}");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Type GetRequiredType(string fullTypeName, string preferredAssemblyName)
        {
            var type = Type.GetType($"{fullTypeName}, {preferredAssemblyName}", throwOnError: false);
            if (type != null)
                return type;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(fullTypeName, throwOnError: false);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Type '{fullTypeName}' was not found.");
            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var segments = folderPath.Split('/');
            var currentPath = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var nextPath = $"{currentPath}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, segments[i]);

                currentPath = nextPath;
            }
        }

        private static void CleanupTestAssets()
        {
            if (AssetDatabase.IsValidFolder(TestRootFolder))
                AssetDatabase.DeleteAsset(TestRootFolder);

            AssetDatabase.Refresh();
        }
    }
}
