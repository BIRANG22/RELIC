using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using TMPro;

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
            Debug.Log("[LocalizationEditingLockTool] 모든 대상 텍스트 로컬라이저가 활성화되어 있습니다.");
            return;
        }

        Debug.LogWarning(
            $"[LocalizationEditingLockTool] 비활성화된 대상 텍스트 로컬라이저가 {disabledCount}개 있습니다. " +
            "빌드 전 Enable Text Localization Editing Lock을 실행하세요.");
    }

    public static int SetHierarchyLocalizersEnabled(GameObject root, bool enabled)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        int changedCount = 0;
        foreach (LocalizedTMPText localizer in root.GetComponentsInChildren<LocalizedTMPText>(true))
        {
            TMP_Text text = localizer.GetComponent<TMP_Text>();
            if (!LocalizedTMPText.ShouldManageText(text))
                continue;

            if (localizer.enabled != enabled)
            {
                Undo.RecordObject(localizer, enabled ? "Enable localized text editing lock" : "Disable localized text editing lock");
                localizer.enabled = enabled;
                EditorUtility.SetDirty(localizer);
                changedCount++;
            }

            if (!enabled && text.text != localizer.KoreanSource)
            {
                Undo.RecordObject(text, "Restore Korean text for editing");
                text.text = localizer.KoreanSource;
                EditorUtility.SetDirty(text);
            }
            else if (enabled)
            {
                localizer.Refresh();
            }
        }

        foreach (LocalizeStringEvent localizer in root.GetComponentsInChildren<LocalizeStringEvent>(true))
        {
            // 마이그레이션된 TMP의 표시 소유자는 LocalizedTMPText 하나입니다.
            if (localizer.GetComponent<LocalizedTMPText>() != null || localizer.enabled == enabled)
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

        LocalizationEditingLockState.SetEnabledForEditor(enabled);
        int changedCount = SetTargetLocalizersEnabled(enabled);
        string state = enabled ? "활성화" : "비활성화";
        Debug.Log($"[LocalizationEditingLockTool] 대상 텍스트 로컬라이저 {changedCount}개를 {state}했습니다.");
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
                disabledCount += CountDisabledInHierarchy(root);
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
            disabledCount += scene.GetRootGameObjects().Sum(CountDisabledInHierarchy);
        }

        return disabledCount;
    }

    private static int CountDisabledInHierarchy(GameObject root)
    {
        int runtimeDisabledCount = root.GetComponentsInChildren<LocalizedTMPText>(true)
            .Count(localizer =>
            {
                TMP_Text text = localizer.GetComponent<TMP_Text>();
                return LocalizedTMPText.ShouldManageText(text) && !localizer.enabled;
            });

        int legacyDisabledCount = root.GetComponentsInChildren<LocalizeStringEvent>(true)
            .Count(localizer => localizer.GetComponent<LocalizedTMPText>() == null && !localizer.enabled);

        return runtimeDisabledCount + legacyDisabledCount;
    }
}
