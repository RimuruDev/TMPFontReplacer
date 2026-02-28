/*
 * Copyright (c) 2026 AbyssMoth
 * Author: RimuruDev
 * Licensed under the MIT License. See LICENSE in the package root for license information.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

namespace AbyssMoth
{
    internal enum FontReplacerSection
    {
        TMP = 0,
        Legacy = 1
    }

    internal enum FontReplacerOperation
    {
        Analyze = 0,
        Replace = 1
    }

    internal enum FontReplacerMatchKind
    {
        TmpTextMeshProUGUI = 0,
        TmpTextMeshPro = 1,
        UguiText = 2,
        TextMesh = 3
    }

    internal readonly struct FontReplacerMatch
    {
        public FontReplacerMatch(
            string prefabPath,
            string objectPath,
            FontReplacerMatchKind kind,
            string currentFontName,
            string replacementFontName)
        {
            PrefabPath = prefabPath ?? string.Empty;
            ObjectPath = objectPath ?? string.Empty;
            Kind = kind;
            CurrentFontName = currentFontName ?? string.Empty;
            ReplacementFontName = replacementFontName ?? string.Empty;
        }

        public string PrefabPath { get; }
        public string ObjectPath { get; }
        public FontReplacerMatchKind Kind { get; }
        public string CurrentFontName { get; }
        public string ReplacementFontName { get; }
    }

    internal sealed class FontReplacerReport
    {
        private readonly List<FontReplacerMatch> matches = new();
        private readonly Dictionary<FontReplacerMatchKind, int> perKindCounts = new();
        private readonly HashSet<string> matchedPrefabs = new(StringComparer.OrdinalIgnoreCase);

        public FontReplacerReport(
            FontReplacerSection section,
            FontReplacerOperation operation,
            string folderPath,
            string sourceFilterLabel,
            string replacementLabel)
        {
            Section = section;
            Operation = operation;
            FolderPath = folderPath ?? string.Empty;
            SourceFilterLabel = sourceFilterLabel ?? TMPFontReplacerConstants.AnyFontLabel;
            ReplacementLabel = replacementLabel ?? TMPFontReplacerConstants.NoReplacementLabel;
        }

        public FontReplacerSection Section { get; }
        public FontReplacerOperation Operation { get; }
        public string FolderPath { get; }
        public string SourceFilterLabel { get; }
        public string ReplacementLabel { get; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public IReadOnlyList<FontReplacerMatch> Matches => matches;
        public int MatchCount => matches.Count;
        public int MatchedPrefabCount => matchedPrefabs.Count;
        public int ScannedPrefabCount { get; private set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public void SetScannedPrefabCount(int scannedPrefabCount) =>
            ScannedPrefabCount = Math.Max(0, scannedPrefabCount);

        public void SetError(string errorMessage) =>
            ErrorMessage = errorMessage ?? string.Empty;

        public void AddMatch(
            string prefabPath,
            string objectPath,
            FontReplacerMatchKind kind,
            string currentFontName,
            string replacementFontName)
        {
            matches.Add(new FontReplacerMatch(prefabPath, objectPath, kind, currentFontName, replacementFontName));

            if (!string.IsNullOrWhiteSpace(prefabPath))
                matchedPrefabs.Add(prefabPath);

            if (perKindCounts.TryGetValue(kind, out var count))
                perKindCounts[kind] = count + 1;
            else
                perKindCounts[kind] = 1;
        }

        public int GetMatchCount(FontReplacerMatchKind kind) =>
            perKindCounts.TryGetValue(kind, out var count)
                ? count
                : 0;

        public string BuildSummary()
        {
            if (HasError)
                return $"{GetSectionLabel()}: {ErrorMessage}";

            var builder = new StringBuilder(capacity: 256);
            builder.Append(GetSectionLabel());
            builder.Append(": ");

            if (Operation == FontReplacerOperation.Replace)
            {
                builder.Append("replaced ");
                builder.Append(MatchCount);
                builder.Append(" font reference(s) in ");
            }
            else
            {
                builder.Append("found ");
                builder.Append(MatchCount);
                builder.Append(" matching font reference(s) in ");
            }

            builder.Append(MatchedPrefabCount);
            builder.Append(" prefab(s) out of ");
            builder.Append(ScannedPrefabCount);
            builder.Append(" scanned.");

            var breakdown = BuildBreakdownSummary();
            if (!string.IsNullOrWhiteSpace(breakdown))
            {
                builder.Append(' ');
                builder.Append(breakdown);
            }

            return builder.ToString();
        }

        public string BuildConsoleReport(int maxMatches = TMPFontReplacerConstants.MaxConsoleMatches)
        {
            var builder = new StringBuilder(capacity: 4096);
            builder.AppendLine(GetSectionLabel());
            builder.AppendLine(BuildSummary());
            builder.AppendLine($"Folder: {FolderPath}");
            builder.AppendLine($"Source Filter: {SourceFilterLabel}");
            builder.AppendLine($"Replacement: {ReplacementLabel}");

            if (MatchCount <= 0)
                return builder.ToString();

            builder.AppendLine("Matches:");

            var visibleCount = Math.Min(maxMatches, MatchCount);
            for (var i = 0; i < visibleCount; i++)
            {
                var match = matches[i];
                builder.Append("- [");
                builder.Append(GetMatchKindLabel(match.Kind));
                builder.Append("] ");
                builder.Append(match.PrefabPath);
                builder.Append(" -> ");
                builder.Append(match.ObjectPath);
                builder.Append(" | Current: ");
                builder.Append(match.CurrentFontName);
                builder.Append(" | Replacement: ");
                builder.AppendLine(match.ReplacementFontName);
            }

            if (MatchCount > visibleCount)
                builder.AppendLine($"- ... +{MatchCount - visibleCount} more match(es)");

            return builder.ToString();
        }

        public static string GetMatchKindLabel(FontReplacerMatchKind kind)
        {
            switch (kind)
            {
                case FontReplacerMatchKind.TmpTextMeshProUGUI:
                    return "TMP UGUI";
                case FontReplacerMatchKind.TmpTextMeshPro:
                    return "TMP 3D";
                case FontReplacerMatchKind.UguiText:
                    return "UGUI Text";
                case FontReplacerMatchKind.TextMesh:
                    return "TextMesh";
                default:
                    return "Unknown";
            }
        }

        private string GetSectionLabel()
        {
            switch (Section)
            {
                case FontReplacerSection.TMP:
                    return TMPFontReplacerConstants.TmpSectionTitle;
                case FontReplacerSection.Legacy:
                    return TMPFontReplacerConstants.LegacySectionTitle;
                default:
                    return "Font Replacer";
            }
        }

        private string BuildBreakdownSummary()
        {
            var parts = new List<string>(capacity: 2);

            switch (Section)
            {
                case FontReplacerSection.TMP:
                    AppendBreakdown(parts, FontReplacerMatchKind.TmpTextMeshProUGUI);
                    AppendBreakdown(parts, FontReplacerMatchKind.TmpTextMeshPro);
                    break;
                case FontReplacerSection.Legacy:
                    AppendBreakdown(parts, FontReplacerMatchKind.UguiText);
                    AppendBreakdown(parts, FontReplacerMatchKind.TextMesh);
                    break;
            }

            return parts.Count > 0
                ? string.Join(", ", parts)
                : string.Empty;
        }

        private void AppendBreakdown(List<string> parts, FontReplacerMatchKind kind)
        {
            var count = GetMatchCount(kind);
            if (count <= 0)
                return;

            parts.Add($"{GetMatchKindLabel(kind)}: {count}");
        }
    }
}
#endif
