using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

/// <summary>원문(TMP/LocalizedTMPText)과 Text 표의 한국어 열을 비교해 잘못된 키만 복구합니다.</summary>
public static class LocalizationTextBindingRepairTool
{
    private const string KoreanHeader = "Korean(ko)";

    [MenuItem("Tools/Localization/Audit And Repair Text Bindings")]
    public static void AuditAndRepairFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        RepairAllBindings();
    }

    public static void RepairAllBindings()
    {
        try
        {
            LocalizationBindingResolver maps = ReadBindingMaps();
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                RepairSummary summary = RepairPrefabs(maps);
                summary += RepairScenes(maps);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[LocalizationTextBindingRepairTool] 검사 완료: 검사 {summary.Checked}, " +
                    $"정상 {summary.Matched}, 복구 {summary.Repaired}, " +
                    $"런타임/미등록 원문 스킵 {summary.Unresolved}.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static RepairSummary RepairPrefabs(LocalizationBindingResolver maps)
    {
        RepairSummary summary = default;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!LocalizationEditorSafetyPolicy.ShouldScanProjectAsset(path))
                continue;

            string prefabYaml = File.ReadAllText(path);
            if (!LocalizationEditorSafetyPolicy.ShouldProcessLocalizationPrefab(prefabYaml))
            {
                Debug.LogError($"[LocalizationTextBindingRepairTool] Missing Script가 있어 저장을 건너뜁니다: {path}");
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RepairSummary assetSummary = RepairHierarchy(root, path, maps);
                if (assetSummary.Repaired > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                summary += assetSummary;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return summary;
    }

    private static RepairSummary RepairScenes(LocalizationBindingResolver maps)
    {
        RepairSummary summary = default;
        HashSet<string> configuredScenePaths = EditorBuildSettings.scenes
            .Select(scene => (scene.path ?? string.Empty).Replace('\\', '/'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!LocalizationEditorSafetyPolicy.ShouldScanSceneAsset(path, configuredScenePaths))
                continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RepairSummary sceneSummary = default;
            foreach (GameObject root in scene.GetRootGameObjects())
                sceneSummary += RepairHierarchy(root, path, maps);

            if (sceneSummary.Repaired > 0)
                EditorSceneManager.SaveScene(scene);
            summary += sceneSummary;
        }

        return summary;
    }

    private static RepairSummary RepairHierarchy(
        GameObject root,
        string assetPath,
        LocalizationBindingResolver resolver)
    {
        RepairSummary summary = default;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!LocalizedTMPText.ShouldManageText(text))
                continue;

            string source = ReadKoreanSource(text);
            if (string.IsNullOrWhiteSpace(source))
                continue;

            string normalizedSource = LocalizationBindingResolver.Normalize(source);

            summary.Checked++;
            LocalizedTMPText existing = text.GetComponent<LocalizedTMPText>();
            LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
            string currentKey = existing != null ? existing.LocalizationKey : localizer != null
                ? localizer.StringReference.TableEntryReference.Key : string.Empty;
            if (resolver.Validate(source, currentKey) == LocalizationBindingStatus.Valid)
            {
                summary.Matched++;
                if (StaticLocalizationMigration.RepairTextBinding(text, currentKey))
                    summary.Repaired++;
                continue;
            }

            string generatedKey = LocalizationProjectScanner.BuildSuggestedKey(
                assetPath,
                GetHierarchyPath(text.transform),
                source);
            string serializedSceneKey = LocalizationProjectScanner.BuildSuggestedKey(
                assetPath,
                "text",
                source);
            string replacementKey;
            if (resolver.Validate(source, generatedKey) == LocalizationBindingStatus.Valid)
                replacementKey = generatedKey;
            else if (resolver.Validate(source, serializedSceneKey) == LocalizationBindingStatus.Valid)
                replacementKey = serializedSceneKey;
            else if (string.IsNullOrWhiteSpace(currentKey))
                replacementKey = resolver.ResolveSource(normalizedSource).Key;
            else
                replacementKey = string.Empty;
            if (!string.IsNullOrWhiteSpace(replacementKey))
            {
                if (StaticLocalizationMigration.RepairTextBinding(text, replacementKey))
                    summary.Repaired++;
            }
            else
            {
                // 수치, 이름 슬롯, New Text 등 런타임 대입값은 정적 키로 연결하지 않습니다.
                // 개별 Warning을 남기면 정상적인 런타임 UI에서도 Console이 오염됩니다.
                summary.Unresolved++;
            }
        }

        return summary;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var names = new Stack<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Push(current.name);
        return string.Join("/", names);
    }

    private static string ReadKoreanSource(TMP_Text text)
    {
        return LocalizationBindingSourcePolicy.GetSourceForRepair(text.text);
    }

    private static LocalizationBindingResolver ReadBindingMaps()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
            LocalizationExcelImporter.WorkbookPath,
            LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);

        int keyIndex = FindHeader(rows[0], "Key");
        int koreanIndex = FindHeader(rows[0], KoreanHeader);
        var entries = new List<LocalizationBindingEntry>();
        foreach (IReadOnlyList<string> row in rows.Skip(1))
        {
            string key = GetValue(row, keyIndex);
            string korean = GetValue(row, koreanIndex);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(korean))
                continue;

            entries.Add(new LocalizationBindingEntry(key, korean));
        }

        return new LocalizationBindingResolver(entries);
    }

    private static int FindHeader(IReadOnlyList<string> headers, string name)
    {
        for (int index = 0; index < headers.Count; index++)
            if (string.Equals(headers[index], name, StringComparison.Ordinal))
                return index;
        throw new InvalidDataException($"Localization worksheet header '{name}' was not found.");
    }

    private static string GetValue(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;

    private struct RepairSummary
    {
        public int Checked;
        public int Matched;
        public int Repaired;
        public int Unresolved;

        public static RepairSummary operator +(RepairSummary left, RepairSummary right) => new RepairSummary
        {
            Checked = left.Checked + right.Checked,
            Matched = left.Matched + right.Matched,
            Repaired = left.Repaired + right.Repaired,
            Unresolved = left.Unresolved + right.Unresolved,
        };
    }
}
