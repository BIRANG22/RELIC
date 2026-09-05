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

    [Header("Menu Button Text")]
    [SerializeField] private string lobbyQuitButtonText = "타이틀로";
    [SerializeField] private string battleQuitButtonText = "저장 후 종료";

    [Header("Confirm Dialog Text")]
    [SerializeField] private string giveUpConfirmMessage = "정말 포기하시겠습니까?";
    [SerializeField] private string quitConfirmMessage = "게임을 종료하겠습니까?";
    [SerializeField] private string confirmYesText = "예";
    [SerializeField] private string confirmNoText = "아니오";

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

        if (!WasEscapePressedThisFrame())
            return;

        // 튜토리얼이 열려 있으면 ESC는 튜토리얼만 닫고 같은 프레임의 다른 UI에는 전달하지 않습니다.
        if (BattleFirstTutorialController.TryHandleEscapeIfOpen())
            return;

        if (closeConfirmDialogWithEscape && TryHideConfirmDialogIfOpen(true))
            return;

        if (closeRecordPanelWithEscape && TryHideRecordIfOpen(true))
            return;

        if (closeOptionPanelWithEscape && TryHideOptionIfOpen(true))
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
        });
    }

    public bool TryHideRecordIfOpen(bool closedByEscape = false)
    {
        if (!IsRecordPanelOpen)
            return false;

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
        confirmCanvas.sortingOrder = Mathf.Min(desiredOrder, ModalPanelSortingOrderCeiling);

        if (confirmDialogInstance.GetComponent<GraphicRaycaster>() == null)
            confirmDialogInstance.AddComponent<GraphicRaycaster>();
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
            giveUpConfirmMessage,
            confirmYesText,
            confirmNoText,
            OnConfirmGiveUpToLobby,
            HideConfirmDialog);
    }

    public void ShowQuitConfirm()
    {
        ShowConfirmDialog(
            GameLocalization.Get("common.confirm_quit_game", quitConfirmMessage),
            OnConfirmQuitGame,
            HideConfirmDialog);
    }

    public void ShowConfirmDialog(string message, System.Action yesAction, System.Action noAction)
    {
        ShowConfirmDialog(
            message,
            GameLocalization.Get("common.yes", confirmYesText),
            GameLocalization.Get("common.no", confirmNoText),
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

        confirmDialogUI.Configure(
            message,
            string.IsNullOrWhiteSpace(yesText) ? GameLocalization.Get("common.yes", confirmYesText) : yesText,
            string.IsNullOrWhiteSpace(noText) ? GameLocalization.Get("common.no", confirmNoText) : noText,
            yesAction,
            noAction);
        confirmDialogInstance.SetActive(true);
        BringConfirmDialogToFront();
    }

    public void HideConfirmDialog()
    {
        if (confirmDialogInstance == null)
            return;

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

        if (SaveSystem.Instance != null)
        {
            if (!SaveSystem.Instance.SaveCurrentProgress())
                Debug.LogWarning("[UIManager] 현재 진행상황 저장에 실패했습니다. 타이틀로 이동합니다.");
        }
        else
        {
            Debug.LogWarning("[UIManager] SaveSystem.Instance를 찾지 못했습니다. 타이틀로 이동합니다.");
        }

        if (GameManager.Instance != null && GameManager.Instance.StateMachine != null)
        {
            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Title);
            TitleManager.RefreshRunButtonsInScene();
            return;
        }

        if (SceneFlowManager.Instance != null)
        {
            await SceneFlowManager.Instance.LoadSceneAsync(SceneName.Title);
            TitleManager.RefreshRunButtonsInScene();
            PlayTitleBgmIfPossible();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Title);
        TitleManager.RefreshRunButtonsInScene();
        PlayTitleBgmIfPossible();
    }

    private void AbandonCurrentBattleRunIfPossible()
    {
        if (DataManager.Instance != null)
            BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);

    }

    private void PlayTitleBgmIfPossible()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayBgm(AudioIds.Bgm.Title);
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

        if (cachedGiveUpButton != null)
        {
            cachedGiveUpButton.gameObject.SetActive(!isLobbyScene);
            cachedGiveUpButton.interactable = !isLobbyScene;
        }

        if (cachedQuitText == null && cachedQuitButton != null)
            cachedQuitText = FindMenuText(cachedQuitButton, "quit_Text");

        if (cachedQuitText != null)
        {
            cachedQuitText.text = isLobbyScene ? lobbyQuitButtonText : battleQuitButtonText;
            RefreshTmpText(cachedQuitText);
        }
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
