using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

public sealed class LocalizationManagerWindow : EditorWindow
{
    private const string KoreanGuiFontPath = "Assets/TextMesh Pro/Fonts/DungGeunMo.otf";
    private readonly List<LocalizationCandidate> candidates = new();
    private Vector2 windowScroll;
    private bool showScan = true;
    private bool showExcel = true;
    private bool showKeys = true;
    private bool showTools;
    private bool scanCompleted;
    private bool scanApplied;
    private string lastScanError;
    private bool scanScenes = true;
    private bool scanPrefabs = true;
    private bool scanScripts = true;
    private bool scanGameData = true;
    private bool actionQueued;
    private bool cleanupAuditReady;

    [MenuItem("Tools/Localization/로컬라이징 관리자 (전체 스캔 및 번역 적용)")]
    public static void Open() => GetWindow<LocalizationManagerWindow>("로컬라이징 관리자");

    private void OnEnable()
    {
        // 스캔 결과는 간단한 상태 표시만 사용합니다.
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("로컬라이징 통합 관리자", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("권장 순서: 전체 스캔 → 신규 키 적용 → 전용 데이터 동기화 → 엑셀 번역 → 엑셀 가져오기 → 게임 언어 전환 테스트. 기존 키 삭제는 마지막에 진행하세요.", MessageType.Info);

        using (new EditorGUI.DisabledScope(actionQueued))
        {
            showScan = EditorGUILayout.Foldout(showScan, "1. 번역 대상 수집 및 등록", true, EditorStyles.foldoutHeader);
            if (showScan)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("스캔 대상", EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        scanScenes = EditorGUILayout.ToggleLeft("씬", scanScenes, GUILayout.Width(70));
                        scanPrefabs = EditorGUILayout.ToggleLeft("프리팹", scanPrefabs, GUILayout.Width(85));
                        scanScripts = EditorGUILayout.ToggleLeft("스크립트", scanScripts, GUILayout.Width(95));
                        scanGameData = EditorGUILayout.ToggleLeft("게임 데이터", scanGameData, GUILayout.Width(115));
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("전체 스캔 (번역 대상 검색)", GUILayout.Height(28)))
                            RunEditorAction("전체 스캔", Scan, scanScenes);
                        if (GUILayout.Button("전체 스캔 및 적용 (키 등록·UI 바인딩)", GUILayout.Height(28)))
                            RunEditorAction("전체 스캔 및 적용", () => { Scan(); ApplySafe(); }, true);
                    }
                    if (GUILayout.Button("안전한 항목 적용 (검토가 필요 없는 항목만)", GUILayout.Height(24)))
                        RunEditorAction("안전 항목 적용", ApplySafe, true);
                    if (GUILayout.Button("전용 데이터 일괄 동기화 (도감·튜토리얼·스킬·캐릭터·유물·룬)", GUILayout.Height(24)))
                        RunEditorAction("전용 데이터 일괄 동기화", SynchronizeSpecializedData, true);
                    EditorGUILayout.HelpBox("전체 스캔과 전용 데이터 동기화는 서로 다른 작업입니다. 데이터 원본을 다시 가져와야 할 때는 두 작업을 순서대로 실행하세요.", MessageType.None);
                }
            }

            EditorGUILayout.Space(5);
            showExcel = EditorGUILayout.Foldout(showExcel, "2. 번역 엑셀 및 String Table", true, EditorStyles.foldoutHeader);
            if (showExcel)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button("번역 엑셀 가져오기 (작성한 번역을 String Table에 반영)", GUILayout.Height(28)))
                        RunEditorAction("번역 엑셀 가져오기", LocalizationExcelImporter.ImportFromMenu, true);
                    EditorGUILayout.HelpBox("엑셀 번역을 작성한 뒤 가져오기를 실행하세요. Import는 자동 번역 기능이 아닙니다.", MessageType.None);
                }
            }

            EditorGUILayout.Space(5);
            showKeys = EditorGUILayout.Foldout(showKeys, "3. 기존 키 정리 및 이름 변경", true, EditorStyles.foldoutHeader);
            if (showKeys)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button("기존 UI 키 검사 (미사용 후보 CSV 출력)", GUILayout.Height(25)))
                        RunEditorAction("기존 UI 키 검사", AuditLegacyUiKeys, false);
                    using (new EditorGUI.DisabledScope(!cleanupAuditReady || !scanScenes || !scanPrefabs || !scanScripts || !scanGameData))
                    {
                        if (GUILayout.Button("미사용 UI 키 제거 (검사된 후보 검토 후 삭제)", GUILayout.Height(25)))
                            RunEditorAction("미사용 UI 키 제거", RemoveAuditedUnusedUiKeys, true);
                    }
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("긴 키 변경 제안 내보내기 (TSV 생성)", GUILayout.Height(25)))
                        RunEditorAction("키 변경 제안 내보내기", LocalizationLegacyKeyMigration.ExportProposals, false);
                    if (GUILayout.Button("승인된 키 이름 변경 (ID·참조 유지하며 이전)", GUILayout.Height(25)))
                        RunEditorAction("승인된 키 변경 적용", LocalizationLegacyKeyMigration.ApplyApproved, true);
                    EditorGUILayout.HelpBox("미사용 키 삭제 및 키 이름 변경은 백업 후 실행하세요. TSV의 승인 표시 없이 임의 이전하지 마세요.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(5);
            showTools = EditorGUILayout.Foldout(showTools, "4. 진단·복구 및 고급 기능", true, EditorStyles.foldoutHeader);
            if (showTools)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button("텍스트 바인딩 검사·복구 (잘못 연결된 TMP 점검)"))
                        RunEditorAction("텍스트 바인딩 검사 및 복구", LocalizationTextBindingRepairTool.AuditAndRepairFromMenu, true);
                    if (GUILayout.Button("플레이어 UI 번역 적용 (정적 UI 바인딩 반영)"))
                        RunEditorAction("플레이어 UI 번역 적용", StaticLocalizationMigration.ApplyFromMenu, true);
                    if (GUILayout.Button("캐릭터 정보 영역 진단 (InfoArea 문제 확인)"))
                        RunEditorAction("캐릭터 정보 영역 진단", CharacterInfoLocalizationDiagnostics.Diagnose, false);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("편집 잠금 활성화"))
                            RunEditorAction("편집 잠금 활성화", LocalizationEditingLockTool.EnableFromMenu, true);
                        if (GUILayout.Button("편집 잠금 해제"))
                            RunEditorAction("편집 잠금 해제", LocalizationEditingLockTool.DisableFromMenu, true);
                        if (GUILayout.Button("잠금 상태 확인"))
                            RunEditorAction("잠금 상태 확인", LocalizationEditingLockTool.ReportStateFromMenu, false);
                    }
                }
            }
        }

        if (actionQueued)
            EditorGUILayout.HelpBox("작업이 예약되었거나 실행 중입니다. Unity Console에서 결과를 확인하세요.", MessageType.Info);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("5. 스캔 상태", EditorStyles.boldLabel);
            if (actionQueued)
                EditorGUILayout.HelpBox("작업 진행 중", MessageType.Info);
            else if (!string.IsNullOrEmpty(lastScanError))
                EditorGUILayout.HelpBox("작업 실패: " + lastScanError + " (자세한 내용은 Console 확인)", MessageType.Error);
            else if (!scanCompleted)
                EditorGUILayout.HelpBox("아직 스캔하지 않았습니다.", MessageType.None);
            else
            {
                LocalizationScanSummary summary = LocalizationScanSummary.Create(candidates);
                EditorGUILayout.HelpBox(
                    (scanApplied ? "스캔 및 적용 완료" : "스캔 완료 (적용 전)") +
                    $" | 신규 {summary.NewCount}건 | 기존 {summary.ExistingCount}건 | 검토 {summary.ReviewCount}건 | 바인딩 누락 {summary.MissingComponentCount}건",
                    summary.ReviewCount > 0 || summary.MissingComponentCount > 0 ? MessageType.Warning : MessageType.Info);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    // 전용 번역 키 생성기를 한곳에서 실행합니다. 전체 스캔은 이 전용 동기화와 별개의 단계입니다.
    private static void SynchronizeSpecializedData()
    {
        int succeeded = 0;
        int failed = 0;
        RunSpecializedSync("도감", LocalizationSyncCommand.SyncRecordLocalizationFromMenu, ref succeeded, ref failed);
        RunSpecializedSync("튜토리얼", LocalizationSyncCommand.SyncTutorialLocalizationFromMenu, ref succeeded, ref failed);
        RunSpecializedSync("스킬 설정", LocalizationSyncCommand.SyncSkillSettingLocalizationFromMenu, ref succeeded, ref failed);
        RunSpecializedSync("캐릭터 설정", LocalizationSyncCommand.SyncCharacterSettingLocalizationFromMenu, ref succeeded, ref failed);
        RunSpecializedSync("유물 상점", LocalizationSyncCommand.ValidateRelicShopRarityLocalizationFromMenu, ref succeeded, ref failed);
        RunSpecializedSync("룬 장착", LocalizationSyncCommand.SyncRuneInstallationLocalizationFromMenu, ref succeeded, ref failed);
        Debug.Log($"[Localization Manager] 전용 데이터 일괄 동기화 종료: 실행 완료 {succeeded}개, 예외 발생 {failed}개. 각 단계의 상세 결과를 Console에서 확인하세요.");
    }

    private static void RunSpecializedSync(string name, Action sync, ref int succeeded, ref int failed)
    {
        try
        {
            sync();
            succeeded++;
        }
        catch (Exception ex)
        {
            failed++;
            Debug.LogError($"[Localization Manager] '{name}' 동기화 중 예외 발생. 나머지 동기화는 계속 진행합니다.\n{ex}");
        }
    }

    private void Scan()
    {
        cleanupAuditReady = false;
        scanCompleted = false;
        scanApplied = false;
        lastScanError = null;
        candidates.Clear();
        Dictionary<string, string> known = ReadKoreanToUniqueKey();
        Dictionary<string, string> knownKoreanByKey = ReadKoreanByKey();
        HashSet<string> knownKeys = knownKoreanByKey.Keys.ToHashSet(StringComparer.Ordinal);
        var bindingResolver = new LocalizationBindingResolver(
            knownKoreanByKey.Select(pair => new LocalizationBindingEntry(pair.Key, pair.Value)));
        if (scanPrefabs)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (LocalizationEditorSafetyPolicy.ShouldScanProjectAsset(path))
                    ScanPrefab(path, known, knownKoreanByKey, knownKeys, bindingResolver);
            }
        }
        if (scanScenes)
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                HashSet<string> configuredScenePaths = GetConfiguredScenePaths();
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Project" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (LocalizationEditorSafetyPolicy.ShouldScanSceneAsset(path, configuredScenePaths))
                        ScanScene(path, known, knownKoreanByKey, knownKeys, bindingResolver);
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }
        if (scanScripts) ScanScripts(known, knownKoreanByKey, knownKeys);
        if (scanGameData) ScanGameData(knownKoreanByKey);
        AddRuntimeUiCandidates(knownKoreanByKey, knownKeys);
        scanCompleted = true;
        Repaint();
    }

    private void ScanGameData(IReadOnlyDictionary<string, string> knownKoreanByKey)
    {
        const string gameDataWorkbook = "Assets/ExcelSource/GameData.xlsx";
        var scannedGameDataKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sheet in LocalizationGameDataSourceSynchronizer.ManagedSheets)
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
            int choiceOrderColumn = FindHeaderIndex(headers, "ChoiceOrder", "선택지 순서");
            // Row 2 contains runtime/schema field names, not game data.
            for (int rowIndex = 2; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                string stableId = row.Count > 0 ? row[0] : string.Empty;
                if (string.IsNullOrWhiteSpace(stableId))
                    continue;

                for (int column = 1; column < headers.Count; column++)
                {
                    string header = headers[column];
                    string korean = column < row.Count ? row[column] : string.Empty;
                    bool isMonsterSkillType = string.Equals(sheet, "MonsterSkill", StringComparison.Ordinal) &&
                                              string.Equals(header.Trim(), "타입", StringComparison.Ordinal);
                    if ((!LocalizationProjectScanner.ShouldCollectGameDataLocalizationValue(sheet, header, korean) && !isMonsterSkillType) ||
                        (isMonsterSkillType && !LocalizationProjectScanner.IsLocalizableKoreanText(korean)))
                        continue;

                    string sharedKey = LocalizationProjectScanner.GetSharedGameDataKey(sheet, stableId, header, korean);
                    string stableKey = string.IsNullOrEmpty(sharedKey)
                        ? BuildGameDataKey(sheet, stableId, row, choiceOrderColumn, header)
                        : sharedKey;
                    // 공통 용어/동일 효과 설명은 한 번만 수집합니다.
                    bool firstOccurrence = scannedGameDataKeys.Add(stableKey);
                    if (!firstOccurrence && !string.IsNullOrEmpty(sharedKey))
                        continue;
                    string key = firstOccurrence ? stableKey :
                        LocalizationProjectScanner.BuildUniqueGameDataKey(sheet, stableId, header, korean);
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

    private void AddRuntimeUiCandidates(
        IReadOnlyDictionary<string, string> knownKoreanByKey,
        ISet<string> knownKeys)
    {
        AddRuntimeUiCandidate("ui.battle.equip_panel.character_text", "장착할 캐릭터를 선택하세요.", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("ui.check.slogan", "정말로 포기하시겠습니까?", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.dustium", "더스티움", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill.move_distance", "이동 거리", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_type.all", "전체", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_type.self", "개인", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_type.ranged", "원거리", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_type.melee", "근거리", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.unique_memory", "발현기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.ability_memory", "구현기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.passive_memory", "본능기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("common.move", "이동", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.common_memory", "일반기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.rare_memory", "레어기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.epic_memory", "에픽기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.unique", "유니크기억", knownKoreanByKey, knownKeys);
        AddRuntimeUiCandidate("battle.skill_rarity.exclusive", "전용기억", knownKoreanByKey, knownKeys);
        foreach (LocalizationWorkbookEntry entry in LocalizationRuntimeSourceCatalog.AllEntries)
            AddRuntimeUiCandidate(entry.Key, entry.Korean, knownKoreanByKey, knownKeys);
    }

    private void AddRuntimeUiCandidate(
        string key,
        string korean,
        IReadOnlyDictionary<string, string> knownKoreanByKey,
        ISet<string> knownKeys)
    {
        bool exists = knownKoreanByKey.TryGetValue(key, out string currentKorean);
        bool sourceChanged = exists && !string.Equals(
            LocalizationBindingResolver.Normalize(currentKorean),
            LocalizationBindingResolver.Normalize(korean),
            StringComparison.Ordinal);
        candidates.Add(new LocalizationCandidate(
            "Runtime UI labels",
            korean,
            key,
            false,
            !exists && !knownKeys.Contains(key),
            false,
            sourceChanged));
    }

    private void ScanScripts(
        Dictionary<string, string> known,
        IReadOnlyDictionary<string, string> knownKoreanByKey,
        HashSet<string> knownKeys)
    {
        var matches = new System.Text.RegularExpressions.Regex("\\\"([^\\\"]*[가-힣][^\\\"]*)\\\"");
        foreach (string path in AssetDatabase.FindAssets("t:Script", new[] { "Assets/Project" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/Debug/", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            string source = LocalizationProjectScanner.RemoveComments(LocalizationProjectScanner.ReadScriptText(path));
            HashSet<string> dynamicDisplayLiterals = LocalizationProjectScanner
                .FindDynamicDisplayLiterals(source)
                .ToHashSet(StringComparer.Ordinal);
            // Warning/Notification UI text is surfaced even if it is not a serialized TMP.
            // Keep unbound script literals as Review: writing an Excel row alone does not
            // replace the hardcoded string at runtime.
            HashSet<string> warningUiLiterals = LocalizationProjectScanner
                .FindWarningUiLiterals(source)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> serializedTextLiterals = LocalizationProjectScanner
                .FindSerializedPlayerTextLiterals(source)
                .ToHashSet(StringComparer.Ordinal);
            foreach (LocalizationSourceEntry entry in LocalizationProjectScanner.FindExplicitLocalizationSources(source))
            {
                knownKoreanByKey.TryGetValue(entry.Key, out string currentKorean);
                bool sourceChanged = knownKeys.Contains(entry.Key) &&
                    !string.IsNullOrEmpty(entry.Korean) &&
                    !string.Equals(currentKorean, entry.Korean, StringComparison.Ordinal);
                candidates.Add(new LocalizationCandidate(
                    path,
                    entry.Korean,
                    entry.Key,
                    false,
                    LocalizationCandidate.IsNewForKnownKeys(entry.Key, knownKeys),
                    false,
                    sourceChanged));
            }
            foreach (System.Text.RegularExpressions.Match match in matches.Matches(source))
            {
                string korean = match.Groups[1].Value;
                if (!LocalizationProjectScanner.IsLocalizableKoreanText(korean)) continue;
                known.TryGetValue(korean, out string key);
                key ??= LocalizationProjectScanner.BuildSuggestedKey(path, "text", korean);
                bool explicitUiOutput = dynamicDisplayLiterals.Contains(korean) ||
                    warningUiLiterals.Contains(korean) || serializedTextLiterals.Contains(korean);
                // Source literals must be wired to a localization API manually.
                // Prevent Full Scan and Apply from silently claiming a live translation.
                bool automatic = LocalizationProjectScanner.IsAutomaticScriptLiteralCandidate(korean);
                candidates.Add(new LocalizationCandidate(
                    explicitUiOutput ? path + " [UI output: bind localization key]" : path + " [source literal: review]",
                    korean,
                    key,
                    false,
                    automatic && LocalizationCandidate.IsNewForKnownKeys(key, knownKeys),
                    !automatic));
            }
        }
    }

    private void ScanPrefab(
        string path,
        Dictionary<string, string> known,
        Dictionary<string, string> knownKoreanByKey,
        HashSet<string> knownKeys,
        LocalizationBindingResolver bindingResolver)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { ScanTexts(root.GetComponentsInChildren<TMP_Text>(true), path, known, knownKoreanByKey, knownKeys, bindingResolver); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private void ScanScene(
        string path,
        Dictionary<string, string> known,
        Dictionary<string, string> knownKoreanByKey,
        HashSet<string> knownKeys,
        LocalizationBindingResolver bindingResolver)
    {
        // Preview Scene은 현재 씬과 Light2D 등록 범위를 공유해 Global Light 충돌을 만든다.
        // 검사 대상만 Single 모드로 열고 Scan()이 전체 Scene Setup을 복원한다.
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            ScanTexts(
                root.GetComponentsInChildren<TMP_Text>(true),
                path,
                known,
                knownKoreanByKey,
                knownKeys,
                bindingResolver);
        }
    }

    private void ScanTexts(
        IEnumerable<TMP_Text> texts,
        string path,
        Dictionary<string, string> known,
        Dictionary<string, string> knownKoreanByKey,
        HashSet<string> knownKeys,
        LocalizationBindingResolver bindingResolver)
    {
        foreach (TMP_Text text in texts)
        {
            if (!LocalizationProjectScanner.IsLocalizableKoreanText(text.text) ||
                !LocalizedTMPText.ShouldManageText(text))
                continue;
            // TMP text와 게임데이터가 편집자가 관리하는 원문입니다. koreanSource는 적용 결과이며 판단 기준이 아닙니다.
            LocalizedTMPText runtimeLocalizer = text.GetComponent<LocalizedTMPText>();
            LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
            string existingKey = runtimeLocalizer != null ? runtimeLocalizer.LocalizationKey : localizer != null
                ? localizer.StringReference.TableEntryReference.Key : null;
            string generatedKey = LocalizationProjectScanner.BuildSuggestedKey(
                path,
                GetHierarchyPath(text.transform),
                text.text);
            string key = !string.IsNullOrWhiteSpace(existingKey)
                ? LocalizationBindingSourcePolicy.SelectStaticBindingKey(
                    text.text,
                    existingKey,
                    generatedKey,
                    bindingResolver)
                : null;
            bool sourceChanged = !string.IsNullOrWhiteSpace(key) &&
                                 knownKoreanByKey.TryGetValue(key, out string tableKorean) &&
                                 !LocalizationBindingSourcePolicy.DoesCurrentSourceMatchTable(text.text, tableKorean);
            if (string.IsNullOrWhiteSpace(key)) known.TryGetValue(text.text, out key);
            if (string.IsNullOrWhiteSpace(key)) key = LocalizationProjectScanner.BuildSuggestedKey(path, text.gameObject.name, text.text);
            candidates.Add(new LocalizationCandidate(
                path,
                text.text,
                key,
                localizer == null,
                LocalizationCandidate.IsNewForKnownKeys(key, knownKeys),
                false,
                sourceChanged));
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var names = new Stack<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Push(current.name);
        return string.Join("/", names);
    }

    private void ApplySafe()
    {
        if (!LocalizationEditorSafetyPolicy.TryValidateWorkbookWritable(
                LocalizationExcelImporter.WorkbookPath,
                out string workbookError))
        {
            throw new InvalidOperationException(workbookError);
        }

        ExcelToBytesConverter.ConvertOrThrow();
        AssetDatabase.ImportAsset("Assets/Resources/Data/GameDataRuntime.csv", ImportAssetOptions.ForceUpdate);

        HashSet<string> retainedCandidateKeys = LocalizationEditorSafetyPolicy.CollectRetainedCandidateKeys(candidates);
        var additions = candidates.Where(candidate => candidate.IsNew && !candidate.RequiresReview).Select(candidate => new LocalizationWorkbookEntry(candidate.Key, candidate.Korean));
        var sourceUpdates = candidates.Where(candidate => candidate.NeedsSourceUpdate && !candidate.RequiresReview).Select(candidate => new LocalizationWorkbookEntry(candidate.Key, candidate.Korean));
        int templateChanges = StaticLocalizationMigration.EnsureRecordMemoryFormatTemplates();
        int updated = LocalizationWorkbookWriter.UpdateExistingEntries(LocalizationExcelImporter.WorkbookPath, sourceUpdates);
        int rows = LocalizationWorkbookWriter.MergeNewEntries(LocalizationExcelImporter.WorkbookPath, additions);
        if (rows > 0 || updated > 0 || templateChanges > 0) LocalizationExcelImporter.Import();
        int optionalMigrationFailures = 0;
        if (!StaticLocalizationMigration.TryRunOptionalMigration(
                "Required static UI bindings",
                StaticLocalizationMigration.ApplyRequiredStaticUiBindings,
                out int explicitStaticBindings))
            optionalMigrationFailures++;
        if (!StaticLocalizationMigration.TryRunOptionalMigration(
                "Record memory dynamic ownership",
                StaticLocalizationMigration.MigrateRecordMemoryDynamicOwnership,
                out int dynamicOwnershipChanges))
            optionalMigrationFailures++;
        if (!StaticLocalizationMigration.TryRunOptionalMigration(
                "TMP binding repair",
                () =>
                {
                    LocalizationTextBindingRepairTool.RepairAllBindings();
                    return 0;
                },
                out _))
            optionalMigrationFailures++;
        // Never delete localization entries as a side effect of Full Scan and Apply.
        // Old keys may still be referenced by ID or by dynamically composed keys.
        int removed = 0;
        int compacted = LocalizationWorkbookWriter.CompactBlankRows(LocalizationExcelImporter.WorkbookPath);
        LocalizationExcelImporter.Import(); // Final authoritative workbook-to-table sync.
        scanApplied = true;
        Debug.Log($"[Localization Manager] Synced GameData runtime CSV, applied {rows} new Excel rows, updated {updated} GameData sources, restored {templateChanges} Record templates, applied {explicitStaticBindings} required static bindings, changed {dynamicOwnershipChanges} dynamic TMP ownerships, skipped {optionalMigrationFailures} failed optional migrations, removed {removed} unused keys, compacted {compacted} blank rows, and completed TMP binding repair stage.");
    }

    private static HashSet<string> GetConfiguredScenePaths()
    {
        return EditorBuildSettings.scenes
            .Select(scene => (scene.path ?? string.Empty).Replace('\\', '/'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void RunEditorAction(string label, Action action, bool requiresSceneSave)
    {
        if (actionQueued)
            return;

        if (!LocalizationEditorSafetyPolicy.CanRunAssetMutation(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            Debug.LogError($"[Localization Manager] {label} 실패: Play Mode에서는 로컬리제이션 에셋을 변경할 수 없습니다.");
            EditorUtility.DisplayDialog(
                "Localization Manager",
                "Play Mode를 종료한 뒤 다시 실행하세요. 런타임 텍스트 감시 중 프리팹을 저장하면 Missing Script가 생길 수 있습니다.",
                "확인");
            return;
        }

        actionQueued = true;
        Repaint();
        EditorApplication.delayCall += () =>
        {
            try
            {
                if (requiresSceneSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                lastScanError = null;
                using (LocalizationEditorFontMutationGuard.ProtectAllProjectFonts())
                    action?.Invoke();
            }
            catch (Exception exception)
            {
                lastScanError = label + " 실행 중 오류";
                string message = exception is InvalidOperationException
                    ? exception.Message
                    : $"{label} 중 오류가 발생했습니다. Console의 첫 예외를 확인하세요.";
                Debug.LogError($"[Localization Manager] {label} 실패: {exception}");
                EditorUtility.DisplayDialog("Localization Manager", message, "확인");
            }
            finally
            {
                if (this != null)
                {
                    actionQueued = false;
                    Repaint();
                }
            }
        };
    }

    // Only legacy, automatically generated UI keys can be considered for deletion.
    // Data keys and explicit semantic UI keys require manual migration.
    private static readonly Regex LegacyUiKeyPattern = new Regex(
        @"^ui\.[a-z0-9_.]+\.[0-9a-f]{8}$", RegexOptions.Compiled);

    private string[] FindUnusedLegacyUiKeys()
    {
        var workbookKeys = ReadKoreanByKey().Keys.ToHashSet(StringComparer.Ordinal);
        var usedKeys = CollectExplicitKeyReferences(workbookKeys);
        usedKeys.UnionWith(CollectCurrentGameDataKeys());
        usedKeys.UnionWith(LocalizationRuntimeSourceCatalog.AllEntries.Select(entry => entry.Key));
        usedKeys.UnionWith(LocalizationEditorSafetyPolicy.CollectRetainedCandidateKeys(candidates));
        return workbookKeys.Where(key => LegacyUiKeyPattern.IsMatch(key) && !usedKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    private void AuditLegacyUiKeys()
    {
        // Audit performs a fresh complete scan before evaluating orphan candidates.
        if (!scanScenes || !scanPrefabs || !scanScripts || !scanGameData)
        {
            EditorUtility.DisplayDialog("Localization Audit", "Enable all scan categories first.", "OK");
            return;
        }
        Scan();
        string[] keys = FindUnusedLegacyUiKeys();
        const string reportPath = "Assets/ExcelSource/LocalizationUnusedUiKeyCandidates.csv";
        var rows = new List<string> { "Key,Status" };
        foreach (string key in keys)
            rows.Add("\"" + key.Replace("\"", "\"\"") + "\",Review before removal");
        System.IO.File.WriteAllLines(reportPath, rows, new System.Text.UTF8Encoding(true));
        AssetDatabase.Refresh();
        cleanupAuditReady = true;
        Debug.Log($"[Localization Manager] {keys.Length} legacy UI keys are not found in scanned references. Review '{reportPath}' before deletion. Dynamically composed keys may not be detected.");
        EditorUtility.DisplayDialog("Localization Audit", $"Found {keys.Length} candidate keys. Review {reportPath}. They have NOT been deleted.", "OK");
    }

    private void RemoveAuditedUnusedUiKeys()
    {
        if (!cleanupAuditReady || !scanScenes || !scanPrefabs || !scanScripts || !scanGameData)
            return;
        string[] keys = FindUnusedLegacyUiKeys();
        if (keys.Length == 0) return;
        if (!EditorUtility.DisplayDialog("Remove unused UI keys?",
                $"Delete {keys.Length} legacy UI key entries from the workbook? This may break dynamically generated references. A backup will be created.",
                "Delete", "Cancel"))
            return;
        string path = LocalizationExcelImporter.WorkbookPath;
        System.IO.File.Copy(path, path + ".pre-unused-ui-cleanup.backup", true);
        int removed = LocalizationWorkbookWriter.RemoveEntries(path, keys);
        LocalizationExcelImporter.Import();
        cleanupAuditReady = false;
        Debug.Log($"[Localization Manager] Removed {removed} audited legacy UI keys. Backup: {path}.pre-unused-ui-cleanup.backup");
    }

    private static int RemoveUnusedEntries(IEnumerable<string> retainedCandidateKeys)
    {
        HashSet<string> workbookKeys = ReadKoreanByKey().Keys.ToHashSet(StringComparer.Ordinal);
        HashSet<string> usedKeys = CollectExplicitKeyReferences(workbookKeys);
        usedKeys.UnionWith(CollectCurrentGameDataKeys());
        usedKeys.UnionWith(LocalizationRuntimeSourceCatalog.AllEntries.Select(entry => entry.Key));
        usedKeys.UnionWith(retainedCandidateKeys ?? Array.Empty<string>());
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
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var scannedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sheet in LocalizationGameDataSourceSynchronizer.ManagedSheets)
        {
            IReadOnlyList<IReadOnlyList<string>> rows;
            try { rows = LocalizationXlsxReader.ReadSheet(gameDataWorkbook, sheet); }
            catch { continue; }
            if (rows.Count < 2)
                continue;

            IReadOnlyList<string> headers = rows[0];
            int choiceOrderColumn = FindHeaderIndex(headers, "ChoiceOrder", "선택지 순서");
            // Row 2 contains runtime/schema field names, not game data.
            for (int rowIndex = 2; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                string stableId = row.Count > 0 ? row[0] : string.Empty;
                if (string.IsNullOrWhiteSpace(stableId))
                    continue;
                for (int column = 1; column < headers.Count; column++)
                {
                    string korean = column < row.Count ? row[column] : string.Empty;
                    if (LocalizationProjectScanner.ShouldCollectGameDataLocalizationValue(sheet, headers[column], korean))
                    {
                        string sharedKey = LocalizationProjectScanner.GetSharedGameDataKey(sheet, stableId, headers[column], korean);
                        if (!string.IsNullOrEmpty(sharedKey))
                        {
                            keys.Add(sharedKey);
                            continue;
                        }
                        string stableKey = BuildGameDataKey(sheet, stableId, row, choiceOrderColumn, headers[column]);
                        keys.Add(scannedKeys.Add(stableKey)
                            ? stableKey
                            : LocalizationProjectScanner.BuildUniqueGameDataKey(sheet, stableId, headers[column], korean));
                    }
                }
            }
        }

        return keys;
    }

    private static string BuildGameDataKey(
        string sheet,
        string stableId,
        IReadOnlyList<string> row,
        int choiceOrderColumn,
        string header)
    {
        if (string.Equals(sheet, "Event", StringComparison.Ordinal) &&
            choiceOrderColumn >= 0 &&
            LocalizationProjectScanner.IsEventChoiceField(header))
        {
            int choiceOrder = choiceOrderColumn < row.Count &&
                int.TryParse(row[choiceOrderColumn], out int parsedOrder)
                ? parsedOrder
                : 0;
            return LocalizationProjectScanner.BuildEventChoiceKey(stableId, choiceOrder, header);
        }

        return LocalizationProjectScanner.BuildGameDataKey(sheet, stableId, header);
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] headerNames)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            foreach (string headerName in headerNames)
            {
                if (string.Equals(headers[i], headerName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        return -1;
    }

    /// <summary>Only unique source text may be reused automatically. Duplicate copy requires contextual review.</summary>
    private static Dictionary<string, string> ReadKoreanToUniqueKey()
    {
        var rows = LocalizationXlsxReader.ReadSheet(LocalizationExcelImporter.WorkbookPath, LocalizationExcelImporter.WorksheetName);
        int key = rows[0].ToList().FindIndex(value => value == "Key");
        int korean = rows[0].ToList().FindIndex(value => value == LocalizationWorkbookWriter.KoreanSourceHeader);
        if (korean < 0) korean = rows[0].ToList().FindIndex(value => value == "Korean(ko)");
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
        int korean = rows[0].ToList().FindIndex(value => value == LocalizationWorkbookWriter.KoreanSourceHeader);
        if (korean < 0) korean = rows[0].ToList().FindIndex(value => value == "Korean(ko)");
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
    public static bool IsNewForKnownKeys(string key, IEnumerable<string> knownKeys) =>
        !string.IsNullOrWhiteSpace(key) && !(knownKeys ?? Array.Empty<string>()).Contains(key, StringComparer.Ordinal);
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
