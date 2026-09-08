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

    /// <summary>Localization Manager용 전체 프로젝트 안전 적용 진입점입니다.</summary>
    public static void ApplyKnownTextAcrossProject()
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            IReadOnlyDictionary<string, string> sourceToKey = ReadSourceToKeyMap();
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
            IReadOnlyDictionary<string, string> sourceToKey = ReadSourceToKeyMap();
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

    private static IReadOnlyDictionary<string, string> ReadSourceToKeyMap()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
            LocalizationExcelImporter.WorkbookPath,
            LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);

        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, KoreanHeader);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (IReadOnlyList<string> row in rows.Skip(1))
        {
            string key = GetValue(row, keyIndex);
            string source = GetValue(row, koreanIndex);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(source))
                continue;

            // 동일 원문은 공통 Key 재사용 후보입니다. 첫 Key를 유지해 기존 연결을 흔들지 않습니다.
            if (!result.ContainsKey(source))
                result[source] = key;
        }

        return result;
    }

    private static int ApplyToPrefabs(IReadOnlyDictionary<string, string> sourceToKey)
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

    private static int ApplyToScenes(IReadOnlyDictionary<string, string> sourceToKey)
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
        IReadOnlyDictionary<string, string> sourceToKey)
    {
        int changedCount = 0;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!LocalizedTMPText.ShouldManageText(text))
                continue;

            if (sourceToKey.TryGetValue(text.text, out string key) && ConfigureText(text, key))
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
