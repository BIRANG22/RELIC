using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// TMP_Dropdown이 Template 복제본(Dropdown List)을 만들지 않고,
/// 프리팹에 배치된 기존 Template을 직접 열어 사용하게 합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DirectTemplateDropdown : MonoBehaviour, IPointerClickHandler, ISubmitHandler, ICancelHandler
{
    private static DirectTemplateDropdown openedDropdown;

    private readonly List<Toggle> optionItems = new();

    private TMP_Dropdown dropdown;
    private Toggle itemTemplate;
    private Transform content;
    private Canvas templateCanvas;
    private DirectTemplateDropdownClickCatcher clickCatcher;
    private int sortingOrderOffset = 50;
    private int openedFrame = -1;
    private bool isOpen;

    public static DirectTemplateDropdown Attach(TMP_Dropdown target)
    {
        if (target == null)
            return null;

        DirectTemplateDropdown controller = target.GetComponent<DirectTemplateDropdown>();
        if (controller == null)
            controller = target.gameObject.AddComponent<DirectTemplateDropdown>();

        controller.Initialize(target);
        return controller;
    }

    public void Configure(int sortingOffset)
    {
        sortingOrderOffset = Mathf.Max(1, sortingOffset);
    }

    private void Initialize(TMP_Dropdown target)
    {
        dropdown = target;

        if (dropdown.template != null)
            dropdown.template.gameObject.SetActive(false);

        CreateClickCatcher();

        // 기본 TMP_Dropdown은 옵션 데이터와 현재 선택값 보관용으로만 사용합니다.
        ColorBlock colors = dropdown.colors;
        colors.disabledColor = colors.normalColor;
        dropdown.colors = colors;
        dropdown.interactable = false;
        dropdown.enabled = false;
    }

    private void CreateClickCatcher()
    {
        if (clickCatcher == null)
            clickCatcher = GetComponentInChildren<DirectTemplateDropdownClickCatcher>(true);

        if (clickCatcher == null)
        {
            GameObject catcherObject = new("Direct Template Click Catcher");
            RectTransform catcherRect = catcherObject.AddComponent<RectTransform>();
            catcherRect.SetParent(transform, false);
            catcherRect.anchorMin = Vector2.zero;
            catcherRect.anchorMax = Vector2.one;
            catcherRect.offsetMin = Vector2.zero;
            catcherRect.offsetMax = Vector2.zero;

            Image catcherImage = catcherObject.AddComponent<Image>();
            catcherImage.color = Color.clear;
            catcherImage.raycastTarget = true;

            clickCatcher = catcherObject.AddComponent<DirectTemplateDropdownClickCatcher>();
        }

        clickCatcher.Initialize(this);
        clickCatcher.transform.SetAsLastSibling();
    }

    private void OnDisable()
    {
        Close();
    }

    private void Update()
    {
        if (!isOpen || Time.frameCount == openedFrame)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        Camera eventCamera = GetEventCamera();

        bool clickedDropdown = RectTransformUtility.RectangleContainsScreenPoint(
            transform as RectTransform,
            pointerPosition,
            eventCamera);

        bool clickedTemplate = dropdown != null
            && dropdown.template != null
            && RectTransformUtility.RectangleContainsScreenPoint(
                dropdown.template,
                pointerPosition,
                eventCamera);

        if (!clickedDropdown && !clickedTemplate)
            Close();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        Toggle();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Toggle();
    }

    public void OnCancel(BaseEventData eventData)
    {
        Close();
    }

    public void Open()
    {
        if (dropdown == null || dropdown.template == null || !gameObject.activeInHierarchy)
            return;

        if (openedDropdown != null && openedDropdown != this)
            openedDropdown.Close();

        if (!PrepareTemplate())
            return;

        BuildOptionItems();
        BringTemplateToFront();

        dropdown.template.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        isOpen = true;
        openedFrame = Time.frameCount;
        openedDropdown = this;
    }

    public void Close()
    {
        if (dropdown != null && dropdown.template != null)
            dropdown.template.gameObject.SetActive(false);

        isOpen = false;

        if (openedDropdown == this)
            openedDropdown = null;
    }

    private void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void HandlePointerClick(PointerEventData eventData)
    {
        OnPointerClick(eventData);
    }

    private bool PrepareTemplate()
    {
        itemTemplate = dropdown.itemText != null
            ? dropdown.itemText.GetComponentInParent<Toggle>()
            : null;

        if (itemTemplate != null
            && !itemTemplate.transform.IsChildOf(dropdown.template))
        {
            itemTemplate = null;
        }

        if (itemTemplate == null)
        {
            Toggle[] templateToggles =
                dropdown.template.GetComponentsInChildren<Toggle>(true);

            if (templateToggles.Length > 0)
                itemTemplate = templateToggles[0];
        }

        if (itemTemplate == null)
        {
            Debug.LogWarning(
                "[DirectTemplateDropdown] Template 안에서 Item Toggle을 찾을 수 없습니다.",
                dropdown);
            return false;
        }

        content = itemTemplate.transform.parent;
        if (content == null)
            return false;

        EnsureContentLayout();

        if (optionItems.Count == 0)
            optionItems.Add(itemTemplate);

        return true;
    }

    private void EnsureContentLayout()
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
            sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void BuildOptionItems()
    {
        int optionCount = dropdown.options.Count;

        while (optionItems.Count < optionCount)
        {
            GameObject itemObject = Instantiate(itemTemplate.gameObject, content, false);
            itemObject.name = $"Item {optionItems.Count}";
            optionItems.Add(itemObject.GetComponent<Toggle>());
        }

        for (int i = 0; i < optionItems.Count; i++)
        {
            Toggle optionToggle = optionItems[i];
            bool hasOption = i < optionCount;
            optionToggle.gameObject.SetActive(hasOption);

            if (!hasOption)
                continue;

            int optionIndex = i;
            TMP_Text optionText = optionToggle.GetComponentInChildren<TMP_Text>(true);
            if (optionText != null)
                optionText.text = dropdown.options[i].text;

            optionToggle.onValueChanged.RemoveAllListeners();
            optionToggle.SetIsOnWithoutNotify(i == dropdown.value);
            optionToggle.onValueChanged.AddListener(
                selected =>
                {
                    if (selected)
                        SelectOption(optionIndex);
                });
        }
    }

    private void SelectOption(int index)
    {
        if (index < 0 || index >= dropdown.options.Count)
            return;

        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.Invoke(index);
        Close();
    }

    private void BringTemplateToFront()
    {
        if (templateCanvas == null)
            templateCanvas = dropdown.template.GetComponent<Canvas>();

        if (templateCanvas == null)
            templateCanvas = dropdown.template.gameObject.AddComponent<Canvas>();

        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = GetHighestCanvasSortingOrder(templateCanvas) + sortingOrderOffset;

        if (dropdown.template.GetComponent<GraphicRaycaster>() == null)
            dropdown.template.gameObject.AddComponent<GraphicRaycaster>();
    }

    private static int GetHighestCanvasSortingOrder(Canvas excludedCanvas)
    {
        int highestOrder = 0;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == excludedCanvas || !canvas.gameObject.activeInHierarchy)
                continue;

            highestOrder = Mathf.Max(highestOrder, canvas.sortingOrder);
        }

        return highestOrder;
    }

    private Camera GetEventCamera()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return parentCanvas.worldCamera;
    }
}

/// <summary>
/// 클릭을 부모 TMP_Dropdown까지 전달하지 않고 직접 Template 열기로 전환합니다.
/// </summary>
public sealed class DirectTemplateDropdownClickCatcher : MonoBehaviour, IPointerClickHandler
{
    private DirectTemplateDropdown owner;

    public void Initialize(DirectTemplateDropdown target)
    {
        owner = target;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandlePointerClick(eventData);
    }
}
