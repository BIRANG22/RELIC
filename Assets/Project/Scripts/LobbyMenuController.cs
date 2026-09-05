using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyMenuController : MonoBehaviour
{
    private const string SettingButtonObjectName = "SettingButton";

    [Header("Menu Panel")]
    [Tooltip("���� ���� �κ� �޴� �г��Դϴ�.")]
    [SerializeField] private GameObject menuPanel;

    [Header("Menu Button")]
    [Tooltip("���� MenuButton�Դϴ�. ESC�� �޴��� �� �� �� ��ư�� Ŭ���� �Ͱ� ���� �帧�� Ÿ�� �� �� �ֽ��ϴ�.")]
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuButtonObjectName = "MenuButton";
    [SerializeField] private bool autoFindMenuButton = true;
    [Tooltip("���� ������ ESC�� �� �� MenuButton Ŭ�� �̺�Ʈ�� �����մϴ�. ���� UIPanelButton ���¿� ���� ������� ���� ���� ����մϴ�.")]
    [SerializeField] private bool openByClickingMenuButton = true;

    [Tooltip("���� ������ MenuButton�� ���� ���� �޴��� ������ ���� �κ� �Ͻ����� ���·� ����մϴ�.")]
    [SerializeField] private bool bindMenuButtonOpenState = true;

    [Header("Continue Button")]
    [Tooltip("�޴� �г� ���� Continue ��ư�Դϴ�. �� ��ư�� ������ ���� CloseMenu�� ���� ������� ������ �մϴ�.")]
    [SerializeField] private Button continueButton;
    [SerializeField] private string continueButtonObjectName = "ContinueButton";
    [SerializeField] private bool autoFindContinueButton = true;
    [SerializeField] private bool bindContinueButtonClick = true;

    [Header("Open Option")]
    [Tooltip("�޴��� �� �� �޴� ���� ù ��° ���� ������ ��ư�� EventSystem ���� ������� �����մϴ�.")]
    [SerializeField] private bool selectFirstButtonOnOpen = true;

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
                ConfigureMenuBlurRoots();
                ApplyOpenedPanelState();
                return;
            }
        }

        OpenMenuDirectly();
    }

    public void CloseMenu()
    {
        FindReferencesIfNeeded();

        // ESC�� ���� ���� ���� ���� �� �ִ� �����պ��� �ݽ��ϴ�.
        // Ȯ��â/����â�� �� �ִ� ���¿��� MenuPanel���� ���� ������ �ʵ��� ������ �и��մϴ�.
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

        ConfigureMenuBlurRoots();
        menuPanel.SetActive(true);
        isMenuOpen = true;
        ApplyOpenedPanelState();
    }

    private void ApplyOpenedPanelState()
    {
        if (menuPanel == null)
            return;

        ConfigureMenuBlurRoots();

        if (selectFirstButtonOnOpen)
            SelectFirstButton();
    }

    private void ConfigureMenuBlurRoots()
    {
        if (menuPanel == null)
            return;

        UIBlurBackground blurBackground = UIBlurBackground.EnsureForPanel(menuPanel);
        if (blurBackground == null)
            return;

        GameObject settingButton = FindSceneObject(SettingButtonObjectName);
        if (settingButton != null)
            blurBackground.AddRuntimeBlurredUiRoot(settingButton);

        LobbyQuestManager.Instance?.ConfigureQuestPanelBlur(blurBackground);
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
        isMenuOpen = menuPanel != null && menuPanel.activeInHierarchy;

        if (isMenuOpen)
            ApplyOpenedPanelState();
        else
            ClearSelectionIfInsideMenu();
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

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null)
                continue;

            GameObject gameObject = candidate.gameObject;
            if (!gameObject.scene.IsValid())
                continue;

            if (gameObject.name == objectName)
                return gameObject;
        }

        return null;
    }
}
