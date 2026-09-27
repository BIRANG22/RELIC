using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class LocalizationEditorSafetyPolicy
{
    private const string DownloadRoot = "Assets/Project/Download/";

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

    public static bool ShouldScanSceneAsset(
        string assetPath,
        ISet<string> configuredScenePaths)
    {
        string normalized = NormalizeAssetPath(assetPath);
        return ShouldScanProjectAsset(normalized) &&
               configuredScenePaths != null &&
               configuredScenePaths.Contains(normalized);
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
