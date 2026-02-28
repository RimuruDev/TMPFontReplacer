# TMP Font Replacer

TMP Font Replacer is an editor package for analyzing and replacing font references inside prefabs. The primary workflow targets TextMeshPro components first, and the package also includes a secondary legacy tab for TextMesh, with optional UGUI Text support when Unity UI is available.

## Installation

### Unity Package Manager

1. Open `Window > Package Manager`.
2. Click the `+` button.
3. Select `Add package from git URL...`.
4. Paste the Git URL of this package repository and confirm.

Example Git URL after publishing the repository:

`https://github.com/RimuruDev/TMPFontReplacer.git`

### Release Download

1. Open the Releases page of this package repository.
2. Download the published `.unitypackage` file if one is attached to the release.
3. Import it through `Assets > Import Package > Custom Package...`.

If the release contains the package source instead of a `.unitypackage`, copy the `TMPFontReplacer` folder into:

`Assets/Plugins/AbyssMoth/`

## Usage

1. Open `AbyssMoth/TMP Font Replacer`.
2. Choose a prefab folder inside `Assets`.
3. Optionally assign a source font to replace only exact matches.
4. Assign the replacement font.
5. Run `Analyze` to preview how many references will change.
6. Run `Replace TMP Fonts` or `Replace Legacy Fonts` when the report looks correct.

If the source font is empty, the tool targets every matching text component in the selected folder except components that already use the replacement font.

The package detects TextMeshPro and Unity UI at runtime, so it keeps working even if those packages are added later or imported from a different location. If TextMeshPro is not available, the TMP tab shows a warning instead of breaking compilation. In that case the legacy tab still works for 3D TextMesh objects.

The package currently targets Unity `6000.3.6f1`.

License: MIT
