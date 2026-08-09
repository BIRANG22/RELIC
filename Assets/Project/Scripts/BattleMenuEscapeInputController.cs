#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
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

    [Header("Menu Root Input Blocker")]
    [SerializeField] private string menuRootObjectName = "MenuRoot";
    [SerializeField] private string menuRootBlockerObjectName = "Image";
    [SerializeField] private Graphic menuRootBlockerGraphic;


    [Header("Pause")]
    [SerializeField] private bool pauseGameWhenMenuPanelOpen = true;
    [SerializeField] private float pauseTimeScale = 0f;
    [SerializeField] private float fallbackResumeTimeScale = 1f;

    private bool isPauseApplied;
    private bool isOtherButtonBlockApplied;
    private float timeScaleBeforePause = 1f;
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
        FindMenuRootBlockerIfNeeded();
    }

    private void OnEnable()
    {
        FindMenuButtonIfNeeded();
        FindMenuPanelIfNeeded();
        FindMenuRootBlockerIfNeeded();
        RefreshPauseState();
    }

    private void OnDisable()
    {
        SetMenuRootBlockerRaycast(false);
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

        // MenuPanel의 바깥 배경인 MenuRoot/Image는 메뉴가 열려 있을 때만
        // Raycast를 받아야 합니다. 메뉴가 닫힌 뒤에도 활성화되어 있으면
        // 강화 패널의 Cancel 같은 뒤쪽 버튼의 호버와 클릭을 가로챕니다.
        SetMenuRootBlockerRaycast(menuPanelOpen);

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

        bool wasMenuPanelOpen = IsMenuPanelOpen();

        // ESC 키 입력은 마우스 포인터 이벤트를 강제로 만들지 않고
        // 버튼의 클릭 동작만 직접 실행합니다.
        // 현재 마우스가 다른 버튼 위에 있을 때 PointerDown/Up/Click을
        // MenuButton으로 강제 전송하면 EventSystem의 호버 상태가 꼬일 수 있습니다.
        menuButton.onClick.Invoke();

        if (wasMenuPanelOpen && !IsMenuPanelOpen())
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(null);
        }

        RefreshPauseState();
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

    private void FindMenuRootBlockerIfNeeded()
    {
        if (menuRootBlockerGraphic != null)
            return;

        FindMenuPanelIfNeeded();

        Transform menuRoot = null;

        if (menuPanel != null)
        {
            Transform current = menuPanel.transform;

            while (current != null)
            {
                if (current.name == menuRootObjectName)
                {
                    menuRoot = current;
                    break;
                }

                current = current.parent;
            }
        }

        if (menuRoot == null)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];

                if (candidate != null && candidate.name == menuRootObjectName)
                {
                    menuRoot = candidate.transform;
                    break;
                }
            }
        }

        if (menuRoot == null)
            return;

        Transform blocker = menuRoot.Find(menuRootBlockerObjectName);

        if (blocker == null)
        {
            for (int i = 0; i < menuRoot.childCount; i++)
            {
                Transform child = menuRoot.GetChild(i);

                if (child != null && child.name == menuRootBlockerObjectName)
                {
                    blocker = child;
                    break;
                }
            }
        }

        if (blocker != null)
            menuRootBlockerGraphic = blocker.GetComponent<Graphic>();
    }

    private void SetMenuRootBlockerRaycast(bool enabled)
    {
        FindMenuRootBlockerIfNeeded();

        if (menuRootBlockerGraphic == null)
            return;

        menuRootBlockerGraphic.raycastTarget = enabled;
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
