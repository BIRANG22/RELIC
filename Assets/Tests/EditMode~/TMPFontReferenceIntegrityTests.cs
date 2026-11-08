#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

public sealed class TMPFontReferenceIntegrityTests
{
    private static readonly Regex FontReferencePattern = new(
        @"^\s*(m_fontAsset|m_sharedMaterial):\s*\{[^}]*guid:\s*([0-9a-f]{32}),",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Test]
    public void ProjectYaml_DoesNotReferenceMissingTmpFontAssetsOrMaterials()
    {
        var missingReferences = new List<string>();

        foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
        {
            if (!IsGameOwnedAsset(assetPath) ||
                !IsYamlAsset(assetPath) ||
                !File.Exists(assetPath))
                continue;

            int lineNumber = 0;
            foreach (string line in File.ReadLines(assetPath))
            {
                lineNumber++;
                Match match = FontReferencePattern.Match(line);
                if (!match.Success)
                    continue;

                string guid = match.Groups[2].Value;
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    missingReferences.Add($"{assetPath}:{lineNumber} {match.Groups[1].Value} -> {guid}");
            }
        }

        Assert.That(
            missingReferences,
            Is.Empty,
            "Missing TMP font/material references:\n" + string.Join("\n", missingReferences));
    }

    private static bool IsYamlAsset(string assetPath)
    {
        string extension = Path.GetExtension(assetPath);
        return extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".unity", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".asset", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGameOwnedAsset(string assetPath)
    {
        return assetPath.StartsWith("Assets/Project/", StringComparison.Ordinal) ||
               string.Equals(
                   Path.GetDirectoryName(assetPath)?.Replace('\\', '/'),
                   "Assets",
                   StringComparison.Ordinal);
    }
}
#endif
