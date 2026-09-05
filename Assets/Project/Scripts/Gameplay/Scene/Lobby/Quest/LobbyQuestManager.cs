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

        // �ؽ�Ʈ�� ����Ʈ ���� ���´� ���� LobbyQuestPanel ������ �״�� ����մϴ�.
        questPanel.Apply(state);

        // DialoguePanel�� ������ ���� �� 0 <-> 100 ���̸� �ε巴�� �̵��մϴ�.
        UpdateDialoguePanelPosition(dialogueOpen);

        // CharacterSettingPanel�� ������ Ȱ��ȭ�Ǿ� �ִ� ���ȿ��� ����Ʈ �г��� ����ϴ�.
        // �� ��ȯ�� ���� ���۵Ǵ��� CharacterSettingPanel�� ������ ������ �̸� ������ �ʽ��ϴ�.
        // CharacterSettingPanel�� ������ �������� �� ��ȯ ���̾ ��� �ٽ� Ȱ��ȭ�Ǹ�,
        // �� ��ȯ Canvas�� ���� sortingOrder ������ ��ȯ ���� �ڿ��� ����մϴ�.
        questPanel.gameObject.SetActive(state.IsVisible && !characterSettingOpen);
        questCanvas.gameObject.SetActive(state.IsVisible && defaultLobbyStateVisible);

        UpdateQuestCanvasSortingForLobbyPanelTransition();
    }

    public void ConfigureQuestPanelBlur(UIBlurBackground blurBackground)
    {
        if (blurBackground == null || questPanel == null)
            return;

        // CultureTank / RelicShop / ErosionSelect�� ���� ĸó�� QuestPanel�� �����մϴ�.
        // ���� QuestPanel�� ������ ���� ���� UIBlurBackground�� ����Ƿ�
        // �г� ���� ���� �������� �ʰ�, ĸó�� �帰 ��濡�� ���Դϴ�.
        blurBackground.AddRuntimeBlurredUiRoot(questPanel.gameObject);
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
        UIBlurBackgroundManager.MarkReplicaDirty();
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

        // LobbyPanelTransition�� Lobby ���� Canvas �ȿ� �����Ƿ� ��ȯ �߿���
        // Quest Canvas�� �� Canvas���� �� �ܰ� �ڷ� �����ϴ�.
        // CharacterSettingPanel�� ������ QuestPanel�� ���� Ȱ��ȭ�Ǿ
        // ��ȯ �̹��� �ڿ� �̹� �� �ִ� ���°� �˴ϴ�.
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
