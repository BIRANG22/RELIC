using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UnitStatusEffectTooltipSide
{
    Right,
    Left
}

public class UnitStatusEffectTooltipUI : MonoBehaviour
{
    private static UnitStatusEffectTooltipUI instance;

    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private UnitStatusEffectTooltipItemUI itemPrefab;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(24f, 0f);
    [SerializeField] private float screenPadding = 12f;

    [Header("Item Layout")]
    [SerializeField] private float itemSpacing = 8f;
    [SerializeField] private Vector2 fallbackItemSize = new Vector2(300f, 80f);
    [SerializeField] private Vector2 contentPadding = Vector2.zero;
    [SerializeField] private bool resizePanelToContent = true;

    [Header("Sorting")]
    [SerializeField] private bool forceTooltipToFront = true;
    [SerializeField] private int sortingOrderOffset = 20;

    private readonly List<UnitStatusEffectTooltipItemUI> spawnedItems = new();
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Canvas tooltipCanvas;
    private Object currentOwner;
    private IReadOnlyList<StatusEffectRuntimeData> currentStatusEffects;
    private int currentStatusEffectsHash;
    private Vector2 lastScreenPosition;
    private UnitStatusEffectTooltipSide lastSide = UnitStatusEffectTooltipSide.Right;

    public static UnitStatusEffectTooltipUI GetOrCreate()
    {
        if (instance != null)
        {
            instance.InitializeIfNeeded();
            return instance;
        }

        instance = FindBestTooltipInScene();

        if (instance != null)
        {
            instance.InitializeIfNeeded();
            return instance;
        }

        Canvas canvas = FindBestCanvasInScene();
        if (canvas == null)
            return null;

        GameObject tooltipObject = new GameObject("UnitStatusEffectTooltipUI_Auto", typeof(RectTransform), typeof(CanvasGroup), typeof(UnitStatusEffectTooltipUI));
        tooltipObject.transform.SetParent(canvas.transform, false);

        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(300f, 80f);
        tooltipRect.pivot = new Vector2(0f, 0.5f);

        CanvasGroup group = tooltipObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject rootObject = new GameObject("EffectRoot", typeof(RectTransform));
        rootObject.transform.SetParent(tooltipObject.transform, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        UnitStatusEffectTooltipUI tooltip = tooltipObject.GetComponent<UnitStatusEffectTooltipUI>();
        tooltip.panelRect = tooltipRect;
        tooltip.canvasGroup = group;
        tooltip.itemRoot = rootRect;
        tooltip.InitializeIfNeeded();
        return tooltip;
    }

    private static UnitStatusEffectTooltipUI FindBestTooltipInScene()
    {
        UnitStatusEffectTooltipUI[] tooltips = FindObjectsOfType<UnitStatusEffectTooltipUI>(true);
        if (tooltips == null || tooltips.Length <= 0)
            return null;

        UnitStatusEffectTooltipUI best = null;
        for (int i = 0; i < tooltips.Length; i++)
        {
            UnitStatusEffectTooltipUI tooltip = tooltips[i];
            if (tooltip == null)
                continue;

            if (tooltip.gameObject.activeInHierarchy)
                return tooltip;

            if (best == null)
                best = tooltip;
        }

        return best;
    }

    private static Canvas FindBestCanvasInScene()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        if (canvases == null || canvases.Length <= 0)
            return null;

        Canvas best = null;
        int bestSortingOrder = int.MinValue;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
                continue;

            if (best == null || canvas.sortingOrder >= bestSortingOrder)
            {
                best = canvas;
                bestSortingOrder = canvas.sortingOrder;
            }
        }

        if (best != null)
            return best;

        return canvases[0];
    }

    private void Awake()
    {
        if (instance == null || gameObject.activeInHierarchy)
            instance = this;

        InitializeIfNeeded();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0f)
            return;

        RefreshCurrentStatusEffectsIfChanged();

        if (canvasGroup != null && canvasGroup.alpha > 0f)
            UpdatePosition(lastScreenPosition, lastSide);
    }

    public void Show(Object owner, IReadOnlyList<StatusEffectRuntimeData> statusEffects, Vector2 screenPosition)
    {
        Show(owner, statusEffects, screenPosition, UnitStatusEffectTooltipSide.Right);
    }

    public void Show(Object owner, IReadOnlyList<StatusEffectRuntimeData> statusEffects, Vector2 screenPosition, UnitStatusEffectTooltipSide side)
    {
        InitializeIfNeeded();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        BringToFront();

        if (statusEffects == null)
        {
            Hide(owner);
            return;
        }

        currentOwner = owner;
        currentStatusEffects = statusEffects;
        lastScreenPosition = screenPosition;
        lastSide = side;

        if (!RebuildItems(statusEffects))
        {
            Hide(owner);
            return;
        }

        currentStatusEffectsHash = CalculateStatusEffectsHash(statusEffects);
        SetVisible(true);
        UpdatePosition(screenPosition, side);
    }

    public void Hide(Object owner)
    {
        if (currentOwner != null && owner != null && currentOwner != owner)
            return;

        currentOwner = null;
        currentStatusEffects = null;
        currentStatusEffectsHash = 0;
        ClearItems();
        SetVisible(false);
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        UpdatePosition(screenPosition, lastSide);
    }

    public void UpdatePosition(Vector2 screenPosition, UnitStatusEffectTooltipSide side)
    {
        InitializeIfNeeded();

        if (panelRect == null || canvasRect == null)
            return;

        lastScreenPosition = screenPosition;
        lastSide = side;

        Camera uiCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 anchorPoint))
            return;

        Vector2 panelSize = GetRectSize(panelRect, fallbackItemSize);
        Vector2 pivot = panelRect.pivot;
        float horizontalGap = Mathf.Abs(screenOffset.x);
        float verticalOffset = screenOffset.y;
        Vector2 targetPosition = anchorPoint;

        if (side == UnitStatusEffectTooltipSide.Left)
        {
            float targetRight = anchorPoint.x - horizontalGap;
            targetPosition.x = targetRight - panelSize.x * (1f - pivot.x);
        }
        else
        {
            float targetLeft = anchorPoint.x + horizontalGap;
            targetPosition.x = targetLeft + panelSize.x * pivot.x;
        }

        targetPosition.y = anchorPoint.y + verticalOffset + panelSize.y * (pivot.y - 0.5f);

        Rect canvasBounds = canvasRect.rect;
        float minX = canvasBounds.xMin + screenPadding + panelSize.x * pivot.x;
        float maxX = canvasBounds.xMax - screenPadding - panelSize.x * (1f - pivot.x);
        float minY = canvasBounds.yMin + screenPadding + panelSize.y * pivot.y;
        float maxY = canvasBounds.yMax - screenPadding - panelSize.y * (1f - pivot.y);

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        panelRect.anchoredPosition = targetPosition;
    }

    private void InitializeIfNeeded()
    {
        if (panelRect == null)
            panelRect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (itemRoot == null)
            itemRoot = panelRect;

        rootCanvas = FindParentCanvasForPosition();
        if (rootCanvas != null)
        {
            canvasRect = rootCanvas.transform as RectTransform;
        }
        else
        {
            canvasRect = transform.parent as RectTransform;
        }

        EnsureTooltipCanvas();

        if (itemRoot != null && itemRoot != panelRect)
        {
            itemRoot.anchorMin = new Vector2(0f, 1f);
            itemRoot.anchorMax = new Vector2(0f, 1f);
            itemRoot.pivot = new Vector2(0f, 1f);
            itemRoot.anchoredPosition = new Vector2(contentPadding.x, -contentPadding.y);
        }
    }


    private Canvas FindParentCanvasForPosition()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            Canvas canvas = parent.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;

            parent = parent.parent;
        }

        Canvas selfCanvas = GetComponent<Canvas>();
        Canvas[] canvases = GetComponentsInParent<Canvas>(true);
        if (canvases == null || canvases.Length <= 0)
            return selfCanvas;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas != selfCanvas)
                return canvas;
        }

        return selfCanvas;
    }

    private void EnsureTooltipCanvas()
    {
        if (!forceTooltipToFront)
            return;

        tooltipCanvas = GetComponent<Canvas>();
        if (tooltipCanvas == null)
            tooltipCanvas = gameObject.AddComponent<Canvas>();

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = GetHighestCanvasSortingOrder() + Mathf.Max(1, sortingOrderOffset);
    }

    private void BringToFront()
    {
        if (!forceTooltipToFront)
            return;

        transform.SetAsLastSibling();

        if (tooltipCanvas == null)
            tooltipCanvas = GetComponent<Canvas>();

        if (tooltipCanvas == null)
            tooltipCanvas = gameObject.AddComponent<Canvas>();

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = GetHighestCanvasSortingOrderExceptSelf() + Mathf.Max(1, sortingOrderOffset);
    }

    private int GetHighestCanvasSortingOrder()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        if (canvases == null || canvases.Length <= 0)
            return 0;

        int highest = int.MinValue;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            highest = Mathf.Max(highest, canvas.sortingOrder);
        }

        return highest == int.MinValue ? 0 : highest;
    }

    private int GetHighestCanvasSortingOrderExceptSelf()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        if (canvases == null || canvases.Length <= 0)
            return 0;

        int highest = int.MinValue;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == tooltipCanvas)
                continue;

            highest = Mathf.Max(highest, canvas.sortingOrder);
        }

        return highest == int.MinValue ? 0 : highest;
    }

    private UnitStatusEffectTooltipItemUI CreateItem()
    {
        if (itemPrefab != null)
            return Instantiate(itemPrefab, itemRoot);

        return CreateFallbackItem();
    }

    private UnitStatusEffectTooltipItemUI CreateFallbackItem()
    {
        if (itemRoot == null)
            return null;

        GameObject itemObject = new GameObject("StatusEffectTooltipItem_Auto", typeof(RectTransform), typeof(UnitStatusEffectTooltipItemUI));
        itemObject.transform.SetParent(itemRoot, false);

        RectTransform rect = itemObject.GetComponent<RectTransform>();
        rect.sizeDelta = fallbackItemSize;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(itemObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, 0f);
        iconRect.sizeDelta = new Vector2(32f, 32f);

        TMP_Text titleText = CreateText("TitleText", itemObject.transform, 18f, FontStyles.Bold);
        RectTransform titleRect = titleText.transform as RectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, -24f);
        titleRect.offsetMax = new Vector2(0f, 0f);

        TMP_Text bodyText = CreateText("DescriptionText", itemObject.transform, 14f, FontStyles.Normal);
        RectTransform bodyRect = bodyText.transform as RectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(40f, 0f);
        bodyRect.offsetMax = new Vector2(0f, -28f);

        UnitStatusEffectTooltipItemUI item = itemObject.GetComponent<UnitStatusEffectTooltipItemUI>();
        item.BindFallbackReferences(null, iconObject.GetComponent<Image>(), titleText, bodyText);
        return item;
    }

    private TMP_Text CreateText(string objectName, Transform parent, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.color = Color.white;
        return text;
    }

    private void RefreshCurrentStatusEffectsIfChanged()
    {
        if (currentOwner == null)
        {
            Hide(null);
            return;
        }

        if (currentStatusEffects == null)
        {
            Hide(currentOwner);
            return;
        }

        int newHash = CalculateStatusEffectsHash(currentStatusEffects);
        if (newHash == currentStatusEffectsHash)
            return;

        if (!RebuildItems(currentStatusEffects))
        {
            Hide(currentOwner);
            return;
        }

        currentStatusEffectsHash = newHash;
        UpdatePosition(lastScreenPosition, lastSide);
    }

    private bool RebuildItems(IReadOnlyList<StatusEffectRuntimeData> statusEffects)
    {
        ClearItems();

        if (statusEffects == null)
            return false;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData statusEffect = statusEffects[i];
            if (statusEffect == null || !statusEffect.IsValid())
                continue;

            UnitStatusEffectTooltipItemUI item = CreateItem();
            if (item == null)
                continue;

            item.Set(statusEffect);
            spawnedItems.Add(item);
        }

        if (spawnedItems.Count <= 0)
            return false;

        ArrangeItemsVertically();
        return true;
    }

    private int CalculateStatusEffectsHash(IReadOnlyList<StatusEffectRuntimeData> statusEffects)
    {
        if (statusEffects == null)
            return 0;

        unchecked
        {
            int hash = 17;
            int validCount = 0;

            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffectRuntimeData statusEffect = statusEffects[i];
                if (statusEffect == null || !statusEffect.IsValid())
                    continue;

                validCount++;
                hash = hash * 31 + i;
                hash = hash * 31 + (statusEffect.EffectId != null ? statusEffect.EffectId.GetHashCode() : 0);
                hash = hash * 31 + statusEffect.Stack;
                hash = hash * 31 + statusEffect.TurnCount;
            }

            hash = hash * 31 + validCount;
            return hash;
        }
    }

    private void ArrangeItemsVertically()
    {
        if (itemRoot == null)
            return;

        float y = 0f;
        float maxWidth = 0f;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] == null)
                continue;

            RectTransform itemRect = spawnedItems[i].transform as RectTransform;
            if (itemRect == null)
                continue;

            Vector2 itemSize = GetRectSize(itemRect, fallbackItemSize);

            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0f, 1f);
            itemRect.sizeDelta = itemSize;
            itemRect.anchoredPosition = new Vector2(0f, -y);

            y += itemSize.y;
            if (i < spawnedItems.Count - 1)
                y += Mathf.Max(0f, itemSpacing);

            maxWidth = Mathf.Max(maxWidth, itemSize.x);
        }

        itemRoot.sizeDelta = new Vector2(maxWidth, y);

        if (resizePanelToContent && panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(
                maxWidth + contentPadding.x * 2f,
                y + contentPadding.y * 2f);
        }
    }

    private Vector2 GetRectSize(RectTransform rect, Vector2 fallbackSize)
    {
        if (rect == null)
            return fallbackSize;

        Vector2 size = rect.rect.size;

        if (size.x <= 0f)
            size.x = Mathf.Abs(rect.sizeDelta.x);

        if (size.y <= 0f)
            size.y = Mathf.Abs(rect.sizeDelta.y);

        if (size.x <= 0f)
            size.x = fallbackSize.x;

        if (size.y <= 0f)
            size.y = fallbackSize.y;

        return size;
    }

    private void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
