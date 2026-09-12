using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

public sealed class LocalizationManagerWindow : EditorWindow
{
    private readonly List<LocalizationCandidate> candidates = new();
    private Vector2 scroll;
    private bool scanScenes = true;
    private bool scanPrefabs = true;
    private bool scanScripts = true;
    private bool scanGameData = true;
    private string filter = string.Empty;

    [MenuItem("Tools/Localization/Localization Manager")]
    public static void Open() => GetWindow<LocalizationManagerWindow>("Localization Manager");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Localization Manager", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        scanScenes = EditorGUILayout.ToggleLeft("Scenes", scanScenes, GUILayout.Width(90));
        scanPrefabs = EditorGUILayout.ToggleLeft("Prefabs", scanPrefabs, GUILayout.Width(90));
        scanScripts = EditorGUILayout.ToggleLeft("Scripts", scanScripts, GUILayout.Width(90));
        scanGameData = EditorGUILayout.ToggleLeft("Game Data", scanGameData, GUILayout.Width(100));
        if (GUILayout.Button("전체 검사")) Scan();
        if (GUILayout.Button("전체 검사 및 적용")) { Scan(); ApplySafe(); }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("전체 안전 항목 적용")) ApplySafe();
        filter = EditorGUILayout.TextField("Search", filter);
        LocalizationScanSummary summary = LocalizationScanSummary.Create(candidates);
        EditorGUILayout.LabelField($"New: {summary.NewCount}  Existing: {summary.ExistingCount}  Missing Component: {summary.MissingComponentCount}  Review: {summary.ReviewCount}  Occurrences: {summary.OccurrenceCount}");
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (LocalizationCandidate candidate in candidates.Where(c => string.IsNullOrEmpty(filter) || c.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || c.Korean.Contains(filter)))
            EditorGUILayout.LabelField($"[{(candidate.RequiresReview ? "Review" : candidate.IsNew ? "New" : "Reuse")}] {candidate.Key} = {candidate.Korean}\n{candidate.AssetPath}", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        candidates.Clear();
        Dictionary<string, string> known = ReadKoreanToUniqueKey();
        Dictionary<string, string> knownKoreanByKey = ReadKoreanByKey();
        if (scanPrefabs) foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" })) ScanPrefab(AssetDatabase.GUIDToAssetPath(guid), known);
        if (scanScenes) foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Project" })) ScanScene(AssetDatabase.GUIDToAssetPath(guid), known);
        if (scanScripts) ScanScripts(known);
        if (scanGameData) ScanGameData(knownKoreanByKey);
        Repaint();
    }

    private void ScanGameData(IReadOnlyDictionary<string, string> knownKoreanByKey)
    {
        const string gameDataWorkbook = "Assets/ExcelSource/GameData.xlsx";
        string[] sheets =
        {
            "Character", "Monster", "SkillMaster", "MonsterSkill", "GridEffect", "Effect",
            "Map", "BattleMap", "Event", "SkillRange", "Rune", "Relic", "Compound", "Item", "Erosion",
        };

        var scannedGameDataKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sheet in sheets)
        {
            IReadOnlyList<IReadOnlyList<string>> rows;
            try { rows = LocalizationXlsxReader.ReadSheet(gameDataWorkbook, sheet); }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Localization Manager] GameData sheet scan skipped: {sheet} ({exception.Message})");
                continue;
            }

            if (rows.Count < 2)
                continue;

            IReadOnlyList<string> headers = rows[0];
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                string stableId = row.Count > 0 ? row[0] : string.Empty;
                if (string.IsNullOrWhiteSpace(stableId))
                    continue;

                for (int column = 1; column < headers.Count; column++)
                {
                    string header = headers[column];
                    string korean = column < row.Count ? row[column] : string.Empty;
                    if (!LocalizationProjectScanner.IsPlayerFacingGameDataColumn(header) ||
                        !LocalizationProjectScanner.IsLocalizableKoreanText(korean))
                        continue;

                    string stableKey = LocalizationProjectScanner.BuildGameDataKey(sheet, stableId, header);
                    // 같은 검사에서 동일 ID/필드가 실제로 반복되는 구조(Event 선택지 등)에만 hash를 붙입니다.
                    string key = scannedGameDataKeys.Add(stableKey)
                        ? stableKey
                        : LocalizationProjectScanner.BuildUniqueGameDataKey(sheet, stableId, header, korean);
                    bool exists = knownKoreanByKey.TryGetValue(key, out string currentKorean);
                    bool sourceChanged = exists && !string.Equals(
                        LocalizationBindingResolver.Normalize(currentKorean),
                        LocalizationBindingResolver.Normalize(korean),
                        StringComparison.Ordinal);
                    candidates.Add(new LocalizationCandidate(
                        $"{gameDataWorkbook}/{sheet}!{rowIndex + 1}:{header}",
                        korean,
                        key,
                        false,
                        !exists,
                        false,
                        sourceChanged));
                }
            }
        }
    }

    private void ScanScripts(Dictionary<string, string> known)
    {
        var matches = new System.Text.RegularExpressions.Regex("\\\"([^\\\"]*[가-힣][^\\\"]*)\\\"");
        foreach (string path in AssetDatabase.FindAssets("t:Script", new[] { "Assets/Project" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/Debug/", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            string source = LocalizationProjectScanner.RemoveComments(LocalizationProjectScanner.ReadScriptText(path));
            bool uiAssignment = source.Contains(".text =", StringComparison.Ordinal);
            foreach (System.Text.RegularExpressions.Match match in matches.Matches(source))
            {
                string korean = match.Groups[1].Value;
                if (!LocalizationProjectScanner.IsLocalizableKoreanText(korean)) continue;
                known.TryGetValue(korean, out string key);
                key ??= LocalizationProjectScanner.BuildSuggestedKey(path, "text", korean);
                candidates.Add(new LocalizationCandidate(path, korean, key, false, !known.ContainsKey(korean), !uiAssignment));
            }
        }
    }

    private void ScanPrefab(string path, Dictionary<string, string> known)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { ScanTexts(root.GetComponentsInChildren<TMP_Text>(true), path, known); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private void ScanScene(string path, Dictionary<string, string> known)
    {
        // 씬을 열고 닫는 과정은 DontSaveInEditor 임시 오브젝트 assertion을 유발할 수 있습니다.
        // 검사만 필요한 단계에서는 Unity YAML의 TMP m_text 직렬화 값을 읽습니다.
        string source = System.IO.File.ReadAllText(path);
        var matches = System.Text.RegularExpressions.Regex.Matches(
            source,
            @"(?m)^\s*m_text:\s*(?<value>.*)$");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string korean = match.Groups["value"].Value.Trim().Trim('"');
            if (!LocalizationProjectScanner.IsLocalizableKoreanText(korean))
                continue;

            known.TryGetValue(korean, out string key);
            key ??= LocalizationProjectScanner.BuildSuggestedKey(path, "text", korean);
            candidates.Add(new LocalizationCandidate(path, korean, key, true, !known.ContainsKey(korean)));
        }
    }

    private void ScanTexts(IEnumerable<TMP_Text> texts, string path, Dictionary<string, string> known)
    {
        foreach (TMP_Text text in texts)
        {
            if (!LocalizationProjectScanner.IsLocalizableKoreanText(text.text) ||
                !LocalizedTMPText.ShouldManageText(text) ||
                text.GetComponent<LocalizedTMPText>() != null)
                continue;
            LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
            string key = localizer != null ? localizer.StringReference.TableEntryReference.Key : null;
            if (string.IsNullOrWhiteSpace(key)) known.TryGetValue(text.text, out key);
            if (string.IsNullOrWhiteSpace(key)) key = LocalizationProjectScanner.BuildSuggestedKey(path, text.gameObject.name, text.text);
            candidates.Add(new LocalizationCandidate(path, text.text, key, localizer == null, !known.ContainsKey(text.text)));
        }
    }

    private void ApplySafe()
    {
        var additions = candidates.Where(candidate => candidate.IsNew && !candidate.RequiresReview).Select(candidate => new LocalizationWorkbookEntry(candidate.Key, candidate.Korean));
        var sourceUpdates = candidates.Where(candidate => candidate.NeedsSourceUpdate && !candidate.RequiresReview).Select(candidate => new LocalizationWorkbookEntry(candidate.Key, candidate.Korean));
        int updated = LocalizationWorkbookWriter.UpdateExistingEntries(LocalizationExcelImporter.WorkbookPath, sourceUpdates);
        int rows = LocalizationWorkbookWriter.MergeNewEntries(LocalizationExcelImporter.WorkbookPath, additions);
        int removed = RemoveUnusedEntries();
        if (rows > 0 || updated > 0 || removed > 0) LocalizationExcelImporter.Import();
        LocalizationTextBindingRepairTool.RepairAllBindings();
        Debug.Log($"[Localization Manager] Applied {rows} new Excel rows, updated {updated} GameData sources, removed {removed} unused keys, and repaired TMP bindings.");
    }

    private static int RemoveUnusedEntries()
    {
        HashSet<string> workbookKeys = ReadKoreanByKey().Keys.ToHashSet(StringComparer.Ordinal);
        HashSet<string> usedKeys = CollectExplicitKeyReferences(workbookKeys);
        usedKeys.UnionWith(CollectCurrentGameDataKeys());
        return LocalizationWorkbookWriter.RemoveEntries(
            LocalizationExcelImporter.WorkbookPath,
            workbookKeys.Where(key => !usedKeys.Contains(key)));
    }

    private static HashSet<string> CollectExplicitKeyReferences(HashSet<string> workbookKeys)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in System.IO.Directory.GetFiles("Assets", "*.*", System.IO.SearchOption.AllDirectories))
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("Assets/Language/", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("Assets/ExcelSource/", StringComparison.OrdinalIgnoreCase))
                continue;
            string extension = System.IO.Path.GetExtension(path);
            if (!string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase))
                continue;

            string source;
            try { source = System.IO.File.ReadAllText(path); }
            catch { continue; }
            foreach (Match match in Regex.Matches(source, @"(?<![A-Za-z0-9_.])[a-z][a-z0-9_.]*(?![A-Za-z0-9_.])"))
            {
                if (workbookKeys.Contains(match.Value))
                    used.Add(match.Value);
            }
        }

        return used;
    }

    private static HashSet<string> CollectCurrentGameDataKeys()
    {
        const string gameDataWorkbook = "Assets/ExcelSource/GameData.xlsx";
        string[] sheets =
        {
            "Character", "Monster", "SkillMaster", "MonsterSkill", "GridEffect", "Effect",
            "Map", "BattleMap", "Event", "SkillRange", "Rune", "Relic", "Compound", "Item", "Erosion",
        };
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var scannedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sheet in sheets)
        {
            IReadOnlyList<IReadOnlyList<string>> rows;
            try { rows = LocalizationXlsxReader.ReadSheet(gameDataWorkbook, sheet); }
            catch { continue; }
            if (rows.Count < 2)
                continue;

            IReadOnlyList<string> headers = rows[0];
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                string stableId = row.Count > 0 ? row[0] : string.Empty;
                if (string.IsNullOrWhiteSpace(stableId))
                    continue;
                for (int column = 1; column < headers.Count; column++)
                {
                    string korean = column < row.Count ? row[column] : string.Empty;
                    if (LocalizationProjectScanner.IsPlayerFacingGameDataColumn(headers[column]) &&
                        LocalizationProjectScanner.IsLocalizableKoreanText(korean))
                    {
                        string stableKey = LocalizationProjectScanner.BuildGameDataKey(sheet, stableId, headers[column]);
                        keys.Add(scannedKeys.Add(stableKey)
                            ? stableKey
                            : LocalizationProjectScanner.BuildUniqueGameDataKey(sheet, stableId, headers[column], korean));
                    }
                }
            }
        }

        return keys;
    }

    /// <summary>Only unique source text may be reused automatically. Duplicate copy requires contextual review.</summary>
    private static Dictionary<string, string> ReadKoreanToUniqueKey()
    {
        var rows = LocalizationXlsxReader.ReadSheet(LocalizationExcelImporter.WorkbookPath, LocalizationExcelImporter.WorksheetName);
        int key = rows[0].ToList().FindIndex(value => value == "Key");
        int korean = rows[0].ToList().FindIndex(value => value == "Korean(ko)");
        return rows.Skip(1)
            .Where(row => key >= 0 && korean >= 0 && key < row.Count && korean < row.Count &&
                          !string.IsNullOrWhiteSpace(row[key]) && !string.IsNullOrWhiteSpace(row[korean]))
            .GroupBy(row => LocalizationBindingResolver.Normalize(row[korean]), StringComparer.Ordinal)
            .Where(group => group.Select(row => row[key]).Distinct(StringComparer.Ordinal).Count() == 1)
            .ToDictionary(group => group.Key, group => group.First()[key], StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ReadKoreanByKey()
    {
        var rows = LocalizationXlsxReader.ReadSheet(LocalizationExcelImporter.WorkbookPath, LocalizationExcelImporter.WorksheetName);
        int key = rows[0].ToList().FindIndex(value => value == "Key");
        int korean = rows[0].ToList().FindIndex(value => value == "Korean(ko)");
        return rows.Skip(1)
            .Where(row => key >= 0 && korean >= 0 && key < row.Count && korean < row.Count && !string.IsNullOrWhiteSpace(row[key]))
            .GroupBy(row => row[key], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last()[korean] ?? string.Empty, StringComparer.Ordinal);
    }
}

public sealed class LocalizationCandidate
{
    public string AssetPath { get; }
    public string Korean { get; }
    public string Key { get; }
    public bool NeedsComponent { get; }
    public bool IsNew { get; }
    public bool RequiresReview { get; }
    public bool NeedsSourceUpdate { get; }
    public LocalizationCandidate(string assetPath, string korean, string key, bool needsComponent, bool isNew, bool requiresReview = false, bool needsSourceUpdate = false) { AssetPath = assetPath; Korean = korean; Key = key; NeedsComponent = needsComponent; IsNew = isNew; RequiresReview = requiresReview; NeedsSourceUpdate = needsSourceUpdate; }
}

public readonly struct LocalizationScanSummary
{
    public int NewCount { get; }
    public int ExistingCount { get; }
    public int MissingComponentCount { get; }
    public int ReviewCount { get; }
    public int OccurrenceCount { get; }

    private LocalizationScanSummary(int newCount, int existingCount, int missingComponentCount, int reviewCount, int occurrenceCount)
    {
        NewCount = newCount;
        ExistingCount = existingCount;
        MissingComponentCount = missingComponentCount;
        ReviewCount = reviewCount;
        OccurrenceCount = occurrenceCount;
    }

    public static LocalizationScanSummary Create(IEnumerable<LocalizationCandidate> candidates)
    {
        LocalizationCandidate[] values = candidates?.ToArray() ?? Array.Empty<LocalizationCandidate>();
        int newCount = values
            .Where(candidate => candidate.IsNew && !candidate.RequiresReview)
            .Select(candidate => candidate.Key + "\u001f" + candidate.Korean)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int existingCount = values
            .Where(candidate => !candidate.IsNew)
            .Select(candidate => candidate.Key)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int reviewCount = values
            .Where(candidate => candidate.RequiresReview)
            .Select(candidate => candidate.AssetPath + "\u001f" + candidate.Korean)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int missingComponentCount = values
            .Where(candidate => candidate.NeedsComponent && !candidate.RequiresReview)
            .Select(candidate => candidate.AssetPath + "\u001f" + candidate.Key + "\u001f" + candidate.Korean)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return new LocalizationScanSummary(newCount, existingCount, missingComponentCount, reviewCount, values.Length);
    }
}
