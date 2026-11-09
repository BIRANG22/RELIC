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

    [MenuItem("Tools/Localization/고급 기능/텍스트 바인딩 검사 및 복구 (연결 오류 수정)")]
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
            // Register previously unregistered Korean static UI text before repairing bindings.
            // Runtime placeholders and intentionally excluded UI are never registered.
            int added = RegisterMissingStaticSources(maps);
            if (added > 0)
            {
                LocalizationExcelImporter.Import();
                maps = ReadBindingMaps();
            }
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                RepairSummary summary = RepairPrefabs(maps);
                summary += RepairScenes(maps);
                // 변경된 프리팹과 씬은 각 저장 API로 이미 저장했다. 전역 SaveAssets는
                // 검사 중 갱신된 Dynamic TMP 아틀라스까지 저장 대상으로 끌어들일 수 있다.
                Debug.Log(
                    $"[LocalizationTextBindingRepairTool] 검사 완료: 검사 {summary.Checked}, " +
                    $"정상 {summary.Matched}, 복구 {summary.Repaired}, " +
                    $"신규 원문 등록 {added}, 비한국어/런타임 스킵 {summary.RuntimeSkipped}, " +
                    $"미등록/충돌 보류 {summary.Unresolved}.");
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

    private static int RegisterMissingStaticSources(LocalizationBindingResolver maps)
    {
        var additions = new List<LocalizationWorkbookEntry>();
        var chosenBySource = new Dictionary<string, string>(StringComparer.Ordinal);
        var claimedKeys = new HashSet<string>(StringComparer.Ordinal);
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!LocalizationEditorSafetyPolicy.ShouldScanProjectAsset(path)) continue;
                if (!LocalizationEditorSafetyPolicy.ShouldProcessLocalizationPrefab(File.ReadAllText(path))) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try { CollectMissingInHierarchy(root, path, maps, chosenBySource, claimedKeys, additions); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            HashSet<string> configured = LocalizationEditorSafetyPolicy.GetEnabledBuildScenePaths();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!LocalizationEditorSafetyPolicy.ShouldScanSceneAsset(path, configured)) continue;
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                    CollectMissingInHierarchy(root, path, maps, chosenBySource, claimedKeys, additions);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        if (additions.Count == 0) return 0;
        int created = LocalizationWorkbookWriter.MergeNewEntries(LocalizationExcelImporter.WorkbookPath, additions);
        Debug.Log($"[LocalizationTextBindingRepairTool] 미등록 한국어 UI 원문 {created}개를 백업 후 Excel에 등록했습니다. 다른 언어 번역은 기존 값만 재사용하며 신규 번역은 별도 입력이 필요합니다.");
        return created;
    }

    private static void CollectMissingInHierarchy(GameObject root, string assetPath,
        LocalizationBindingResolver maps, IDictionary<string, string> chosenBySource,
        ISet<string> claimedKeys, ICollection<LocalizationWorkbookEntry> additions)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!LocalizedTMPText.ShouldManageText(text)) continue;
            string source = ReadKoreanSource(text);
            if (!LocalizationProjectScanner.IsLocalizableKoreanText(source)) continue;
            string normalized = LocalizationBindingResolver.Normalize(source);
            LocalizedTMPText local = text.GetComponent<LocalizedTMPText>();
            LocalizeStringEvent legacy = text.GetComponent<LocalizeStringEvent>();
            string boundKey = local != null ? local.LocalizationKey : legacy != null
                ? legacy.StringReference.TableEntryReference.Key : string.Empty;
            if (LocalizationEditorSafetyPolicy.IsRetiredShopGoodsNameKey(boundKey))
                continue; // Dynamic shop item name; do not re-register a placeholder.

            if (maps.Validate(source, boundKey) == LocalizationBindingStatus.Valid) continue;
            LocalizationKeyResolution existing = maps.ResolveSource(normalized);
            // A source with multiple existing keys may intentionally have different meanings.
            // Do not create another key or bind it to a random translation.
            if (existing.Status == LocalizationBindingStatus.AmbiguousKey) continue;
            if (existing.IsUnique || chosenBySource.ContainsKey(normalized)) continue;
            string generated = LocalizationProjectScanner.BuildSuggestedKey(assetPath, GetHierarchyPath(text.transform), source);
            // Avoid reusing a key already registered for a different source.
            if (maps.TryGetKorean(generated, out string registered) &&
                !string.Equals(registered, normalized, StringComparison.Ordinal))
                generated = LocalizationProjectScanner.BuildSuggestedKey(assetPath, "text", source);
            if (maps.TryGetKorean(generated, out _) || !claimedKeys.Add(generated)) continue;
            chosenBySource[normalized] = generated;
            additions.Add(new LocalizationWorkbookEntry(generated, source));
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
        HashSet<string> configuredScenePaths = LocalizationEditorSafetyPolicy.GetEnabledBuildScenePaths();
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
            if (!LocalizationProjectScanner.IsLocalizableKoreanText(source))
            {
                summary.RuntimeSkipped++;
                continue;
            }

            summary.Checked++;
            LocalizedTMPText existing = text.GetComponent<LocalizedTMPText>();
            LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
            string currentKey = existing != null ? existing.LocalizationKey : localizer != null
                ? localizer.StringReference.TableEntryReference.Key : string.Empty;
            if (LocalizationEditorSafetyPolicy.IsRetiredShopGoodsNameKey(currentKey))
            {
                summary.RuntimeSkipped++;
                continue; // Never bind a stale shop template text to a new static key.
            }

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
            else
                replacementKey = resolver.ResolveSource(normalizedSource).Key;
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
        public int RuntimeSkipped;

        public static RepairSummary operator +(RepairSummary left, RepairSummary right) => new RepairSummary
        {
            Checked = left.Checked + right.Checked,
            Matched = left.Matched + right.Matched,
            Repaired = left.Repaired + right.Repaired,
            Unresolved = left.Unresolved + right.Unresolved,
            RuntimeSkipped = left.RuntimeSkipped + right.RuntimeSkipped,
        };
    }
}
