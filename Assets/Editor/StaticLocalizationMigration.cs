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
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Localization key is required.", nameof(key));

        LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
        if (localizer != null)
        {
            bool sameReference =
                localizer.StringReference.TableReference.TableCollectionName == LocalizationExcelImporter.TableCollectionName &&
                localizer.StringReference.TableEntryReference.Key == key;
            bool hasTextListener = Enumerable.Range(0, localizer.OnUpdateString.GetPersistentEventCount())
                .Any(index => localizer.OnUpdateString.GetPersistentTarget(index) == text);

            if (sameReference && hasTextListener)
                return false;
        }
        else
        {
            localizer = Undo.AddComponent<LocalizeStringEvent>(text.gameObject);
        }

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

        EditorUtility.SetDirty(localizer);
        return true;
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

            if (result.TryGetValue(source, out string existingKey) && existingKey != key)
            {
                throw new InvalidDataException(
                    $"Korean source '{source}' is assigned to both '{existingKey}' and '{key}'. " +
                    "정적 텍스트 자동 연결을 위해 한국어 원문을 고유하게 유지하세요.");
            }

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
