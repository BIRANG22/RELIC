using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CultureTankInventorySlotClickRelay : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Button button;
    private string itemId;
    private bool selectionEnabled;
    private Action<string> onSelected;
    private Action<string, PointerEventData> onDropped;
    private bool dragging;

    private RectTransform dragVisual;
    private Image dragVisualImage;
    private Canvas dragCanvas;
    private GameObject dragCanvasObject;

    private const int DragSortingOrder = 10000;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        BindButton();
    }

    private void OnDisable()
    {
        dragging = false;
        DestroyDragVisual();
    }

    public void Configure(
        Button targetButton,
        string configuredItemId,
        bool enabled,
        Action<string> callback,
        Action<string, PointerEventData> dropCallback = null)
    {
        string nextItemId = string.IsNullOrWhiteSpace(configuredItemId) ? string.Empty : configuredItemId.Trim();
        bool keepActiveDrag = dragging &&
                              !string.IsNullOrWhiteSpace(itemId) &&
                              string.Equals(itemId, nextItemId, StringComparison.OrdinalIgnoreCase) &&
                              dropCallback != null;

        button = targetButton != null ? targetButton : GetComponent<Button>();
        itemId = nextItemId;
        selectionEnabled = enabled && !string.IsNullOrWhiteSpace(itemId);
        onSelected = callback;
        onDropped = dropCallback;

        if (!keepActiveDrag)
        {
            dragging = false;
            DestroyDragVisual();
        }

        BindButton();
        if (button != null)
            button.interactable = selectionEnabled;
    }

    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (dragging)
            return;

        InvokeSelection();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 슬롯 루트에 Button이 없는 StorageSlotUI도 클릭할 수 있게 합니다.
        // Button이 있으면 Button.onClick에서 처리하므로 중복 실행하지 않습니다.
        if (button != null || dragging)
            return;

        InvokeSelection();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = selectionEnabled && !string.IsNullOrWhiteSpace(itemId) && onDropped != null;
        if (!dragging)
            return;

        CreateDragVisual(eventData);
        UpdateDragVisualPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        UpdateDragVisualPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            DestroyDragVisual();
            return;
        }

        string draggedItemId = itemId;
        dragging = false;
        DestroyDragVisual();
        onDropped?.Invoke(draggedItemId, eventData);
    }

    private void CreateDragVisual(PointerEventData eventData)
    {
        DestroyDragVisual();

        Image sourceIcon = FindSourceIcon();
        if (sourceIcon == null || sourceIcon.sprite == null)
            return;

        Canvas sourceCanvas = GetComponentInParent<Canvas>();
        if (sourceCanvas == null)
            return;

        Canvas rootCanvas = sourceCanvas.rootCanvas != null ? sourceCanvas.rootCanvas : sourceCanvas;

        dragCanvasObject = new GameObject(
            "CultureTankStorageDragCanvas",
            typeof(RectTransform),
            typeof(Canvas));

        RectTransform dragCanvasRect = dragCanvasObject.GetComponent<RectTransform>();
        dragCanvasRect.SetParent(rootCanvas.transform, false);
        dragCanvasRect.anchorMin = Vector2.zero;
        dragCanvasRect.anchorMax = Vector2.one;
        dragCanvasRect.offsetMin = Vector2.zero;
        dragCanvasRect.offsetMax = Vector2.zero;
        dragCanvasRect.localScale = Vector3.one;
        dragCanvasRect.SetAsLastSibling();

        dragCanvas = dragCanvasObject.GetComponent<Canvas>();
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        dragCanvas.sortingOrder = DragSortingOrder;
        dragCanvas.additionalShaderChannels = rootCanvas.additionalShaderChannels;

        GameObject visualObject = new GameObject(
            "CultureTankStorageDragIcon",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));

        dragVisual = visualObject.GetComponent<RectTransform>();
        dragVisual.SetParent(dragCanvas.transform, false);
        dragVisual.SetAsLastSibling();
        dragVisual.anchorMin = new Vector2(0.5f, 0.5f);
        dragVisual.anchorMax = new Vector2(0.5f, 0.5f);
        dragVisual.pivot = new Vector2(0.5f, 0.5f);

        RectTransform sourceRect = sourceIcon.rectTransform;
        Vector2 sourceSize = sourceRect.rect.size;
        if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            sourceSize = sourceRect.sizeDelta;
        if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            sourceSize = new Vector2(64f, 64f);

        dragVisual.sizeDelta = sourceSize;
        dragVisual.localScale = Vector3.one;

        dragVisualImage = visualObject.GetComponent<Image>();
        dragVisualImage.sprite = sourceIcon.sprite;
        dragVisualImage.color = sourceIcon.color;
        dragVisualImage.material = sourceIcon.material;
        dragVisualImage.preserveAspect = sourceIcon.preserveAspect;
        dragVisualImage.raycastTarget = false;

        CanvasGroup canvasGroup = visualObject.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.ignoreParentGroups = true;

        UpdateDragVisualPosition(eventData);
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragVisual == null || dragCanvas == null || eventData == null)
            return;

        RectTransform canvasRect = dragCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera uiCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (dragCanvas.worldCamera != null ? dragCanvas.worldCamera : eventData.pressEventCamera);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                uiCamera,
                out Vector2 localPoint))
        {
            dragVisual.anchoredPosition = localPoint;
        }
    }

    private Image FindSourceIcon()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        Image fallback = null;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null || !image.enabled)
                continue;

            string objectName = image.gameObject.name;
            if (string.Equals(objectName, "Icon", StringComparison.OrdinalIgnoreCase) ||
                objectName.IndexOf("ItemIcon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return image;
            }

            if (fallback == null && image.transform != transform)
                fallback = image;
        }

        return fallback;
    }

    private void DestroyDragVisual()
    {
        if (dragCanvasObject != null)
            Destroy(dragCanvasObject);
        else if (dragVisual != null)
            Destroy(dragVisual.gameObject);

        dragVisual = null;
        dragVisualImage = null;
        dragCanvas = null;
        dragCanvasObject = null;
    }

    private void InvokeSelection()
    {
        if (!selectionEnabled || string.IsNullOrWhiteSpace(itemId))
            return;

        onSelected?.Invoke(itemId);
    }
}
