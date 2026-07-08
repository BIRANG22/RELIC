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
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject optionPanelPrefab;
    [SerializeField] private GameObject confirmDialogPrefab;

    [Header("Option Panel Sorting")]
    [SerializeField] private bool bringOptionPanelToFront = true;
    [SerializeField] private bool overrideOptionPanelSorting = true;
    [SerializeField] private int optionPanelSortingOrderOffset = 10;

    [Header("Confirm Dialog Sorting")]
    [SerializeField] private bool bringConfirmDialogToFront = true;
    [SerializeField] private bool overrideConfirmDialogSorting = true;
    [SerializeField] private int confirmDialogSortingOrderOffset = 20;

    [Header("Confirm Dialog Text")]
    [SerializeField] private string giveUpConfirmMessage = "타이틀로 돌아가겠습니까?";
    [SerializeField] private string quitConfirmMessage = "게임을 종료하겠습니까?";
    [SerializeField] private string confirmYesText = "예";
    [SerializeField] private string confirmNoText = "아니오";

    [Header("Input")]
    [SerializeField] private bool closeOptionPanelWithEscape = true;
    [SerializeField] private bool closeConfirmDialogWithEscape = true;

    private static readonly Vector3 OptionPanelDefaultScale = Vector3.one;

    private static int lastOptionClosedByEscapeFrame = -1;
    private static int lastConfirmClosedByEscapeFrame = -1;

    private GameObject optionPanelInstance;
    private GameObject confirmDialogInstance;
    private BootstrapConfirmDialogUI confirmDialogUI;

    public static bool WasOptionPanelClosedByEscapeThisFrame => lastOptionClosedByEscapeFrame == Time.frameCount;
    public static bool WasConfirmDialogClosedByEscapeThisFrame => lastConfirmClosedByEscapeFrame == Time.frameCount;

    public bool IsOptionPanelOpen => optionPanelInstance != null && optionPanelInstance.activeInHierarchy;
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
        CreateConfirmDialog();
        HideAll();
    }

    private void Update()
    {
        if (IsTypingInputFieldSelected())
            return;

        if (!WasEscapePressedThisFrame())
            return;

        if (closeConfirmDialogWithEscape && TryHideConfirmDialogIfOpen(true))
            return;

        if (closeOptionPanelWithEscape)
            TryHideOptionIfOpen(true);
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
            ApplyOptionPanelScale();
            optionPanelInstance.SetActive(true);
            BringOptionPanelToFront();
        }
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
        optionCanvas.sortingOrder = GetHighestCanvasSortingOrder(optionCanvas) + Mathf.Max(1, optionPanelSortingOrderOffset);

        if (optionPanelInstance.GetComponent<GraphicRaycaster>() == null)
            optionPanelInstance.AddComponent<GraphicRaycaster>();
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
        confirmCanvas.sortingOrder = GetHighestCanvasSortingOrder(confirmCanvas) + Mathf.Max(1, confirmDialogSortingOrderOffset);

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
        ShowConfirmDialog(giveUpConfirmMessage, OnConfirmGiveUpToTitle, HideConfirmDialog);
    }

    public void ShowQuitConfirm()
    {
        ShowConfirmDialog(quitConfirmMessage, OnConfirmQuitGame, HideConfirmDialog);
    }

    public void ShowConfirmDialog(string message, System.Action yesAction, System.Action noAction)
    {
        if (confirmDialogInstance == null)
            CreateConfirmDialog();

        if (confirmDialogInstance == null || confirmDialogUI == null)
        {
            Debug.LogWarning("[UIManager] ConfirmDialogPrefab is not assigned.");
            return;
        }

        confirmDialogUI.Configure(message, confirmYesText, confirmNoText, yesAction, noAction);
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

    private async void OnConfirmGiveUpToTitle()
    {
        HideConfirmDialog();
        HideOption();
        UIPanelButton.CloseCurrentOpenedPanel();
        Time.timeScale = 1f;
        AbandonCurrentBattleRunIfPossible();

        if (GameManager.Instance != null && GameManager.Instance.StateMachine != null)
        {
            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Title);
            return;
        }

        if (SceneFlowManager.Instance != null)
        {
            await SceneFlowManager.Instance.LoadSceneAsync(SceneName.Title);
            PlayTitleBgmIfPossible();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Title);
        PlayTitleBgmIfPossible();
    }

    private void AbandonCurrentBattleRunIfPossible()
    {
        if (DataManager.Instance != null)
            BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();
    }

    private void PlayTitleBgmIfPossible()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayBgm(BgmType.Title);
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
        if (optionPanelInstance == null)
            return;

        ClearSelectedObjectIfInsideOptionPanel();
        optionPanelInstance.SetActive(false);
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
        HideOption();
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
