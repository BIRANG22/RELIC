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
            BindingMaps maps = ReadBindingMaps();
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

    private static RepairSummary RepairPrefabs(BindingMaps maps)
    {
        RepairSummary summary = default;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RepairSummary assetSummary = RepairHierarchy(root, maps);
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

    private static RepairSummary RepairScenes(BindingMaps maps)
    {
        RepairSummary summary = default;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RepairSummary sceneSummary = default;
            foreach (GameObject root in scene.GetRootGameObjects())
                sceneSummary += RepairHierarchy(root, maps);

            if (sceneSummary.Repaired > 0)
                EditorSceneManager.SaveScene(scene);
            summary += sceneSummary;
        }

        return summary;
    }

    private static RepairSummary RepairHierarchy(GameObject root, BindingMaps maps)
    {
        RepairSummary summary = default;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!LocalizedTMPText.ShouldManageText(text))
                continue;

            string source = ReadKoreanSource(text);
            if (string.IsNullOrWhiteSpace(source))
                continue;

            string normalizedSource = source.Trim();

            summary.Checked++;
            LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
            string currentKey = localizer != null
                ? localizer.StringReference.TableEntryReference.Key
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(currentKey) &&
                maps.KoreanByKey.TryGetValue(currentKey, out string currentKorean) &&
                string.Equals(currentKorean, normalizedSource, StringComparison.Ordinal))
            {
                summary.Matched++;
                if (StaticLocalizationMigration.RepairTextBinding(text, currentKey))
                    summary.Repaired++;
                continue;
            }

            if (maps.KeyByKorean.TryGetValue(normalizedSource, out string repairedKey))
            {
                if (StaticLocalizationMigration.RepairTextBinding(text, repairedKey))
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

    private static string ReadKoreanSource(TMP_Text text)
    {
        LocalizedTMPText runtimeLocalizer = text.GetComponent<LocalizedTMPText>();
        if (runtimeLocalizer == null)
            return text.text;

        SerializedObject serialized = new SerializedObject(runtimeLocalizer);
        string source = serialized.FindProperty("koreanSource")?.stringValue;
        return string.IsNullOrWhiteSpace(source) ? text.text : source;
    }

    private static BindingMaps ReadBindingMaps()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
            LocalizationExcelImporter.WorkbookPath,
            LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);

        int keyIndex = FindHeader(rows[0], "Key");
        int koreanIndex = FindHeader(rows[0], KoreanHeader);
        var keyByKorean = new Dictionary<string, string>(StringComparer.Ordinal);
        var koreanByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (IReadOnlyList<string> row in rows.Skip(1))
        {
            string key = GetValue(row, keyIndex);
            string korean = GetValue(row, koreanIndex);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(korean))
                continue;

            koreanByKey[key] = korean;
            if (!keyByKorean.ContainsKey(korean))
                keyByKorean[korean] = key;
        }

        return new BindingMaps(keyByKorean, koreanByKey);
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

    private readonly struct BindingMaps
    {
        public BindingMaps(Dictionary<string, string> keyByKorean, Dictionary<string, string> koreanByKey)
        {
            KeyByKorean = keyByKorean;
            KoreanByKey = koreanByKey;
        }

        public Dictionary<string, string> KeyByKorean { get; }
        public Dictionary<string, string> KoreanByKey { get; }
    }

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
