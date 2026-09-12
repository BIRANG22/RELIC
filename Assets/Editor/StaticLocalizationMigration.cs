using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

public static class StaticLocalizationMigration
{
    private static readonly string[] TargetScenePaths =
    {
        "Assets/Project/Scenes/YDM/Title.unity",
        "Assets/Project/Scenes/YDM/Lobby.unity",
        "Assets/Project/Scenes/YDM/Battle.unity",
    };

    private const string PrefabRoot = "Assets/Project/PrefabsR";
    private const string KoreanHeader = "Korean(ko)";

    [MenuItem("Tools/Localization/Apply Excel Localization To Player UI")]
    public static void ApplyFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ApplyAndLog();
    }

    public static bool TryApplyAutomatically(out string reason)
    {
        Scene[] dirtyScenes = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt)
            .Where(scene => scene.isLoaded && scene.isDirty)
            .ToArray();
        if (dirtyScenes.Length > 0)
        {
            reason = "저장되지 않은 씬이 열려 있습니다: " +
                     string.Join(", ", dirtyScenes.Select(scene => scene.path));
            return false;
        }

        ApplyAndLog();
        reason = string.Empty;
        return true;
    }

    /// <summary>Uses a shared prefab source when present; otherwise migrates the 25 serialized Lobby scene instances in one pass.</summary>
    public static void MigrateRuneInstallationTexts()
    {
        const string installationKey = "ui.rune.installation";
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            bool prefabSourceFound = false;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = ConfigureRuneInstallationTexts(root, installationKey);
                    if (changed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabSourceFound = true;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            if (!prefabSourceFound)
            {
                const string lobbyPath = "Assets/Project/Scenes/YDM/Lobby.unity";
                Scene scene = EditorSceneManager.OpenScene(lobbyPath, OpenSceneMode.Single);
                int changed = scene.GetRootGameObjects().Sum(root => ConfigureRuneInstallationTexts(root, installationKey));
                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                Debug.Log($"[StaticLocalizationMigration] Rune Installation scene bindings: {changed}");
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(originalSetup); }
    }

    [MenuItem("Tools/Localization/Migrate Character InfoArea Dynamic Ownership")]
    public static void MigrateCharacterInfoAreaDynamicOwnership()
    {
        const string lobbyPath = "Assets/Project/Scenes/YDM/Lobby.unity";
        string[] dynamicTextNames = { "TitleText", "RarityText", "EffectText", "TypeText", "CostText", "ValueText" };
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(lobbyPath, OpenSceneMode.Single);
            Transform area = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(candidate => PathEquals(candidate, "Canvas/PositionPanel/CharacterSettingPanel/Setting_Area/InfoArea"));
            if (area == null)
                throw new InvalidOperationException("CharacterSettingPanel/Setting_Area/InfoArea was not found in Lobby.");

            TMP_Text[] dynamicTexts = dynamicTextNames.Select(textName =>
                area.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(candidate =>
                    candidate.name == textName || (textName == "TypeText" && candidate.name == "TpyeText"))).ToArray();
            for (int index = 0; index < dynamicTexts.Length; index++)
                if (dynamicTexts[index] == null)
                    throw new InvalidOperationException($"Character InfoArea dynamic text '{dynamicTextNames[index]}' was not found.");

            int changed = 0;
            foreach (TMP_Text text in dynamicTexts)
            {
                if (text.GetComponent<LocalizationIgnore>() == null)
                {
                    Undo.AddComponent<LocalizationIgnore>(text.gameObject);
                    changed++;
                }
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[StaticLocalizationMigration] Character InfoArea dynamic ownership bindings: {changed}");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(originalSetup); }
    }

    [MenuItem("Tools/Localization/Repair CharacterSetting InfoText Bindings")]
    public static void MigrateCharacterSettingInfoText()
    {
        const string lobbyPath = "Assets/Project/Scenes/YDM/Lobby.unity";
        const string infoTextPath = "Canvas/PositionPanel/CharacterSettingPanel/Info_Area/InfoText";
        const string characterInfoTextPath = "Canvas/PositionPanel/CharacterSettingPanel/Info_Area/CharacterinfoText";
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(lobbyPath, OpenSceneMode.Single);
            Transform target = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(candidate => PathEquals(candidate, infoTextPath));
            TMP_Text text = target?.GetComponent<TMP_Text>();
            if (text == null)
                throw new InvalidOperationException($"CharacterSetting InfoText was not found at '{infoTextPath}'.");

            Transform dynamicTarget = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(candidate => PathEquals(candidate, characterInfoTextPath));
            TMP_Text dynamicText = dynamicTarget?.GetComponent<TMP_Text>();
            if (dynamicText == null)
                throw new InvalidOperationException($"Dynamic CharacterInfoText was not found at '{characterInfoTextPath}'.");

            bool infoTextChanged = ConfigureDynamicText(text);
            bool characterInfoTextChanged = ConfigureDynamicText(dynamicText);
            CharacterInfoPanel[] panels = UnityEngine.Object.FindObjectsByType<CharacterInfoPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int storyTextReferences = 0;
            foreach (CharacterInfoPanel panel in panels)
            {
                var serializedPanel = new SerializedObject(panel);
                SerializedProperty storyText = serializedPanel.FindProperty("storyText");
                if (storyText == null)
                    throw new InvalidOperationException("CharacterInfoPanel.storyText serialized field was not found.");

                if (storyText.objectReferenceValue == text)
                    storyTextReferences++;
            }

            bool changed = infoTextChanged || characterInfoTextChanged;
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log(
                "[CharacterSetting InfoText Repair]\n" +
                $"Target InfoText: {GetPath(text.transform)}\n" +
                $"CharacterInfoPanel count: {panels.Length}\n" +
                $"storyText references to InfoText: {storyTextReferences}\n" +
                "StoryWriter: InfoText (hover and karma acquisition information)\n" +
                $"CharacterInfoText: {GetPath(dynamicText.transform)} (character introduction)\n" +
                $"InfoText LocalizedTMPText: {(text.GetComponent<LocalizedTMPText>() == null ? "None" : "Unexpected")}\n" +
                $"Result: {(storyTextReferences > 0 ? "SUCCESS" : "MANUAL_BINDING_REQUIRED")}");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(originalSetup); }
    }

    public static bool ConfigureDynamicText(TMP_Text text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        bool changed = false;
        LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
        if (localizer != null)
        {
            Undo.DestroyObjectImmediate(localizer);
            changed = true;
        }

        LocalizeStringEvent legacyLocalizer = text.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
        {
            Undo.DestroyObjectImmediate(legacyLocalizer);
            changed = true;
        }

        if (text.GetComponent<LocalizationIgnore>() == null)
        {
            Undo.AddComponent<LocalizationIgnore>(text.gameObject);
            changed = true;
        }

        return changed;
    }

    public static int ConfigureDynamicTexts(IEnumerable<TMP_Text> texts)
    {
        return (texts ?? Array.Empty<TMP_Text>())
            .Where(text => text != null)
            .Count(ConfigureDynamicText);
    }

    /// <summary>Record 기억 상세의 값은 RecordPanelUI가 조합하므로 정적 로컬라이저가 소유하면 안 됩니다.</summary>
    public static int MigrateRecordMemoryDynamicOwnership()
    {
        const string prefabPath = "Assets/Project/PrefabsR/Record.prefab";
        string[] paths = { "Info/Memory/method", "Info/Memory/consumption", "Info/Memory/Point" };
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            TMP_Text[] texts = paths.Select(path => root.transform.Find(path)?.GetComponent<TMP_Text>()).ToArray();
            if (texts.Any(text => text == null))
                throw new InvalidOperationException("Record memory dynamic TMP binding could not be found.");

            int changed = ConfigureDynamicTexts(texts);
            if (changed > 0)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return changed;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>동적 Record 출력이 사용하는 포맷 템플릿을 워크북의 정식 원문으로 복원합니다.</summary>
    public static int EnsureRecordMemoryFormatTemplates()
    {
        LocalizationWorkbookEntry[] entries =
        {
            new(LocalizationKeys.Record.Method, "방식 : {0}"),
            new(LocalizationKeys.Record.Consumption, "소모 : {0}"),
            new(LocalizationKeys.Record.Effect, "효과 : {0}"),
        };
        int added = LocalizationWorkbookWriter.MergeNewEntries(LocalizationExcelImporter.WorkbookPath, entries);
        int updated = LocalizationWorkbookWriter.UpdateExistingEntries(LocalizationExcelImporter.WorkbookPath, entries);
        return added + updated;
    }

    private static bool PathEquals(Transform value, string expected)
    {
        return GetPath(value) == expected;
    }

    private static string GetPath(Transform value)
    {
        return value.parent == null ? value.name : GetPath(value.parent) + "/" + value.name;
    }

    private static int ConfigureRuneInstallationTexts(GameObject root, string key)
    {
        int changed = 0;
        foreach (Transform installation in root.GetComponentsInChildren<Transform>(true).Where(candidate => candidate.name == "Installation"))
        {
            Transform textRoot = installation.Find("Text") ?? installation.Find("Text (TMP)");
            TMP_Text text = textRoot?.GetComponent<TMP_Text>();
            if (text != null && LocalizedTMPText.ShouldManageText(text) && ConfigureText(text, key))
                changed++;
        }
        return changed;
    }

    /// <summary>Localization Manager용 전체 프로젝트 안전 적용 진입점입니다.</summary>
    public static void ApplyKnownTextAcrossProject()
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            LocalizationBindingResolver sourceToKey = ReadSourceToKeyMap();
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (ApplyToHierarchy(root, sourceToKey) > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed++;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (scene.GetRootGameObjects().Sum(root => ApplyToHierarchy(root, sourceToKey)) > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                    changed++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[StaticLocalizationMigration] 전체 프로젝트 안전 적용 Asset {changed}개.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(originalSetup); }
    }

    private static void ApplyAndLog()
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            LocalizationExcelImporter.Import();
            LocalizationBindingResolver sourceToKey = ReadSourceToKeyMap();
            int prefabCount = ApplyToPrefabs(sourceToKey);
            int sceneCount = ApplyToScenes(sourceToKey);

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[StaticLocalizationMigration] 연결 완료: 프리팹 텍스트 {prefabCount}개, " +
                $"씬 텍스트 {sceneCount}개. 등록되지 않은 텍스트는 변경하지 않았습니다.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
    }

    public static bool ConfigureText(TMP_Text text, string key)
    {
        return ConfigureText(text, key, false);
    }

    public static bool RepairTextBinding(TMP_Text text, string key)
    {
        return ConfigureText(text, key, true);
    }

    private static bool ConfigureText(TMP_Text text, string key, bool overwriteExistingKey)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Localization key is required.", nameof(key));

        // LocalizeStringEvent의 갱신 콜백보다 먼저 원문을 보존합니다.
        string koreanSource = text.text;
        LocalizedTMPText existingRuntimeLocalizer = text.GetComponent<LocalizedTMPText>();
        if (existingRuntimeLocalizer != null &&
            !overwriteExistingKey &&
            !string.IsNullOrWhiteSpace(existingRuntimeLocalizer.LocalizationKey))
            return false;

        LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
        bool bindingAlreadyValid = false;
        bool runtimeLocalizerCreated = false;
        if (localizer != null)
        {
            bool sameReference =
                localizer.StringReference.TableReference.TableCollectionName == LocalizationExcelImporter.TableCollectionName &&
                localizer.StringReference.TableEntryReference.Key == key;
            bool hasTextListener = Enumerable.Range(0, localizer.OnUpdateString.GetPersistentEventCount())
                .Any(index => localizer.OnUpdateString.GetPersistentTarget(index) == text);

            if (sameReference && hasTextListener)
                bindingAlreadyValid = true;

            // 이미 설정된 Key는 원문 일치만으로 다른 의미의 Key로 바꾸지 않습니다.
            if (!bindingAlreadyValid && !overwriteExistingKey &&
                localizer.StringReference.TableReference.TableCollectionName == LocalizationExcelImporter.TableCollectionName &&
                !string.IsNullOrWhiteSpace(localizer.StringReference.TableEntryReference.Key))
                return false;
        }
        else
        {
            localizer = Undo.AddComponent<LocalizeStringEvent>(text.gameObject);
        }

        if (!bindingAlreadyValid)
        {
            Undo.RecordObject(localizer, "Configure localized text");
            localizer.StringReference = new LocalizedString(
                LocalizationExcelImporter.TableCollectionName,
                key);

            bool listenerExists = Enumerable.Range(0, localizer.OnUpdateString.GetPersistentEventCount())
                .Any(index => localizer.OnUpdateString.GetPersistentTarget(index) == text);
            if (!listenerExists)
            {
                PropertyInfo textProperty = text.GetType().GetProperty(nameof(TMP_Text.text));
                MethodInfo setter = textProperty?.GetSetMethod();
                if (setter == null)
                    throw new InvalidOperationException($"'{text.GetType().Name}' does not expose a text setter.");

                var callback = (UnityAction<string>)Delegate.CreateDelegate(
                    typeof(UnityAction<string>),
                    text,
                    setter);
                UnityEventTools.AddPersistentListener(localizer.OnUpdateString, callback);
                int listenerIndex = localizer.OnUpdateString.GetPersistentEventCount() - 1;
                localizer.OnUpdateString.SetPersistentListenerState(
                    listenerIndex,
                    UnityEventCallState.EditorAndRuntime);
            }
        }

        EditorUtility.SetDirty(localizer);
        Type runtimeLocalizerType = Type.GetType("LocalizedTMPText, Assembly-CSharp");
        if (runtimeLocalizerType != null)
        {
            Component runtimeLocalizer = text.GetComponent(runtimeLocalizerType);
            if (runtimeLocalizer == null)
            {
                runtimeLocalizer = Undo.AddComponent(text.gameObject, runtimeLocalizerType);
                runtimeLocalizerCreated = true;
            }

            Undo.RecordObject(runtimeLocalizer, "Configure runtime localized TMP text");
            var serializedRuntimeLocalizer = new SerializedObject(runtimeLocalizer);
            serializedRuntimeLocalizer.FindProperty("localizationKey").stringValue = key;
            serializedRuntimeLocalizer.FindProperty("koreanSource").stringValue = koreanSource;
            serializedRuntimeLocalizer.FindProperty("automaticallyRegistered").boolValue = true;
            serializedRuntimeLocalizer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtimeLocalizer);

            // 런타임 표시와 빈 번역 처리의 소유자는 LocalizedTMPText입니다.
            // 이전 LocalizeStringEvent가 남아 있으면 다른 키가 같은 TMP를 다시 덮어씁니다.
            // 표시 권한을 LocalizedTMPText 하나로 단일화합니다.
            Undo.DestroyObjectImmediate(localizer);
        }
        return !bindingAlreadyValid || runtimeLocalizerCreated;
    }

    private static LocalizationBindingResolver ReadSourceToKeyMap()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
            LocalizationExcelImporter.WorkbookPath,
            LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);

        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, KoreanHeader);
        var entries = new List<LocalizationBindingEntry>();

        foreach (IReadOnlyList<string> row in rows.Skip(1))
        {
            string key = GetValue(row, keyIndex);
            string source = GetValue(row, koreanIndex);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(source))
                continue;

            entries.Add(new LocalizationBindingEntry(key, source));
        }

        return new LocalizationBindingResolver(entries);
    }

    private static int ApplyToPrefabs(LocalizationBindingResolver sourceToKey)
    {
        int changedTextCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changedInPrefab = ApplyToHierarchy(root, sourceToKey);
                if (changedInPrefab == 0)
                    continue;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedTextCount += changedInPrefab;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return changedTextCount;
    }

    private static int ApplyToScenes(LocalizationBindingResolver sourceToKey)
    {
        int changedTextCount = 0;
        foreach (string path in TargetScenePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Target localization scene was not found.", path);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int changedInScene = scene.GetRootGameObjects()
                .Sum(root => ApplyToHierarchy(root, sourceToKey));
            if (changedInScene == 0)
                continue;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            changedTextCount += changedInScene;
        }

        return changedTextCount;
    }

    private static int ApplyToHierarchy(
        GameObject root,
        LocalizationBindingResolver sourceToKey)
    {
        int changedCount = 0;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!LocalizedTMPText.ShouldManageText(text))
                continue;

            LocalizationKeyResolution resolution = sourceToKey.ResolveSource(text.text);
            if (resolution.IsUnique && ConfigureText(text, resolution.Key))
                changedCount++;
        }

        return changedCount;
    }

    private static int FindHeader(IReadOnlyList<string> headers, string name)
    {
        for (int index = 0; index < headers.Count; index++)
        {
            if (string.Equals(headers[index], name, StringComparison.Ordinal))
                return index;
        }

        throw new InvalidDataException($"Localization worksheet header '{name}' was not found.");
    }

    private static string GetValue(IReadOnlyList<string> row, int index)
    {
        return index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
    }
}
