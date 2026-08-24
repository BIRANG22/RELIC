using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LobbyQuestManager : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";

    [SerializeField] private LobbyQuestTextConfig textConfig = new();
    [SerializeField] private Canvas questCanvas;
    [SerializeField] private LobbyQuestPanel questPanel;

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
            questCanvas.gameObject.SetActive(state.IsVisible);

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
}
