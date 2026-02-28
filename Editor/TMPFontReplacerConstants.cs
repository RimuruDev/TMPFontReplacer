/*
 * Copyright (c) 2026 AbyssMoth
 * Author: RimuruDev
 * Licensed under the MIT License. See LICENSE in the package root for license information.
 */

#if UNITY_EDITOR
namespace AbyssMoth
{
    internal static class TMPFontReplacerConstants
    {
        public const string MenuItemPath = "AbyssMoth/TMP Font Replacer";
        public const string WindowTitle = "TMP Font Replacer";
        public const string TmpSectionTitle = "TMP Font Replacer";
        public const string LegacySectionTitle = "Legacy Font Replacer";
        public const string DefaultFolderPath = "Assets";
        public const string AnyFontLabel = "<any font>";
        public const string NoReplacementLabel = "<none>";
        public const string NonAssetLabel = "<not an asset>";
        public const string MissingTmpMessage =
            "TextMeshPro is not available in this project. The TMP tab is disabled until the required TextMeshPro package is installed.";
        public const string LegacyTextMeshOnlyMessage =
            "Legacy mode currently works with 3D TextMesh components. UGUI Text support becomes available when the Unity UI package is installed.";
        public const int MaxVisibleMatches = 100;
        public const int MaxConsoleMatches = 256;
        public const float WindowMinWidth = 720f;
        public const float WindowMinHeight = 500f;
        public const float SmallSpacing = 6f;
        public const float MediumSpacing = 8f;
        public const float LargeSpacing = 10f;
        public const float ActionButtonHeight = 30f;
    }
}
#endif
