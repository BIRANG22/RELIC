#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject optionPanelPrefab;

    [Header("Option Panel Sorting")]
    [SerializeField] private bool bringOptionPanelToFront = true;
    [SerializeField] private bool overrideOptionPanelSorting = true;
    [SerializeField] private int optionPanelSortingOrderOffset = 10;

    [Header("Input")]
    [SerializeField] private bool closeOptionPanelWithEscape = true;

    private static readonly Vector3 OptionPanelDefaultScale = Vector3.one;

    private static int lastOptionClosedByEscapeFrame = -1;

    private GameObject optionPanelInstance;

    public static bool WasOptionPanelClosedByEscapeThisFrame => lastOptionClosedByEscapeFrame == Time.frameCount;

    public bool IsOptionPanelOpen => optionPanelInstance != null && optionPanelInstance.activeInHierarchy;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;
    }

    private void Start()
    {
        CreateOptionPanel();
        HideAll();
    }

    private void Update()
    {
        if (!closeOptionPanelWithEscape)
            return;

        if (!IsOptionPanelOpen)
            return;

        if (IsTypingInputFieldSelected())
            return;

        if (WasEscapePressedThisFrame())
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
