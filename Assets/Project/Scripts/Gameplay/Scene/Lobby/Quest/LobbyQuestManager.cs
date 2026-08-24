using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LobbyQuestManager : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";

    [SerializeField] private LobbyQuestTextConfig textConfig = new();
    [SerializeField] private Canvas questCanvas;
    [SerializeField] private LobbyQuestPanel questPanel;
    [SerializeField] private string[] hideWhenAnyActiveObjectNames =
    {
        "DialoguePanel",
        "CharacterSettingPanel",
        "SkillSettingPanel",
        "RuneSettingPanel",
        "SkillIconSelectPanel_0",
        "SkillIconSelectPanel_1",
        "SkillIconSelectPanel_2",
        "RuneIconSelectPanel",
        "RelicShopPanel",
        "CultureTankPanel",
        "ResearchResultPanel",
        "ErosionSelectPanel",
        "StageSelectPanel",
        "StoragePanel"
    };

    public static LobbyQuestManager Instance { get; private set; }

    public LobbyTutorialProgress CurrentProgress =>
        GetLobby()?.TutorialProgress ?? LobbyTutorialProgress.NotStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void LateUpdate()
    {
        Refresh();
    }

    public bool CanUseFeature(LobbyTutorialProgress required)
    {
        return LobbyQuestState.CanUseFeature(CurrentProgress, required);
    }

    public void Refresh()
    {
        if (!IsLobbySceneActive())
        {
            if (questCanvas != null)
                questCanvas.gameObject.SetActive(false);
            return;
        }

        if (questCanvas == null || questPanel == null)
        {
            Debug.LogWarning(
                "[LobbyQuestManager] Scene-placed quest canvas or panel is missing.",
                this);
            return;
        }

        LobbyQuestState state = LobbyQuestState.Build(GetLobby(), textConfig);
        if (questCanvas != null)
            questCanvas.gameObject.SetActive(state.IsVisible && IsDefaultLobbyStateVisible());

        if (questPanel != null)
            questPanel.Apply(state);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Refresh();
    }

    private static LobbyRuntimeData GetLobby()
    {
        return DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
    }

    private static bool IsLobbySceneActive()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && activeScene.name == LobbySceneName;
    }

    private bool IsDefaultLobbyStateVisible()
    {
        if (hideWhenAnyActiveObjectNames == null ||
            hideWhenAnyActiveObjectNames.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < hideWhenAnyActiveObjectNames.Length; i++)
        {
            string objectName = hideWhenAnyActiveObjectNames[i];
            if (string.IsNullOrWhiteSpace(objectName))
                continue;

            if (IsLobbyObjectActive(objectName))
                return false;
        }

        return true;
    }

    private static bool IsLobbyObjectActive(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject target = objects[i];
            if (target == null ||
                !string.Equals(target.name, objectName, StringComparison.Ordinal) ||
                !target.scene.IsValid() ||
                target.scene.name != LobbySceneName)
            {
                continue;
            }

            if (target.activeInHierarchy)
                return true;
        }

        return false;
    }
}
