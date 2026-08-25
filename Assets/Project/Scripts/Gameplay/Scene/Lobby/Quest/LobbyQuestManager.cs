using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LobbyQuestManager : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";
    private const string DialoguePanelObjectName = "DialoguePanel";
    private const string CharacterSettingPanelObjectName = "CharacterSettingPanel";
    private const float DefaultPanelOffsetY = 0f;
    private const float DialoguePanelOffsetY = 100f;

    [SerializeField] private LobbyQuestTextConfig textConfig = new();
    [SerializeField] private Canvas questCanvas;
    [SerializeField] private LobbyQuestPanel questPanel;

    [Header("Dialogue Position Animation")]
    [SerializeField, Min(0f)] private float dialogueMoveDuration = 0.25f;
    [SerializeField] private AnimationCurve dialogueMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private string[] hideWhenAnyActiveObjectNames =
    {
        "SkillSettingPanel",
        "RuneSettingPanel",
        "SkillIconSelectPanel_0",
        "SkillIconSelectPanel_1",
        "SkillIconSelectPanel_2",
        "RuneIconSelectPanel",
        "ResearchResultPanel",
        "StageSelectPanel",
        "StoragePanel"
    };

    private RectTransform questPanelRectTransform;
    private LobbyPanelTransition lobbyPanelTransition;
    private Canvas lobbyPanelTransitionCanvas;
    private bool questCanvasSortingCaptured;
    private bool questCanvasSortingForced;
    private bool originalQuestOverrideSorting;
    private int originalQuestSortingLayerId;
    private int originalQuestSortingOrder;
    private bool hasDialogueMoveTarget;
    private bool isDialogueMovePlaying;
    private float dialogueMoveStartY;
    private float dialogueMoveTargetY;
    private float dialogueMoveElapsed;

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
        CacheQuestPanelRectTransform();
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

        CacheQuestPanelRectTransform();

        LobbyQuestState state = LobbyQuestState.Build(GetLobby(), textConfig);
        bool defaultLobbyStateVisible = IsDefaultLobbyStateVisible();
        bool characterSettingOpen = IsLobbyObjectActive(CharacterSettingPanelObjectName);
        bool dialogueOpen = IsLobbyObjectActive(DialoguePanelObjectName);

        // 텍스트와 퀘스트 진행 상태는 기존 LobbyQuestPanel 로직을 그대로 사용합니다.
        questPanel.Apply(state);

        // DialoguePanel이 열리고 닫힐 때 0 <-> 100 사이를 부드럽게 이동합니다.
        UpdateDialoguePanelPosition(dialogueOpen);

        // CharacterSettingPanel이 실제로 활성화되어 있는 동안에만 퀘스트 패널을 숨깁니다.
        // 씬 전환이 먼저 시작되더라도 CharacterSettingPanel이 켜지기 전에는 미리 숨기지 않습니다.
        // CharacterSettingPanel이 꺼지는 순간에는 씬 전환 중이어도 즉시 다시 활성화되며,
        // 씬 전환 Canvas의 높은 sortingOrder 때문에 전환 연출 뒤에서 대기합니다.
        questPanel.gameObject.SetActive(state.IsVisible && !characterSettingOpen);
        questCanvas.gameObject.SetActive(state.IsVisible && defaultLobbyStateVisible);

        UpdateQuestCanvasSortingForLobbyPanelTransition();
    }

    public void ConfigureQuestPanelBlur(UIBlurBackground blurBackground)
    {
        if (blurBackground == null || questPanel == null)
            return;

        // CultureTank / RelicShop / ErosionSelect의 블러 캡처에 QuestPanel도 포함합니다.
        // 원본 QuestPanel은 블러가 열린 동안 UIBlurBackground가 숨기므로
        // 패널 위로 직접 렌더되지 않고, 캡처된 흐린 배경에만 보입니다.
        blurBackground.SetRuntimeBlurredUiRoots(new[] { questPanel.gameObject });
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreQuestCanvasSorting();
        lobbyPanelTransition = null;
        lobbyPanelTransitionCanvas = null;
        hasDialogueMoveTarget = false;
        isDialogueMovePlaying = false;
        CacheQuestPanelRectTransform();
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

    private void CacheQuestPanelRectTransform()
    {
        if (questPanel == null)
        {
            questPanelRectTransform = null;
            return;
        }

        if (questPanelRectTransform == null || questPanelRectTransform.gameObject != questPanel.gameObject)
            questPanelRectTransform = questPanel.transform as RectTransform;
    }

    private void UpdateDialoguePanelPosition(bool dialogueOpen)
    {
        if (questPanelRectTransform == null)
            return;

        float requestedTargetY = dialogueOpen ? DialoguePanelOffsetY : DefaultPanelOffsetY;

        if (!hasDialogueMoveTarget || !Mathf.Approximately(dialogueMoveTargetY, requestedTargetY))
            BeginDialoguePanelMove(requestedTargetY);

        if (!isDialogueMovePlaying)
            return;

        if (dialogueMoveDuration <= 0f)
        {
            SetQuestPanelY(dialogueMoveTargetY);
            isDialogueMovePlaying = false;
            return;
        }

        dialogueMoveElapsed += Time.unscaledDeltaTime;
        float normalizedTime = Mathf.Clamp01(dialogueMoveElapsed / dialogueMoveDuration);
        float curvedTime = dialogueMoveCurve != null
            ? dialogueMoveCurve.Evaluate(normalizedTime)
            : normalizedTime;

        float y = Mathf.LerpUnclamped(dialogueMoveStartY, dialogueMoveTargetY, curvedTime);
        SetQuestPanelY(y);

        if (normalizedTime >= 1f)
        {
            SetQuestPanelY(dialogueMoveTargetY);
            isDialogueMovePlaying = false;
        }
    }

    private void BeginDialoguePanelMove(float targetY)
    {
        hasDialogueMoveTarget = true;
        dialogueMoveTargetY = targetY;
        dialogueMoveElapsed = 0f;

        if (questPanelRectTransform == null)
        {
            isDialogueMovePlaying = false;
            return;
        }

        dialogueMoveStartY = questPanelRectTransform.anchoredPosition.y;

        if (dialogueMoveDuration <= 0f || Mathf.Approximately(dialogueMoveStartY, dialogueMoveTargetY))
        {
            SetQuestPanelY(dialogueMoveTargetY);
            isDialogueMovePlaying = false;
            return;
        }

        isDialogueMovePlaying = true;
    }

    private void SetQuestPanelY(float y)
    {
        if (questPanelRectTransform == null)
            return;

        Vector2 anchoredPosition = questPanelRectTransform.anchoredPosition;
        anchoredPosition.y = y;
        questPanelRectTransform.anchoredPosition = anchoredPosition;
    }

    private void UpdateQuestCanvasSortingForLobbyPanelTransition()
    {
        if (questCanvas == null)
            return;

        CacheLobbyPanelTransition();

        bool transitionPlaying =
            lobbyPanelTransition != null &&
            lobbyPanelTransition.IsPlaying &&
            lobbyPanelTransitionCanvas != null;

        if (!transitionPlaying)
        {
            RestoreQuestCanvasSorting();
            return;
        }

        CaptureQuestCanvasSorting();

        // LobbyPanelTransition은 Lobby 메인 Canvas 안에 있으므로 전환 중에만
        // Quest Canvas를 그 Canvas보다 한 단계 뒤로 보냅니다.
        // CharacterSettingPanel이 닫히며 QuestPanel이 먼저 활성화되어도
        // 전환 이미지 뒤에 이미 떠 있는 상태가 됩니다.
        questCanvas.overrideSorting = true;
        questCanvas.sortingLayerID = lobbyPanelTransitionCanvas.sortingLayerID;
        questCanvas.sortingOrder = lobbyPanelTransitionCanvas.sortingOrder - 1;
        questCanvasSortingForced = true;
    }

    private void CacheLobbyPanelTransition()
    {
        if (lobbyPanelTransition != null &&
            lobbyPanelTransition.gameObject != null &&
            lobbyPanelTransition.gameObject.scene.IsValid() &&
            lobbyPanelTransition.gameObject.scene.name == LobbySceneName)
        {
            if (lobbyPanelTransitionCanvas == null)
                lobbyPanelTransitionCanvas = lobbyPanelTransition.GetComponentInParent<Canvas>();
            return;
        }

        lobbyPanelTransition = null;
        lobbyPanelTransitionCanvas = null;

        LobbyPanelTransition[] transitions = Resources.FindObjectsOfTypeAll<LobbyPanelTransition>();
        for (int i = 0; i < transitions.Length; i++)
        {
            LobbyPanelTransition candidate = transitions[i];
            if (candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                candidate.gameObject.scene.name != LobbySceneName)
            {
                continue;
            }

            lobbyPanelTransition = candidate;
            lobbyPanelTransitionCanvas = candidate.GetComponentInParent<Canvas>();
            return;
        }
    }

    private void CaptureQuestCanvasSorting()
    {
        if (questCanvasSortingCaptured || questCanvas == null)
            return;

        originalQuestOverrideSorting = questCanvas.overrideSorting;
        originalQuestSortingLayerId = questCanvas.sortingLayerID;
        originalQuestSortingOrder = questCanvas.sortingOrder;
        questCanvasSortingCaptured = true;
    }

    private void RestoreQuestCanvasSorting()
    {
        if (!questCanvasSortingCaptured || !questCanvasSortingForced || questCanvas == null)
            return;

        questCanvas.overrideSorting = originalQuestOverrideSorting;
        questCanvas.sortingLayerID = originalQuestSortingLayerId;
        questCanvas.sortingOrder = originalQuestSortingOrder;
        questCanvasSortingForced = false;
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
