using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class LocalizationEditorSafetyPolicy
{
    private const string DownloadRoot = "Assets/Project/Download/";
    // Permanently exclude this unused debug scene from localization scanning and repair.
    private const string ExcludedDebugScene = "Assets/Project/Scenes/YDM/DebugBattle.unity";
    private static readonly HashSet<string> WarnedBrokenScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex SourcePrefabReference = new Regex(
        @"m_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

    // Shop goods names are replaced at runtime by GameDataLocalization.
    // Retired template keys must not be imported again from serialized prefab placeholders.
    public static bool IsRetiredShopGoodsNameKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        for (int slot = 1; slot <= 4; slot++)
        {
            if (string.Equals(key, "ui.shop.goods_" + slot + "_skill_name_placeholder", StringComparison.Ordinal) ||
                string.Equals(key, "ui.shop.goods_" + slot + "_relic_name_placeholder", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    public static bool TryValidateWorkbookWritable(string workbookPath, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(workbookPath) || !File.Exists(workbookPath))
        {
            error = $"로컬리제이션 Excel 파일을 찾을 수 없습니다: {workbookPath}";
            return false;
        }

        string directory = Path.GetDirectoryName(workbookPath) ?? string.Empty;
        string ownerPath = Path.Combine(directory, "~$" + Path.GetFileName(workbookPath));
        if (File.Exists(ownerPath))
        {
            error = "Localization.xlsx가 Excel에서 열려 있습니다. Excel을 완전히 닫은 뒤 다시 실행하세요.";
            return false;
        }

        try
        {
            using FileStream unused = File.Open(
                workbookPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);
            return true;
        }
        catch (IOException)
        {
            error = "Localization.xlsx가 다른 프로그램에서 사용 중입니다. 파일을 닫은 뒤 다시 실행하세요.";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            error = "Localization.xlsx에 쓸 수 없습니다. 파일 권한과 읽기 전용 상태를 확인하세요.";
            return false;
        }
    }

    public static bool ShouldScanProjectAsset(string assetPath)
    {
        string normalized = NormalizeAssetPath(assetPath);
        return normalized.StartsWith("Assets/Project/", StringComparison.OrdinalIgnoreCase) &&
               !normalized.StartsWith(DownloadRoot, StringComparison.OrdinalIgnoreCase);
    }

    // Only checked scenes in Unity Build Settings are eligible for localization scene operations.
    public static HashSet<string> GetEnabledBuildScenePaths()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => NormalizeAssetPath(scene.path))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static bool ShouldScanSceneAsset(
        string assetPath,
        ISet<string> configuredScenePaths)
    {
        string normalized = NormalizeAssetPath(assetPath);
        if (string.Equals(normalized, ExcludedDebugScene, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!ShouldScanProjectAsset(normalized) ||
            configuredScenePaths == null || !configuredScenePaths.Contains(normalized))
            return false;

        // Unity opens a scene before the localization scan/repair can inspect its texts.
        // A missing source prefab triggers Unity's own scene-load errors and makes saving unsafe.
        // Check serialized references first, and never mutate or save a broken scene.
        if (TryFindMissingSourcePrefab(normalized, out string missingGuid))
        {
            if (WarnedBrokenScenes.Add(normalized))
                Debug.LogWarning($"[Localization] 씬 검사/복구 제외: {normalized} (Missing Prefab GUID: {missingGuid}). " +
                                 "프리팹과 .meta 파일 복원 후 다시 스캔하세요. 씬 파일은 변경하지 않았습니다.");
            return false;
        }
        return true;
    }

    private static bool TryFindMissingSourcePrefab(string scenePath, out string missingGuid)
    {
        missingGuid = null;
        if (!File.Exists(scenePath))
            return false;
        // Serialized text scenes can be inspected without opening the scene in the editor.
        // Binary scenes and unreadable assets are left to Unity's normal validation.
        try
        {
            string yaml = File.ReadAllText(scenePath);
            foreach (Match match in SourcePrefabReference.Matches(yaml))
            {
                string guid = match.Groups[1].Value;
                if (guid == "00000000000000000000000000000000")
                    continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    continue;
                missingGuid = guid;
                return true;
            }
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"[Localization] 씬 참조 검사 실패: {scenePath} ({exception.Message})");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogWarning($"[Localization] 씬 참조 검사 실패: {scenePath} ({exception.Message})");
        }
        return false;
    }

    public static bool ShouldProcessLocalizationPrefab(string yaml)
    {
        return string.IsNullOrEmpty(yaml) ||
               yaml.IndexOf("m_Script: {fileID: 0}", StringComparison.Ordinal) < 0;
    }

    public static bool CanRunAssetMutation(bool editorIsPlaying) => !editorIsPlaying;

    public static HashSet<string> CollectRetainedCandidateKeys(
        IEnumerable<LocalizationCandidate> scanCandidates)
    {
        return (scanCandidates ?? Array.Empty<LocalizationCandidate>())
            .Where(candidate => candidate != null &&
                                !candidate.RequiresReview &&
                                !string.IsNullOrWhiteSpace(candidate.Key))
            .Select(candidate => candidate.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string NormalizeAssetPath(string assetPath) =>
        (assetPath ?? string.Empty).Replace('\\', '/');
}
