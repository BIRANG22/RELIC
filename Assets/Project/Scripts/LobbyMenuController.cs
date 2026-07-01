using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyMenuController : MonoBehaviour
{
    [Header("Menu Panel")]
    [Tooltip("열고 닫을 로비 메뉴 패널입니다.")]
    [SerializeField] private GameObject menuPanel;

    [Header("Menu Button")]
    [Tooltip("기존 MenuButton입니다. ESC로 메뉴를 열 때 이 버튼을 클릭한 것과 같은 흐름을 타게 할 수 있습니다.")]
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuButtonObjectName = "MenuButton";
    [SerializeField] private bool autoFindMenuButton = true;
    [Tooltip("켜져 있으면 ESC로 열 때 MenuButton 클릭 이벤트를 실행합니다. 기존 UIPanelButton 상태와 같은 방식으로 열기 위해 사용합니다.")]
    [SerializeField] private bool openByClickingMenuButton = true;

    [Tooltip("켜져 있으면 MenuButton을 직접 눌러 메뉴를 열었을 때도 로비 일시정지 상태로 기록합니다.")]
    [SerializeField] private bool bindMenuButtonOpenState = true;

    [Header("Continue Button")]
    [Tooltip("메뉴 패널 안의 Continue 버튼입니다. 이 버튼을 눌렀을 때도 CloseMenu와 같은 방식으로 닫히게 합니다.")]
    [SerializeField] private Button continueButton;
    [SerializeField] private string continueButtonObjectName = "ContinueButton";
    [SerializeField] private bool autoFindContinueButton = true;
    [SerializeField] private bool bindContinueButtonClick = true;

    [Header("Open Option")]
    [Tooltip("메뉴를 열 때 Hierarchy의 마지막 자식으로 보내 가장 앞에 보이게 합니다.")]
    [SerializeField] private bool bringMenuPanelToFront = true;
    [Tooltip("메뉴를 열 때 메뉴 안의 첫 번째 선택 가능한 버튼을 EventSystem 선택 대상으로 지정합니다.")]
    [SerializeField] private bool selectFirstButtonOnOpen = true;
    [Tooltip("메뉴 패널에 Canvas가 없으면 추가해서 정렬 순서를 보장합니다.")]
    [SerializeField] private bool forceCanvasSorting = true;
    [SerializeField] private int sortingOrder = 1000;
    [SerializeField] private bool addGraphicRaycaster = true;

    public GameObject MenuPanel => menuPanel;
    public bool IsMenuOpen => isMenuOpen && menuPanel != null && menuPanel.activeInHierarchy;

    private bool isMenuOpen;
    private bool isExecutingMenuButtonClick;

    private void Awake()
    {
        FindReferencesIfNeeded();
        BindMenuButtonOpenStateIfNeeded();
        BindContinueButtonIfNeeded();
    }

    private void OnEnable()
    {
        FindReferencesIfNeeded();
        BindMenuButtonOpenStateIfNeeded();
        BindContinueButtonIfNeeded();
    }

    private void OnDestroy()
    {
        if (menuButton != null)
            menuButton.onClick.RemoveListener(MarkMenuOpenedByMenuButton);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(CloseMenu);
    }

    public void ToggleMenu()
    {
        if (IsMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        FindReferencesIfNeeded();

        if (IsMenuOpen)
        {
            ApplyOpenedPanelState();
            return;
        }

        if (openByClickingMenuButton && menuButton != null && !isExecutingMenuButtonClick)
        {
            isExecutingMenuButtonClick = true;
            ExecuteButtonClick(menuButton);
            isExecutingMenuButtonClick = false;

            if (menuPanel != null && menuPanel.activeInHierarchy)
            {
                isMenuOpen = true;
                ApplyOpenedPanelState();
                return;
            }
        }

        OpenMenuDirectly();
    }

    public void CloseMenu()
    {
        FindReferencesIfNeeded();

        // ESC로 닫을 때는 가장 위에 떠 있는 프리팹부터 닫습니다.
        // 확인창/설정창이 떠 있는 상태에서 MenuPanel까지 같이 닫히지 않도록 순서를 분리합니다.
        if (UIManager.WasConfirmDialogClosedByEscapeThisFrame || UIManager.WasOptionPanelClosedByEscapeThisFrame)
            return;

        if (UIManager.Instance != null && UIManager.Instance.TryHideConfirmDialogIfOpen(true))
        {
            ClearSelectionIfInsideMenu();
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.TryHideOptionIfOpen(true))
        {
            ClearSelectionIfInsideMenu();
            return;
        }

        if (UIPanelButton.HasCurrentOpenedPanel && UIPanelButton.TryCloseCurrentOpenedPanel())
        {
            if (menuPanel == null || !menuPanel.activeInHierarchy)
                isMenuOpen = false;

            ClearSelectionIfInsideMenu();
            return;
        }

        if (menuPanel != null)
            menuPanel.SetActive(false);

        isMenuOpen = false;
        ClearSelectionIfInsideMenu();
    }

    public void OpenMenuDirectly()
    {
        FindReferencesIfNeeded();

        if (menuPanel == null)
            return;

        menuPanel.SetActive(true);
        isMenuOpen = true;
        ApplyOpenedPanelState();
    }

    private void ApplyOpenedPanelState()
    {
        if (menuPanel == null)
            return;

        if (bringMenuPanelToFront)
            menuPanel.transform.SetAsLastSibling();

        if (forceCanvasSorting)
            ApplyCanvasSorting();

        if (selectFirstButtonOnOpen)
            SelectFirstButton();
    }

    private void ApplyCanvasSorting()
    {
        if (menuPanel == null)
            return;

        Canvas canvas = menuPanel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = menuPanel.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (!addGraphicRaycaster)
            return;

        GraphicRaycaster raycaster = menuPanel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            menuPanel.AddComponent<GraphicRaycaster>();
    }

    private void SelectFirstButton()
    {
        if (menuPanel == null || EventSystem.current == null)
            return;

        Selectable[] selectables = menuPanel.GetComponentsInChildren<Selectable>(true);

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];

            if (selectable == null)
                continue;

            if (!selectable.gameObject.activeInHierarchy || !selectable.interactable)
                continue;

            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            return;
        }
    }

    private void ClearSelectionIfInsideMenu()
    {
        if (menuPanel == null || EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
            return;

        if (selectedObject.transform.IsChildOf(menuPanel.transform))
            EventSystem.current.SetSelectedGameObject(null);
    }


    private void BindMenuButtonOpenStateIfNeeded()
    {
        if (!bindMenuButtonOpenState)
            return;

        FindReferencesIfNeeded();

        if (menuButton == null)
            return;

        menuButton.onClick.RemoveListener(MarkMenuOpenedByMenuButton);
        menuButton.onClick.AddListener(MarkMenuOpenedByMenuButton);
    }

    private void MarkMenuOpenedByMenuButton()
    {
        // MenuButton을 직접 클릭해 UIPanelButton이 메뉴를 열 때도
        // ESC로 연 것과 같은 일시정지 상태로 기록합니다.
        isMenuOpen = true;
    }

    private void BindContinueButtonIfNeeded()
    {
        if (!bindContinueButtonClick)
            return;

        FindReferencesIfNeeded();

        if (continueButton == null)
            return;

        continueButton.onClick.RemoveListener(CloseMenu);
        continueButton.onClick.AddListener(CloseMenu);
    }

    private void ExecuteButtonClick(Button button)
    {
        if (button == null)
            return;

        if (!button.gameObject.activeInHierarchy || !button.interactable)
            return;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            button.onClick.Invoke();
            return;
        }

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left,
            position = GetPointerPosition()
        };

        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
    }

    private Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private void FindReferencesIfNeeded()
    {
        if (autoFindMenuButton && menuButton == null)
            menuButton = FindButtonByName(menuButtonObjectName);

        if (autoFindContinueButton && continueButton == null)
            continueButton = FindButtonByName(continueButtonObjectName);
    }

    private Button FindButtonByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            if (button.gameObject.name == objectName)
                return button;
        }

        return null;
    }
}
