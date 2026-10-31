#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Threading.Tasks;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : Singleton<UIManager>
{
    private const int ModalPanelSortingOrderCeiling = 30000;
    private const string LobbyQuitButtonLocalizationKey = "ui.assets.project.scripts.core.managers.uimanager.text";
    private const string BattleQuitButtonLocalizationKey = "ui.assets.project.scripts.core.managers.uimanager.text.acf962d7";
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject optionPanelPrefab;
    [SerializeField] private GameObject recordPanelPrefab;
    [SerializeField] private GameObject confirmDialogPrefab;

    [Header("Option Panel Sorting")]
    [SerializeField] private bool bringOptionPanelToFront = true;
    [SerializeField] private bool overrideOptionPanelSorting = true;
    [SerializeField] private int optionPanelSortingOrderOffset = 10;

    [Header("Record Panel Sorting")]
    [SerializeField] private bool bringRecordPanelToFront = true;
    [SerializeField] private bool overrideRecordPanelSorting = true;
    [SerializeField] private int recordPanelSortingOrderOffset = 15;

    [Header("Confirm Dialog Sorting")]
    [SerializeField] private bool bringConfirmDialogToFront = true;
    [SerializeField] private bool overrideConfirmDialogSorting = true;
    [SerializeField] private int confirmDialogSortingOrderOffset = 20;

    [Header("Title Modal Background")]
    [SerializeField] private GameObject titleBackPanel;

    [Header("Input")]
    [SerializeField] private bool closeOptionPanelWithEscape = true;
    [SerializeField] private bool closeRecordPanelWithEscape = true;
    [SerializeField] private bool closeConfirmDialogWithEscape = true;

    private static readonly Vector3 OptionPanelDefaultScale = Vector3.one;

    private static int lastOptionClosedByEscapeFrame = -1;
    private static int lastRecordClosedByEscapeFrame = -1;
    private static int lastConfirmClosedByEscapeFrame = -1;

    private GameObject optionPanelInstance;
    private OptionPanelTransition optionPanelTransition;
    private GameObject recordPanelInstance;
    private RecordPanelUI recordPanelUI;
    private OptionPanelTransition recordPanelTransition;
    private GameObject confirmDialogInstance;
    private BootstrapConfirmDialogUI confirmDialogUI;
    private GameObject cachedMenuPanel;
    private Button cachedGiveUpButton;
    private Button cachedRecordButton;
    private Button cachedQuitButton;
    private TMP_Text cachedQuitText;
    private bool menuCameraPauseActive;

    public static bool WasOptionPanelClosedByEscapeThisFrame => lastOptionClosedByEscapeFrame == Time.frameCount;
    public static bool WasRecordPanelClosedByEscapeThisFrame => lastRecordClosedByEscapeFrame == Time.frameCount;
    public static bool WasModalPanelClosedByBackInputThisFrame =>
        WasRecordPanelClosedByEscapeThisFrame || WasOptionPanelClosedByEscapeThisFrame ||
        WasConfirmDialogClosedByEscapeThisFrame;
    public static bool WasConfirmDialogClosedByEscapeThisFrame => lastConfirmClosedByEscapeFrame == Time.frameCount;

    public bool IsOptionPanelOpen => optionPanelInstance != null && optionPanelInstance.activeInHierarchy;
    public bool IsRecordPanelOpen => recordPanelInstance != null && recordPanelInstance.activeInHierarchy;
    public bool IsConfirmDialogOpen => confirmDialogInstance != null && confirmDialogInstance.activeInHierarchy;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;
    }

    private void Start()
    {
        CreateOptionPanel();
        CreateRecordPanel();
        CreateConfirmDialog();
        HideAll();
    }

    private void Update()
    {
        UpdateMenuButtonRuntimeState();

        if (IsTypingInputFieldSelected())
            return;

        bool escapePressed = WasEscapePressedThisFrame();
        bool rightClickPressed = WasRightMousePressedThisFrame();
        if (!escapePressed && !rightClickPressed)
            return;

        if (WasModalPanelClosedByBackInputThisFrame)
            return;

        // Both inputs dismiss one foreground modal per press. Right-click must
        // never open the title quit dialog or fall through to the lobby menu.
        if (closeConfirmDialogWithEscape && TryHideConfirmDialogIfOpen(true))
            return;

        if (closeRecordPanelWithEscape && TryHideRecordIfOpen(true))
            return;

        if (closeOptionPanelWithEscape && TryHideOptionIfOpen(true))
            return;

        if (rightClickPressed)
            return;

        if (IsTitleScene())
            ShowQuitConfirm();
    }

    protected override void OnDestroy()
    {
        ReleaseMenuCameraPause();
        base.OnDestroy();
    }

    private void CreateRecordPanel()
    {
        if (recordPanelInstance != null)
            return;

        if (recordPanelPrefab == null)
            return;

        if (mainCanvas == null)
        {
            Debug.LogError("[UIManager] MainCanvas is not assigned.");
            return;
        }

        recordPanelInstance = Instantiate(recordPanelPrefab, mainCanvas.transform, false);
        recordPanelUI = recordPanelInstance.GetComponent<RecordPanelUI>();
        recordPanelTransition = recordPanelInstance.GetComponent<OptionPanelTransition>();
        if (recordPanelTransition == null)
            recordPanelTransition = recordPanelInstance.AddComponent<OptionPanelTransition>();

        if (recordPanelUI == null)
            Debug.LogError("[UIManager] RecordPanelPrefab에 RecordPanelUI 컴포넌트가 없습니다.");

        BringRecordPanelToFront();
        recordPanelInstance.SetActive(false);
    }

    private void CreateOptionPanel()
    {
        if (optionPanelInstance != null)
            return;

        if (mainCanvas == null)
        {
            Debug.LogError("[UIManager] MainCanvas is not assigned.");
            return;
        }

        if (optionPanelPrefab == null)
        {
            Debug.LogError("[UIManager] OptionPanelPrefab is not assigned.");
            return;
        }

        optionPanelInstance = Instantiate(optionPanelPrefab, mainCanvas.transform, false);
        optionPanelTransition = optionPanelInstance.GetComponent<OptionPanelTransition>();
        if (optionPanelTransition == null)
            optionPanelTransition = optionPanelInstance.AddComponent<OptionPanelTransition>();

        ApplyOptionPanelScale();
        BringOptionPanelToFront();
        optionPanelInstance.SetActive(false);
    }

    private void CreateConfirmDialog()
    {
        if (confirmDialogInstance != null)
            return;

        if (confirmDialogPrefab == null)
            return;

        if (mainCanvas == null)
        {
            Debug.LogError("[UIManager] MainCanvas is not assigned.");
            return;
        }

        confirmDialogInstance = Instantiate(confirmDialogPrefab, mainCanvas.transform, false);
        confirmDialogUI = confirmDialogInstance.GetComponent<BootstrapConfirmDialogUI>();

        if (confirmDialogUI == null)
            confirmDialogUI = confirmDialogInstance.AddComponent<BootstrapConfirmDialogUI>();

        BringConfirmDialogToFront();
        confirmDialogInstance.SetActive(false);
    }

    private GameObject ResolveTitleBackPanel()
    {
        if (!IsTitleScene())
            return null;

        if (titleBackPanel != null && titleBackPanel.scene.IsValid() &&
            titleBackPanel.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            return titleBackPanel;

        // UIManager may persist across scenes, so never retain a panel from an old scene.
        titleBackPanel = null;
        TitleManager title = FindFirstObjectByType<TitleManager>(FindObjectsInactive.Include);
        if (title == null)
            return null;

        foreach (GameObject root in title.gameObject.scene.GetRootGameObjects())
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == "BackPanel" && child.GetComponent<RectTransform>() != null)
                {
                    titleBackPanel = child.gameObject;
                    return titleBackPanel;
                }
            }
        }
        return null;
    }

    private void RefreshTitleBackPanel(bool opening = false)
    {
        GameObject background = ResolveTitleBackPanel();
        if (background == null)
            return;

        bool visible = opening || IsOptionPanelOpen || IsRecordPanelOpen;
        background.SetActive(visible);
        if (visible)
        {
            // Keep the background behind both modal prefabs in the same Canvas.
            if (optionPanelInstance != null && optionPanelInstance.activeInHierarchy &&
                optionPanelInstance.transform.parent == background.transform.parent)
                optionPanelInstance.transform.SetAsLastSibling();
            if (recordPanelInstance != null && recordPanelInstance.activeInHierarchy &&
                recordPanelInstance.transform.parent == background.transform.parent)
                recordPanelInstance.transform.SetAsLastSibling();
        }
    }

    public void ShowOption()
    {
        if (optionPanelInstance == null)
            CreateOptionPanel();

        if (optionPanelInstance != null)
        {
            if (optionPanelTransition == null)
            {
                optionPanelTransition = optionPanelInstance.GetComponent<OptionPanelTransition>();
                if (optionPanelTransition == null)
                    optionPanelTransition = optionPanelInstance.AddComponent<OptionPanelTransition>();
            }

            ApplyOptionPanelScale();
            RefreshTitleBackPanel(true);
            optionPanelInstance.SetActive(true);
            BringOptionPanelToFront();
            optionPanelTransition.PlayOpen();
        }
    }

    public void ShowRecord()
    {
        ShowRecord(false);
    }

    /// <summary>
    /// 도감을 엽니다. revealAll이 true이면 저장 데이터는 변경하지 않고
    /// 현재 도감 UI에서만 모든 기억/파편/유물/재료를 공개합니다.
    /// </summary>
    public void ShowRecord(bool revealAll)
    {
        if (recordPanelInstance == null)
            CreateRecordPanel();

        if (recordPanelInstance == null)
        {
            Debug.LogWarning("[UIManager] RecordPanelPrefab is not assigned.");
            return;
        }

        if (recordPanelUI == null)
            recordPanelUI = recordPanelInstance.GetComponent<RecordPanelUI>();

        if (recordPanelTransition == null)
        {
            recordPanelTransition = recordPanelInstance.GetComponent<OptionPanelTransition>();
            if (recordPanelTransition == null)
                recordPanelTransition = recordPanelInstance.AddComponent<OptionPanelTransition>();
        }

        if (recordPanelUI != null)
            recordPanelUI.SetDebugRevealAll(revealAll);

        RefreshTitleBackPanel(true);
        recordPanelInstance.transform.localScale = Vector3.one;
        recordPanelInstance.SetActive(true);
        BringRecordPanelToFront();
        recordPanelTransition.PlayOpen();
    }

    public void HideRecord()
    {
        HideRecord(false);
    }

    /// <summary>
    /// 도감 패널을 닫습니다.
    /// immediate가 true이면 전환 연출 없이 즉시 비활성화합니다.
    /// </summary>
    public void HideRecord(bool immediate)
    {
        if (recordPanelInstance == null)
            return;

        ClearSelectedObjectIfInsideRecordPanel();

        if (immediate || !recordPanelInstance.activeInHierarchy)
        {
            recordPanelInstance.SetActive(false);
            RefreshTitleBackPanel();
            return;
        }

        if (recordPanelTransition == null)
        {
            recordPanelTransition = recordPanelInstance.GetComponent<OptionPanelTransition>();
            if (recordPanelTransition == null)
                recordPanelTransition = recordPanelInstance.AddComponent<OptionPanelTransition>();
        }

        recordPanelTransition.PlayClose(() =>
        {
            if (recordPanelInstance != null)
                recordPanelInstance.SetActive(false);
            RefreshTitleBackPanel();
        });
    }

    public bool TryHideRecordIfOpen(bool closedByEscape = false)
    {
        if (!IsRecordPanelOpen)
            return false;

        // Info가 열려 있으면 Record 자체는 유지하고 Info만 먼저 닫습니다.
        RecordPanelUI recordPanel = recordPanelInstance.GetComponent<RecordPanelUI>();
        if (recordPanel != null && recordPanel.TryCloseInfo())
        {
            lastRecordClosedByEscapeFrame = Time.frameCount;
            return true;
        }

        HideRecord();

        if (closedByEscape)
            lastRecordClosedByEscapeFrame = Time.frameCount;

        return true;
    }

    private void ApplyOptionPanelScale()
    {
        if (optionPanelInstance == null)
            return;

        Transform optionTransform = optionPanelInstance.transform;
        optionTransform.localScale = OptionPanelDefaultScale;

        if (optionTransform is RectTransform rectTransform)
            rectTransform.localScale = OptionPanelDefaultScale;
    }

    private void BringOptionPanelToFront()
    {
        if (!bringOptionPanelToFront)
            return;

        if (optionPanelInstance == null)
            return;

        optionPanelInstance.transform.SetAsLastSibling();

        if (!overrideOptionPanelSorting)
            return;

        Canvas optionCanvas = optionPanelInstance.GetComponent<Canvas>();
        if (optionCanvas == null)
            optionCanvas = optionPanelInstance.AddComponent<Canvas>();

        optionCanvas.overrideSorting = true;
        int desiredOrder = GetHighestCanvasSortingOrder(optionCanvas) + Mathf.Max(1, optionPanelSortingOrderOffset);
        optionCanvas.sortingOrder = Mathf.Min(desiredOrder, ModalPanelSortingOrderCeiling);

        if (optionPanelInstance.GetComponent<GraphicRaycaster>() == null)
            optionPanelInstance.AddComponent<GraphicRaycaster>();
    }

    private void BringRecordPanelToFront()
    {
        if (!bringRecordPanelToFront || recordPanelInstance == null)
            return;

        recordPanelInstance.transform.SetAsLastSibling();

        if (!overrideRecordPanelSorting)
            return;

        Canvas recordCanvas = recordPanelInstance.GetComponent<Canvas>();
        if (recordCanvas == null)
            recordCanvas = recordPanelInstance.AddComponent<Canvas>();

        recordCanvas.overrideSorting = true;
        int desiredOrder = GetHighestCanvasSortingOrder(recordCanvas) + Mathf.Max(1, recordPanelSortingOrderOffset);
        recordCanvas.sortingOrder = Mathf.Min(desiredOrder, ModalPanelSortingOrderCeiling);

        if (recordPanelInstance.GetComponent<GraphicRaycaster>() == null)
            recordPanelInstance.AddComponent<GraphicRaycaster>();
    }

    private void BringConfirmDialogToFront()
    {
        if (!bringConfirmDialogToFront)
            return;

        if (confirmDialogInstance == null)
            return;

        confirmDialogInstance.transform.SetAsLastSibling();

        if (!overrideConfirmDialogSorting)
            return;

        Canvas confirmCanvas = confirmDialogInstance.GetComponent<Canvas>();
        if (confirmCanvas == null)
            confirmCanvas = confirmDialogInstance.AddComponent<Canvas>();

        confirmCanvas.overrideSorting = true;
        int desiredOrder = GetHighestCanvasSortingOrder(confirmCanvas) + Mathf.Max(1, confirmDialogSortingOrderOffset);

        // 튜토리얼 전투 중에는 FocusOverlay가 사라진 단계에서도 확인창이 클릭 가능해야 합니다.
        // IsInputRestricted만 검사하면 대화/연출 전환 구간에서 정렬이 되돌아갈 수 있습니다.
        bool tutorialBattle = IsTutorialBattleRun() || BattleFirstTutorialController.IsInputRestricted;
        if (tutorialBattle)
        {
            // 별도 Screen Space Overlay Canvas를 사용하여 TutorialMode보다 항상 위에 표시합니다.
            // 기존 MainCanvas의 정렬이나 다른 확인창의 표시 방식은 변경하지 않습니다.
            confirmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            confirmCanvas.sortingLayerID = SortingLayer.NameToID("Default");
            confirmCanvas.sortingOrder = 32760;
        }
        else
        {
            confirmCanvas.sortingOrder = Mathf.Min(desiredOrder, ModalPanelSortingOrderCeiling);
        }

        GraphicRaycaster confirmRaycaster = confirmDialogInstance.GetComponent<GraphicRaycaster>();
        if (confirmRaycaster == null)
            confirmRaycaster = confirmDialogInstance.AddComponent<GraphicRaycaster>();
        confirmRaycaster.enabled = true;
        confirmRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        if (tutorialBattle)
        {
            CanvasGroup dialogGroup = confirmDialogInstance.GetComponent<CanvasGroup>();
            if (dialogGroup == null)
                dialogGroup = confirmDialogInstance.AddComponent<CanvasGroup>();
            dialogGroup.alpha = 1f;
            dialogGroup.interactable = true;
            dialogGroup.blocksRaycasts = true;
            dialogGroup.ignoreParentGroups = true;

            // 선택 버튼이 포커스 차단 레이어보다 우선 Raycast 대상이 되도록 보장합니다.
            foreach (Button button in confirmDialogInstance.GetComponentsInChildren<Button>(true))
                button.interactable = true;
        }

    }

    private int GetHighestCanvasSortingOrder(Canvas optionCanvas)
    {
        int highestOrder = mainCanvas != null ? mainCanvas.sortingOrder : 0;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
                continue;

            if (canvas == optionCanvas)
                continue;

            if (!canvas.gameObject.activeInHierarchy)
                continue;

            if (canvas.sortingOrder > highestOrder)
                highestOrder = canvas.sortingOrder;
        }

        return highestOrder;
    }

    public void ShowGiveUpConfirm()
    {
        ShowBattleGiveUpConfirm();
    }

    public void ShowBattleGiveUpConfirm()
    {
        if (IsLobbyScene())
            return;

        ShowConfirmDialog(
            GetGiveUpConfirmationMessage(),
            GameLocalization.Get(LocalizationKeys.Dialog.Yes),
            GameLocalization.Get(LocalizationKeys.Dialog.No),
            OnConfirmGiveUpToLobby,
            HideConfirmDialog);
    }

    // 확인창 Slogan은 동적 문구입니다. 영어 테이블 항목이 아직 반영되지 않은 경우에도
    // 실제 확인 문구가 Untranslated로 표시되지 않도록 언어별 원문을 사용합니다.
    private static string GetGiveUpConfirmationMessage()
    {
        string localeCode = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code;
        if (!string.IsNullOrEmpty(localeCode) && localeCode.StartsWith("en", System.StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get("ui.check.slogan", "Are you sure you want to give up?");
        if (!string.IsNullOrEmpty(localeCode) && localeCode.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase))
            return GameLocalization.Get("ui.check.slogan", "정말로 포기하시겠습니까?");
        return GameLocalization.Get("ui.check.slogan");
    }

    public void ShowQuitConfirm()
    {
        ShowConfirmDialog(
            GameLocalization.Get(LocalizationKeys.Dialog.QuitConfirm),
            OnConfirmQuitGame,
            HideConfirmDialog);
    }

    public void ShowConfirmDialog(string message, System.Action yesAction, System.Action noAction)
    {
        ShowConfirmDialog(
            message,
            GameLocalization.Get(LocalizationKeys.Dialog.Yes),
            GameLocalization.Get(LocalizationKeys.Dialog.No),
            yesAction,
            noAction);
    }

    public void ShowConfirmDialog(
        string message,
        string yesText,
        string noText,
        System.Action yesAction,
        System.Action noAction)
    {
        if (confirmDialogInstance == null)
            CreateConfirmDialog();

        if (confirmDialogInstance == null || confirmDialogUI == null)
        {
            Debug.LogWarning("[UIManager] ConfirmDialogPrefab is not assigned.");
            return;
        }

        if (TutorialBattleSequence.IsPresentationRunning)
            TutorialBattleSequence.NotifyModalInteraction();

        confirmDialogUI.Configure(
            message,
            string.IsNullOrWhiteSpace(yesText) ? GameLocalization.Get(LocalizationKeys.Dialog.Yes) : yesText,
            string.IsNullOrWhiteSpace(noText) ? GameLocalization.Get(LocalizationKeys.Dialog.No) : noText,
            yesAction,
            noAction);
        confirmDialogInstance.SetActive(true);
        BringConfirmDialogToFront();
    }

    public void HideConfirmDialog()
    {
        if (confirmDialogInstance == null)
            return;

        if (TutorialBattleSequence.IsPresentationRunning)
            TutorialBattleSequence.NotifyModalInteraction();

        if (confirmDialogUI != null)
            confirmDialogUI.ClearButtonAnimationState();

        ClearSelectedObjectIfInsideConfirmDialog();
        confirmDialogInstance.SetActive(false);
    }

    public bool TryHideConfirmDialogIfOpen(bool closedByEscape = false)
    {
        if (!IsConfirmDialogOpen)
            return false;

        HideConfirmDialog();

        if (closedByEscape)
            lastConfirmClosedByEscapeFrame = Time.frameCount;

        return true;
    }

    private async void OnConfirmGiveUpToLobby()
    {
        HideConfirmDialog();
        HideRecord(true);
        HideOption(true);
        UIPanelButton.CloseCurrentOpenedPanel();
        Time.timeScale = 1f;
        // The abandon service restores the pre-run HP immediately. Freeze the battle
        // HUD first so the defeated party does not visibly heal during scene loading.
        FreezeBattleHudBeforeAbandon();
        AbandonCurrentBattleRunIfPossible();

        if (GameManager.Instance != null && GameManager.Instance.StateMachine != null)
        {
            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
            return;
        }

        if (SceneFlowManager.Instance != null)
        {
            await SceneFlowManager.Instance.LoadSceneAsync(SceneName.Lobby);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Lobby);
    }

    public async void SaveAndReturnToTitle()
    {
        HideConfirmDialog();
        HideRecord(true);
        HideOption(true);
        UIPanelButton.CloseCurrentOpenedPanel();
        Time.timeScale = 1f;


        if (GameManager.Instance != null && GameManager.Instance.StateMachine != null)
        {
            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Title);
            await TitleManager.RefreshRunButtonsAfterSceneReadyAsync();
            return;
        }

        if (SceneFlowManager.Instance != null)
        {
            await SceneFlowManager.Instance.LoadSceneAsync(SceneName.Title);
            await TitleManager.RefreshRunButtonsAfterSceneReadyAsync();
            PlayTitleBgmIfPossible();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Title);
        await TitleManager.RefreshRunButtonsAfterSceneReadyAsync();
        PlayTitleBgmIfPossible();
    }

    private static void FreezeBattleHudBeforeAbandon()
    {
        // Keep the last rendered health values on screen until the lobby loads.
        // Disabling these presenters stops HP polling and fill interpolation;
        // it does not change character data or affect the lobby snapshot restore.
        BattleCharacterPanelUI[] panels = UnityEngine.Object.FindObjectsByType<BattleCharacterPanelUI>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (BattleCharacterPanelUI panel in panels)
        {
            if (panel != null)
                panel.enabled = false;
        }

        BattleCharacterHUDController[] worldHuds = UnityEngine.Object.FindObjectsByType<BattleCharacterHUDController>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (BattleCharacterHUDController hud in worldHuds)
        {
            if (hud != null)
                hud.enabled = false;
        }
    }

    private void AbandonCurrentBattleRunIfPossible()
    {
        if (DataManager.Instance == null)
            return;

        BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);

        // 탐사를 포기한 직후 초기화된 상태를 즉시 저장합니다.
        // 로비로 돌아온 뒤 강제 종료해도 이전 배틀 이어하기 세이브가 남지 않도록 합니다.
        SaveSystem.Instance?.SaveCurrentProgress();
    }

    private void PlayTitleBgmIfPossible()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayBgm(BgmState.TitleMain);
    }

    private void OnConfirmQuitGame()
    {
        HideConfirmDialog();
        HideOption();
        UIPanelButton.CloseCurrentOpenedPanel();
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void HideOption()
    {
        HideOption(false);
    }

    /// <summary>
    /// 설정 패널을 닫습니다.
    /// immediate가 true이면 연출 없이 즉시 비활성화합니다.
    /// </summary>
    public void HideOption(bool immediate)
    {
        if (optionPanelInstance == null)
            return;

        ClearSelectedObjectIfInsideOptionPanel();

        if (immediate || !optionPanelInstance.activeInHierarchy)
        {
            optionPanelInstance.SetActive(false);
            RefreshTitleBackPanel();
            return;
        }

        if (optionPanelTransition == null)
        {
            optionPanelTransition = optionPanelInstance.GetComponent<OptionPanelTransition>();
            if (optionPanelTransition == null)
                optionPanelTransition = optionPanelInstance.AddComponent<OptionPanelTransition>();
        }

        optionPanelTransition.PlayClose(() =>
        {
            if (optionPanelInstance != null)
                optionPanelInstance.SetActive(false);
            RefreshTitleBackPanel();
        });
    }

    public bool TryHideOptionIfOpen(bool closedByEscape = false)
    {
        if (!IsOptionPanelOpen)
            return false;

        HideOption();

        if (closedByEscape)
            lastOptionClosedByEscapeFrame = Time.frameCount;

        return true;
    }

    public void HideAll()
    {
        HideRecord(true);
        HideOption(true);
        HideConfirmDialog();
    }

    private void ClearSelectedObjectIfInsideOptionPanel()
    {
        if (optionPanelInstance == null)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return;

        if (eventSystem.currentSelectedGameObject.transform.IsChildOf(optionPanelInstance.transform))
            eventSystem.SetSelectedGameObject(null);
    }

    private void ClearSelectedObjectIfInsideRecordPanel()
    {
        if (recordPanelInstance == null)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return;

        if (eventSystem.currentSelectedGameObject.transform.IsChildOf(recordPanelInstance.transform))
            eventSystem.SetSelectedGameObject(null);
    }

    private void ClearSelectedObjectIfInsideConfirmDialog()
    {
        if (confirmDialogInstance == null)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return;

        if (eventSystem.currentSelectedGameObject.transform.IsChildOf(confirmDialogInstance.transform))
            eventSystem.SetSelectedGameObject(null);
    }


    private void UpdateMenuButtonRuntimeState()
    {
        GameObject menuPanel = UIPanelButton.FindMenuPanelInScene();
        UpdateMenuCameraPause(menuPanel);

        if (menuPanel != cachedMenuPanel)
        {
            if (cachedRecordButton != null)
                cachedRecordButton.onClick.RemoveListener(ShowRecord);

            cachedMenuPanel = menuPanel;
            EnsureMenuPanelTextRefresher(cachedMenuPanel);
            cachedGiveUpButton = FindMenuButton(menuPanel, "Giveup");
            cachedRecordButton = FindMenuButton(menuPanel, "Record");
            cachedQuitButton = FindMenuButton(menuPanel, "Quit");
            cachedQuitText = FindMenuText(cachedQuitButton, "quit_Text");

            if (cachedRecordButton != null)
            {
                cachedRecordButton.onClick.RemoveListener(ShowRecord);
                cachedRecordButton.onClick.AddListener(ShowRecord);
            }
        }

        bool isLobbyScene = IsLobbyScene();
        bool isTutorialBattle = IsTutorialBattleRun();

        if (cachedGiveUpButton != null)
        {
            bool canUseGiveUp = !isLobbyScene && !isTutorialBattle;
            cachedGiveUpButton.gameObject.SetActive(canUseGiveUp);
            cachedGiveUpButton.interactable = canUseGiveUp;
        }

        if (cachedQuitText == null && cachedQuitButton != null)
            cachedQuitText = FindMenuText(cachedQuitButton, "quit_Text");

        if (cachedQuitText != null)
        {
            cachedQuitText.text = isLobbyScene
                ? GameLocalization.Get(LobbyQuitButtonLocalizationKey)
                : GameLocalization.Get(BattleQuitButtonLocalizationKey);
            RefreshTmpText(cachedQuitText);
        }
    }

    private static bool IsTutorialBattleRun()
    {
        DataManager dataManager = DataManager.Instance;
        BattleRuntimeData battleRuntime = dataManager?.BattleRuntimeStore?.Get();
        return battleRuntime?.IsTutorialBattle == true;
    }

    private static void EnsureMenuPanelTextRefresher(GameObject menuPanel)
    {
        if (menuPanel == null)
            return;

        if (menuPanel.GetComponent<MenuPanelTextRefresher>() == null)
            menuPanel.AddComponent<MenuPanelTextRefresher>();
    }

    private static void RefreshTmpText(TMP_Text text)
    {
        if (text == null)
            return;

        if (text.font != null && text.font.material != null)
            text.fontSharedMaterial = text.font.material;

        text.UpdateMeshPadding();
        text.SetAllDirty();
        text.ForceMeshUpdate(true, true);
    }

    private void UpdateMenuCameraPause(GameObject menuPanel)
    {
        bool shouldPause = menuPanel != null && menuPanel.activeInHierarchy;

        if (shouldPause == menuCameraPauseActive)
            return;

        if (shouldPause)
        {
            CameraMouseParallaxController.BeginUiPanelPause();
            menuCameraPauseActive = true;
            return;
        }

        ReleaseMenuCameraPause();
    }

    private void ReleaseMenuCameraPause()
    {
        if (!menuCameraPauseActive)
            return;

        CameraMouseParallaxController.EndUiPanelPause();
        menuCameraPauseActive = false;
    }

    private static TMP_Text FindMenuText(Button button, string objectName)
    {
        if (button == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && string.Equals(text.gameObject.name, objectName, System.StringComparison.Ordinal))
                return text;
        }

        return null;
    }

    private static Button FindMenuButton(GameObject menuPanel, string objectName)
    {
        if (menuPanel == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        Button[] buttons = menuPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && string.Equals(button.gameObject.name, objectName, System.StringComparison.Ordinal))
                return button;
        }

        return null;
    }

    private static bool IsLobbyScene()
    {
        return string.Equals(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            SceneName.Lobby,
            System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTitleScene()
    {
        return string.Equals(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            SceneName.Title,
            System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool WasRightMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(1);
#else
        return false;
#endif
    }

    private bool WasEscapePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private bool IsTypingInputFieldSelected()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return false;

        GameObject selectedObject = eventSystem.currentSelectedGameObject;

        if (selectedObject.GetComponent<TMP_InputField>() != null)
            return true;

        return selectedObject.GetComponent<InputField>() != null;
    }
}
