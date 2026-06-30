#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleMenuEscapeInputController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool enableEscapeInput = true;

    [Header("Menu Button")]
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuButtonObjectName = "MenuButton";
    [SerializeField] private bool autoFindMenuButton = true;

    [Header("Close")]
    [SerializeField] private bool closeOpenedPanelFirst = true;

    [Header("Pause")]
    [SerializeField] private bool pauseGameWhenMenuPanelOpen = true;
    [SerializeField] private float pauseTimeScale = 0f;
    [SerializeField] private float fallbackResumeTimeScale = 1f;

    private bool isPauseApplied;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
        FindMenuButtonIfNeeded();
    }

    private void OnEnable()
    {
        FindMenuButtonIfNeeded();
        RefreshPauseState();
    }

    private void OnDisable()
    {
        ReleasePauseIfNeeded();
    }

    private void OnDestroy()
    {
        ReleasePauseIfNeeded();
    }

    private void Update()
    {
        if (enableEscapeInput && WasEscapePressedThisFrame() && !IsTypingInputFieldSelected())
        {
            if (UIManager.WasConfirmDialogClosedByEscapeThisFrame || UIManager.WasOptionPanelClosedByEscapeThisFrame)
                return;

            if (UIManager.Instance != null && UIManager.Instance.TryHideConfirmDialogIfOpen(true))
                return;

            if (UIManager.Instance != null && UIManager.Instance.TryHideOptionIfOpen(true))
                return;

            if (!closeOpenedPanelFirst || !UIPanelButton.TryCloseCurrentOpenedPanel())
                ClickMenuButton();
        }

        RefreshPauseState();
    }

    private void RefreshPauseState()
    {
        bool shouldPause = pauseGameWhenMenuPanelOpen && UIPanelButton.HasCurrentOpenedPanel;

        if (shouldPause)
        {
            ApplyPauseIfNeeded();
            Time.timeScale = pauseTimeScale;
            return;
        }

        ReleasePauseIfNeeded();
    }

    private void ApplyPauseIfNeeded()
    {
        if (isPauseApplied)
            return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = pauseTimeScale;
        isPauseApplied = true;
    }

    private void ReleasePauseIfNeeded()
    {
        if (!isPauseApplied)
            return;

        float resumeTimeScale = timeScaleBeforePause > 0.0001f
            ? timeScaleBeforePause
            : fallbackResumeTimeScale;

        Time.timeScale = resumeTimeScale;
        isPauseApplied = false;
    }

    private void ClickMenuButton()
    {
        FindMenuButtonIfNeeded();

        if (menuButton == null)
        {
            Debug.LogWarning("[BattleMenuEscapeInputController] MenuButton을 찾지 못했습니다.");
            return;
        }

        if (!menuButton.gameObject.activeInHierarchy || !menuButton.interactable)
            return;

        ExecutePointerClick(menuButton.gameObject);
    }

    private void ExecutePointerClick(GameObject target)
    {
        if (target == null)
            return;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            menuButton.onClick.Invoke();
            return;
        }

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left,
            position = GetPointerPosition()
        };

        ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
    }

    private void FindMenuButtonIfNeeded()
    {
        if (!autoFindMenuButton || menuButton != null)
            return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            if (button.gameObject.name != menuButtonObjectName)
                continue;

            menuButton = button;
            return;
        }
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

    private Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
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
