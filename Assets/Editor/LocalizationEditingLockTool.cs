using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

public static class LocalizationEditingLockTool
{
    private static readonly string[] TargetScenePaths =
    {
        "Assets/Project/Scenes/YDM/Title.unity",
        "Assets/Project/Scenes/YDM/Lobby.unity",
        "Assets/Project/Scenes/YDM/Battle.unity",
    };

    private const string PrefabRoot = "Assets/Project/PrefabsR";

    [MenuItem("Tools/Localization/Disable Text Localization Editing Lock")]
    public static void DisableFromMenu()
    {
        SetTargetsFromMenu(false);
    }

    [MenuItem("Tools/Localization/Enable Text Localization Editing Lock")]
    public static void EnableFromMenu()
    {
        SetTargetsFromMenu(true);
    }

    [MenuItem("Tools/Localization/Report Text Localization Editing Lock State")]
    public static void ReportStateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int disabledCount = CountDisabledTargetLocalizers();
        if (disabledCount == 0)
        {
            Debug.Log("[LocalizationEditingLockTool] 모든 대상 LocalizeStringEvent가 활성화되어 있습니다.");
            return;
        }

        Debug.LogWarning(
            $"[LocalizationEditingLockTool] 비활성화된 대상 LocalizeStringEvent가 {disabledCount}개 있습니다. " +
            "빌드 전 Enable Text Localization Editing Lock을 실행하세요.");
    }

    public static int SetHierarchyLocalizersEnabled(GameObject root, bool enabled)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        int changedCount = 0;
        foreach (LocalizeStringEvent localizer in root.GetComponentsInChildren<LocalizeStringEvent>(true))
        {
            if (localizer.enabled == enabled)
                continue;

            Undo.RecordObject(localizer, enabled ? "Enable localized text editing lock" : "Disable localized text editing lock");
            localizer.enabled = enabled;
            EditorUtility.SetDirty(localizer);
            changedCount++;
        }

        return changedCount;
    }

    private static void SetTargetsFromMenu(bool enabled)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int changedCount = SetTargetLocalizersEnabled(enabled);
        string state = enabled ? "활성화" : "비활성화";
        Debug.Log($"[LocalizationEditingLockTool] 대상 LocalizeStringEvent {changedCount}개를 {state}했습니다.");
    }

    private static int SetTargetLocalizersEnabled(bool enabled)
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            int changedCount = ApplyToPrefabs(enabled);
            changedCount += ApplyToScenes(enabled);

            AssetDatabase.SaveAssets();
            return changedCount;
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
    }

    private static int CountDisabledTargetLocalizers()
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            return CountDisabledInPrefabs() + CountDisabledInScenes();
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
    }

    private static int ApplyToPrefabs(bool enabled)
    {
        int changedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changedInPrefab = SetHierarchyLocalizersEnabled(root, enabled);
                if (changedInPrefab == 0)
                    continue;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedCount += changedInPrefab;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return changedCount;
    }

    private static int ApplyToScenes(bool enabled)
    {
        int changedCount = 0;
        foreach (string path in TargetScenePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Target localization scene was not found.", path);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int changedInScene = scene.GetRootGameObjects()
                .Sum(root => SetHierarchyLocalizersEnabled(root, enabled));
            if (changedInScene == 0)
                continue;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            changedCount += changedInScene;
        }

        return changedCount;
    }

    private static int CountDisabledInPrefabs()
    {
        int disabledCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                disabledCount += root.GetComponentsInChildren<LocalizeStringEvent>(true)
                    .Count(localizer => !localizer.enabled);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return disabledCount;
    }

    private static int CountDisabledInScenes()
    {
        int disabledCount = 0;
        foreach (string path in TargetScenePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Target localization scene was not found.", path);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            disabledCount += scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<LocalizeStringEvent>(true))
                .Count(localizer => !localizer.enabled);
        }

        return disabledCount;
    }
}
