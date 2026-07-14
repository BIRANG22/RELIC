#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;
using System.Collections.Generic;
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

    [Header("Menu Panel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private string menuPanelObjectName = "MenuPanel";
    [SerializeField] private bool autoFindMenuPanel = true;
    [SerializeField] private int menuPanelSortingOrder = 10000;
    [SerializeField] private bool blockOtherButtonsWhenMenuPanelOpen = true;


    [Header("Inventory Panel Toggle")]
    [SerializeField] private bool enableInventoryControlKeyToggle = true;

    [Header("Inventory Panel Close")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private RectTransform inventoryPanelRect;
    [SerializeField] private string inventoryPanelObjectName = "InventoryPanel";
    [SerializeField] private bool autoFindInventoryPanel = true;
    [SerializeField] private float inventoryClosedY = 1080f;
    [SerializeField] private float inventoryOpenedY = 0f;
    [SerializeField] private float inventoryCloseDuration = 0.2f;
    [SerializeField] private float inventoryOpenCheckTolerance = 5f;

    [Header("Pause")]
    [SerializeField] private bool pauseGameWhenMenuPanelOpen = true;
    [SerializeField] private float pauseTimeScale = 0f;
    [SerializeField] private float fallbackResumeTimeScale = 1f;

    private bool isPauseApplied;
    private bool isOtherButtonBlockApplied;
    private float timeScaleBeforePause = 1f;
    private Coroutine inventoryMoveCoroutine;
    private readonly List<SelectableState> blockedSelectableStates = new();

    private struct SelectableState
    {
        public Selectable Selectable;
        public bool WasInteractable;
    }

    private void Awake()
    {
        FindMenuButtonIfNeeded();
        FindMenuPanelIfNeeded();
        FindInventoryPanelIfNeeded();
    }

    private void OnEnable()
    {
        FindMenuButtonIfNeeded();
        FindMenuPanelIfNeeded();
        FindInventoryPanelIfNeeded();
        RefreshPauseState();
    }

    private void OnDisable()
    {
        ReleaseOtherButtonBlockIfNeeded();
        ReleasePauseIfNeeded();
    }

    private void OnDestroy()
    {
        ReleaseOtherButtonBlockIfNeeded();
        ReleasePauseIfNeeded();
    }

    private void Update()
    {
        if (enableInventoryControlKeyToggle && WasControlPressedThisFrame() && !IsTypingInputFieldSelected())
        {
            if (IsMenuPanelOpen())
                return;

            ToggleInventoryPanelByControlKey();
            RefreshPauseState();
            return;
        }

        if (enableEscapeInput && WasEscapePressedThisFrame() && !IsTypingInputFieldSelected())
        {
            if (UIManager.WasConfirmDialogClosedByEscapeThisFrame || UIManager.WasOptionPanelClosedByEscapeThisFrame)
                return;

            if (UIManager.Instance != null && UIManager.Instance.TryHideConfirmDialogIfOpen(true))
                return;

            if (UIManager.Instance != null && UIManager.Instance.TryHideOptionIfOpen(true))
                return;

            ClickMenuButton();
        }

        RefreshPauseState();
    }

    private void RefreshPauseState()
    {
        bool menuPanelOpen = IsMenuPanelOpen();
        bool shouldPause = pauseGameWhenMenuPanelOpen && menuPanelOpen;

        if (menuPanelOpen)
        {
            BringMenuPanelToFront();
            ApplyOtherButtonBlockIfNeeded();
        }
        else
        {
            ReleaseOtherButtonBlockIfNeeded();
        }

        if (shouldPause)
        {
            ApplyPauseIfNeeded();
            Time.timeScale = pauseTimeScale;
            return;
        }

        ReleasePauseIfNeeded();
    }

    private bool IsMenuPanelOpen()
    {
        FindMenuPanelIfNeeded();
        return menuPanel != null && menuPanel.activeInHierarchy;
    }

    private void BringMenuPanelToFront()
    {
        FindMenuPanelIfNeeded();

        if (menuPanel == null)
            return;

        menuPanel.transform.SetAsLastSibling();

        Canvas canvas = menuPanel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = menuPanel.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = menuPanelSortingOrder;

        if (menuPanel.GetComponent<GraphicRaycaster>() == null)
            menuPanel.AddComponent<GraphicRaycaster>();
    }

    private void ApplyOtherButtonBlockIfNeeded()
    {
        if (!blockOtherButtonsWhenMenuPanelOpen)
            return;

        if (isOtherButtonBlockApplied)
            return;

        blockedSelectableStates.Clear();

        Selectable[] selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];

            if (selectable == null)
                continue;

            if (ShouldAllowSelectableWhileMenuPanelOpen(selectable))
                continue;

            blockedSelectableStates.Add(new SelectableState
            {
                Selectable = selectable,
                WasInteractable = selectable.interactable
            });

            selectable.interactable = false;
        }

        isOtherButtonBlockApplied = true;
    }

    private void ReleaseOtherButtonBlockIfNeeded()
    {
        if (!isOtherButtonBlockApplied)
            return;

        for (int i = 0; i < blockedSelectableStates.Count; i++)
        {
            SelectableState state = blockedSelectableStates[i];

            if (state.Selectable == null)
                continue;

            state.Selectable.interactable = state.WasInteractable;
        }

        blockedSelectableStates.Clear();
        isOtherButtonBlockApplied = false;
    }

    private bool ShouldAllowSelectableWhileMenuPanelOpen(Selectable selectable)
    {
        if (selectable == null)
            return false;

        if (menuButton != null && selectable.gameObject == menuButton.gameObject)
            return true;

        if (selectable.gameObject.name == menuButtonObjectName)
            return true;

        FindMenuPanelIfNeeded();

        if (menuPanel != null)
        {
            Transform selectableTransform = selectable.transform;
            if (selectableTransform == menuPanel.transform || selectableTransform.IsChildOf(menuPanel.transform))
                return true;
        }

        return false;
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
        RefreshPauseState();
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

    private bool ToggleInventoryPanelByControlKey()
    {
        FindInventoryPanelIfNeeded();

        if (inventoryPanelRect == null)
        {
            Debug.LogWarning("[BattleMenuEscapeInputController] InventoryPanel을 찾지 못했습니다.");
            return false;
        }

        if (!inventoryPanelRect.gameObject.activeInHierarchy)
            return false;

        if (IsInventoryPanelOpen())
        {
            InventoryPanelSelectionResetter.ResetAllSelectionsExcept(null);
            ClearSelectedObjectIfChildOf(inventoryPanelRect.gameObject);
            StartMoveInventoryPanelToClosedPosition();
            return true;
        }

        StartMoveInventoryPanelToOpenedPosition();
        return true;
    }

    private bool IsInventoryPanelOpen()
    {
        if (inventoryPanelRect == null)
            return false;

        float currentY = inventoryPanelRect.anchoredPosition.y;

        if (Mathf.Abs(currentY - inventoryOpenedY) <= inventoryOpenCheckTolerance)
            return true;

        return currentY < inventoryClosedY - inventoryOpenCheckTolerance;
    }

    private void StartMoveInventoryPanelToClosedPosition()
    {
        StartMoveInventoryPanelToTargetPosition(inventoryClosedY);
    }

    private void StartMoveInventoryPanelToOpenedPosition()
    {
        StartMoveInventoryPanelToTargetPosition(inventoryOpenedY);
    }

    private void StartMoveInventoryPanelToTargetPosition(float targetY)
    {
        if (inventoryPanelRect == null)
            return;

        if (inventoryMoveCoroutine != null)
            StopCoroutine(inventoryMoveCoroutine);

        inventoryMoveCoroutine = StartCoroutine(MoveInventoryPanelRoutine(targetY));
    }

    private IEnumerator MoveInventoryPanelRoutine(float targetY)
    {
        Vector2 startPosition = inventoryPanelRect.anchoredPosition;
        Vector2 targetPosition = new Vector2(0f, targetY);

        float time = 0f;
        float duration = Mathf.Max(0.01f, inventoryCloseDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            inventoryPanelRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        inventoryPanelRect.anchoredPosition = targetPosition;
        inventoryMoveCoroutine = null;
    }

    private void FindMenuPanelIfNeeded()
    {
        if (!autoFindMenuPanel || menuPanel != null)
            return;

        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null)
                continue;

            if (candidate.name != menuPanelObjectName)
                continue;

            menuPanel = candidate;
            return;
        }
    }

    private void FindInventoryPanelIfNeeded()
    {
        if (inventoryPanel != null && inventoryPanelRect == null)
            inventoryPanelRect = inventoryPanel.GetComponent<RectTransform>();

        if (!autoFindInventoryPanel || inventoryPanelRect != null)
            return;

        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null)
                continue;

            if (candidate.name != inventoryPanelObjectName)
                continue;

            inventoryPanel = candidate;
            inventoryPanelRect = candidate.GetComponent<RectTransform>();
            return;
        }
    }

    private void ClearSelectedObjectIfChildOf(GameObject root)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null || eventSystem.currentSelectedGameObject == null || root == null)
            return;

        if (eventSystem.currentSelectedGameObject.transform.IsChildOf(root.transform))
            eventSystem.SetSelectedGameObject(null);
    }

    private bool WasControlPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.wasPressedThisFrame ||
             Keyboard.current.rightCtrlKey.wasPressedThisFrame))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
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
